using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Project_QLThoiTrang.Controllers;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

// Chỉ tạo và xóa database fixture do chính runner này cấp tên; không dùng database cửa hàng.
// Email và HTTP API đều dùng bộ gửi giả; không gửi thư hoặc phát sinh phí nhà cung cấp.
var database = "QOANH_Verification_Password_" + Guid.NewGuid().ToString("N")[..12];
var connection = new SqlConnectionStringBuilder
{
    DataSource = Environment.GetEnvironmentVariable("QOANH_TEST_SERVER") ?? @".\MSSQLSERVER01",
    InitialCatalog = database,
    IntegratedSecurity = true,
    TrustServerCertificate = true,
};
var factory = new Factory(connection.ConnectionString);
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            ["EmailApi:ApiKey"] = "re_fake_test_key",
            ["EmailApi:From"] = "no-reply@example.test",
            ["PasswordReset:HashKey"] = "test-only-key-with-more-than-32-characters",
        }
    )
    .Build();
var mail = new FakeMail();
var store = new StoreService(factory, config);
var recovery = new PasswordRecovery(store, mail, config, NullLogger<PasswordRecovery>.Instance);
var results = new List<object>();
int failed = 0;
void Assert(bool condition)
{
    if (!condition)
        throw new Exception("Assertion failed");
}
void Reject(Action action)
{
    try
    {
        action();
    }
    catch (Exception error) when (error is InvalidOperationException or ValidationException)
    {
        return;
    }
    throw new Exception("Expected rejection");
}
void Check(string name, Action action)
{
    try
    {
        action();
        results.Add(new { name, result = "PASS" });
        Console.WriteLine("PASS " + name);
    }
    catch (Exception error)
    {
        failed++;
        results.Add(
            new
            {
                name,
                result = "FAIL",
                error = error.ToString(),
            }
        );
        Console.WriteLine("FAIL " + name + ": " + error.Message);
    }
}
Customer Customer(string phone = "")
{
    var customer = store.Register(
        new RegisterInput
        {
            Name = "Recovery fixture",
            Email = Guid.NewGuid().ToString("N") + "@example.test",
            Password = "Original@123",
            ConfirmPassword = "Original@123",
        }
    );
    if (phone != "")
        store.Write(d =>
        {
            d.Customers.Single(c => c.Id == customer.Id).Phone = phone;
            return 0;
        });
    return customer;
}
ResetInput Reset(string contact, string code, string channel = "email") =>
    new()
    {
        Contact = contact,
        Channel = channel,
        Code = code,
        Password = "Changed@12345",
        ConfirmPassword = "Changed@12345",
    };
void AgeChallenges(int customer)
{
    using var db = factory.CreateDbContext();
    foreach (var row in db.ResetCodes.Where(r => r.CustomerId == customer))
        row.CreatedUtc = DateTime.UtcNow.AddMinutes(-2);
    db.SaveChanges();
}

