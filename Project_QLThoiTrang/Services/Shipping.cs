// VAI TRÒ FILE: Tạo/đồng bộ vận đơn GHN. StoreService khóa và lưu trạng thái yêu cầu;
// ShippingService gọi HTTP;
// Unknown biểu thị cần đối soát chứ chưa biết đã tạo hay chưa.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // HÀM ClaimShipment: Khóa/lưu yêu cầu tạo vận đơn trước khi gọi mạng: kiểm tra quyền, trạng
    // thái đơn, số còn thu và thanh toán chuyển khoản. RequestHash phải khớp yêu cầu đã gửi; chặn
    // lần Requesting chưa quá một phút.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ requestHash (string): SHA-256 của payload GHN để bảo đảm thử lại không âm thầm thay
    // nội dung đã gửi.
    // THAM SỐ expectedOutstanding (decimal): số tiền còn phải thu khi chuẩn bị gửi GHN; so lại với
    // SQL trước nhận quyền gửi vận đơn.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và mã vận
    // đơn; ý nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA o (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (ThanhToanRow): entity của bảng ThanhToan, thông tin phương thức/trạng thái
    // thanh toán gốc; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và
    // mã vận đơn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public Shipment ClaimShipment(
        string id,
        string requestHash,
        decimal expectedOutstanding,
        int actor
    ) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Sales");
            // BIẾN legacyId (int): số nguyên 32 bit.
            // BIẾN OUT parsed (int): mã đơn SQL tách từ chuỗi QH-SQL-... nếu TryParse thành công.
            int legacyId =
                id.StartsWith("QH-SQL-") && int.TryParse(id[7..], out var parsed) ? parsed : 0;
            // BIẾN o (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL; kết quả tra cứu có
            // thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var o = db.DonHang.SingleOrDefault(o =>
                o.QPublicId == id || (o.QPublicId == null && o.MaDonHang == legacyId)
            );
            // ĐIỀU KIỆN: Đơn không tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(o != null, "Đơn không tồn tại.");
            // ĐIỀU KIỆN: Đơn chưa đủ điều kiện tạo vận đơn. Không đạt thì Require ném lỗi và dừng
            // luồng xử lý.
            Require(
                o!.TrangThai is "Đã xác nhận" or "Đang chuẩn bị" or "Đang giao",
                "Đơn chưa đủ điều kiện tạo vận đơn."
            );
            // BIẾN p (ThanhToanRow): entity của bảng ThanhToan, thông tin phương thức/trạng thái
            // thanh toán gốc; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc
            // ?? ngay sau.
            var p = db.ThanhToan.SingleOrDefault(p => p.MaDonHang == o.MaDonHang);
            // ĐIỀU KIỆN: Số tiền đã thay đổi, tải lại trước khi tạo vận đơn. Không đạt thì Require
            // ném lỗi và dừng
            // luồng ghi.
            Require(
                (p?.TrangThai == "Đã thanh toán" ? 0 : o.TongThanhToan) == expectedOutstanding,
                "Số tiền đã thay đổi, tải lại trước khi tạo vận đơn."
            );
            // ĐIỀU KIỆN: Đơn chuyển khoản phải thu đủ trước khi tạo vận đơn. Không đạt thì Require
            // ném lỗi và dừng
            // luồng ghi.
            Require(
                p?.PhuongThuc != "Chuyển khoản" || p.TrangThai == "Đã thanh toán",
                "Đơn chuyển khoản phải thu đủ trước khi tạo vận đơn."
            );
            // BIẾN s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái
            // và mã vận đơn; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
            // ngay sau.
            var s = db.Shipments.SingleOrDefault(s => s.OrderId == o.MaDonHang);
            if (s == null)
            {
                s = new()
                {
                    OrderId = o.MaDonHang,
                    ClientCode = id,
                    RequestHash = requestHash,
                };
                db.Shipments.Add(s);
            }

            if (s.Status == "Rejected" && s.TrackingCode == null)
                s.RequestHash = requestHash;
            // ĐIỀU KIỆN: Không đổi nội dung yêu cầu đã gửi GHN. Đối soát vận đơn trước. Không đạt
            // thì Require ném lỗi
            // và dừng luồng ghi.
            Require(
                s.RequestHash == requestHash,
                "Không đổi nội dung yêu cầu đã gửi GHN. Đối soát vận đơn trước."
            );
            // ĐIỀU KIỆN: Vận đơn đang được xử lý. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(
                s.Status != "Requesting" || s.UpdatedUtc < DateTime.UtcNow.AddMinutes(-1),
                "Vận đơn đang được xử lý."
            );
            s.Status = "Requesting";
            s.UpdatedUtc = DateTime.UtcNow;
            return s;
        });

    // HÀM ShipmentResult: Lưu mã, trạng thái, thông báo lỗi và thời điểm cập nhật vận đơn trong
    // SQL; giữ mã cũ nếu lần này không có mã mới.
    // THAM SỐ orderId (int): khóa số nguyên MaDonHang trong SQL của vận đơn.
    // THAM SỐ code (string?): mã vận đơn GHN trả về; null thì giữ mã vận đơn cũ.
    // THAM SỐ status (string): trạng thái đích muốn cập nhật; phải nằm trong tập chuyển trạng thái
    // được phép của luồng tương ứng.
    // THAM SỐ error (string): nội dung lỗi vận đơn cần lưu, được giới hạn độ dài; không dùng để tự
    // suy ra đã tạo vận đơn thành công.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và
    // mã vận đơn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void ShipmentResult(int orderId, string? code, string status, string error, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Sales");
            // BIẾN s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái
            // và mã vận đơn.
            var s = db.Shipments.Single(s => s.OrderId == orderId);
            s.TrackingCode = code ?? s.TrackingCode;
            s.Status = Text(status, 1, 60, "Trạng thái vận đơn");
            s.Error = error.Length > 300 ? error[..300] : error;
            s.UpdatedUtc = DateTime.UtcNow;
            return 0;
        });
}

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
// THAM SỐ KHỞI TẠO config (IConfiguration): cấu hình ứng dụng lấy từ appsettings và biến môi
// trường, như SMTP/GHN/khóa OTP; do DI hoặc nơi gọi constructor truyền vào.
// THAM SỐ KHỞI TẠO http (HttpClient): HttpClient được cấp cho gọi GHN, có timeout cấu hình khi đăng
// ký service; do DI hoặc nơi gọi constructor truyền vào.
public sealed class ShippingService(StoreService store, IConfiguration config, HttpClient http)
{
    // THUỘC TÍNH ShippingService.Ready (bool): giá trị suy ra từ cấu hình bắt buộc đã có; không tự
    // chứng minh dịch vụ ngoài đang kết nối thành công.
    public bool Ready => (config.GetSection("Ghn").Get<GhnOptions>() ?? new()).Ready;

