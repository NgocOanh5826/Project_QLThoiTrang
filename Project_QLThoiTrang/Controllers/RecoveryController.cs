using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RecoveryController(PasswordRecovery recovery) : Controller
{
    // Forgot: Hiển thị form gửi OTP qua email và đặt lại mật khẩu.
    public IActionResult Forgot()
    {
        SetChannelAvailability();
        return View(
            new ResetInput
            {
                Contact = TempData.Peek("RecoveryContact") as string ?? "",
                Channel = "email",
            }
        );
    }

    // Forgot: Yêu cầu gửi OTP tới email đã đăng ký.
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Forgot(ForgotInput input, CancellationToken cancellationToken)
    {
        SetChannelAvailability();
        if (!ModelState.IsValid)
        {
            if (WantsJson())
                return BadRequest(new { message = "Kiểm tra lại email đã đăng ký." });
            return View(new ResetInput { Contact = input.Contact, Channel = input.Channel });
        }
        if (!await recovery.Request(input, cancellationToken))
        {
            ModelState.AddModelError(
                "",
                "Chức năng gửi email chưa sẵn sàng. Vui lòng liên hệ hỗ trợ."
            );
            if (WantsJson())
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = "Chức năng gửi email chưa sẵn sàng. Vui lòng liên hệ hỗ trợ." }
                );
            return View(new ResetInput { Contact = input.Contact, Channel = input.Channel });
        }
        TempData["RecoveryContact"] = input.Contact.Trim();
        TempData["RecoveryChannel"] = input.Channel;
        TempData["RecoveryRequestedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string message =
            "Nếu thông tin khớp tài khoản khách hàng đang hoạt động, mã OTP sẽ được gửi. "
            + "Nhập mã nhận được bên dưới. Mã có hiệu lực 10 phút.";
        if (WantsJson())
            return Json(new { message, resendAfter = 60 });
        TempData["Success"] = message;
        return RedirectToAction(nameof(Forgot));
    }

    // Reset: Hiển thị form xác minh OTP và mật khẩu mới.
    public IActionResult Reset() =>
        View(
            new ResetInput
            {
                Contact = TempData["RecoveryContact"] as string ?? "",
                Channel = "email",
            }
        );

    // Reset: Xác minh OTP, đặt mật khẩu mới và đăng xuất.
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Reset(ResetInput input, bool inline = false)
    {
        if (!ModelState.IsValid)
            return ResetForm(input, inline);
        if (!recovery.Complete(input))
        {
            ModelState.AddModelError(
                "",
                "Mã OTP không hợp lệ, hết hạn hoặc đã hết số lần thử. Kiểm tra email, "
                    + "hoặc yêu cầu mã mới."
            );
            return ResetForm(input, inline);
        }
        HttpContext.Session.Clear();
        if (User.Identity?.IsAuthenticated == true)
            await HttpContext.SignOutAsync();
        TempData.Remove("RecoveryContact");
        TempData.Remove("RecoveryChannel");
        TempData.Remove("RecoveryRequestedAt");
        TempData["Success"] = "Đã đặt lại mật khẩu. Đăng nhập bằng mật khẩu mới.";
        return RedirectToAction("Login", "Account");
    }

    // SetChannelAvailability: Cập nhật trạng thái gửi email và thời gian chờ gửi lại.
    void SetChannelAvailability()
    {
        ViewBag.EmailReady = recovery.EmailReady;
        ViewBag.ResendSeconds = long.TryParse(
            TempData.Peek("RecoveryRequestedAt") as string,
            out var requestedAt
        )
            ? (int)Math.Clamp(60 - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - requestedAt), 0, 60)
            : 0;
    }

    // ResetForm: Hiển thị lại form tương ứng khi xác minh thất bại.
    IActionResult ResetForm(ResetInput input, bool inline)
    {
        if (!inline)
            return View("Reset", input);
        SetChannelAvailability();
        return View("Forgot", input);
    }

    // WantsJson: Kiểm tra yêu cầu có cần phản hồi JSON.
    bool WantsJson() =>
        Request.Headers.Accept.Any(value =>
            value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true
        );
}
