// VAI TRÒ FILE: Chat theo khách và khôi phục mật khẩu OTP; gồm phần StoreService, hợp đồng gửi
// mail, SMTP và
// PasswordRecovery. Các mã/token có giới hạn thời gian và chống gửi trùng.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // THUỘC TÍNH StoreService.BankOptions (BankTransferOptions): cấu hình chuyển khoản được đọc từ
    // section BankTransfer, dùng kiểm tra và chụp QR vào đơn.
    public BankTransferOptions BankOptions =>
        configuration?.GetSection("BankTransfer").Get<BankTransferOptions>() ?? new();

    // HÀM ChatAccess: Cho phép khách đọc/gửi trong cuộc hội thoại của chính mình; tài khoản khác
    // phải là nhân viên Support/Admin. Khách được trao đổi phải còn hoạt động.
    // THAM SỐ db (StoreDbContext): DbContext đang dùng trong giao dịch; đọc DbSet để lấy dữ liệu,
    // sửa entity rồi SaveChanges để đẩy SQL.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ customer (int): mã khách dương của hội thoại; không phải id người nhân viên đang hỗ
    // trợ.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static void ChatAccess(Data.StoreDbContext db, int actor, int customer)
    {
        // ĐIỀU KIỆN: Khách hàng không hoạt động. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(
            db.KhachHang.Any(c => c.MaKH == customer && c.TrangThai),
            "Khách hàng không hoạt động."
        );
        if (actor == customer && actor > 0)
            return;
        NativePermission(db, actor, "Support");
    }

    // HÀM Messages: Đọc tối đa 100 tin trong đúng hội thoại sau khi kiểm tra quyền. before dùng tải
    // lịch sử, after dùng lấy tin mới; kết quả luôn trả theo thứ tự mã tăng dần.
    // THAM SỐ customer (int): mã khách dương của hội thoại; không phải id người nhân viên đang hỗ
    // trợ.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ before (long): mã tin đầu của trang hiện có; lấy tin Id nhỏ hơn để tải lịch sử, 0 bỏ
    // mốc này. Mặc định khi không truyền: 0.
    // THAM SỐ after (long): mốc mã tin đã tải; chỉ lấy tin có Id lớn hơn mốc này, 0 là chưa có mốc.
    // Mặc định khi không truyền: 0.
    // TRẢ VỀ (List<ChatMessage>): danh sách/truy vấn các phần tử: tin nhắn gồm người gửi, nội dung,
    // thời gian và token chống gửi trùng; có thể rỗng khi không có dữ liệu khớp.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA m (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token chống gửi
    // trùng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeRead; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public List<ChatMessage> Messages(int customer, int actor, long before = 0, long after = 0) =>
        NativeRead(db =>
        {
            ChatAccess(db, actor, customer);
            // BIẾN query (IQueryable<ChatMessage>): danh sách/truy vấn các phần tử: tin nhắn gồm
            // người gửi, nội dung, thời gian và token chống gửi trùng.
            var query = db.ChatMessages.Where(m =>
                m.CustomerId == customer && (before == 0 || m.Id < before) && m.Id > after
            );
            return (after > 0 ? query.OrderBy(m => m.Id) : query.OrderByDescending(m => m.Id))
                .Take(100)
                .ToList()
                .OrderBy(m => m.Id)
                .ToList();
        });

    // HÀM SendMessage: Thêm tin nhắn 1–2.000 ký tự: kiểm tra quyền, clientToken khác rỗng và giới
    // hạn dưới 20 tin/phút. Nếu token đã lưu cùng nội dung/người gửi thì trả tin cũ; nếu chưa có
    // hội thoại thì tạo ChatThread rồi thêm ChatMessage.
    // THAM SỐ customer (int): mã khách dương của hội thoại; không phải id người nhân viên đang hỗ
    // trợ.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ text (string): nội dung tin nhắn sau Trim, cần 1–2.000 ký tự.
    // THAM SỐ clientToken (Guid): Guid cho một lần gửi tin; thử lại cùng tin phải giữ token để
    // server không thêm bản sao.
    // TRẢ VỀ (ChatMessage): ChatMessage mới được lưu hoặc tin đã có đúng token/nội dung/người gửi;
    // lỗi quyền/giới hạn ném ngoại lệ.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA m (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token chống gửi
    // trùng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA t (ChatThread): hội thoại của một khách, chứa mốc đã đọc của khách và nhân viên;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public ChatMessage SendMessage(int customer, int actor, string text, Guid clientToken) =>
        NativeWrite(db =>
        {
            ChatAccess(db, actor, customer);
            text = Text(text, 1, 2000, "Tin nhắn");
            // ĐIỀU KIỆN: Mã tin nhắn không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(clientToken != Guid.Empty, "Mã tin nhắn không hợp lệ.");
            // BIẾN old (ChatMessage): tin đã tồn tại theo CustomerId/ClientToken; chỉ trả lại nếu
            // người gửi và nội dung trùng khớp.
            var old = db.ChatMessages.SingleOrDefault(m =>
                m.CustomerId == customer && m.ClientToken == clientToken
            );
            // CHỐNG GỬI TRÙNG: token đã tồn tại thì kiểm tra quyền/nội dung và trả kết quả cũ,
            // không thêm đơn/tin nhắn
            // lần hai.
            if (old != null)
            {
                // ĐIỀU KIỆN: Mã tin nhắn đã sử dụng. Không đạt thì Require ném lỗi và dừng luồng xử
                // lý.
                Require(old.SenderId == actor && old.Text == text, "Mã tin nhắn đã sử dụng.");
                return old;
            }

            // BIẾN since (DateTime): mốc UTC lùi một phút, dùng đếm tin đã gửi để giới hạn tốc độ
            // gửi chat.
            var since = DateTime.UtcNow.AddMinutes(-1);
            // ĐIỀU KIỆN: Bạn gửi quá nhanh. Vui lòng chờ một phút. Không đạt thì Require ném lỗi và
            // dừng luồng xử lý.
            Require(
                db.ChatMessages.Count(m => m.SenderId == actor && m.CreatedUtc > since) < 20,
                "Bạn gửi quá nhanh. Vui lòng chờ một phút."
            );
            // BIẾN thread (ChatThread): hội thoại của một khách, chứa mốc đã đọc của khách và nhân
            // viên; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay
            // sau.
            var thread = db.ChatThreads.SingleOrDefault(t => t.CustomerId == customer);
            if (thread == null)
            {
                thread = new() { CustomerId = customer };
                db.ChatThreads.Add(thread);
            }

            thread.UpdatedUtc = DateTime.UtcNow;
            // BIẾN message (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token
            // chống gửi trùng; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa SQL đã
            // commit.
            var message = new ChatMessage
            {
                CustomerId = customer,
                SenderId = actor,
                Text = text,
                ClientToken = clientToken,
                CreatedUtc = DateTime.UtcNow,
            };
            db.ChatMessages.Add(message);
            return message;
        });

    // HÀM MarkChatRead: Cập nhật mốc đã đọc cho khách hoặc nhân viên; ép last vào khoảng 0..mã tin
    // lớn nhất và chỉ tăng mốc đọc để việc tải lại không làm tin đã đọc thành chưa đọc.
    // THAM SỐ customer (int): mã khách dương của hội thoại; không phải id người nhân viên đang hỗ
    // trợ.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ last (long): mã tin cuối cùng đã đọc; service chặn trong khoảng mã tin thật và chỉ
    // tăng mốc đã đọc.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA t (ChatThread): hội thoại của một khách, chứa mốc đã đọc của khách và nhân viên;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA m (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token chống gửi
    // trùng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void MarkChatRead(int customer, int actor, long last) =>
        NativeWrite(db =>
        {
            ChatAccess(db, actor, customer);
            // BIẾN t (ChatThread): hội thoại của một khách, chứa mốc đã đọc của khách và nhân viên;
            // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var t = db.ChatThreads.SingleOrDefault(t => t.CustomerId == customer);
            if (t == null)
                return 0;
            // BIẾN max (long): mã tin lớn nhất thực sự tồn tại trong hội thoại, dùng giới hạn mốc
            // đã đọc người dùng gửi.
            long max =
                db.ChatMessages.Where(m => m.CustomerId == customer).Select(m => (long?)m.Id).Max()
                ?? 0;
            last = Math.Min(Math.Max(0, last), max);
            if (actor > 0)
                t.CustomerReadId = Math.Max(t.CustomerReadId, last);
            else
                t.StaffReadId = Math.Max(t.StaffReadId, last);
            return 0;
        });

    // HÀM ChatInbox: Support/Admin lấy tối đa 200 hội thoại mới cập nhật nhất, kèm tên khách và số
    // tin khách gửi sau mốc nhân viên đã đọc.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (List<(int Id, string Name, DateTime Updated, int Unread)>): danh sách/truy vấn các
    // phần tử: (int Id, string Name, DateTime Updated, int Unread); có thể rỗng khi không có dữ
    // liệu khớp.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA m (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token chống gửi
    // trùng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA t (<anonymous type: int MaKH, string HoTen, DateTime UpdatedUtc, int Unread>):
    // đối tượng hoặc danh sách tạm chỉ chứa các trường được liệt kê trong new { ... } của phép
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeRead; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public List<(int Id, string Name, DateTime Updated, int Unread)> ChatInbox(int actor) =>
        NativeRead(db =>
        {
            NativePermission(db, actor, "Support");
            return (
                from t in db.ChatThreads
                join c in db.KhachHang on t.CustomerId equals c.MaKH
                orderby t.UpdatedUtc descending
                select new
                {
                    c.MaKH,
                    c.HoTen,
                    t.UpdatedUtc,
                    Unread = db.ChatMessages.Count(m =>
                        m.CustomerId == t.CustomerId && m.SenderId > 0 && m.Id > t.StaffReadId
                    ),
                }
            )
                .Take(200)
                .ToList()
                .Select(t => (t.MaKH, t.HoTen, t.UpdatedUtc, t.Unread))
                .ToList();
        });

    // HÀM IssueReset: Tạo OTP 6 số cho khách đang hoạt động: tối đa 1 lần/phút và 5 lần/giờ, hết
    // hạn sau 10 phút. Vô hiệu mã chưa dùng trước đó, lưu HMAC thay vì OTP rõ và chụp dấu vết mật
    // khẩu hiện tại.
    // THAM SỐ email (string): địa chỉ email đầu vào; các luồng tài khoản chuẩn hóa trim/hoa thường
    // trước khi tra cứu.
    // THAM SỐ secret (string): khóa bí mật cấu hình dùng ký HMAC cho OTP; không phải OTP, không ghi
    // ra giao diện hoặc log.
    // TRẢ VỀ ((Guid Id, string Email, string Code)?): tuple nullable (Id, Email, Code) để gửi OTP;
    // null khi không có khách hợp lệ hoặc bị giới hạn phát mã.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần thử và
    // trạng thái sử dụng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    internal (Guid Id, string Email, string Code)? IssueReset(string email, string secret) =>
        NativeWrite(db =>
        {
            email = email.Trim().ToLowerInvariant();
            // BIẾN c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; kết quả tra cứu
            // có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var c = db.KhachHang.SingleOrDefault(c => c.Email == email && c.TrangThai);
            if (c == null)
                return null;
            // BIẾN now (DateTime): thời điểm UTC hiện tại dùng đồng nhất khi tạo hạn OTP hoặc kiểm
            // tra mã còn hiệu lực.
            var now = DateTime.UtcNow;
            // BIẾN hour (DateTime): mốc UTC lùi một giờ, dùng đếm số yêu cầu cấp OTP gần đây.
            var hour = now.AddHours(-1);
            // BIẾN minute (DateTime): mốc UTC lùi một phút, dùng chặn cấp OTP dồn dập.
            var minute = now.AddMinutes(-1);
            if (
                db.ResetCodes.Any(r => r.CustomerId == c.MaKH && r.CreatedUtc > minute)
                || db.ResetCodes.Count(r => r.CustomerId == c.MaKH && r.CreatedUtc > hour) >= 5
            )
                return null;
            // BIẾN LẶP old (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần
            // thử và trạng thái sử dụng; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong
            // vòng foreach này.
            foreach (
                var old in db.ResetCodes.Where(r => r.CustomerId == c.MaKH && r.UsedUtc == null)
            )
                old.UsedUtc = now;
            // BIẾN code (string): OTP ngẫu nhiên 6 chữ số để gửi email; database chỉ lưu HMAC,
            // không lưu chuỗi code rõ này.
            string code = RandomNumberGenerator.GetInt32(1000000).ToString("D6");
            // BIẾN row (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần thử
            // và trạng thái sử dụng; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa SQL
            // đã commit.
            var row = new ResetCode
            {
                Id = Guid.NewGuid(),
                CustomerId = c.MaKH,
                Stamp = PasswordStamp(c.MatKhau),
                CreatedUtc = now,
                ExpiresUtc = now.AddMinutes(10),
            };
            row.Hash = OtpHash(row.Id, code, secret);
            db.ResetCodes.Add(row);
            return ((Guid Id, string Email, string Code)?)(row.Id, c.Email, code);
        });

    // HÀM ResetDelivered: Đánh dấu OTP đã gửi email thành công; ResetPassword chỉ chấp nhận mã có
    // Sent = true.
    // THAM SỐ id (Guid): mã Guid challenge OTP cần đánh dấu đã gửi.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA r (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần thử và
    // trạng thái sử dụng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    internal void ResetDelivered(Guid id) =>
        NativeWrite(db =>
        {
            // BIẾN code (ResetCode): bản ghi ResetCode được đánh dấu Sent sau khi SMTP gửi thành
            // công, không phải chuỗi OTP rõ.
            var code = db.ResetCodes.Single(r => r.Id == id);
            code.Sent = true;
            return 0;
        });

    // HÀM ResetPassword: Đổi mật khẩu bằng OTP: kiểm tra mẫu và khóa HMAC, khách hoạt động, mã đã
    // gửi/chưa dùng/chưa hết 10 phút/chưa thử đủ 5 lần và stamp chưa đổi. So sánh HMAC theo thời
    // gian cố định, băm mật khẩu mới rồi vô hiệu các mã còn lại.
    // THAM SỐ input (ResetInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ secret (string): khóa bí mật cấu hình dùng ký HMAC cho OTP; không phải OTP, không ghi
    // ra giao diện hoặc log.
    // TRẢ VỀ (bool): true khi OTP hợp lệ và mật khẩu đã được băm/lưu; false khi khách hoặc mã không
    // thỏa điều kiện.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần thử và
    // trạng thái sử dụng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần thử và
    // trạng thái sử dụng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public bool ResetPassword(ResetInput input, string secret)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        // ĐIỀU KIỆN: Chức năng khôi phục chưa cấu hình. Không đạt thì Require ném lỗi và dừng luồng
        // xử lý.
        Require(secret.Length >= 32, "Chức năng khôi phục chưa cấu hình.");
        return NativeWrite(db =>
        {
            // BIẾN email (string): email đã Trim và chuẩn hóa chữ thường để so sánh/ghi nhận nhất
            // quán.
            var email = input.Email.Trim().ToLowerInvariant();
            // BIẾN c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; kết quả tra cứu
            // có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var c = db.KhachHang.SingleOrDefault(c => c.Email == email && c.TrangThai);
            if (c == null)
                return false;
            // BIẾN r (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần thử và
            // trạng thái sử dụng; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra
            // hoặc ?? ngay sau.
            var r = db
                .ResetCodes.Where(r => r.CustomerId == c.MaKH && r.Sent && r.UsedUtc == null)
                .OrderByDescending(r => r.CreatedUtc)
                .FirstOrDefault();
            // BIẾN now (DateTime): thời điểm UTC hiện tại dùng đồng nhất khi tạo hạn OTP hoặc kiểm
            // tra mã còn hiệu lực.
            var now = DateTime.UtcNow;
            if (
                r == null
                || r.ExpiresUtc <= now
                || r.Attempts >= 5
                || r.Stamp != PasswordStamp(c.MatKhau)
            )
                return false;
            r.Attempts++;
            if (
                !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(r.Hash),
                    Convert.FromHexString(OtpHash(r.Id, input.Code, secret))
                )
            )
                return false;
            c.MatKhau = hasher.HashPassword(new Customer(), input.Password);
            r.UsedUtc = now;
            // BIẾN LẶP other (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số
            // lần thử và trạng thái sử dụng; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng
            // trong vòng foreach này.
            foreach (
                var other in db.ResetCodes.Where(x => x.CustomerId == c.MaKH && x.UsedUtc == null)
            )
                other.UsedUtc = now;
            return true;
        });
    }

    // HÀM PasswordStamp: Tạo SHA-256 của hash mật khẩu hiện tại; dùng để vô hiệu OTP nếu mật khẩu
    // đã đổi từ lúc phát mã.
    // THAM SỐ hash (string): chuỗi hash mật khẩu đã lưu, dùng tạo stamp hoặc kiểm tra cấu trúc theo
    // hàm đang gọi.
    // TRẢ VỀ (string): chuỗi ký tự; ý nghĩa được xác định bởi biểu thức return bên dưới.
    static string PasswordStamp(string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

    // HÀM OtpHash: Tạo HMAC-SHA256 từ mã challenge và OTP bằng khóa cấu hình; database chỉ giữ kết
    // quả này, không lưu OTP rõ.
    // THAM SỐ id (Guid): mã Guid challenge OTP cần đánh dấu đã gửi.
    // THAM SỐ code (string): OTP 6 chữ số để gửi email hoặc đưa vào phép HMAC.
    // THAM SỐ secret (string): khóa bí mật cấu hình dùng ký HMAC cho OTP; không phải OTP, không ghi
    // ra giao diện hoặc log.
    // TRẢ VỀ (string): chuỗi ký tự; ý nghĩa được xác định bởi biểu thức return bên dưới.
    static string OtpHash(Guid id, string code, string secret) =>
        Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes(id.ToString("N") + ":" + code)
            )
        );
}

