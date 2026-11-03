using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
public class RecoveryController(PasswordRecovery recovery):Controller
{
 public IActionResult Forgot(){ViewBag.Ready=recovery.Ready;return View(new ForgotInput());}
 [HttpPost,EnableRateLimiting("login")] public async Task<IActionResult> Forgot(ForgotInput input,CancellationToken cancellationToken)
 {ViewBag.Ready=recovery.Ready;if(!ModelState.IsValid)return View(input);await recovery.Request(input.Email,cancellationToken);TempData["Success"]="Nếu email thuộc tài khoản khách hàng đang hoạt động, mã OTP sẽ được gửi khi dịch vụ email đã cấu hình.";return RedirectToAction(nameof(Reset));}
 public IActionResult Reset()=>View(new ResetInput());
 [HttpPost,EnableRateLimiting("login")] public IActionResult Reset(ResetInput input)
 {if(!ModelState.IsValid)return View(input);if(!recovery.Complete(input)){ModelState.AddModelError("","Mã không hợp lệ, hết hạn hoặc chức năng email chưa sẵn sàng.");return View(input);}TempData["Success"]="Đã đặt lại mật khẩu. Vui lòng đăng nhập.";return RedirectToAction("Login","Account");}
}
