// VAI TRÒ FILE: Service nghiệp vụ trung tâm. SQL Server là nguồn dữ liệu; Read dựng StoreData,
// Write quản lý
// transaction/khóa/kiểm tra rồi PersistNative lưu. Những file StoreService partial khác dùng chung
// factory/hasher
// và helper.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

// Compatibility facade for the existing QOANH views/controllers; SQL is the source of truth.
// THAM SỐ KHỞI TẠO factory (IDbContextFactory<StoreDbContext>): factory tạo DbContext riêng mỗi lần
// đọc/ghi, tránh giữ một context dùng chung giữa request; do DI hoặc nơi gọi constructor truyền
// vào.
// THAM SỐ KHỞI TẠO configuration (IConfiguration?): cấu hình tùy chọn của StoreService; null dùng
// các giá trị mặc định trong service; do DI hoặc nơi gọi constructor truyền vào.
public sealed partial class StoreService(
    IDbContextFactory<StoreDbContext> factory,
    IConfiguration? configuration = null
)
{
    // BIẾN CỦA LỚP hasher (PasswordHasher<Customer>): bộ băm/xác minh ASP.NET Core Identity cho
    // Customer, dùng salt/định dạng Identity thay vì lưu mật khẩu rõ.
    readonly PasswordHasher<Customer> hasher = new();

    // HÀM Read: Đọc một ảnh dữ liệu StoreData từ SQL trong transaction Serializable; NoTracking
    // dùng cho truy vấn chỉ đọc. Callback chọn phần cần trả về, rồi transaction được commit.
    // KIỂU T: kiểu kết quả do callback trả, được suy ra từ chỗ gọi; một hàm đọc/ghi có thể trả
    // entity, danh sách, bool hoặc số.
    // THAM SỐ read (Func<StoreData, T>): hàm callback chỉ đọc, nhận StoreData hoặc DbContext rồi
    // trả kết quả kiểu T cho caller.
    // TRẢ VỀ (T): kết quả T do callback quyết định; chỉ trả sau khi transaction của hàm hoàn tất.
    public T Read<T>(Func<StoreData, T> read)
    {
        // BIẾN db (StoreDbContext): DbContext riêng cho lần đọc/ghi này; using bảo đảm giải phóng
        // kết nối khi kết thúc phạm vi.
        using var db = factory.CreateDbContext();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        // BIẾN tx (IDbContextTransaction): transaction SQL của lần xử lý; chỉ Commit sau khi mọi
        // kiểm tra và bước ghi cần thiết thành công.
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        // BIẾN result (T): kết quả kiểu T do callback read trả về sau khi đọc ảnh StoreData; đây là
        // giá trị hàm Read trả cho controller.
        var result = read(Load(db));
        // HOÀN TẤT GIAO DỊCH: chấp nhận toàn bộ thay đổi/ảnh đọc trong transaction; ngoại lệ trước
        // bước này không
        // được coi là thành công.
        tx.Commit();
        return result;
    }

    // HÀM Write: Cổng ghi nghiệp vụ dùng chung: mở transaction → lấy khóa ứng dụng độc quyền → Load
    // dữ liệu → chạy callback thêm/sửa/xóa → chuẩn hóa tồn → Validate → tính giá vốn →
    // PersistNative → SaveChanges → Commit. Nếu có ngoại lệ trước Commit, transaction bị hủy khi
    // dispose.
    // KIỂU T: kiểu kết quả do callback trả, được suy ra từ chỗ gọi; một hàm đọc/ghi có thể trả
    // entity, danh sách, bool hoặc số.
    // THAM SỐ write (Func<StoreData, T>): hàm callback thay đổi dữ liệu và trả T; transaction bao
    // ngoài chỉ hoàn tất sau SaveChanges/Commit.
    // THAM SỐ actorId (int?): id người thực hiện để ghi nhật ký; null dành cho thao tác không
    // truyền người cụ thể theo luồng gọi. Mặc định khi không truyền: null.
    // THAM SỐ reason (string): lý do nghiệp vụ do người dùng nhập; Text/attribute kiểm tra độ dài
    // trước khi ghi nhật ký hoặc phiếu. Mặc định khi không truyền: "Điều chỉnh dữ liệu".
    // TRẢ VỀ (T): kết quả T do callback quyết định; chỉ trả sau khi transaction của hàm hoàn tất.
    // BIẾN LAMBDA s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản phẩm;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA g (IGrouping<int, WarehouseStock>): một nhóm phần tử có cùng Key do GroupBy tạo
    // ra; nhóm được duyệt để cộng hoặc lưu chung; tên này chỉ có nghĩa trong lambda tương ứng,
    // không phải biến toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: PrepareStock → Validate → PersistNative; xem định nghĩa các hàm này để theo
    // tiếp bước đọc/ghi.
    public T Write<T>(
        Func<StoreData, T> write,
        int? actorId = null,
        string reason = "Điều chỉnh dữ liệu"
    )
    {
        // BIẾN db (StoreDbContext): DbContext riêng cho lần đọc/ghi này; using bảo đảm giải phóng
        // kết nối khi kết thúc phạm vi.
        using var db = factory.CreateDbContext();
        // BIẾN tx (IDbContextTransaction): transaction SQL của lần xử lý; chỉ Commit sau khi mọi
        // kiểm tra và bước ghi cần thiết thành công.
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        db.Database.ExecuteSqlRaw(
            (
                "DECLARE @result int; EXEC @result=sys.sp_getapplock "
                + "@Resource=N'QOANH.Store.Write', @LockMode='Exclusive', "
                + "@LockOwner='Transaction', @LockTimeout=15000; IF "
                + "@result < 0 THROW 51000, N'Không lấy được khóa nghiệp "
                + "vụ. Vui lòng thử lại.', 1;"
            )
        );
        // BIẾN data (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng.
        var data = Load(db);
        // BIẾN beforeVariantStocks (Dictionary<int, int>): bảng tra VariantId → tổng tồn trước
        // callback, làm mốc tính giá vốn sau nhập/nhận lại.
        var beforeVariantStocks = data
            .Stocks.GroupBy(s => s.VariantId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));
        // BIẾN oldStatuses (Dictionary<string, string>): bảng tra mã đơn công khai → trạng thái
        // trước callback, dùng phát hiện đơn vừa chuyển trạng thái.
        var oldStatuses = data.Orders.ToDictionary(o => o.Id, o => o.Status);
        // BIẾN approvedReturns (HashSet<int>): tập mã phiếu trả đã Approved trước callback, giúp
        // chỉ tính các phiếu vừa được duyệt.
        var approvedReturns = data
            .Returns.Where(r => r.Status == "Approved")
            .Select(r => r.Id)
            .ToHashSet();
        // BIẾN before (Dictionary<int, int>): bảng tra ProductId → tồn tổng trước callback, dùng so
        // sánh và tự bổ sung nhật ký thay đổi.
        var before = data.Products.ToDictionary(p => p.Id, p => p.Stock);
        // BIẾN movementCount (int): số biến động tồn trước callback; các phần tử sau mốc này là
        // nhật ký vừa phát sinh.
        var movementCount = data.StockMovements.Count;
        // BIẾN result (T): kết quả kiểu T của callback write; chỉ trả cho caller sau Validate, đồng
        // bộ SQL và Commit thành công.
        var result = write(data);
        // BIẾN LẶP p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
        // thể; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var p in data.Products)
        {
            // BIẾN previous (int): tồn tổng sản phẩm trước callback, dùng so sánh với tồn sau để bổ
            // sung nhật ký nếu chưa được ghi.
            int previous = before.GetValueOrDefault(p.Id);
            if (
                previous != p.Stock
                && !data.StockMovements.Skip(movementCount).Any(m => m.ProductId == p.Id)
            )
                data.StockMovements.Add(
                    new StockMovement
                    {
                        Id = Next(data.StockMovements.Select(x => x.Id)),
                        ProductId = p.Id,
                        Before = previous,
                        Delta = p.Stock - previous,
                        After = p.Stock,
                        ActorId = actorId,
                        Reason = reason,
                        Reference = "AUTO-" + Guid.NewGuid().ToString("N"),
                    }
                );
        }

        PrepareStock(data);
        Validate(data);
        UpdateCosting(db, data, beforeVariantStocks, oldStatuses, approvedReturns, movementCount);
        PersistNative(db, data);
        // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
        // hoàn tất khi Commit
        // thành công.
        db.SaveChanges();
        // HOÀN TẤT GIAO DỊCH: chấp nhận toàn bộ thay đổi/ảnh đọc trong transaction; ngoại lệ trước
        // bước này không
        // được coi là thành công.
        tx.Commit();
        return result;
    }

    // HÀM Next: Sinh mã nghiệp vụ bằng mã lớn nhất hiện có cộng 1; tập rỗng cho kết quả 1. Mã này
    // phục vụ mô hình StoreData, không thay thế mọi cột IDENTITY của SQL.
    // THAM SỐ ids (IEnumerable<int>): tập mã hiện có; Next tìm max rồi cộng 1 để tạo mã nghiệp vụ
    // tiếp theo.
    // TRẢ VỀ (int): số nguyên 32 bit; ý nghĩa được xác định bởi biểu thức return bên dưới.
    static int Next(IEnumerable<int> ids) => ids.DefaultIfEmpty().Max() + 1;

    // HÀM Require: Chốt kiểm tra nghiệp vụ: condition phải đúng để chạy tiếp; nếu sai thì ném
    // InvalidOperationException với message để tầng gọi hiển thị hoặc xử lý lỗi.
    // THAM SỐ condition (bool): biểu thức bắt buộc đúng; false làm Require ném
    // InvalidOperationException.
    // THAM SỐ message (string): thông báo nghiệp vụ trả khi condition sai; không phải thông báo
    // thành công.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    // HÀM Validate: Kiểm tra toàn bộ StoreData trước khi ghi: tồn 0–100.000, email/mã ưu đãi/danh
    // mục không trùng, số điện thoại không trùng trong từng nhóm khách/nhân viên, vai trò hợp lệ,
    // mật khẩu là Identity hash và số tiền/số lượng đơn hợp lệ.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
    // giới hạn lượt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản phẩm;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static void Validate(StoreData d)
    {
        // ĐIỀU KIỆN: Tồn kho phải từ 0 đến 100.000. Không đạt thì Require ném lỗi và dừng luồng xử
        // lý.
        Require(
            d.Products.All(p => p.Stock is >= 0 and <= 100000),
            "Tồn kho phải từ 0 đến 100.000."
        );
        // ĐIỀU KIỆN: Email đã được sử dụng. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(
            d.Customers.Select(c => c.Email.Trim().ToUpperInvariant()).Distinct().Count()
                == d.Customers.Count,
            "Email đã được sử dụng."
        );
        // ĐIỀU KIỆN: Mã ưu đãi đã tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(
            d.Promotions.Select(p => p.Code.ToUpperInvariant()).Distinct().Count()
                == d.Promotions.Count,
            "Mã ưu đãi đã tồn tại."
        );
        // ĐIỀU KIỆN: Danh mục đã tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(
            d.Categories.Distinct(StringComparer.OrdinalIgnoreCase).Count() == d.Categories.Count,
            "Danh mục đã tồn tại."
        );
        // BIẾN LẶP group (IGrouping<bool, Customer>): một nhóm phần tử có cùng Key do GroupBy tạo
        // ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi lượt nhận một phần tử của nguồn sau in,
        // chỉ dùng trong vòng foreach này.
        foreach (var group in d.Customers.GroupBy(c => c.Role == "Customer"))
        {
            // BIẾN phones (List<string>): các số điện thoại không rỗng đã Trim của một nhóm
            // khách/nhân viên, dùng kiểm tra trùng trong nhóm.
            var phones = group
                .Where(c => !string.IsNullOrWhiteSpace(c.Phone))
                .Select(c => c.Phone.Trim())
                .ToList();
            // ĐIỀU KIỆN: Duplicate phone number. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(phones.Distinct().Count() == phones.Count, "Duplicate phone number.");
        }

        // BIẾN LẶP c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
        // vai trò và trạng thái; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
        // foreach này.
        foreach (var c in d.Customers)
        {
            // ĐIỀU KIỆN: Profile field is too long. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(
                c.Phone.Length <= 15 && c.Address.Length <= 255 && c.Email.Length <= 150,
                "Profile field is too long."
            );
            // ĐIỀU KIỆN: Vai trò không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(
                c.Role is "Admin" or "Customer" or "Catalog" or "Inventory" or "Sales" or "Support",
                "Vai trò không hợp lệ."
            );
            // ĐIỀU KIỆN: Mật khẩu phải là Identity hash; không chấp nhận văn bản rõ hoặc hash sai
            // định dạng. Không
            // đạt thì Require ném lỗi và dừng luồng ghi.
            Require(
                PasswordStorage.IsIdentityHash(c.PasswordHash),
                (
                    "Mật khẩu phải là Identity hash; không chấp nhận văn bản "
                    + "rõ hoặc hash sai định dạng."
                )
            );
        }

        // ĐIỀU KIỆN: Invalid variant stock. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(d.Stocks.All(s => s.Quantity >= 0), "Invalid variant stock.");
        // BIẾN LẶP p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
        // thể; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var p in d.Products)
            Validator.ValidateObject(p, new ValidationContext(p), true);
        // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var o in d.Orders)
        {
            // ĐIỀU KIỆN: Chi tiết đơn hàng không hợp lệ. Không đạt thì Require ném lỗi và dừng
            // luồng xử lý.
            Require(
                o.Lines.Count > 0
                    && o.Lines.All(l =>
                        l.Quantity > 0 && l.Price >= 0 && !string.IsNullOrWhiteSpace(l.Size)
                    ),
                "Chi tiết đơn hàng không hợp lệ."
            );
            // ĐIỀU KIỆN: Số tiền đơn hàng không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng
            // xử lý.
            Require(
                o.Discount >= 0
                    && o.Discount <= o.Subtotal
                    && o.PaidAmount >= 0
                    && o.PaidAmount <= o.Total
                    && o.RefundedAmount >= 0
                    && o.RefundedAmount <= o.PaidAmount,
                "Số tiền đơn hàng không hợp lệ."
            );
        }
    }

    // HÀM Authenticate: Tìm tài khoản đang hoạt động theo email, kiểm tra định dạng hash và mật
    // khẩu bằng Identity; sai thì ghi nhận thất bại khi đã tìm thấy tài khoản. Hash cũ cần nâng cấp
    // được băm lại; thành công trả về tài khoản kèm mã lịch sử đăng nhập.
    // THAM SỐ email (string): địa chỉ email đầu vào; các luồng tài khoản chuẩn hóa trim/hoa thường
    // trước khi tra cứu.
    // THAM SỐ password (string): mật khẩu rõ chỉ dùng làm đầu vào xác minh/băm; không lưu trực tiếp
    // vào database.
    // THAM SỐ ip (string?): IP của yêu cầu đăng nhập, được cắt theo giới hạn cột trước lưu lịch sử.
    // Mặc định khi không truyền: null.
    // THAM SỐ device (string?): chuỗi User-Agent mô tả trình duyệt/thiết bị, được cắt độ dài trước
    // lưu. Mặc định khi không truyền: null.
    // TRẢ VỀ (Customer?): Customer khi email/mật khẩu hợp lệ và tài khoản hoạt động; null khi xác
    // thực không thành công.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Read → Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public Customer? Authenticate(
        string email,
        string password,
        string? ip = null,
        string? device = null
    )
    {
        // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
        // trò và trạng thái; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
        // ngay sau.
        var c = Read(d =>
            d.Customers.FirstOrDefault(c =>
                c.Active && c.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)
            )
        );
        if (c == null)
            return null;
        if (!PasswordStorage.IsIdentityHash(c.PasswordHash))
        {
            LogLogin(c, false, ip, device);
            return null;
        }

        // BIẾN verified (PasswordVerificationResult): kết quả VerifyHashedPassword để phân biệt mật
        // khẩu sai, đúng hoặc cần băm nâng cấp.
        var verified = hasher.VerifyHashedPassword(c, c.PasswordHash, password);
        if (verified == PasswordVerificationResult.Failed)
        {
            LogLogin(c, false, ip, device);
            return null;
        }

        if (verified == PasswordVerificationResult.SuccessRehashNeeded)
        {
            c.PasswordHash = hasher.HashPassword(c, password);
            Write(d =>
            {
                d.Customers.Single(x => x.Id == c.Id).PasswordHash = c.PasswordHash;
                return 0;
            });
        }

        c.LoginHistoryId = LogLogin(c, true, ip, device);
        return c;
    }

    // HÀM Register: Thêm khách hàng mới: kiểm tra DataAnnotations và email chưa dùng, chuẩn hóa
    // tên/email, băm mật khẩu rồi thêm vào d.Customers trong Write. Vai trò mặc định lấy từ model
    // Customer.
    // THAM SỐ input (RegisterInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai trò
    // và trạng thái; ý nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public Customer Register(RegisterInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        return Write(d =>
        {
            // ĐIỀU KIỆN: Email đã được sử dụng. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(
                !d.Customers.Any(c =>
                    c.Email.Equals(input.Email.Trim(), StringComparison.OrdinalIgnoreCase)
                ),
                "Email đã được sử dụng."
            );
            // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
            // vai trò và trạng thái; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa SQL
            // đã commit.
            var c = new Customer
            {
                Id = Next(d.Customers.Select(c => c.Id)),
                Name = input.Name.Trim(),
                Email = input.Email.Trim().ToLowerInvariant(),
            };
            c.PasswordHash = hasher.HashPassword(c, input.Password);
            d.Customers.Add(c);
            return c;
        });
    }
}