public interface IMailSender
{
    // HÀM SendCode: Gửi OTP qua SMTP TLS tới email được truyền vào; triển khai thật dùng
    // SmtpMailSender, kiểm thử có thể thay bằng IMailSender giả lập.
    // THAM SỐ email (string): địa chỉ email đầu vào; các luồng tài khoản chuẩn hóa trim/hoa thường
    // trước khi tra cứu.
    // THAM SỐ code (string): OTP 6 chữ số để gửi email hoặc đưa vào phép HMAC.
    // THAM SỐ cancellationToken (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền
    // xuống I/O để dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm sau các bước
    // await.
    Task SendCode(string email, string code, CancellationToken cancellationToken);
}

// THAM SỐ KHỞI TẠO config (IConfiguration): cấu hình ứng dụng lấy từ appsettings và biến môi
// trường, như SMTP/GHN/khóa OTP; do DI hoặc nơi gọi constructor truyền vào.
public sealed class SmtpMailSender(IConfiguration config) : IMailSender
{
    // HÀM SendCode: Gửi OTP qua SMTP TLS tới email được truyền vào; triển khai thật dùng
    // SmtpMailSender, kiểm thử có thể thay bằng IMailSender giả lập.
    // THAM SỐ email (string): địa chỉ email đầu vào; các luồng tài khoản chuẩn hóa trim/hoa thường
    // trước khi tra cứu.
    // THAM SỐ code (string): OTP 6 chữ số để gửi email hoặc đưa vào phép HMAC.
    // THAM SỐ cancellationToken (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền
    // xuống I/O để dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm sau các bước
    // await.
    public async Task SendCode(string email, string code, CancellationToken cancellationToken)
    {
        // BIẾN o (SmtpOptions): cấu hình kết nối gửi email: máy chủ, cổng, tài khoản, TLS và địa
        // chỉ gửi.
        var o = config.GetSection("Smtp").Get<SmtpOptions>() ?? new();
        if (!o.Ready || !o.EnableSsl)
            throw new InvalidOperationException("SMTP chưa cấu hình TLS.");
        // BIẾN client (SmtpClient): SmtpClient; được khởi tạo mới trong bộ nhớ tại đây, không đồng
        // nghĩa SQL đã commit.
        using var client = new System.Net.Mail.SmtpClient(o.Host, o.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new System.Net.NetworkCredential(o.User, o.Password),
            Timeout = 15000,
        };
        // BIẾN message (MailMessage): MailMessage; được khởi tạo mới trong bộ nhớ tại đây, không
        // đồng nghĩa SQL đã commit.
        using var message = new System.Net.Mail.MailMessage(
            o.From,
            email,
            "Mã khôi phục tài khoản QOANH",
            $"Mã OTP: {code}\nMã có hiệu lực 10 phút, tối đa 5 lần thử. Không cung cấp mã cho người khác."
        );
        await client.SendMailAsync(message, cancellationToken);
    }
}

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
// THAM SỐ KHỞI TẠO mail (IMailSender): IMailSender gửi OTP; triển khai thật là SmtpMailSender, kiểm
// thử có thể thay bộ gửi giả; do DI hoặc nơi gọi constructor truyền vào.
// THAM SỐ KHỞI TẠO config (IConfiguration): cấu hình ứng dụng lấy từ appsettings và biến môi
// trường, như SMTP/GHN/khóa OTP; do DI hoặc nơi gọi constructor truyền vào.
// THAM SỐ KHỞI TẠO logger (ILogger<PasswordRecovery>): bộ ghi log của lớp, ghi trạng thái/lỗi theo
// các lệnh Log bên dưới; do DI hoặc nơi gọi constructor truyền vào.
public sealed class PasswordRecovery(
    StoreService store,
    IMailSender mail,
    IConfiguration config,
    ILogger<PasswordRecovery> logger
)
{
    // THUỘC TÍNH PasswordRecovery.Ready (bool): giá trị suy ra từ cấu hình bắt buộc đã có; không tự
    // chứng minh dịch vụ ngoài đang kết nối thành công.
    public bool Ready =>
        (config.GetSection("Smtp").Get<SmtpOptions>() ?? new()).Ready
        && (config["PasswordReset:HashKey"]?.Length ?? 0) >= 32;

    // HÀM Request: Chỉ phát/gửi OTP khi SMTP và khóa HMAC đã sẵn sàng. Sau khi gửi thành công mới
    // đánh dấu Sent; lỗi gửi được ghi loại lỗi, không ghi OTP hoặc địa chỉ nhận vào log.
    // THAM SỐ email (string): địa chỉ email đầu vào; các luồng tài khoản chuẩn hóa trim/hoa thường
    // trước khi tra cứu.
    // THAM SỐ cancellationToken (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền
    // xuống I/O để dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm sau các bước
    // await.
    public async Task Request(string email, CancellationToken cancellationToken)
    {
        if (!Ready)
            return;
        // BIẾN challenge ((Guid Id, string Email, string Code)?): challenge OTP vừa được cấp gồm
        // Id/Email/Code; null nghĩa không được phát mã hoặc không có khách phù hợp.
        var challenge = store.IssueReset(email, config["PasswordReset:HashKey"]!);
        if (challenge == null)
            return;
        try
        {
            await mail.SendCode(challenge.Value.Email, challenge.Value.Code, cancellationToken);
            store.ResetDelivered(challenge.Value.Id);
        }
        // BIẾN LỖI ex (Exception): ngoại lệ bắt từ khối try; Message chứa thông báo, còn loại lỗi
        // dùng quyết định cách báo/ghi log ở catch.
        catch (Exception ex)
            when (ex
                    is System.Net.Mail.SmtpException
                        or InvalidOperationException
                        or OperationCanceledException
                        or FormatException
                        or ArgumentException
            )
        {
            logger.LogWarning(
                "Password recovery delivery failed ({ErrorType}); no OTP or recipient logged.",
                ex.GetType().Name
            );
        }
    }

    // HÀM Complete: Chỉ cho hoàn tất đặt lại mật khẩu khi cấu hình khôi phục đã sẵn sàng; sau đó
    // chuyển kiểm tra OTP sang StoreService.ResetPassword.
    // THAM SỐ input (ResetInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (bool): cờ đúng/sai; ý nghĩa được xác định bởi biểu thức return bên dưới.
    public bool Complete(ResetInput input) =>
        Ready && store.ResetPassword(input, config["PasswordReset:HashKey"]!);
}