try
{
    using (var db = factory.CreateDbContext())
        db.Database.EnsureCreated();
    Check(
        "contact-normalization-and-channel-validation",
        () =>
        {
            Assert(
                RecoveryContact.TryNormalize("  USER@example.test ", "email", out var email)
                    && email == "user@example.test"
            );
            Assert(!RecoveryContact.TryNormalize("090 123 4567", "sms", out _));
            Assert(!RecoveryContact.TryNormalize("+84abc901234567", "sms", out _));
            Assert(!RecoveryContact.TryNormalize("user@example.test", "sms", out _));
            var badChannel = new ForgotInput { Channel = "other", Contact = "test" };
            Reject(() =>
                Validator.ValidateObject(badChannel, new ValidationContext(badChannel), true)
            );
            var bad = new ForgotInput { Channel = "email", Contact = "0901234567" };
            Assert(!Validator.TryValidateObject(bad, new ValidationContext(bad), [], true));
        }
    );
    var emailCustomer = Customer();
    recovery.Request(emailCustomer.Email, default).GetAwaiter().GetResult();
    var emailCode = mail.Code;
    Check(
        "email-otp-stored-as-hash-and-marked-sent",
        () =>
        {
            using var db = factory.CreateDbContext();
            var code = db.ResetCodes.Single(r => r.CustomerId == emailCustomer.Id);
            Assert(
                emailCode.Length == 6
                    && code.Hash.Length == 64
                    && code.Hash != emailCode
                    && code.Sent
            );
        }
    );
    Check(
        "resend-throttled-without-second-message",
        () =>
        {
            int count = mail.Calls;
            recovery.Request(emailCustomer.Email, default).GetAwaiter().GetResult();
            Assert(mail.Calls == count);
        }
    );
    Check(
        "wrong-otp-attempt-persists",
        () =>
        {
            Assert(
                !recovery.Complete(
                    Reset(emailCustomer.Email, emailCode == "000000" ? "000001" : "000000")
                )
            );
            using var db = factory.CreateDbContext();
            Assert(db.ResetCodes.Single(r => r.CustomerId == emailCustomer.Id).Attempts == 1);
        }
    );
    Check(
        "email-reset-invalidates-old-password-and-replay",
        () =>
        {
            Assert(recovery.Complete(Reset(emailCustomer.Email, emailCode)));
            Assert(!recovery.Complete(Reset(emailCustomer.Email, emailCode)));
            Assert(store.Authenticate(emailCustomer.Email, "Original@123") == null);
            Assert(store.Authenticate(emailCustomer.Email, "Changed@12345") != null);
        }
    );
    Check(
        "legacy-sms-request-rejected-without-delivery",
        () =>
        {
            int calls = mail.Calls;
            Reject(() =>
                recovery
                    .Request(new ForgotInput { Channel = "sms", Contact = "0902223333" }, default)
                    .GetAwaiter()
                    .GetResult()
            );
            Reject(() => recovery.Complete(Reset(emailCustomer.Email, emailCode, "sms")));
            Assert(mail.Calls == calls);
        }
    );
    var cross = Customer();
    recovery.Request(cross.Email, default).GetAwaiter().GetResult();
    var oldCode = mail.Code;
    AgeChallenges(cross.Id);
    recovery.Request(cross.Email, default).GetAwaiter().GetResult();
    Check(
        "new-email-code-invalidates-previous-code",
        () =>
        {
            using var db = factory.CreateDbContext();
            Assert(db.ResetCodes.Count(r => r.CustomerId == cross.Id && r.UsedUtc == null) == 1);
            Assert(recovery.Complete(Reset(cross.Email, mail.Code)));
            Assert(!recovery.Complete(Reset(cross.Email, oldCode)));
        }
    );
    var locked = Customer();
    recovery.Request(locked.Email, default).GetAwaiter().GetResult();
    var lockedCode = mail.Code;
    Check(
        "five-failures-lock-challenge",
        () =>
        {
            var wrong = lockedCode == "000000" ? "000001" : "000000";
            for (int i = 0; i < 6; i++)
                Assert(!recovery.Complete(Reset(locked.Email, wrong)));
            Assert(!recovery.Complete(Reset(locked.Email, lockedCode)));
            using var db = factory.CreateDbContext();
            Assert(db.ResetCodes.Single(r => r.CustomerId == locked.Id).Attempts == 5);
        }
    );
    var expired = Customer();
    recovery.Request(expired.Email, default).GetAwaiter().GetResult();
    var expiredCode = mail.Code;
    using (var db = factory.CreateDbContext())
    {
        db.ResetCodes.Single(r => r.CustomerId == expired.Id).ExpiresUtc =
            DateTime.UtcNow.AddSeconds(-1);
        db.SaveChanges();
    }
    Check(
        "expired-otp-denied",
        () => Assert(!recovery.Complete(Reset(expired.Email, expiredCode)))
    );
    var failedDelivery = Customer();
    mail.Fail = true;
    recovery.Request(failedDelivery.Email, default).GetAwaiter().GetResult();
    mail.Fail = false;
    Check(
        "failed-delivery-cannot-authorize-reset",
        () =>
        {
            using var db = factory.CreateDbContext();
            Assert(!db.ResetCodes.Single(r => r.CustomerId == failedDelivery.Id).Sent);
            Assert(!recovery.Complete(Reset(failedDelivery.Email, mail.Code)));
        }
    );
    Check(
        "unknown-account-has-generic-result-without-send",
        () =>
        {
            int count = mail.Calls;
            Assert(
                recovery
                    .Request(new ForgotInput { Contact = "missing@example.test" }, default)
                    .GetAwaiter()
                    .GetResult()
            );
            Assert(mail.Calls == count);
        }
    );
    var account = Customer();
    Check(
        "change-password-rejects-wrong-current-and-reuse",
        () =>
        {
            Reject(() =>
                store.ChangePassword(
                    account.Id,
                    new PasswordInput
                    {
                        CurrentPassword = "Wrong@123",
                        NewPassword = "Changed@12345",
                        ConfirmPassword = "Changed@12345",
                    }
                )
            );
            Reject(() =>
                store.ChangePassword(
                    account.Id,
                    new PasswordInput
                    {
                        CurrentPassword = "Original@123",
                        NewPassword = "Original@123",
                        ConfirmPassword = "Original@123",
                    }
                )
            );
            Assert(store.Authenticate(account.Email, "Original@123") != null);
        }
    );
    Check(
        "change-controller-signs-out-and-clears-session",
        () =>
        {
            var auth = new FakeAuth();
            var session = new FakeSession();
            var http = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection()
                    .AddSingleton<IAuthenticationService>(auth)
                    .BuildServiceProvider(),
                Session = session,
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                            new Claim(ClaimTypes.Role, "Customer"),
                        ],
                        "fixture"
                    )
                ),
            };
            var controller = new AccountController(store)
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new TempDataDictionary(http, new FakeTempData()),
            };
            controller.Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(
                new ActionContext(
                    http,
                    new Microsoft.AspNetCore.Routing.RouteData(),
                    new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
                )
            );
            var response = controller
                .Password(
                    new PasswordInput
                    {
                        CurrentPassword = "Original@123",
                        NewPassword = "Changed@12345",
                        ConfirmPassword = "Changed@12345",
                    }
                )
                .GetAwaiter()
                .GetResult();
            Assert(response is RedirectToActionResult { ActionName: "Login" });
            Assert(auth.SignedOut && session.Cleared);
            Assert(store.Authenticate(account.Email, "Original@123") == null);
            Assert(store.Authenticate(account.Email, "Changed@12345") != null);
        }
    );
    Check(
        "staff-account-can-change-password-with-signed-id",
        () =>
        {
            int staffId;
            string email = "staff-" + Guid.NewGuid().ToString("N") + "@example.test";
            using (var db = factory.CreateDbContext())
            {
                var staff = new Project_QLThoiTrang.Data.Native.NhanVienRow
                {
                    HoTen = "Password staff fixture",
                    Email = email,
                    ChucVu = "Support",
                    TrangThai = true,
                    NgayVaoLam = DateTime.Today,
                    MatKhau =
                        new Microsoft.AspNetCore.Identity.PasswordHasher<Customer>().HashPassword(
                            new Customer(),
                            "Original@123"
                        ),
                };
                db.NhanVien.Add(staff);
                db.SaveChanges();
                staffId = staff.MaNV;
            }
            store.ChangePassword(
                -staffId,
                new PasswordInput
                {
                    CurrentPassword = "Original@123",
                    NewPassword = "Changed@12345",
                    ConfirmPassword = "Changed@12345",
                }
            );
            Assert(store.Authenticate(email, "Original@123") == null);
            Assert(
                store.Authenticate(email, "Changed@12345") is { Role: "Support" } changed
                    && changed.Id == -staffId
            );
        }
    );
    Check(
        "missing-key-does-not-send-or-accept-reset",
        () =>
        {
            var unavailable = new PasswordRecovery(
                store,
                mail,
                new ConfigurationBuilder().Build(),
                NullLogger<PasswordRecovery>.Instance
            );
            int mailCalls = mail.Calls;
            Assert(!unavailable.Ready);
            Assert(
                !unavailable
                    .Request(new ForgotInput { Contact = emailCustomer.Email }, default)
                    .GetAwaiter()
                    .GetResult()
            );
            Assert(!unavailable.Complete(Reset(emailCustomer.Email, "123456")));
            Assert(mail.Calls == mailCalls);
        }
    );
    Check(
        "resend-api-authorization-json-and-error-handling",
        () =>
        {
            var handler = new EmailHandler();
            var sender = new ResendMailSender(new HttpClient(handler), config);
            sender.SendCode(" USER@example.test ", "001234", default).GetAwaiter().GetResult();
            Assert(handler.Uri?.AbsoluteUri == "https://api.resend.com/emails");
            Assert(handler.Method == HttpMethod.Post);
            Assert(handler.Authorization == "Bearer re_fake_test_key");
            using var body = JsonDocument.Parse(handler.Body);
            Assert(body.RootElement.GetProperty("from").GetString() == "no-reply@example.test");
            Assert(body.RootElement.GetProperty("to")[0].GetString() == "user@example.test");
            Assert(body.RootElement.GetProperty("text").GetString()!.Contains("001234"));
            Assert(!handler.Body.Contains("re_fake_test_key"));
            foreach (
                var status in new[]
                {
                    HttpStatusCode.Unauthorized,
                    HttpStatusCode.Forbidden,
                    HttpStatusCode.TooManyRequests,
                    HttpStatusCode.InternalServerError,
                }
            )
            {
                handler.Status = status;
                Reject(() =>
                    sender.SendCode("user@example.test", "123456", default).GetAwaiter().GetResult()
                );
            }
            handler.Status = HttpStatusCode.OK;
            foreach (
                var response in new[]
                {
                    "{}",
                    "{\"id\":null}",
                    "{\"id\":123}",
                    "[]",
                    "{\"id\":\"\"}",
                }
            )
            {
                handler.Response = response;
                Reject(() =>
                    sender.SendCode("user@example.test", "123456", default).GetAwaiter().GetResult()
                );
            }
        }
    );
    Check(
        "api-key-and-sender-required-before-send",
        () =>
        {
            foreach (
                var values in new[]
                {
                    new Dictionary<string, string?> { ["EmailApi:From"] = "no-reply@example.test" },
                    new Dictionary<string, string?> { ["EmailApi:ApiKey"] = "re_fake_key" },
                    new Dictionary<string, string?>
                    {
                        ["EmailApi:ApiKey"] = "re_bad key",
                        ["EmailApi:From"] = "no-reply@example.test",
                    },
                }
            )
            {
                var invalidConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(values)
                    .Build();
                var handler = new EmailHandler();
                var sender = new ResendMailSender(new HttpClient(handler), invalidConfig);
                Reject(() =>
                    sender.SendCode("user@example.test", "123456", default).GetAwaiter().GetResult()
                );
                Assert(handler.Uri == null);
            }
        }
    );
    Check(
        "inline-forgot-json-and-no-script-flow",
        () =>
        {
            var customer = Customer();
            var http = new DefaultHttpContext { Session = new FakeSession() };
            var controller = new RecoveryController(recovery)
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new TempDataDictionary(http, new FakeTempData()),
                Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(
                    new ActionContext(
                        http,
                        new Microsoft.AspNetCore.Routing.RouteData(),
                        new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
                    )
                ),
            };
            http.Request.Headers.Accept = "application/json";
            var result = controller
                .Forgot(new ForgotInput { Contact = customer.Email }, default)
                .GetAwaiter()
                .GetResult();
            Assert(result is JsonResult);
            var json = JsonSerializer.SerializeToElement(((JsonResult)result).Value);
            Assert(json.GetProperty("resendAfter").GetInt32() == 60);
            Assert(!json.TryGetProperty("code", out _));
            http.Request.Headers.Accept = "text/html";
            var fallback = controller
                .Forgot(new ForgotInput { Contact = customer.Email }, default)
                .GetAwaiter()
                .GetResult();
            Assert(fallback is RedirectToActionResult { ActionName: "Forgot" });
            Assert(controller.TempData["RecoveryContact"] as string == customer.Email);
        }
    );
    Check(
        "inline-form-errors-stay-on-forgot-page",
        () =>
        {
            var http = new DefaultHttpContext { Session = new FakeSession() };
            var controller = new RecoveryController(recovery)
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new TempDataDictionary(http, new FakeTempData()),
            };
            controller.ModelState.AddModelError("Code", "Invalid OTP fixture");
            var result = controller
                .Reset(Reset(emailCustomer.Email, "123456"), inline: true)
                .GetAwaiter()
                .GetResult();
            Assert(result is ViewResult { ViewName: "Forgot", Model: ResetInput });
            http.Request.Headers.Accept = "application/json";
            var rejected = controller
                .Forgot(new ForgotInput { Contact = "invalid" }, default)
                .GetAwaiter()
                .GetResult();
            Assert(rejected is BadRequestObjectResult);
        }
    );
}
catch (Exception error)
{
    failed++;
    results.Add(
        new
        {
            name = "fixture-or-unexpected-failure",
            result = "FAIL",
            error = error.ToString(),
        }
    );
    Console.WriteLine("FAIL fixture: " + error.Message);
}
finally
{
    Directory.CreateDirectory("BuildReview/password_recovery");
    File.WriteAllText(
        "BuildReview/password_recovery/test-results.json",
        JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })
    );
    if (
        !database.StartsWith("QOANH_Verification_Password_", StringComparison.Ordinal)
        || new SqlConnectionStringBuilder(factory.Connection).InitialCatalog != database
    )
        throw new Exception("Refusing cleanup outside the database created by this runner.");
    using var db = factory.CreateDbContext();
    db.Database.EnsureDeleted();
}
Console.WriteLine($"Password checks: {results.Count - failed}/{results.Count} passed.");
Environment.ExitCode = failed == 0 ? 0 : 1;

