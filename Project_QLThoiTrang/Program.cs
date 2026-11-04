// VAI TRÒ FILE: Điểm khởi động ứng dụng: đăng ký MVC/service/SQL/Session/cookie, xử lý lệnh quản
// trị và xếp thứ tự
// middleware. Đọc HUONG_DAN_CODE.md để tra toàn bộ luồng chức năng.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Services;

// BƯỚC 1 - KHỞI TẠO: đọc cấu hình appsettings, biến môi trường và tham số; builder.Services là nơi

// đăng ký đối
// tượng được tự truyền vào constructor controller/service.
// BIẾN builder (WebApplicationBuilder): đối tượng dựng ứng dụng: đọc cấu hình và đăng ký các
// service, MVC, SQL, cookie, Session trước Build.
var builder = WebApplication.CreateBuilder(args);

// MVC VÀ CSRF: bật controller + Razor view; AutoValidateAntiforgeryToken kiểm tra token cho các

// phương thức
// không an toàn như POST. Form TagHelper tạo trường token để server đối chiếu.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute())
);

// SERVICE NGHIỆP VỤ: StoreService dùng DbContextFactory để tạo context cho từng lần đọc/ghi; các

// file partial
// cùng tạo thành một lớp service.
builder.Services.AddSingleton<StoreService>();
builder.Services.AddSingleton<DatabaseSchemaStatus>();
builder.Services.AddSingleton<PdfDocuments>();

// PDF TIẾNG VIỆT: chọn thư mục font từ Pdf:FontDirectory hoặc thư mục Fonts của hệ điều hành;

// PdfDocuments dùng
// font này khi xuất chứng từ.
PdfSharp.Fonts.GlobalFontSettings.FontResolver = new DocumentFontResolver(
    builder.Configuration["Pdf:FontDirectory"]
        ?? Environment.GetFolderPath(Environment.SpecialFolder.Fonts)
);

// OTP: đăng ký bộ gửi mail thật và service khôi phục; thiếu cấu hình SMTP/HMAC thì chức năng chưa

// sẵn sàng,
// không tự gửi mail.
builder.Services.AddTransient<IMailSender, SmtpMailSender>();
builder.Services.AddTransient<PasswordRecovery>();

// GHN: HttpClient của ShippingService có timeout 25 giây; cấu hình token/shop/gateway được kiểm

// tra trong
// ShippingService.Options.
builder.Services.AddHttpClient<ShippingService>(c => c.Timeout = TimeSpan.FromSeconds(25));

// KẾT NỐI SQL: lấy ConnectionStrings:Store; thiếu chuỗi kết nối thì dừng khởi tạo. Mapping nằm ở
// Data/StoreDbContext.cs và các file mapping partial.
builder.Services.AddDbContextFactory<StoreDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Store")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Store")
    )
);
builder.Services.AddDistributedMemoryCache();

// SESSION: giỏ khách vãng lai và checkout token/báo giá lưu theo phiên trình duyệt, hết hạn sau 2

// giờ không hoạt
// động; cookie HttpOnly không cho JavaScript đọc trực tiếp.
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// COOKIE ĐĂNG NHẬP: chọn trang Login/Denied và thời hạn mặc định 8 giờ; AccountController.SignIn

// có thể đặt 72
// giờ khi RememberMe được chọn.
builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

// GIỚI HẠN ĐĂNG NHẬP/OTP: policy login theo IP, tối đa 20 yêu cầu mỗi phút, không xếp hàng; vượt

// giới hạn trả
// 429. Chỉ action có EnableRateLimiting("login") dùng policy này.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy(
        "login",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }
            )
    );
});

// KIỂM TRA COOKIE LẠI: đọc tài khoản hiện tại từ SQL. Id sai, tài khoản bị khóa, quyền đổi hoặc

// hash mật khẩu
// đổi thì RejectPrincipal và đăng xuất; cookie không được giữ quyền cũ.
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
        options.Events.OnValidatePrincipal = async context =>
        {
            // BIẾN OUT id (int): mã tài khoản đọc từ claim; chỉ dùng khi TryParse thành công.
            if (
                !int.TryParse(
                    context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
                    out var id
                )
            )
            {
                context.RejectPrincipal();
                return;
            }

            // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
            // vai trò và trạng thái.
            var c = context
                .HttpContext.RequestServices.GetRequiredService<StoreService>()
                .CurrentAccount(id);
            if (
                c == null
                || !c.Active
                || c.Role != context.Principal!.FindFirstValue(ClaimTypes.Role)
                || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(c.PasswordHash)))
                    != context.Principal.FindFirstValue("security_stamp")
            )
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );
            }
        }
);

