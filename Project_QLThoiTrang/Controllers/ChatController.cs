using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
[
    Authorize(Roles = "Customer,Admin,Support"),
    ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)
]
public class ChatController(StoreService store) : Controller
{
    // THUỘC TÍNH ChatController.Actor (int): id lấy từ claim của người đang đăng nhập; có thể âm
    // với nhân viên, dương với khách; không lấy từ ô input do người dùng tự chọn.
    int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // HÀM Conversation: Khách luôn dùng id của chính mình; Admin/Support mới dùng customerId được
    // chọn để mở hội thoại khách cần hỗ trợ.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // TRẢ VỀ (int): số nguyên 32 bit; ý nghĩa được xác định bởi biểu thức return bên dưới.
    int Conversation(int customerId) => User.IsInRole("Customer") ? Actor : customerId;

    // HÀM Index: Nhân viên chưa chọn khách thì mở Inbox; đã chọn khách hoặc tài khoản Customer thì
    // kiểm tra khách còn hoạt động rồi mở trang chat.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Chat/Index. Tham số hàm: (int customerId = 0).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Chat/Inbox.cshtml;
    // Project_QLThoiTrang/Views/Chat/Index.cshtml.
    public IActionResult Index(int customerId = 0)
    {
        if (!User.IsInRole("Customer") && customerId == 0)
            return View("Inbox", store.ChatInbox(Actor));
        // BIẾN target (int): mã khách của hội thoại được phép mở; khách luôn là chính mình, nhân
        // viên có thể chọn khách.
        int target = Conversation(customerId);
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (store.NativeRead(db => db.KhachHang.Any(c => c.MaKH == target && c.TrangThai)) == false)
            return NotFound();
        ViewBag.CustomerId = target;
        return View();
    }

    // HÀM Messages: API JSON lấy tin cũ/tin mới theo before/after sau kiểm tra quyền; trả id, nội
    // dung, cờ người gửi và thời gian UTC cho chat.js.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ before (long): mã tin đầu của trang hiện có; lấy tin Id nhỏ hơn để tải lịch sử, 0 bỏ
    // mốc này. Mặc định khi không truyền: 0.
    // THAM SỐ after (long): mốc mã tin đã tải; chỉ lấy tin có Id lớn hơn mốc này, 0 là chưa có mốc.
    // Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA m (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token chống gửi
    // trùng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Chat/Messages. Tham số hàm: (int customerId, long before = 0, long after = 0).
    public IActionResult Messages(int customerId, long before = 0, long after = 0)
    {
        try
        {
            return Json(
                store
                    .Messages(Conversation(customerId), Actor, before, after)
                    .Select(m => new
                    {
                        id = m.Id,
                        text = m.Text,
                        mine = m.SenderId == Actor,
                        staff = m.SenderId < 0,
                        created = DateTime.SpecifyKind(m.CreatedUtc, DateTimeKind.Utc),
                    })
            );
        }
        catch (InvalidOperationException)
        {
            return Forbid();
        }
    }

    // HÀM Send: API POST gửi tin với clientToken chống gửi trùng; service kiểm tra nội
    // dung/quyền/tốc độ, lỗi nghiệp vụ trả HTTP 400 kèm thông báo.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ text (string): nội dung tin nhắn sau Trim, cần 1–2.000 ký tự.
    // THAM SỐ clientToken (Guid): Guid cho một lần gửi tin; thử lại cùng tin phải giữ token để
    // server không thêm bản sao.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Chat/Send. Tham số hàm: (int customerId, string text, Guid clientToken).
    [HttpPost]
    public IActionResult Send(int customerId, string text, Guid clientToken)
    {
        try
        {
            // BIẾN m (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token chống gửi
            // trùng.
            var m = store.SendMessage(Conversation(customerId), Actor, text, clientToken);
            return Json(new { id = m.Id });
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // HÀM Read: API POST đánh dấu đã đọc tới mã last trong đúng hội thoại; không đủ quyền trả
    // Forbid.
    // KIỂU T: kiểu kết quả do callback trả, được suy ra từ chỗ gọi; một hàm đọc/ghi có thể trả
    // entity, danh sách, bool hoặc số.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ last (long): mã tin cuối cùng đã đọc; service chặn trong khoảng mã tin thật và chỉ
    // tăng mốc đã đọc.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Chat/Read. Tham số hàm: (int customerId, long last).
    [HttpPost]
    public IActionResult Read(int customerId, long last)
    {
        try
        {
            store.MarkChatRead(Conversation(customerId), Actor, last);
            return Ok();
        }
        catch (InvalidOperationException)
        {
            return Forbid();
        }
    }
}
