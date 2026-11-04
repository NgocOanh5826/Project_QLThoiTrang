using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Project_QLThoiTrang.Controllers;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

var database = "QOANH_Verification_Product_" + Guid.NewGuid().ToString("N")[..12];
var connection = new SqlConnectionStringBuilder
{
    DataSource = Environment.GetEnvironmentVariable("QOANH_TEST_SERVER") ?? @".\MSSQLSERVER01",
    InitialCatalog = database,
    IntegratedSecurity = true,
    TrustServerCertificate = true,
};
var factory = new Factory(connection.ConnectionString);
var store = new StoreService(factory);
var root = Path.GetFullPath(
    Path.Combine("BuildReview", "product_upload", "fixture-" + Guid.NewGuid().ToString("N"))
);
Directory.CreateDirectory(root);
var images = new ProductImages(new Hosting { WebRootPath = root });
var png = Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII="
);
var results = new List<object>();
int failed = 0;

void Assert(bool condition)
{
    if (!condition)
        throw new Exception("Assertion failed");
}

void Check(string name, Action action)
{
    try
    {
        action();
        results.Add(new { name, status = "PASS" });
        Console.WriteLine("PASS " + name);
    }
    catch (Exception ex)
    {
        failed++;
        results.Add(
            new
            {
                name,
                status = "FAIL",
                error = ex.ToString(),
            }
        );
        Console.WriteLine("FAIL " + name + ": " + ex.Message);
    }
}

AdminController Admin()
{
    var http = new DefaultHttpContext();
    return new AdminController(store)
    {
        ControllerContext = new ControllerContext { HttpContext = http },
        TempData = new TempDataDictionary(http, new TempData()),
    };
}

IFormFile Upload(byte[] bytes, string name = "photo.png") =>
    new FormFile(new MemoryStream(bytes), 0, bytes.Length, "imageFile", name);

Product Input(string name = "Upload fixture") =>
    new()
    {
        Name = name,
        Category = "Áo nam",
        Price = 150000,
        Color = "Trắng",
        Stock = 0,
        IsNew = false,
    };

try
{
    using (var db = factory.CreateDbContext())
        db.Database.EnsureCreated();
    Check(
        "create-category-and-reject-duplicate",
        () =>
        {
            Assert(Admin().Category("Áo nam") is RedirectToActionResult);
            Assert(store.Read(d => d.Categories.Contains("Áo nam")));
            Assert(Admin().Category("Áo nam") is ViewResult);
        }
    );
    Check(
        "reactivate-existing-inactive-category",
        () =>
        {
            using (var db = factory.CreateDbContext())
            {
                db.DanhMuc.Add(new DanhMucRow { TenDanhMuc = "Danh mục cũ", TrangThai = false });
                db.SaveChanges();
            }
            Assert(Admin().Category("Danh mục cũ") is RedirectToActionResult);
            Assert(store.Read(d => d.Categories.Contains("Danh mục cũ")));
        }
    );
    var input = Input();
    Check(
        "upload-and-product-image-survive-database-reload",
        () =>
        {
            var result = Admin()
                .Product(input, Upload(png), images, default)
                .GetAwaiter()
                .GetResult();
            Assert(result is RedirectToActionResult);
            var saved = store.Read(d => d.Products.Single(p => p.Id == input.Id));
            Assert(
                saved.Image.StartsWith("/images/products/uploads/") && saved.Image == input.Image
            );
            Assert(
                File.ReadAllBytes(
                        Path.Combine(
                            root,
                            saved.Image.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                        )
                    )
                    .SequenceEqual(png)
            );
            Assert(saved.Stock == 0);
        }
    );
    var firstImage = input.Image;
    Check(
        "edit-without-upload-preserves-image",
        () =>
        {
            input.Name = "Edited fixture";
            Assert(
                Admin().Product(input, null, images, default).GetAwaiter().GetResult()
                    is RedirectToActionResult
            );
            Assert(store.Read(d => d.Products.Single(p => p.Id == input.Id).Image) == firstImage);
        }
    );
    Check(
        "replace-image-keeps-old-file-for-order-history",
        () =>
        {
            Assert(
                Admin().Product(input, Upload(png), images, default).GetAwaiter().GetResult()
                    is RedirectToActionResult
            );
            Assert(input.Image != firstImage);
            Assert(
                File.Exists(
                    Path.Combine(
                        root,
                        firstImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                    )
                )
            );
        }
    );
    Check(
        "reject-fake-image-svg-and-oversize",
        () =>
        {
            foreach (
                var upload in new[]
                {
                    Upload("not an image"u8.ToArray()),
                    Upload("<svg></svg>"u8.ToArray(), "image.svg"),
                    Upload(new byte[ProductImages.MaxBytes + 1]),
                }
            )
            {
                var controller = Admin();
                var invalid = Input();
                Assert(
                    controller.Product(invalid, upload, images, default).GetAwaiter().GetResult()
                        is ViewResult
                );
                Assert(!controller.ModelState.IsValid && invalid.Id == 0);
            }
        }
    );
    Check(
        "failed-save-removes-only-new-upload",
        () =>
        {
            int count = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length;
            var invalid = Input(new string('x', 121));
            try
            {
                Admin().Product(invalid, Upload(png), images, default).GetAwaiter().GetResult();
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException) { }
            Assert(Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length == count);
            Assert(
                File.Exists(
                    Path.Combine(
                        root,
                        firstImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                    )
                )
            );
        }
    );
    Check(
        "reject-untrusted-image-path-and-invalid-category",
        () =>
        {
            Assert(!ProductImages.IsAllowedPath("/images/products/uploads/../../bad.png"));
            Assert(!ProductImages.IsAllowedPath("https://example.test/image.png"));
            Assert(
                !ProductImages.IsAllowedPath(
                    "/images/products/uploads/" + new string('a', 32) + ".png\n"
                )
            );
            var invalid = Input();
            invalid.Category = "missing";
            Assert(
                Admin().Product(invalid, null, images, default).GetAwaiter().GetResult()
                    is ViewResult
            );
        }
    );
    Check(
        "category-filter-and-collection-intersection",
        () =>
        {
            var shop = new ShopController(store);
            var all = (CatalogViewModel)
                ((ViewResult)shop.Index(q: null, category: " áo NAM ")).Model!;
            Assert(all.Products.Count == 1);
            var fresh = (CatalogViewModel)
                ((ViewResult)shop.Index(q: null, category: "Áo nam", collection: "new")).Model!;
            Assert(fresh.Products.Count == 0);
            input.IsNew = true;
            Admin().Product(input, null, images, default).GetAwaiter().GetResult();
            fresh = (CatalogViewModel)
                ((ViewResult)shop.Index(q: null, category: "Áo nam", collection: "new")).Model!;
            Assert(fresh.Products.Count == 1);
        }
    );
    if (args.Contains("--ui"))
        Check("browser-product-upload-category-and-mobile", () => RunBrowser());
}
finally
{
    File.WriteAllText(
        "BuildReview/product_upload/test-results.json",
        JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })
    );
    if (!database.StartsWith("QOANH_Verification_Product_", StringComparison.Ordinal))
        throw new Exception("Unsafe database cleanup");
    using var db = factory.CreateDbContext();
    db.Database.EnsureDeleted();
    var boundary =
        Path.GetFullPath(Path.Combine("BuildReview", "product_upload"))
        + Path.DirectorySeparatorChar;
    if (!root.StartsWith(boundary, StringComparison.OrdinalIgnoreCase))
        throw new Exception("Unsafe upload fixture cleanup");
    Directory.Delete(root, true);
}
Console.WriteLine($"Product checks: {results.Count - failed}/{results.Count} passed.");
Environment.ExitCode = failed == 0 ? 0 : 1;

