using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // FindRecoveryCustomer: Tìm khách đang hoạt động theo email.
    static KhachHangRow? FindRecoveryCustomer(StoreDbContext db, string email) =>
        db.KhachHang.SingleOrDefault(c => c.Email == email && c.TrangThai);

    // IssueReset: Tạo OTP có thời hạn và vô hiệu mã cũ.
    internal (Guid Id, string Destination, string Code)? IssueReset(
        string contact,
        string secret,
        string channel = "email"
    ) =>
        NativeWrite(db =>
        {
            Require(secret.Length >= 32, "Chức năng khôi phục chưa được cấu hình.");
            if (!RecoveryContact.TryNormalize(contact, channel, out var destination))
                return null;
            var customer = FindRecoveryCustomer(db, destination);
            if (customer == null)
                return null;
            var now = DateTime.UtcNow;
            if (
                db.ResetCodes.Any(r =>
                    r.CustomerId == customer.MaKH && r.CreatedUtc > now.AddMinutes(-1)
                )
                || db.ResetCodes.Count(r =>
                    r.CustomerId == customer.MaKH && r.CreatedUtc > now.AddHours(-1)
                ) >= 5
            )
                return null;
            foreach (
                var old in db.ResetCodes.Where(r =>
                    r.CustomerId == customer.MaKH && r.UsedUtc == null
                )
            )
                old.UsedUtc = now;
            string code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6");
            var challenge = new ResetCode
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.MaKH,
                Stamp = PasswordStamp(customer.MatKhau),
                CreatedUtc = now,
                ExpiresUtc = now.AddMinutes(10),
            };
            challenge.Hash = OtpHash(challenge.Id, code, secret, channel, destination);
            db.ResetCodes.Add(challenge);
            return ((Guid, string, string)?)(challenge.Id, destination, code);
        });

    // ResetDelivered: Đánh dấu OTP đã được dịch vụ gửi chấp nhận.
    internal void ResetDelivered(Guid id) =>
        NativeWrite(db =>
        {
            db.ResetCodes.Single(r => r.Id == id).Sent = true;
            return 0;
        });

    // ResetPassword: Kiểm tra OTP và lưu mật khẩu mới.
    public bool ResetPassword(ResetInput input, string secret)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        Require(secret.Length >= 32, "Chức năng khôi phục chưa được cấu hình.");
        if (!RecoveryContact.TryNormalize(input.Contact, input.Channel, out var destination))
            return false;
        return NativeWrite(db =>
        {
            var customer = FindRecoveryCustomer(db, destination);
            if (customer == null)
                return false;
            var challenge = db
                .ResetCodes.Where(r => r.CustomerId == customer.MaKH && r.Sent && r.UsedUtc == null)
                .OrderByDescending(r => r.CreatedUtc)
                .FirstOrDefault();
            var now = DateTime.UtcNow;
            if (
                challenge == null
                || challenge.ExpiresUtc <= now
                || challenge.Attempts >= 5
                || challenge.Stamp != PasswordStamp(customer.MatKhau)
            )
                return false;
            challenge.Attempts++;
            if (
                !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(challenge.Hash),
                    Convert.FromHexString(
                        OtpHash(challenge.Id, input.Code, secret, input.Channel, destination)
                    )
                )
            )
                return false;
            customer.MatKhau = hasher.HashPassword(new Customer(), input.Password);
            foreach (
                var old in db.ResetCodes.Where(r =>
                    r.CustomerId == customer.MaKH && r.UsedUtc == null
                )
            )
                old.UsedUtc = now;
            return true;
        });
    }

    // PasswordStamp: Tạo dấu xác minh từ hash mật khẩu hiện tại.
    static string PasswordStamp(string hash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

    // OtpHash: Ký HMAC gắn OTP với yêu cầu và email nhận.
    static string OtpHash(
        Guid id,
        string code,
        string secret,
        string channel,
        string destination
    ) =>
        Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes($"{id:N}:{channel}:{destination}:{code}")
            )
        );
}

public interface IMailSender
{
    // SendCode: Gửi mã OTP qua email.
    Task SendCode(string email, string code, CancellationToken cancellationToken);
}

public sealed class PasswordRecovery(
    StoreService store,
    IMailSender mail,
    IConfiguration config,
    ILogger<PasswordRecovery> logger
)
{
    public bool KeyReady => (config["PasswordReset:HashKey"]?.Length ?? 0) >= 32;
    public bool EmailReady =>
        KeyReady && (config.GetSection("EmailApi").Get<EmailApiOptions>() ?? new()).Ready;
    public bool Ready => EmailReady;

    // Request: Cấp OTP và yêu cầu gửi qua email API.
    public async Task Request(string email, CancellationToken cancellationToken) =>
        await Request(new ForgotInput { Contact = email }, cancellationToken);

    // Request: Cấp OTP và yêu cầu gửi qua email API.
    public async Task<bool> Request(ForgotInput input, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        if (!EmailReady)
            return false;
        var challenge = store.IssueReset(
            input.Contact,
            config["PasswordReset:HashKey"]!,
            input.Channel
        );
        if (challenge == null)
            return true;
        try
        {
            await mail.SendCode(
                challenge.Value.Destination,
                challenge.Value.Code,
                cancellationToken
            );
            store.ResetDelivered(challenge.Value.Id);
        }
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or InvalidOperationException
                        or OperationCanceledException
                        or FormatException
                        or ArgumentException
                        or System.Text.Json.JsonException
            )
        {
            logger.LogWarning(
                "OTP delivery failed ({Channel}, {ErrorType}).",
                input.Channel,
                ex.GetType().Name
            );
        }
        return true;
    }

    // Complete: Xác minh OTP và hoàn tất đặt lại mật khẩu.
    public bool Complete(ResetInput input) =>
        KeyReady && store.ResetPassword(input, config["PasswordReset:HashKey"]!);
}
