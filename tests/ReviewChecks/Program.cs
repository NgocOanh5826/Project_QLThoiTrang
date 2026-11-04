// VAI TRÒ FILE: BỘ KIỂM THỬ ReviewChecks: chạy với QOANH_TEST_CONNECTION trỏ tới database
// QOANH_Verification_*
// riêng. Các kịch bản bên dưới kiểm tra kết quả thực tế và các điều kiện phải từ chối; không dùng
// database vận
// hành.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

var connection =
    Environment.GetEnvironmentVariable("QOANH_TEST_CONNECTION")
    ?? throw new Exception("Set QOANH_TEST_CONNECTION to an isolated fixture.");
if (!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("QOANH_Verification_"))
    throw new Exception("Isolated fixture required.");
var factory = new Factory(connection);
var store = new StoreService(factory);
var results = new List<object>();
int failed = 0;
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

void Check(string id, Action action)
{
    try
    {
        action();
        results.Add(new { id, status = "PASS" });
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
        Console.WriteLine("FAIL " + id + ": " + ex.Message);
    }
}

byte[] png = Convert.FromBase64String(
    (
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4"
        + "2mP8/x8AAwMCAO+a5S8AAAAASUVORK5CYII="
    )
);
var image = new ReviewUpload("photo.png", png);