// RunBrowser: Chạy giao diện trên database và thư mục ảnh thử nghiệm riêng.
void RunBrowser()
{
    using (var db = factory.CreateDbContext())
    {
        db.NhanVien.Add(new NhanVienRow
        {
            HoTen = "Product test admin",
            Email = "admin.product@example.test",
            ChucVu = "Admin",
            TrangThai = true,
            NgayVaoLam = DateTime.Today,
            MatKhau = new Microsoft.AspNetCore.Identity.PasswordHasher<Customer>()
                .HashPassword(new Customer(), "FixtureAdmin123"),
        });
        db.SaveChanges();
    }
    var sourceRoot = Path.GetFullPath("Project_QLThoiTrang");
    foreach (var directory in new[] { "css", "js" })
    {
        Directory.CreateDirectory(Path.Combine(root, directory));
        foreach (var file in Directory.GetFiles(Path.Combine(sourceRoot, "wwwroot", directory)))
            File.Copy(file, Path.Combine(root, directory, Path.GetFileName(file)), true);
    }
    var start = new System.Diagnostics.ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Project_QLThoiTrang.dll"));
    foreach (var argument in new[] { "--urls", "http://127.0.0.1:5197", "--contentRoot", sourceRoot, "--webroot", root })
        start.ArgumentList.Add(argument);
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    start.Environment["ConnectionStrings__Store"] = connection.ConnectionString;
    start.Environment.Remove("ASPNETCORE_HTTPS_PORT");
    start.Environment.Remove("HTTPS_PORT");
    using var server = System.Diagnostics.Process.Start(start)!;
    var log = server.StandardOutput.ReadToEndAsync();
    var errors = server.StandardError.ReadToEndAsync();
    try
    {
        using var client = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(2) };
        bool ready = false;
        string lastFailure = "";
        for (int attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                using var response = client.GetAsync("http://127.0.0.1:5197/Account/Login")
                    .GetAwaiter().GetResult();
                ready = response.IsSuccessStatusCode;
                lastFailure = response.StatusCode.ToString();
                if (ready) break;
            }
            catch (HttpRequestException ex) { lastFailure = ex.Message; }
            catch (TaskCanceledException ex) { lastFailure = ex.Message; }
            Thread.Sleep(300);
        }
        if (!ready) throw new Exception("Preview unavailable: " + lastFailure);
        var node = new System.Diagnostics.ProcessStartInfo("node")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        node.ArgumentList.Add("BuildReview/product_upload/ui_checks.mjs");
        using var browser = System.Diagnostics.Process.Start(node)!;
        var browserOutput = browser.StandardOutput.ReadToEndAsync();
        var browserErrors = browser.StandardError.ReadToEndAsync();
        if (!browser.WaitForExit(90000))
        {
            browser.Kill(true);
            throw new Exception("Browser check timed out");
        }
        var browserLog = browserOutput.GetAwaiter().GetResult() + browserErrors.GetAwaiter().GetResult();
        File.WriteAllText("BuildReview/product_upload/browser.log", browserLog);
        if (browser.ExitCode != 0) throw new Exception(browserLog);
    }
    finally
    {
        if (!server.HasExited) server.Kill(true);
        server.WaitForExit();
        File.WriteAllText("BuildReview/product_upload/server.log", log.GetAwaiter().GetResult());
        File.WriteAllText("BuildReview/product_upload/server-error.log", errors.GetAwaiter().GetResult());
    }
}

sealed class Factory(string connection) : IDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<StoreDbContext>().UseSqlServer(connection).Options);
}

sealed class Hosting : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "ProductChecks";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Development";
}

sealed class TempData : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) =>
        new Dictionary<string, object>();

    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
