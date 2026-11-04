// VAI TRÒ FILE: BỘ KIỂM THỬ FeatureChecks: chạy với QOANH_TEST_CONNECTION trỏ tới database
// QOANH_Verification_*
// riêng. Các kịch bản bên dưới kiểm tra kết quả thực tế và các điều kiện phải từ chối; không dùng
// database vận
// hành.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

var connection =
    Environment.GetEnvironmentVariable("QOANH_TEST_CONNECTION")
    ?? throw new Exception("Set QOANH_TEST_CONNECTION");
if (!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("QOANH_Verification_"))
    throw new Exception("Isolated database required");
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            { "BankTransfer:Enabled", "true" },
            { "BankTransfer:QrImage", "/images/payments/transfer-qr.png" },
            { "Smtp:Host", "fake.test" },
            { "Smtp:From", "no-reply@example.test" },
            { "PasswordReset:HashKey", "test-only-secret-with-more-than-32-characters" },
            { "Ghn:Enabled", "true" },
            { "Ghn:Token", "FAKE-NOT-A-CREDENTIAL" },
            { "Ghn:ShopId", "1" },
        }
    )
    .Build();
var factory = new Factory(connection);
var store = new StoreService(factory, config);
var results = new List<object>();
int failed = 0;
void Check(string id, Action action)
{
    try
    {
        action();
        results.Add(
            new
            {
                id,
                status = "PASS",
                error = "",
            }
        );
        Console.WriteLine("PASS " + id);
    }
    catch (Exception ex)
    {
        failed++;
        results.Add(
            new
            {
                id,
                status = "FAIL",
                error = ex.ToString(),
            }
        );
        Console.WriteLine("FAIL " + id + " " + ex.Message);
    }
}

void Assert(bool value)
{
    if (!value)
        throw new Exception("Assertion failed");
}

