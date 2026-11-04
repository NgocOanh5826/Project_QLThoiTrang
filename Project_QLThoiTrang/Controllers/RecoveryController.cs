using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO recovery (PasswordRecovery): PasswordRecovery phối hợp phát OTP, gửi mail và đổi
// mật khẩu; do DI hoặc nơi gọi constructor truyền vào.
public class RecoveryController(PasswordRecovery recovery) : Controller
{
    // HÀM Forgot: Mở form quên mật khẩu, ViewBag.Ready cho biết SMTP/khóa OTP đã cấu hình hay chưa.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Recovery/Forgot. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Recovery/Forgot.cshtml.
    public IActionResult Forgot()
    {
        ViewBag.Ready = recovery.Ready;
        return View(new ForgotInput());
    }

    // HÀM Forgot: Kiểm tra email rồi yêu cầu gửi OTP qua PasswordRecovery; thông báo chung không
    // xác nhận email có trong hệ thống. Áp dụng policy login để giới hạn yêu cầu.
    // THAM SỐ input (ForgotInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ cancellationToken (CancellationToken): tín hiệu hủy tác vụ từ request/caller; truyền
    // xuống I/O để dừng khi yêu cầu bị hủy.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // ĐIỂM VÀO: POST /Recovery/Forgot. Tham số hàm: (ForgotInput input, CancellationToken
    // cancellationToken).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Recovery/Forgot.cshtml.
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Forgot(ForgotInput input, CancellationToken cancellationToken)
    {
        ViewBag.Ready = recovery.Ready;
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        await recovery.Request(input.Email, cancellationToken);
        TempData["Success"] = (
            "Nếu email thuộc tài khoản khách hàng đang hoạt động, mã "
            + "OTP sẽ được gửi khi dịch vụ email đã cấu hình."
        );
        return RedirectToAction(nameof(Reset));
    }

    // HÀM Reset: Mở form nhập email, OTP và mật khẩu mới.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Recovery/Reset. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Recovery/Reset.cshtml.
    public IActionResult Reset() => View(new ResetInput());

    // HÀM Reset: Kiểm tra mẫu và hoàn tất khôi phục; OTP không hợp lệ/hết hạn/hết số lần thử thì
    // trả lại form, thành công chuyển sang đăng nhập.
    // THAM SỐ input (ResetInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Recovery/Reset. Tham số hàm: (ResetInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Recovery/Reset.cshtml.
    [HttpPost, EnableRateLimiting("login")]
    public IActionResult Reset(ResetInput input)
    {
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        // KIỂM TRA FORM: nhánh này thêm lỗi ", "; ModelState sẽ ngăn bước lưu bên dưới.
        if (!recovery.Complete(input))
        {
            ModelState.AddModelError(
                "",
                "Mã không hợp lệ, hết hạn hoặc chức năng email chưa sẵn sàng."
            );
            return View(input);
        }

        TempData["Success"] = "Đã đặt lại mật khẩu. Vui lòng đăng nhập.";
        return RedirectToAction("Login", "Account");
    }
}
