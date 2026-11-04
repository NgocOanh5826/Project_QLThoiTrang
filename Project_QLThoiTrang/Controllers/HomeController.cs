using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
public class HomeController(StoreService store) : Controller
{
    // HÀM Index: Trang chủ nhận danh sách sản phẩm Active từ StoreService để hiển thị các khu vực
    // giới thiệu hàng hóa.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Home/Index. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Home/Index.cshtml.
    public IActionResult Index() => View(store.Read(d => d.Products.Where(p => p.Active).ToList()));

    // HÀM Info: Tra nội dung trang thông tin theo id trong bảng pages; id không được hỗ trợ trả
    // 404, có thì ghép thông tin cửa hàng cho view.
    // THAM SỐ id (string): khóa trang thông tin như about/contact/returns/privacy; chỉ khóa có
    // trong pages được hiển thị. Mặc định khi không truyền: "about".
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Home/Info. Tham số hàm: (string id = "about").
    // GIAO DIỆN: Project_QLThoiTrang/Views/Home/Info.cshtml.
    public IActionResult Info(string id = "about")
    {
        // BIẾN pages (Dictionary<string, (string Title, string Text)>): bảng tra khóa trang thông
        // tin → cặp tiêu đề/nội dung được phép hiển thị.
        var pages = new Dictionary<string, (string Title, string Text)>
        {
            ["about"] = (
                "Về QOANH",
                (
                    "Những thiết kế đơn giản, chất liệu dễ chịu và phom dáng "
                    + "dành cho mỗi ngày. QOANH mang đến một tủ đồ dễ phối, "
                    + "bền với thời gian."
                )
            ),
            ["contact"] = (
                "Liên hệ",
                (
                    "Cần tư vấn kích cỡ hoặc hỗ trợ đơn hàng? Liên hệ cửa "
                    + "hàng theo thông tin dưới đây."
                )
            ),
            ["returns"] = (
                "Chính sách đổi trả",
                (
                    "Bạn có thể gửi yêu cầu trả toàn bộ đơn trong chi tiết "
                    + "đơn đã giao. Cửa hàng kiểm tra hàng và ghi nhận hoàn "
                    + "tiền. Chính sách kinh doanh thực tế cần được cửa hàng "
                    + "xác nhận trước khi triển khai."
                )
            ),
            ["guide"] = (
                "Hướng dẫn mua hàng",
                (
                    "Chọn sản phẩm và kích cỡ → thêm vào giỏ → kiểm tra đơn "
                    + "→ đăng nhập → nhập địa chỉ → xác nhận đặt hàng. Bản "
                    + "demo dùng thanh toán khi nhận hàng, không thu tiền trực "
                    + "tuyến."
                )
            ),
            ["stores"] = (
                "Hệ thống cửa hàng",
                (
                    "Thông tin cửa hàng mẫu được hiển thị dưới đây. Bạn có "
                    + "thể cập nhật trong phần cài đặt quản trị."
                )
            ),
            ["terms"] = (
                "Điều khoản sử dụng",
                (
                    "Đây là website demo quản lý thời trang. Đơn hàng và tài "
                    + "khoản phục vụ thử nghiệm; không có giao dịch thanh toán "
                    + "thật."
                )
            ),
            ["privacy"] = (
                "Quyền riêng tư",
                (
                    "Dữ liệu được lưu trong SQL Server; mật khẩu được băm "
                    + "bằng ASP.NET Core Identity. Không nhập thông tin cá "
                    + "nhân nhạy cảm trong môi trường thử nghiệm."
                )
            ),
        };
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        // BIẾN OUT info ((string Title, string Text)): cặp Title/Text của trang thông tin tìm được
        // trong pages.
        if (!pages.TryGetValue(id, out var info))
            return NotFound();
        ViewData["Title"] = info.Title;
        ViewData["Text"] = info.Text;
        return View(store.Read(d => d.Settings));
    }

    // HÀM Privacy: Chuyển URL tương thích sang Info với id privacy.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Home/Privacy. Tham số hàm: ().
    public IActionResult Privacy() => RedirectToAction(nameof(Info), new { id = "privacy" });

    // HÀM Error: Dựng trang lỗi với mã request để hỗ trợ tra log; không cache nội dung lỗi.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Home/Error. Tham số hàm: ().
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
        );
}