void Reject(Action action)
{
    try
    {
        action();
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new Exception("Expected rejection");
}

string suffix = Guid.NewGuid().ToString("N")[..8];
Customer customer = store.Register(
    new()
    {
        Name = "Feature test",
        Email = "feature-" + suffix + "@example.test",
        Password = "Feature@12345",
        ConfirmPassword = "Feature@12345",
    }
);
int support = store.Staff().First(s => s.Role == "Support" && s.Active).Id;
Guid messageToken = Guid.NewGuid();
Check(
    "CHAT01-persist-and-idempotency",
    () =>
    {
        var m = store.SendMessage(customer.Id, customer.Id, "Hello persisted chat", messageToken);
        var again = store.SendMessage(
            customer.Id,
            customer.Id,
            "Hello persisted chat",
            messageToken
        );
        Assert(
            m.Id == again.Id
                && new StoreService(factory).Messages(customer.Id, customer.Id).Count == 1
        );
    }
);
Check("CHAT02-other-customer-denied", () => Reject(() => store.Messages(customer.Id, 1)));
Check(
    "CHAT03-support-reply-and-read",
    () =>
    {
        store.SendMessage(customer.Id, support, "Support reply", Guid.NewGuid());
        var messages = store.Messages(customer.Id, customer.Id);
        Assert(messages.Count == 2 && messages.Last().SenderId < 0);
        store.MarkChatRead(customer.Id, support, messages.Last().Id);
        Assert(store.ChatInbox(support).Single(t => t.Id == customer.Id).Unread == 0);
    }
);
Check("CHAT04-catalog-denied", () => Reject(() => store.Messages(customer.Id, -2)));
Check(
    "CHAT05-reused-token-tamper-denied",
    () => Reject(() => store.SendMessage(customer.Id, customer.Id, "Different text", messageToken))
);
var mail = new FakeMail();
var recovery = new PasswordRecovery(store, mail, config, NullLogger<PasswordRecovery>.Instance);
string otp = "";
Check(
    "OTP01-hash-and-send-fake-mail",
    () =>
    {
        recovery.Request(customer.Email, default).GetAwaiter().GetResult();
        otp = mail.Code;
        using var db = factory.CreateDbContext();
        var code = db.ResetCodes.Single(r => r.CustomerId == customer.Id);
        Assert(otp.Length == 6 && code.Hash != otp && code.Hash.Length == 64 && code.Sent);
    }
);
Check(
    "OTP02-wrong-code-counted",
    () =>
    {
        string wrong = otp == "000000" ? "000001" : "000000";
        Assert(
            !recovery.Complete(
                new()
                {
                    Email = customer.Email,
                    Code = wrong,
                    Password = "Changed@12345",
                    ConfirmPassword = "Changed@12345",
                }
            )
        );
        using var db = factory.CreateDbContext();
        Assert(db.ResetCodes.Single(r => r.CustomerId == customer.Id).Attempts == 1);
    }
);
Check(
    "OTP03-reset-invalidates-old-password",
    () =>
    {
        Assert(
            recovery.Complete(
                new()
                {
                    Email = customer.Email,
                    Code = otp,
                    Password = "Changed@12345",
                    ConfirmPassword = "Changed@12345",
                }
            )
        );
        Assert(
            store.Authenticate(customer.Email, "Feature@12345") == null
                && store.Authenticate(customer.Email, "Changed@12345") != null
        );
    }
);
Check(
    "OTP04-replay-denied",
    () =>
        Assert(
            !recovery.Complete(
                new()
                {
                    Email = customer.Email,
                    Code = otp,
                    Password = "Other@12345",
                    ConfirmPassword = "Other@12345",
                }
            )
        )
);
Check(
    "OTP05-unknown-account-no-mail",
    () =>
    {
        mail.Code = "";
        recovery.Request("missing-" + suffix + "@example.test", default).GetAwaiter().GetResult();
        Assert(mail.Code == "");
    }
);
Check(
    "OTP06-expired-denied",
    () =>
    {
        var c = store.Register(
            new()
            {
                Name = "Expired OTP",
                Email = "expired-" + suffix + "@example.test",
                Password = "Feature@12345",
                ConfirmPassword = "Feature@12345",
            }
        );
        recovery.Request(c.Email, default).GetAwaiter().GetResult();
        using (var db = factory.CreateDbContext())
        {
            db.ResetCodes.Single(r => r.CustomerId == c.Id).ExpiresUtc = DateTime.UtcNow.AddMinutes(
                -1
            );
            // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
            // hoàn tất khi Commit
            // thành công.
            db.SaveChanges();
        }

        Assert(
            !recovery.Complete(
                new()
                {
                    Email = c.Email,
                    Code = mail.Code,
                    Password = "Changed@12345",
                    ConfirmPassword = "Changed@12345",
                }
            )
        );
    }
);
Check(
    "OTP07-five-failures-lock-code",
    () =>
    {
        var c = store.Register(
            new()
            {
                Name = "Locked OTP",
                Email = "locked-" + suffix + "@example.test",
                Password = "Feature@12345",
                ConfirmPassword = "Feature@12345",
            }
        );
        recovery.Request(c.Email, default).GetAwaiter().GetResult();
        string correct = mail.Code,
            wrong = correct == "000000" ? "000001" : "000000";
        for (int i = 0; i < 6; i++)
            Assert(
                !recovery.Complete(
                    new()
                    {
                        Email = c.Email,
                        Code = wrong,
                        Password = "Changed@12345",
                        ConfirmPassword = "Changed@12345",
                    }
                )
            );
        Assert(
            !recovery.Complete(
                new()
                {
                    Email = c.Email,
                    Code = correct,
                    Password = "Changed@12345",
                    ConfirmPassword = "Changed@12345",
                }
            )
        );
        using var db = factory.CreateDbContext();
        Assert(db.ResetCodes.Single(r => r.CustomerId == c.Id).Attempts == 5);
    }
);
int product = store.Write(d =>
{
    var p = new Product
    {
        Id = d.Products.Max(p => p.Id) + 1,
        Name = "Cost fixture " + suffix,
        Category = d.Categories[0],
        Stock = 0,
        Price = 100000,
        Color = "Trắng",
    };
    d.Products.Add(p);
    return p.Id;
});
int variant = store.Read(d => d.Variants.First(v => v.ProductId == product && v.Size == "M").Id);
int receipt = 0;
Check(
    "COST01-new-receipt-establishes-cost",
    () =>
    {
        store.Receive(
            new()
            {
                VariantId = variant,
                WarehouseId = 1,
                SupplierId = 1,
                Quantity = 10,
                UnitCost = 40000,
                Reference = "COST1-" + suffix,
            },
            -3
        );
        Assert(
            store.NativeRead(db =>
                db.InventoryValues.Single(v => v.VariantId == variant).AverageCost
            ) == 40000
        );
        receipt = store.NativeRead(db =>
            db.PhieuNhap.Single(p => p.QReference == "IN-COST1-" + suffix).MaPhieuNhap
        );
    }
);
Check(
    "COST02-weighted-average",
    () =>
    {
        store.Receive(
            new()
            {
                VariantId = variant,
                WarehouseId = 1,
                SupplierId = 1,
                Quantity = 10,
                UnitCost = 60000,
                Reference = "COST2-" + suffix,
            },
            -3
        );
        Assert(
            store.NativeRead(db =>
                db.InventoryValues.Single(v => v.VariantId == variant).AverageCost
            ) == 50000
        );
    }
);
Order order = null!;
CheckoutInput Input() =>
    new()
    {
        Name = "Feature buyer",
        Phone = "0901234567",
        Address = "Test address",
        Payment = "Chuyển khoản",
        CheckoutToken = Guid.NewGuid().ToString("N"),
    };
List<CartItem> Cart() =>
    new()
    {
        new()
        {
            ProductId = product,
            VariantId = variant,
            Size = "M",
            Color = "Trắng",
            Quantity = 2,
        },
    };
Check(
    "BANK01-transfer-snapshot-and-cost",
    () =>
    {
        order = store.PlaceOrder(customer.Id, Input(), Cart());
        var saved = store.Read(d => d.Orders.Single(o => o.Id == order.Id));
        Assert(
            saved.Payment == "Chuyển khoản"
                && saved.BankQrImage == "/images/payments/transfer-qr.png"
                && saved.TransferReference?.StartsWith("QO") == true
                && saved.PaidAmount == 0
                && saved.Lines.All(l => l.UnitCost == 50000)
        );
    }
);
Check(
    "BANK02-unconfigured-rejected",
    () => Reject(() => new StoreService(factory).PlaceOrder(customer.Id, Input(), Cart()))
);
Check("BANK03-admin-cannot-buy", () => Reject(() => store.PlaceOrder(-1, Input(), Cart())));
Check(
    "BANK04-confirm-receipt-while-pending",
    () =>
    {
        store.RecordPayment(order.Id, order.Total, "BANK-" + suffix, -4);
        Assert(store.Read(d => d.Orders.Single(o => o.Id == order.Id)).PaidAmount == order.Total);
        Reject(() => store.RecordPayment(order.Id, order.Total, "BANK-" + suffix, -4));
    }
);
Check(
    "COST03-snapshot-does-not-change",
    () =>
    {
        store.SetInventoryValue(variant, 18, 70000, "Opening value reconciliation", -3);
        Assert(
            store.Read(d =>
                d.Orders.Single(o => o.Id == order.Id).Lines.All(l => l.UnitCost == 50000)
            )
        );
    }
);
Check(
    "DEBT01-new-receipt-liability",
    () =>
        Assert(
            store.NativeRead(db => db.SupplierAccounts.Single(a => a.ReceiptId == receipt).Amount)
                == 400000
        )
);
Check(
    "DEBT02-partial-payment-and-overpay-guard",
    () =>
    {
        store.PaySupplier(receipt, 200000, "SUP-" + suffix, -4);
        Reject(() => store.PaySupplier(receipt, 200001, "SUP-OVER-" + suffix, -4));
        Reject(() => store.PaySupplier(receipt, 1, "SUP-" + suffix, -4));
        Assert(
            store.NativeRead(db =>
                db.SupplierPayments.Where(p => p.ReceiptId == receipt).Sum(p => p.Amount)
            ) == 200000
        );
    }
);
Check(
    "DEBT03-customer-cannot-pay",
    () => Reject(() => store.PaySupplier(receipt, 1, "UNAUTH-" + suffix, customer.Id))
);
store.ChangeOrderStatus(order.Id, "Đã xác nhận", -4);
var handler = new FakeGhn(order.Id);
var shipping = new ShippingService(store, config, new HttpClient(handler));
Check(
    "SHIP01-create-fake-provider-no-COD-for-paid-order",
    () =>
    {
        shipping
            .Create(
                new()
                {
                    Id = order.Id,
                    Province = "Test province",
                    Ward = "Test ward",
                },
                -4,
                default
            )
            .GetAwaiter()
            .GetResult();
        Assert(
            handler.CreateCalls == 1
                && handler.Cod == 0
                && store.NativeRead(db =>
                    db.Shipments.Single(s => s.OrderId == order.DatabaseId).TrackingCode
                )
                    == "FAKE-" + order.DatabaseId
        );
    }
);
Check(
    "SHIP02-duplicate-create-reconciles",
    () =>
    {
        shipping
            .Create(
                new()
                {
                    Id = order.Id,
                    Province = "Test province",
                    Ward = "Test ward",
                },
                -4,
                default
            )
            .GetAwaiter()
            .GetResult();
        Assert(handler.CreateCalls == 1);
    }
);
Check(
    "SHIP03-provider-delivered-not-auto-payment-or-order-status",
    () => Assert(store.Read(d => d.Orders.Single(o => o.Id == order.Id).Status) == "Đã xác nhận")
);
store.ChangeOrderStatus(order.Id, "Đang giao", -4);
store.ChangeOrderStatus(order.Id, "Đã giao", -4);
Check(
    "PROFIT01-known-cost-margin",
    () =>
    {
        var lines = store
            .Profit(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
            .Where(l => l.OrderId == order.Id)
            .ToList();
        Assert(
            lines.Sum(l => l.Revenue) == 200000
                && lines.Sum(l => l.Cost) == 100000
                && lines.Sum(l => l.Profit) == 100000
        );
    }
);
Check(
    "PROFIT02-partial-return-reverses-cost",
    () =>
    {
        var line = store.Read(d => d.Orders.Single(o => o.Id == order.Id).Lines.First());
        store.RequestReturn(order.Id, "Feature partial return", customer.Id, line.Id, 1);
        int id = store.Read(d => d.Returns.Last(r => r.OrderId == order.Id).Id);
        store.ResolveReturn(id, true, "Received and refunded", "FRET-" + suffix, -4);
        var lines = store
            .Profit(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
            .Where(l => l.OrderId == order.Id)
            .ToList();
        Assert(lines.Sum(l => l.Revenue) == 100000 && lines.Sum(l => l.Cost) == 50000);
    }
);
Check(
    "COST04-customer-revaluation-denied",
    () => Reject(() => store.SetInventoryValue(variant, 19, 1, "Unauthorized value", customer.Id))
);
Check(
    "PAY-preserve-legacy-method",
    () =>
    {
        var input = Input();
        input.Payment = "COD";
        var legacy = store.PlaceOrder(customer.Id, input, Cart());
        using (var db = factory.CreateDbContext())
        {
            db.ThanhToan.Single(t => t.MaDonHang == legacy.DatabaseId).PhuongThuc = "Ví điện tử";
            // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
            // hoàn tất khi Commit
            // thành công.
            db.SaveChanges();
        }

        store.Write(d => 0);
        Assert(
            store.NativeRead(db =>
                db.ThanhToan.Single(t => t.MaDonHang == legacy.DatabaseId).PhuongThuc
            ) == "Ví điện tử"
        );
    }
);
Check(
    "SHIP04-timeout-remains-uncertain-after-later-rejection",
    () =>
    {
        var input = Input();
        input.Payment = "COD";
        var o = store.PlaceOrder(customer.Id, input, Cart());
        store.ChangeOrderStatus(o.Id, "Đã xác nhận", -4);
        var request = new ShipmentInput
        {
            Id = o.Id,
            Province = "Test province",
            Ward = "Test ward",
        };
        var timeout = new ShippingService(store, config, new HttpClient(new ErrorGhn(true)));
        Reject(() => timeout.Create(request, -4, default).GetAwaiter().GetResult());
        var rejected = new ShippingService(store, config, new HttpClient(new ErrorGhn(false)));
        Reject(() => rejected.Create(request, -4, default).GetAwaiter().GetResult());
        Assert(
            store.NativeRead(db => db.Shipments.Single(s => s.OrderId == o.DatabaseId).Status)
                == "Unknown"
        );
        Reject(() => store.ChangeOrderStatus(o.Id, "Đã hủy", -4));
    }
);
Check(
    "PDF-multipage-Vietnamese-table",
    () =>
    {
        PdfSharp.Fonts.GlobalFontSettings.FontResolver = new DocumentFontResolver(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts)
        );
        var bytes = new PdfDocuments().Create(
            "PHIẾU NHẬP NHIỀU DÒNG",
            "QA-MULTIPAGE",
            new[] { "Kiểm tra tiếng Việt và phân trang" },
            new[] { "Sản phẩm", "Số lượng", "Đơn giá", "Thành tiền" },
            Enumerable
                .Range(1, 55)
                .Select(i =>
                    new[]
                    {
                        "Áo sơ mi thời trang số " + i + " / Trắng / XL",
                        "2",
                        "100,000",
                        "200,000",
                    }
                )
                .ToList(),
            new[] { "Tổng cộng: 11,000,000 đ" }
        );
        using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(
            new MemoryStream(bytes),
            PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import
        );
        Assert(pdf.PageCount > 1);
        Directory.CreateDirectory("delivery/verify-extracted/pdfs");
        File.WriteAllBytes("delivery/verify-extracted/pdfs/multipage.pdf", bytes);
    }
);
Directory.CreateDirectory("delivery");
File.WriteAllText(
    "delivery/features-check-results.json",
    JsonSerializer.Serialize(
        new
        {
            executedUtc = DateTime.UtcNow,
            total = results.Count,
            failed,
            results,
        },
        new JsonSerializerOptions { WriteIndented = true }
    )
);
Console.WriteLine($"{results.Count - failed}/{results.Count} passed");
return failed == 0 ? 0 : 1;

class Factory(string connection) : IDbContextFactory<StoreDbContext>
{
    // CHỨC NĂNG: Tạo DbContext trỏ tới database kiểm thử đã truyền vào Factory; không dùng chuỗi
    // kết nối
    // production.
    public StoreDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
}

class FakeMail : IMailSender
{
    public string Code = "";

    // CHỨC NĂNG: Gửi OTP qua SMTP TLS tới email được truyền vào; triển khai thật dùng

    // SmtpMailSender, kiểm thử có
    // thể thay bằng IMailSender giả lập.
    public Task SendCode(string email, string code, CancellationToken token)
    {
        Code = code;
        return Task.CompletedTask;
    }
}

class ErrorGhn(bool timeout) : HttpMessageHandler
{
    // CHỨC NĂNG: HTTP giả lập cho kịch bản kiểm thử GHN; trả phản hồi được định nghĩa trong thân
    // hàm, không gọi
    // dịch vụ thật.
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken token
    )
    {
        if (timeout)
            throw new TaskCanceledException("Fake timeout");
        return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    "{\"code\":400,\"message\":\"Rejected\"}",
                    Encoding.UTF8,
                    "application/json"
                ),
            }
        );
    }
}

class FakeGhn(string id) : HttpMessageHandler
{
    public int CreateCalls;
    public long Cod;

    // CHỨC NĂNG: HTTP giả lập cho kịch bản kiểm thử GHN; trả phản hồi được định nghĩa trong thân

    // hàm, không gọi
    // dịch vụ thật.
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken token
    )
    {
        if (request.Method == HttpMethod.Post)
        {
            CreateCalls++;
            using var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync(token).Result);
            Cod = body.RootElement.GetProperty("cod_amount").GetInt64();
        }

        var dbId = id;
        var payload = new
        {
            code = 200,
            data = new
            {
                order_code = "FAKE-" + CurrentOrderId(),
                client_order_code = id,
                status = "delivered",
            },
        };
        return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                ),
            }
        );
    }

    // CHỨC NĂNG: Lấy mã đơn kiểm thử hiện tại để đối chiếu yêu cầu GHN giả lập.
    int CurrentOrderId()
    {
        var cs = Environment.GetEnvironmentVariable("QOANH_TEST_CONNECTION")!;
        using var db = new Factory(cs).CreateDbContext();
        return db.DonHang.Single(o => o.QPublicId == id).MaDonHang;
    }
}