// Synthetic MP4 container signature for validation/range tests, not playback QA.
var video = new ReviewUpload(
    "clip.mp4",
    new byte[]
    {
        0,
        0,
        0,
        24,
        102,
        116,
        121,
        112,
        105,
        115,
        111,
        109,
        0,
        0,
        0,
        0,
        105,
        115,
        111,
        109,
        109,
        112,
        52,
        50,
    }
);
Check(
    "UPLOAD-image-and-video-signatures",
    () =>
        Assert(
            ReviewUploads
                .Validate([image, video])
                .Select(m => m.ContentType)
                .SequenceEqual(new[] { "image/png", "video/mp4" })
        )
);
Check(
    "UPLOAD-spoofed-extension-rejected",
    () =>
        Reject(() =>
            ReviewUploads.Validate([new("fake.png", "<script>alert(1)</script>"u8.ToArray())])
        )
);
Check(
    "UPLOAD-html-svg-executable-rejected",
    () =>
    {
        foreach (var name in new[] { "file.html", "file.svg", "file.exe" })
            Reject(() => ReviewUploads.Validate([new(name, png)]));
    }
);
Check(
    "UPLOAD-image-limit",
    () =>
    {
        var bytes = new byte[ReviewUploads.ImageLimit + 1];
        png.CopyTo(bytes, 0);
        Reject(() => ReviewUploads.Validate([new("large.png", bytes)]));
    }
);
Check(
    "UPLOAD-video-limit",
    () =>
    {
        var bytes = new byte[ReviewUploads.VideoLimit + 1];
        video.Content.CopyTo(bytes, 0);
        Reject(() => ReviewUploads.Validate([new("large.mp4", bytes)]));
    }
);
Check(
    "UPLOAD-count-and-video-count",
    () =>
    {
        Reject(() => ReviewUploads.Validate(Enumerable.Repeat(image, 6).ToList()));
        Reject(() => ReviewUploads.Validate([video, video]));
    }
);
Check(
    "UPLOAD-total-limit",
    () =>
    {
        var v = new byte[ReviewUploads.VideoLimit];
        video.Content.CopyTo(v, 0);
        var i = new byte[ReviewUploads.ImageLimit];
        png.CopyTo(i, 0);
        Reject(() =>
            ReviewUploads.Validate([
                new("clip.mp4", v),
                new("1.png", i),
                new("2.png", i),
                new("3.png", i),
            ])
        );
    }
);
var email = "review-" + Guid.NewGuid().ToString("N") + "@example.test";
var customer = store.Register(
    new()
    {
        Name = "Review upload test",
        Email = email,
        Password = "Review@12345",
        ConfirmPassword = "Review@12345",
    }
);
var variant = store.Read(d =>
    d.Variants.First(v =>
        v.Active && v.Stock > 0 && d.Products.Any(p => p.Id == v.ProductId && p.Active)
    )
);
Check(
    "REVIEW-undelivered-rejected",
    () =>
        Reject(() =>
            store.Review(customer.Id, variant.ProductId, 5, "Chưa có đơn được giao", [image])
        )
);
var order = store.PlaceOrder(
    customer.Id,
    new()
    {
        Name = "Review test",
        Phone = "0901234567",
        Address = "Verification address",
        CheckoutToken = Guid.NewGuid().ToString("N"),
    },
    [
        new()
        {
            ProductId = variant.ProductId,
            VariantId = variant.Id,
            Size = variant.Size,
            Color = variant.Color,
            Quantity = 1,
        },
    ]
);
store.ChangeOrderStatus(order.Id, "Đã xác nhận", -1);
store.RecordPayment(order.Id, order.Total, "REVIEW-" + Guid.NewGuid().ToString("N"), -1);
store.ChangeOrderStatus(order.Id, "Đang giao", -1);
store.ChangeOrderStatus(order.Id, "Đã giao", -1);
Check(
    "REVIEW-invalid-stars-and-content-no-write",
    () =>
    {
        Reject(() => store.Review(customer.Id, variant.ProductId, 0, "Nội dung kiểm thử", [image]));
        Reject(() => store.Review(customer.Id, variant.ProductId, 5, "abc", [image]));
        Assert(!store.NativeRead(db => db.DanhGia.Any(r => r.MaKH == customer.Id)));
    }
);
Check(
    "REVIEW-invalid-file-no-write",
    () =>
    {
        Reject(() =>
            store.Review(
                customer.Id,
                variant.ProductId,
                5,
                "Nội dung kiểm thử",
                [new("fake.jpg", png)]
            )
        );
        Assert(!store.NativeRead(db => db.DanhGia.Any(r => r.MaKH == customer.Id)));
    }
);
Check(
    "REVIEW-media-failure-rolls-back-review",
    () =>
    {
        using var db = factory.CreateDbContext();
        db.Database.ExecuteSqlRaw(
            (
                "CREATE TRIGGER dbo.QA_RejectReviewMedia ON "
                + "dbo.QOANH_ReviewMedia AFTER INSERT AS THROW "
                + "51000,'Intentional isolated QA failure',1;"
            )
        );
        try
        {
            bool rejected = false;
            try
            {
                store.Review(customer.Id, variant.ProductId, 4, "Atomic rollback fixture", [image]);
            }
            catch (DbUpdateException)
            {
                rejected = true;
            }

            Assert(rejected && !store.NativeRead(x => x.DanhGia.Any(r => r.MaKH == customer.Id)));
        }
        finally
        {
            db.Database.ExecuteSqlRaw("DROP TRIGGER dbo.QA_RejectReviewMedia;");
        }
    }
);
Check(
    "REVIEW-persist-metadata-and-bytes",
    () =>
    {
        store.Review(customer.Id, variant.ProductId, 4, "Ảnh và video kiểm thử", [image, video]);
        var r = store
            .Reviews(variant.ProductId)
            .Single(r =>
                r.Id
                == store.NativeRead(db => db.DanhGia.Single(x => x.MaKH == customer.Id).MaDanhGia)
            );
        Assert(r.Media.Count == 2 && r.Media.Count(m => m.IsVideo) == 1);
        Assert(
            store
                .NativeRead(db =>
                    db.ReviewMedia.Single(m =>
                        m.ReviewId == r.Id && m.ContentType == "image/png"
                    ).Content
                )
                .SequenceEqual(png)
        );
    }
);
Check(
    "REVIEW-duplicate-rejected",
    () =>
        Reject(() =>
            store.Review(customer.Id, variant.ProductId, 5, "Không được ghi trùng", [image])
        )
);
var review = store.NativeRead(db => db.DanhGia.Single(r => r.MaKH == customer.Id));
Check(
    "REVIEW-moderation-hides-metadata",
    () =>
    {
        store.ModerateReview(review.MaDanhGia, false, -1);
        Assert(store.Reviews(variant.ProductId).All(r => r.Id != review.MaDanhGia));
        Assert(
            store.Reviews(includeHidden: true).Single(r => r.Id == review.MaDanhGia).Media.Count
                == 2
        );
        store.ModerateReview(review.MaDanhGia, true, -1);
    }
);
var httpEmail = "review-http-" + Guid.NewGuid().ToString("N") + "@example.test";
var httpCustomer = store.Register(
    new()
    {
        Name = "HTTP review fixture",
        Email = httpEmail,
        Password = "Review@12345",
        ConfirmPassword = "Review@12345",
    }
);
var httpOrder = store.PlaceOrder(
    httpCustomer.Id,
    new()
    {
        Name = "HTTP review fixture",
        Phone = "0901234567",
        Address = "Verification address",
        CheckoutToken = Guid.NewGuid().ToString("N"),
    },
    [
        new()
        {
            ProductId = variant.ProductId,
            VariantId = variant.Id,
            Size = variant.Size,
            Color = variant.Color,
            Quantity = 1,
        },
    ]
);
store.ChangeOrderStatus(httpOrder.Id, "Đã xác nhận", -1);
store.RecordPayment(
    httpOrder.Id,
    httpOrder.Total,
    "REVIEW-HTTP-" + Guid.NewGuid().ToString("N"),
    -1
);
store.ChangeOrderStatus(httpOrder.Id, "Đang giao", -1);
store.ChangeOrderStatus(httpOrder.Id, "Đã giao", -1);
File.WriteAllText(
    "delivery/review-media-fixture.json",
    JsonSerializer.Serialize(
        new
        {
            email = httpEmail,
            customerId = httpCustomer.Id,
            productId = variant.ProductId,
            reviewId = review.MaDanhGia,
            media = store.Reviews(variant.ProductId).Single(r => r.Id == review.MaDanhGia).Media,
        }
    )
);
File.WriteAllText(
    "delivery/review-media-results.json",
    JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })
);
Environment.ExitCode = failed == 0 ? 0 : 1;

class Factory(string connection) : IDbContextFactory<StoreDbContext>
{
    // CHỨC NĂNG: Tạo DbContext trỏ tới database kiểm thử đã truyền vào Factory; không dùng chuỗi
    // kết nối
    // production.
    public StoreDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
}