sealed class Factory(string connection) : IDbContextFactory<StoreDbContext>
{
    public string Connection => connection;

    public StoreDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
}

sealed class FakeMail : IMailSender
{
    public int Calls;
    public string Code = "";
    public bool Fail;

    public Task SendCode(string email, string code, CancellationToken cancellationToken)
    {
        Calls++;
        Code = code;
        if (Fail)
            throw new InvalidOperationException("Simulated sender failure");
        return Task.CompletedTask;
    }
}

sealed class EmailHandler : HttpMessageHandler
{
    public Uri? Uri;
    public HttpMethod? Method;
    public string? Authorization;
    public string Body = "";
    public HttpStatusCode Status = HttpStatusCode.OK;
    public string Response = "{\"id\":\"" + Guid.NewGuid() + "\"}";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        Uri = request.RequestUri;
        Method = request.Method;
        Authorization = request.Headers.Authorization?.ToString();
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(Status) { Content = new StringContent(Response) };
    }
}

sealed class FakeAuth : IAuthenticationService
{
    public bool SignedOut;

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
        Task.FromResult(AuthenticateResult.NoResult());

    public Task ChallengeAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) => Task.CompletedTask;

    public Task ForbidAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    ) => Task.CompletedTask;

    public Task SignInAsync(
        HttpContext context,
        string? scheme,
        ClaimsPrincipal principal,
        AuthenticationProperties? properties
    ) => Task.CompletedTask;

    public Task SignOutAsync(
        HttpContext context,
        string? scheme,
        AuthenticationProperties? properties
    )
    {
        SignedOut = true;
        return Task.CompletedTask;
    }
}

sealed class FakeSession : ISession
{
    public bool Cleared;
    public bool IsAvailable => true;
    public string Id => "fixture";
    public IEnumerable<string> Keys => [];

    public void Clear() => Cleared = true;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) { }

    public void Set(string key, byte[] value) { }

    public bool TryGetValue(string key, out byte[] value)
    {
        value = [];
        return false;
    }
}

sealed class FakeTempData : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) =>
        new Dictionary<string, object>();

    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