    // HÀM Options: Đọc cấu hình Ghn, bắt buộc đủ token/shop và chỉ chấp nhận URL gateway GHN trong
    // danh sách cố định.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (GhnOptions): cấu hình gateway GHN, token kết nối và mã shop; ý nghĩa được xác định
    // bởi biểu thức return bên dưới.
    GhnOptions Options()
    {
        // BIẾN o (GhnOptions): cấu hình gateway GHN, token kết nối và mã shop.
        var o = config.GetSection("Ghn").Get<GhnOptions>() ?? new();
        if (!o.Ready)
            throw new InvalidOperationException("GHN chưa được cấu hình.");
        if (
            o.BaseUrl
            is not (
                "https://dev-online-gateway.ghn.vn/shiip/public-api/"
                or "https://online-gateway.ghn.vn/shiip/public-api/"
            )
        )
            throw new InvalidOperationException("Endpoint GHN không hợp lệ.");
        return o;
    }

    // HÀM Send: Gửi HTTP tới GHN với Token/ShopId; 4xx thành GhnRejectedException, lỗi/kết quả
    // không rõ thành InvalidOperationException. Chỉ trả data khi HTTP thành công và mã JSON bằng
    // 200.
    // THAM SỐ method (HttpMethod): phương thức HTTP của lời gọi GHN, ví dụ GET hoặc POST.
    // THAM SỐ route (string): phần đường dẫn API nối sau gateway GHN đã được kiểm tra cho phép.
    // THAM SỐ body (object?): payload JSON cần gửi GHN; null với yêu cầu không có body.
    // THAM SỐ token (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền xuống I/O để
    // dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task<JsonElement>): Task<JsonElement>; ý nghĩa được xác định bởi biểu thức return bên
    // dưới.
    async Task<JsonElement> Send(
        HttpMethod method,
        string route,
        object? body,
        CancellationToken token
    )
    {
        // BIẾN o (GhnOptions): cấu hình gateway GHN, token kết nối và mã shop.
        var o = Options();
        // BIẾN request (HttpRequestMessage): HttpRequestMessage; được khởi tạo mới trong bộ nhớ tại
        // đây, không đồng nghĩa SQL đã commit.
        using var request = new HttpRequestMessage(method, o.BaseUrl + route);
        request.Headers.Add("Token", o.Token);
        request.Headers.Add("ShopId", o.ShopId.ToString());
        if (body != null)
            request.Content = JsonContent.Create(body);
        // BIẾN response (HttpResponseMessage): HttpResponseMessage.
        using var response = await http.SendAsync(request, token);
        // BIẾN json (JsonDocument): tài liệu JSON đã parse; cần dispose sau khi dùng.
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        // BIẾN root (JsonElement): một phần dữ liệu JSON phản hồi, được đọc các trường bằng
        // GetProperty.
        var root = json.RootElement;
        if ((int)response.StatusCode is >= 400 and < 500)
            throw new GhnRejectedException(
                "GHN từ chối yêu cầu. Kiểm tra địa chỉ, quyền tài khoản hoặc cấu hình."
            );
        // BIẾN OUT status (JsonElement): trường code JSON GHN để xác nhận API báo thành công.
        if (
            !response.IsSuccessStatusCode
            || !root.TryGetProperty("code", out var status)
            || status.GetInt32() != 200
        )
            throw new InvalidOperationException(
                "Chưa xác định được kết quả GHN. Đối soát trước khi thử lại."
            );
        return root.GetProperty("data").Clone();
    }

