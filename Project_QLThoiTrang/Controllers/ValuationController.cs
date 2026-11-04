using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
[Authorize(Roles = "Admin,Inventory")]
public class ValuationController(StoreService store) : Controller
{
    // HÀM Index: Inventory/Admin xem tồn và giá vốn hiện có theo biến thể; dữ liệu đọc từ
    // InventoryValues và StoreData.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Valuation/Index. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Valuation/Index.cshtml.
    public IActionResult Index()
    {
        ViewBag.Values = store.NativeRead(db => db.InventoryValues.ToList());
        return View(store.Read(d => d));
    }

    // HÀM Edit: Mở form khai báo giá vốn cho biến thể tồn tại; đưa id vào ViewBag để xác định dòng
    // cần cập nhật.
    // THAM SỐ id (int): mã biến thể cần xem/sửa; form Variant dùng 0 cho thêm mới.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Valuation/Edit. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Valuation/Edit.cshtml.
    public IActionResult Edit(int id)
    {
        // BIẾN d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng.
        var d = store.Read(d => d);
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (!d.Variants.Any(v => v.Id == id))
            return NotFound();
        ViewBag.VariantId = id;
        return View(d);
    }

    // HÀM Edit: Nhận giá vốn, tồn expected và lý do; SetInventoryValue kiểm tra chống ghi đè, lưu
    // giá vốn cho giao dịch tiếp theo, lỗi quay lại form cùng biến thể.
    // THAM SỐ variantId (int): mã biến thể sản phẩm, dùng phân biệt đúng tổ hợp size/màu thay vì
    // chỉ mã sản phẩm.
    // THAM SỐ expected (int): số tồn người dùng đã thấy lúc mở form; so với tồn hiện tại để phát
    // hiện thay đổi và tránh ghi đè.
    // THAM SỐ cost (decimal): giá vốn VND cho một đơn vị hàng; phải không âm và nằm trong giới hạn
    // của hàm.
    // THAM SỐ note (string): ghi chú hoặc căn cứ điều chỉnh; được kiểm tra độ dài trước khi ghi
    // nhận người/thời điểm cập nhật.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Valuation/Edit. Tham số hàm: (int variantId, int expected, decimal cost,
    // string note).
    [HttpPost]
    public IActionResult Edit(int variantId, int expected, decimal cost, string note)
    {
        try
        {
            // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để
            // không lưu dữ liệu
            // sai.
            if (!ModelState.IsValid)
                throw new InvalidOperationException("Giá vốn không hợp lệ.");
            store.SetInventoryValue(
                variantId,
                expected,
                cost,
                note,
                int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
            );
            TempData["Success"] = "Đã ghi giá vốn cho các giao dịch phát sinh tiếp theo.";
            return RedirectToAction(nameof(Index));
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id = variantId });
        }
    }
}
