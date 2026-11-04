using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
// THAM SỐ KHỞI TẠO pdf (PdfDocuments): PdfDocuments dựng byte PDF từ dữ liệu chứng từ đã kiểm tra
// quyền; do DI hoặc nơi gọi constructor truyền vào.
[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DocumentsController(StoreService store, PdfDocuments pdf) : Controller
{
    // THUỘC TÍNH DocumentsController.Actor (int): id lấy từ claim của người đang đăng nhập; có thể
    // âm với nhân viên, dương với khách; không lấy từ ô input do người dùng tự chọn.
    int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // THUỘC TÍNH DocumentsController.Sales (bool): true nếu người dùng hiện tại là Admin hoặc
    // Sales, dùng kiểm tra quyền tải chứng từ.
    bool Sales => User.IsInRole("Admin") || User.IsInRole("Sales");

    // HÀM Order: Xuất PDF đơn hàng: chỉ Admin/Sales hoặc Customer sở hữu đơn được tải; chứa dòng
    // hàng, giảm giá, vận chuyển, tổng thu và hoàn tiền.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Documents/Order. Tham số hàm: (string id).
    public IActionResult Order(string id)
    {
        // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var o = store.Read(d => d.Orders.FirstOrDefault(o => o.Id == id));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (o == null || !Sales && !(User.IsInRole("Customer") && o.CustomerId == Actor))
            return NotFound();
        // BIẾN info (List<string>): danh sách/truy vấn các phần tử: string; được khởi tạo mới trong
        // bộ nhớ tại đây, không đồng nghĩa SQL đã commit.
        var info = new List<string>
        {
            o.Created.ToString("dd/MM/yyyy HH:mm"),
            o.Name,
            o.Phone,
            o.Address,
            "Trạng thái: " + o.Status,
            "Thanh toán: " + o.Payment,
        };
        if (o.TransferReference != null)
            info.Add("Nội dung chuyển khoản: " + o.TransferReference);
        return File(
            pdf.Create(
                "ĐƠN HÀNG",
                o.Id,
                info,
                new[] { "Sản phẩm / size / màu", "SL", "Đơn giá", "Thành tiền" },
                o.Lines.Select(l =>
                        new[]
                        {
                            l.Name + " / " + l.Size + " / " + l.Color,
                            l.Quantity.ToString(),
                            l.Price.ToString("N0"),
                            (l.Price * l.Quantity).ToString("N0"),
                        }
                    )
                    .ToList(),
                new[]
                {
                    "Tiền hàng: " + o.Subtotal.ToString("N0") + " đ",
                    "Giảm giá: " + o.Discount.ToString("N0") + " đ",
                    "Vận chuyển: " + o.Shipping.ToString("N0") + " đ",
                    "Tổng cộng: " + o.Total.ToString("N0") + " đ",
                    "Đã nhận: "
                        + o.PaidAmount.ToString("N0")
                        + " đ; Đã hoàn: "
                        + o.RefundedAmount.ToString("N0")
                        + " đ",
                }
            ),
            "application/pdf",
            "DonHang-" + o.DatabaseId + ".pdf"
        );
    }

    // HÀM Receipt: Inventory/Admin tải PDF phiếu nhập và tổng các dòng; không tìm thấy phiếu trả
    // 404.
    // THAM SỐ id (int): khóa SQL của phiếu nhập kho cần xem/in, không phải mã đơn bán hàng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ChiTietPhieuNhapRow): entity của bảng ChiTietPhieuNhap, số lượng và giá nhập
    // từng biến thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Documents/Receipt. Tham số hàm: (int id).
    [Authorize(Roles = "Admin,Inventory")]
    public IActionResult Receipt(int id)
    {
        // BIẾN p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; kết quả tra cứu có
        // thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var p = store.NativeRead(db => db.PhieuNhap.SingleOrDefault(p => p.MaPhieuNhap == id));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (p == null)
            return NotFound();
        // BIẾN lines (List<ChiTietPhieuNhapRow>): danh sách/truy vấn các phần tử: entity của bảng
        // ChiTietPhieuNhap, số lượng và giá nhập từng biến thể; ToList lấy kết quả ra danh sách để
        // dùng tiếp trong phạm vi này.
        var lines = store.NativeRead(db =>
            db.ChiTietPhieuNhap.Where(l => l.MaPhieuNhap == id).ToList()
        );
        return File(
            pdf.Create(
                "PHIẾU NHẬP KHO",
                p.QReference ?? ("PN-" + p.MaPhieuNhap),
                new[]
                {
                    p.NgayNhap.ToString("dd/MM/yyyy HH:mm"),
                    $"Kho #{p.MaKho} / Nhà cung cấp #{p.MaNCC} / Nhân viên #{p.MaNV}",
                },
                new[] { "Biến thể", "SL", "Giá nhập", "Thành tiền" },
                lines
                    .Select(l =>
                        new[]
                        {
                            "SKU / biến thể #" + l.MaBienThe,
                            l.SoLuong.ToString(),
                            l.GiaNhap.ToString("N0"),
                            (l.SoLuong * l.GiaNhap).ToString("N0"),
                        }
                    )
                    .ToList(),
                new[]
                {
                    "Tổng chứng từ: " + p.TongTien.ToString("N0") + " đ",
                    "Tổng chi tiết: " + lines.Sum(l => l.SoLuong * l.GiaNhap).ToString("N0") + " đ",
                }
            ),
            "application/pdf",
            "PhieuNhap-" + id + ".pdf"
        );
    }

    // HÀM Return: Xuất PDF phiếu trả cho Admin/Sales hoặc chính khách sở hữu, kèm lý do, tình
    // trạng, số lượng và tiền hoàn.
    // THAM SỐ id (int): mã phiếu yêu cầu trả hàng cần xem/in.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn và tình
    // trạng hàng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Documents/Return. Tham số hàm: (int id).
    public IActionResult Return(int id)
    {
        // BIẾN r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
        // hoàn; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var r = store.Read(d => d.Returns.FirstOrDefault(r => r.Id == id));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (r == null || !Sales && !(User.IsInRole("Customer") && r.CustomerId == Actor))
            return NotFound();
        // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền.
        var o = store.Read(d => d.Orders.Single(o => o.Id == r.OrderId));
        return File(
            pdf.Create(
                "PHIẾU TRẢ HÀNG",
                "PT-" + r.Id,
                new[]
                {
                    "Đơn: " + o.Id,
                    r.Created.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    "Lý do: " + r.Reason,
                    "Trạng thái: "
                        + (
                            r.Status == "Approved" ? "Đã duyệt"
                            : r.Status == "Rejected" ? "Từ chối"
                            : "Chờ xử lý"
                        ),
                    "Chứng từ: " + r.Reference,
                },
                new[] { "Sản phẩm", "SL trả", "Tiền hàng hoàn", "Tình trạng" },
                r.Lines.Select(l =>
                        new[]
                        {
                            o.Lines.Single(x => x.Id == l.OrderLineId).Name,
                            l.Quantity.ToString(),
                            l.Refund.ToString("N0"),
                            l.Condition,
                        }
                    )
                    .ToList(),
                new[]
                {
                    "Số tiền hoàn theo phiếu: " + r.Refund.ToString("N0") + " đ",
                    "Ghi chú: " + r.Resolution,
                }
            ),
            "application/pdf",
            "PhieuTra-" + id + ".pdf"
        );
    }
}