// BƯỚC 2 - TẠO ỨNG DỤNG: service đã đăng ký xong; bên dưới tách tác vụ quản trị dòng lệnh khỏi

// luồng phục vụ
// website.
// BIẾN app (WebApplication): ứng dụng web đã Build, dùng cấu hình pipeline/route và bắt đầu nhận
// HTTP ở Run.
var app = builder.Build();

// Explicit administration commands only. Normal startup never creates or replaces data.
// LỆNH XUẤT SCHEMA: --sql-script chỉ in SQL tạo schema từ EF rồi kết thúc; không tự thực thi SQL
// lên database.
if (args.Contains("--sql-script"))
{
    // BIẾN db (StoreDbContext): DbContext riêng cho lần đọc/ghi này; using bảo đảm giải phóng kết
    // nối khi kết thúc phạm vi.
    using var db = app
        .Services.GetRequiredService<IDbContextFactory<StoreDbContext>>()
        .CreateDbContext();
    Console.Write(db.Database.GenerateCreateScript());
    return;
}

// LỆNH QUẢN TRỊ CHỦ ĐỘNG: kiểm tra schema đã triển khai rồi mới seed/import/hash theo tham số.

// --init-db ở đây
// kiểm tra database; không tự tạo schema thiếu từ EF.
if (
    args.Contains("--init-db")
    || args.Contains("--seed-demo")
    || args.Contains("--import-json")
    || args.Contains("--hash-passwords")
)
{
    // BIẾN db (StoreDbContext): DbContext riêng cho lần đọc/ghi này; using bảo đảm giải phóng kết
    // nối khi kết thúc phạm vi.
    using var db = app
        .Services.GetRequiredService<IDbContextFactory<StoreDbContext>>()
        .CreateDbContext();
    // Deployment SQL is authoritative: never create a partial schema from the EF model.
    if (!db.Database.CanConnect())
        throw new InvalidOperationException(
            "Cannot connect to the configured database. Deploy delivery/Deploy.sql first."
        );
    db.Database.ExecuteSqlRaw(
        (
            "IF OBJECT_ID(N'dbo.QOANH_Configuration',N'U') IS NULL "
            + "OR COL_LENGTH(N'dbo.DonHang',N'QCheckoutToken') IS NULL "
            + "OR OBJECT_ID(N'dbo.QOANH_ChatThreads',N'U') IS NULL "
            + "THROW 51001, N'Missing QOANH schema. Run "
            + "delivery/Deploy.sql for a new database, or "
            + "04-qoanh-extensions.sql and "
            + "07-customer-finance-features.sql for an existing "
            + "original database.', 1;"
        )
    );
    // BIẾN store (StoreService): StoreService.
    var store = app.Services.GetRequiredService<StoreService>();
    if (args.Contains("--hash-passwords"))
        Console.WriteLine("Converted legacy password records: " + store.HashLegacyPasswords());
    if (args.Contains("--seed-demo"))
        store.SeedDemo();
    // BIẾN importIndex (int): vị trí tham số --import-json trong args; -1 nghĩa là không yêu cầu
    // nhập JSON.
    int importIndex = Array.IndexOf(args, "--import-json");
    if (importIndex >= 0)
    {
        if (importIndex + 1 >= args.Length)
            throw new ArgumentException("Supply JSON path after --import-json");
        store.ImportJson(args[importIndex + 1]);
    }

    Console.WriteLine("Database operation completed.");
    return;
}

// XỬ LÝ LỖI NGOÀI DEVELOPMENT: dùng trang Home/Error và HSTS thay vì để thông tin lỗi kỹ thuật đi

// thẳng ra giao
// diện.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTP launch profile remains usable locally; HTTPS deployments keep redirection.
if (
    app.Configuration["HTTPS_PORT"] != null
    || app.Configuration["ASPNETCORE_HTTPS_PORT"] != null
    || app.Urls.Any(u => u.StartsWith("https://"))
)
    app.UseHttpsRedirection();

// BƯỚC 3 - PIPELINE: guard schema → file tĩnh → xác định route → giới hạn yêu cầu → Session → xác

// thực cookie →
// phân quyền. Thứ tự này để action có route, Session và User trước khi chạy.
app.UseMiddleware<DatabaseSchemaGuard>();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ĐỊNH TUYẾN MẶC ĐỊNH: /{controller}/{action}/{id?}; thiếu controller/action thì dùng Home/Index.

// Vì vậy
// /Admin/Product?id=1 vào overload GET, còn form POST cùng đường dẫn vào overload có HttpPost.
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// BƯỚC 4 - CHẠY SERVER: bắt đầu nhận yêu cầu HTTP theo các cấu hình và middleware đã thiết lập.
app.Run();
