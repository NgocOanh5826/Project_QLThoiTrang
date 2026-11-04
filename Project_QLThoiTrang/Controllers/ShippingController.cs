using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
// THAM SỐ KHỞI TẠO shipping (ShippingService): ShippingService gọi GHN và lưu/đồng bộ vận đơn; do
// DI hoặc nơi gọi constructor truyền vào.
[Authorize(Roles = "Admin,Sales")]
public class ShippingController(StoreService store, ShippingService shipping) : Controller
{
    // THUỘC TÍNH ShippingController.Actor (int): id lấy từ claim của người đang đăng nhập; có thể
    // âm với nhân viên, dương với khách; không lấy từ ô input do người dùng tự chọn.
    int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // HÀM Index: Admin/Sales mở thông tin gửi hàng của đơn, trạng thái cấu hình GHN và vận đơn đã
    // lưu nếu có; id không tồn tại trả 404.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
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
    // ĐIỂM VÀO: GET /Shipping/Index. Tham số hàm: (string id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shipping/Index.cshtml.
    public IActionResult Index(string id)
    {
        // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var o = store.Read(d => d.Orders.FirstOrDefault(o => o.Id == id));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (o == null)
            return NotFound();
        ViewBag.Order = o;
        ViewBag.Ready = shipping.Ready;
        ViewBag.Shipment = store.NativeRead(db =>
            db.Shipments.SingleOrDefault(s => s.OrderId == o.DatabaseId)
        );
        return View(new ShipmentInput { Id = id });
    }

    // HÀM Create: Nhận tỉnh/phường, khối lượng và kích thước; ShippingService.Create kiểm tra điều
    // kiện rồi gửi GHN. Lỗi mạng/kết quả chưa rõ yêu cầu đối soát trước khi thử lại.
    // THAM SỐ input (ShipmentInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ token (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền xuống I/O để
    // dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // ĐIỂM VÀO: POST /Shipping/Create. Tham số hàm: (ShipmentInput input, CancellationToken token).
    [HttpPost]
    public async Task<IActionResult> Create(ShipmentInput input, CancellationToken token)
    {
        try
        {
            // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để
            // không lưu dữ liệu
            // sai.
            if (!ModelState.IsValid)
                throw new InvalidOperationException(
                    "Kiểm tra tỉnh/phường, trọng lượng và kích thước."
                );
            await shipping.Create(input, Actor, token);
            TempData["Success"] = "Đã gửi yêu cầu vận đơn GHN.";
        }
        // BIẾN LỖI ex (Exception): ngoại lệ bắt từ khối try; Message chứa thông báo, còn loại lỗi
        // dùng quyết định cách báo/ghi log ở catch.
        catch (Exception ex)
            when (ex
                    is InvalidOperationException
                        or HttpRequestException
                        or OperationCanceledException
            )
        {
            TempData["Error"] =
                ex is InvalidOperationException
                    ? ex.Message
                    : "Không kết nối được GHN; cần đối soát trước khi thử lại.";
        }

        return RedirectToAction(nameof(Index), new { id = input.Id });
    }

    // HÀM Sync: Lấy lại trạng thái vận đơn bằng mã đơn đã gửi qua ShippingService.Sync; cập nhật
    // kết quả trong SQL rồi quay lại trang vận chuyển.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ token (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền xuống I/O để
    // dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // ĐIỂM VÀO: POST /Shipping/Sync. Tham số hàm: (string id, CancellationToken token).
    [HttpPost]
    public async Task<IActionResult> Sync(string id, CancellationToken token)
    {
        try
        {
            await shipping.Sync(id, Actor, token);
            TempData["Success"] = "Đã đồng bộ trạng thái vận đơn.";
        }
        // BIẾN LỖI ex (Exception): ngoại lệ bắt từ khối try; Message chứa thông báo, còn loại lỗi
        // dùng quyết định cách báo/ghi log ở catch.
        catch (Exception ex)
            when (ex
                    is InvalidOperationException
                        or HttpRequestException
                        or OperationCanceledException
            )
        {
            TempData["Error"] = "Chưa đồng bộ được. Kiểm tra cấu hình và đối soát GHN.";
        }

        return RedirectToAction(nameof(Index), new { id });
    }
}
