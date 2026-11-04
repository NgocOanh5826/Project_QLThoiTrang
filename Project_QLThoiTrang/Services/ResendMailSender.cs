using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed class ResendMailSender(HttpClient http, IConfiguration config) : IMailSender
{
    // SendCode: Gửi OTP qua Resend API và kiểm tra mã thư trả về.
    public async Task SendCode(string email, string code, CancellationToken cancellationToken)
    {
        var options = config.GetSection("EmailApi").Get<EmailApiOptions>() ?? new();
        if (!options.Ready || !RecoveryContact.TryNormalize(email, "email", out var destination))
            throw new InvalidOperationException("Dịch vụ email chưa được cấu hình hợp lệ.");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.resend.com/emails"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(
            new
            {
                from = options.From,
                to = new[] { destination },
                reply_to = string.IsNullOrWhiteSpace(options.ReplyTo) ? null : options.ReplyTo,
                subject = "Mã đặt lại mật khẩu QOANH",
                text = $"Mã OTP của bạn: {code}\nMã có hiệu lực 10 phút, tối đa 5 lần thử. "
                    + "Không chia sẻ mã cho người khác. Nếu bạn không yêu cầu, hãy bỏ qua thư này.",
            }
        );
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Dịch vụ email chưa chấp nhận yêu cầu.");
        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );
        if (
            json.RootElement.ValueKind != JsonValueKind.Object
            || !json.RootElement.TryGetProperty("id", out var id)
            || id.ValueKind != JsonValueKind.String
            || !Guid.TryParse(id.GetString(), out _)
        )
            throw new InvalidOperationException("Dịch vụ email trả kết quả không hợp lệ.");
    }
}