    // HÀM Create: Kiểm tra quyền Sales/Admin, địa chỉ/kích thước và số COD nguyên không quá 50
    // triệu. Có mã vận đơn thì đồng bộ; chưa có thì chụp request hash, ClaimShipment, gọi GHN và
    // lưu kết quả. Lỗi mạng được ghi Unknown để đối soát trước khi thử lại.
    // THAM SỐ input (ShipmentInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ token (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền xuống I/O để
    // dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm sau các bước
    // await.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và
    // mã vận đơn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    public async Task Create(ShipmentInput input, int actor, CancellationToken token)
    {
        Options();
        Validator.ValidateObject(input, new ValidationContext(input), true);
        // BIẾN user (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
        // vai trò và trạng thái.
        var user = store.CurrentAccount(actor);
        if (user?.Role is not ("Admin" or "Sales") || !user.Active)
            throw new InvalidOperationException("Không có quyền tạo vận đơn.");
        // BIẾN order (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền.
        var order = store.Read(d => d.Orders.Single(o => o.Id == input.Id));
        // BIẾN existing (Shipment): bản ghi/dòng đã tồn tại để gộp hoặc tránh tạo trùng; yêu
        // cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và mã vận đơn.
        var existing = store.NativeRead(db =>
            db.Shipments.SingleOrDefault(s => s.OrderId == order.DatabaseId)
        );
        if (existing?.TrackingCode != null)
        {
            await Sync(input.Id, actor, token);
            return;
        }

        // BIẾN cod (decimal): tiền còn phải thu hộ = tổng đơn - tiền đã nhận; GHN yêu cầu VND
        // nguyên và giới hạn COD.
        decimal cod = order.Total - order.PaidAmount;
        if (cod != decimal.Truncate(cod))
            throw new InvalidOperationException("GHN requires an integer VND collection amount.");
        if (cod > 50000000m)
            throw new InvalidOperationException("COD vượt giới hạn GHN 50 triệu đồng.");
        // BIẾN body (kiểu tạm suy ra từ new { ... }): payload gửi GHN: người nhận, địa chỉ mới, cân
        // nặng/kích thước, mã đơn, COD và các mặt hàng.
        var body = new
        {
            to_name = order.Name,
            to_phone = order.Phone,
            to_address = order.Address,
            to_province_name = input.Province.Trim(),
            to_ward_name = input.Ward.Trim(),
            is_new_to_address = true,
            client_order_code = order.Id,
            weight = input.Weight,
            length = input.Length,
            width = input.Width,
            height = input.Height,
            service_type_id = 2,
            payment_type_id = 1,
            required_note = "CHOXEMHANGKHONGTHU",
            cod_amount = (long)cod,
            insurance_value = (long)Math.Min(order.Subtotal, 5000000),
            items = order
                .Lines.Select(l => new
                {
                    name = l.Name,
                    code = l.VariantId.ToString(),
                    quantity = l.Quantity,
                })
                .ToArray(),
        };
        // BIẾN hash (string): SHA-256 của payload JSON GHN, dùng khóa nội dung một yêu cầu gửi vận
        // đơn.
        string hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)))
        );
        // BIẾN claim (Shipment): bản ghi Shipment sau khi nhận quyền gửi trong transaction; mang
        // OrderId để lưu kết quả API.
        var claim = store.ClaimShipment(input.Id, hash, cod, actor);
        try
        {
            // BIẾN data (JsonElement): trường data trong JSON GHN tạo vận đơn thành công, dùng lấy
            // order_code.
            var data = await Send(HttpMethod.Post, "v2/shipping-order/create", body, token);
            // BIẾN code (string): mã vận đơn order_code GHN trả về, không phải OTP hay mã đơn công
            // khai của cửa hàng.
            string code =
                data.GetProperty("order_code").GetString()
                ?? throw new InvalidOperationException("GHN thiếu mã vận đơn.");
            store.ShipmentResult(claim.OrderId, code, "ready_to_pick", "", actor);
        }
        // BIẾN LỖI ex (GhnRejectedException): ngoại lệ bắt từ khối try; Message chứa thông báo, còn
        // loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (GhnRejectedException ex)
        {
            // BIẾN uncertain (bool): true nếu lần gửi trước có trạng thái Unknown/Requesting; không
            // được kết luận từ chối chắc chắn cho lần đó.
            bool uncertain = existing?.Status is "Unknown" or "Requesting";
            store.ShipmentResult(
                claim.OrderId,
                null,
                uncertain ? "Unknown" : "Rejected",
                uncertain
                    ? "Lần gửi trước chưa xác định kết quả; cần đồng bộ/đối soát GHN."
                    : ex.Message,
                actor
            );
            throw;
        }
        // BIẾN LỖI ex (Exception): ngoại lệ bắt từ khối try; Message chứa thông báo, còn loại lỗi
        // dùng quyết định cách báo/ghi log ở catch.
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or OperationCanceledException
                        or JsonException
                        or InvalidOperationException
            )
        {
            store.ShipmentResult(
                claim.OrderId,
                null,
                "Unknown",
                "Chưa xác định kết quả. Đồng bộ/đối soát GHN trước khi gửi lại.",
                actor
            );
            throw new InvalidOperationException(
                (
                    "Chưa xác định kết quả GHN. Không tạo đơn mới bằng mã "
                    + "khác; đồng bộ để đối soát."
                )
            );
        }
    }

    // HÀM Sync: Lấy vận đơn GHN theo ClientCode đã lưu, kiểm tra GHN trả đúng mã đơn rồi cập nhật
    // mã vận đơn/trạng thái; không tự đổi trạng thái giao hàng của đơn trong StoreData.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ token (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền xuống I/O để
    // dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm sau các bước
    // await.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và
    // mã vận đơn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    public async Task Sync(string id, int actor, CancellationToken token)
    {
        Options();
        // BIẾN user (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
        // vai trò và trạng thái.
        var user = store.CurrentAccount(actor);
        if (user?.Role is not ("Admin" or "Sales") || !user.Active)
            throw new InvalidOperationException("Không có quyền đồng bộ.");
        // BIẾN order (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền.
        var order = store.Read(d => d.Orders.Single(o => o.Id == id));
        // BIẾN s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và mã
        // vận đơn; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var s =
            store.NativeRead(db => db.Shipments.SingleOrDefault(s => s.OrderId == order.DatabaseId))
            ?? throw new InvalidOperationException("Đơn chưa gửi GHN.");
        // BIẾN data (JsonElement): trường data trong JSON tra vận đơn, phải có client_order_code
        // khớp trước khi cập nhật trạng thái.
        var data = await Send(
            HttpMethod.Get,
            "v2/shipping-order/detail-by-client-code?client_order_code="
                + Uri.EscapeDataString(s.ClientCode),
            null,
            token
        );
        if (data.GetProperty("client_order_code").GetString() != s.ClientCode)
            throw new InvalidOperationException("GHN trả sai mã đơn.");
        store.ShipmentResult(
            order.DatabaseId,
            data.GetProperty("order_code").GetString(),
            data.GetProperty("status").GetString() ?? "Unknown",
            "",
            actor
        );
    }
}

// THAM SỐ KHỞI TẠO message (string): thông báo lỗi chuyển tới lớp ngoại lệ cơ sở; do DI hoặc nơi
// gọi constructor truyền vào.
public sealed class GhnRejectedException(string message) : InvalidOperationException(message);
