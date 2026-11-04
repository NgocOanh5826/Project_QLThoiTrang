using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Project_QLThoiTrang.Models;

public class ForgotInput : IValidatableObject
{
    [Required, RegularExpression("^email$", ErrorMessage = "Chỉ hỗ trợ nhận mã qua email.")]
    public string Channel { get; set; } = "email";

    [
        Required(ErrorMessage = "Nhập email đã đăng ký."),
        StringLength(150, ErrorMessage = "Thông tin liên hệ không quá 150 ký tự.")
    ]
    public string Contact { get; set; } = "";

    [ValidateNever]
    public string Email
    {
        get => Contact;
        set => Contact = value;
    }

    // Validate: Kiểm tra email và từ chối kênh nhận mã không hỗ trợ.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Channel != "email" || string.IsNullOrWhiteSpace(Contact))
            yield break;
        if (!RecoveryContact.TryNormalize(Contact, Channel, out _))
            yield return new ValidationResult("Email chưa đúng định dạng.", [nameof(Contact)]);
    }
}

public class ResetInput : ForgotInput
{
    [
        Required(ErrorMessage = "Nhập mã OTP."),
        RegularExpression("^[0-9]{6}$", ErrorMessage = "Mã OTP gồm đúng 6 chữ số.")
    ]
    public string Code { get; set; } = "";

    [
        Required(ErrorMessage = "Nhập mật khẩu mới."),
        MinLength(8, ErrorMessage = "Mật khẩu cần ít nhất 8 ký tự."),
        StringLength(100, ErrorMessage = "Mật khẩu không quá 100 ký tự."),
        RegularExpression(
            @"^(?=.*\p{L})(?=.*\d).+$",
            ErrorMessage = "Mật khẩu cần có ít nhất một chữ cái và một chữ số."
        )
    ]
    public string Password { get; set; } = "";

    [
        Required(ErrorMessage = "Nhập lại mật khẩu mới."),
        Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")
    ]
    public string ConfirmPassword { get; set; } = "";
}

public class EmailApiOptions
{
    public string ApiKey { get; set; } = "";
    public string From { get; set; } = "";
    public string ReplyTo { get; set; } = "";
    public bool Ready =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && ApiKey.StartsWith("re_", StringComparison.Ordinal)
        && !ApiKey.Any(char.IsWhiteSpace)
        && new EmailAddressAttribute().IsValid(From)
        && (string.IsNullOrWhiteSpace(ReplyTo) || new EmailAddressAttribute().IsValid(ReplyTo));
}

public static class RecoveryContact
{
    // TryNormalize: Kiểm tra và chuẩn hóa email nhận OTP.
    public static bool TryNormalize(string? contact, string channel, out string normalized)
    {
        normalized = "";
        var value = contact?.Trim() ?? "";
        if (channel != "email" || value.Length > 150 || !new EmailAddressAttribute().IsValid(value))
            return false;
        normalized = value.ToLowerInvariant();
        return true;
    }
}
