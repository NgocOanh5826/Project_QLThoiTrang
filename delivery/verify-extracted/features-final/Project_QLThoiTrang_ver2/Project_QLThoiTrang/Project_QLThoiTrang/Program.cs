using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;
using Project_QLThoiTrang.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(options=>options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSingleton<StoreService>();
builder.Services.AddSingleton<PdfDocuments>();
PdfSharp.Fonts.GlobalFontSettings.FontResolver=new DocumentFontResolver(builder.Configuration["Pdf:FontDirectory"]??Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
builder.Services.AddTransient<IMailSender,SmtpMailSender>();
builder.Services.AddTransient<PasswordRecovery>();
builder.Services.AddHttpClient<ShippingService>(c=>c.Timeout=TimeSpan.FromSeconds(25));
builder.Services.AddDbContextFactory<StoreDbContext>(options=>options.UseSqlServer(builder.Configuration.GetConnectionString("Store")??throw new InvalidOperationException("Missing ConnectionStrings:Store")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options=>{options.Cookie.HttpOnly=true;options.Cookie.IsEssential=true;options.IdleTimeout=TimeSpan.FromHours(2);});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options=>{options.LoginPath="/Account/Login";options.AccessDeniedPath="/Account/Denied";options.ExpireTimeSpan=TimeSpan.FromHours(8);});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddPolicy("login",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
builder.Services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,options=>options.Events.OnValidatePrincipal=async context=>
{
 if(!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id)){context.RejectPrincipal();return;}
 var c=context.HttpContext.RequestServices.GetRequiredService<StoreService>().CurrentAccount(id);
 if(c==null||!c.Active||c.Role!=context.Principal!.FindFirstValue(ClaimTypes.Role)||Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(c.PasswordHash)))!=context.Principal.FindFirstValue("security_stamp")){context.RejectPrincipal();await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);}
});
var app=builder.Build();
// Explicit administration commands only. Normal startup never creates or replaces data.
if(args.Contains("--sql-script"))
{
 using var db=app.Services.GetRequiredService<IDbContextFactory<StoreDbContext>>().CreateDbContext();
 Console.Write(db.Database.GenerateCreateScript());return;
}
if(args.Contains("--init-db")||args.Contains("--seed-demo")||args.Contains("--import-json")||args.Contains("--hash-passwords"))
{
 using var db=app.Services.GetRequiredService<IDbContextFactory<StoreDbContext>>().CreateDbContext();
 // Deployment SQL is authoritative: never create a partial schema from the EF model.
 if(!db.Database.CanConnect())throw new InvalidOperationException("Cannot connect to the configured database. Deploy delivery/Deploy.sql first.");
 db.Database.ExecuteSqlRaw("IF OBJECT_ID(N'dbo.QOANH_Configuration',N'U') IS NULL OR COL_LENGTH(N'dbo.DonHang',N'QCheckoutToken') IS NULL OR OBJECT_ID(N'dbo.QOANH_ChatThreads',N'U') IS NULL THROW 51001, N'Missing QOANH schema. Run delivery/Deploy.sql for a new database, or 04-qoanh-extensions.sql and 07-customer-finance-features.sql for an existing original database.', 1;");
 var store=app.Services.GetRequiredService<StoreService>();
 if(args.Contains("--hash-passwords"))Console.WriteLine("Converted legacy password records: "+store.HashLegacyPasswords());
 if(args.Contains("--seed-demo"))store.SeedDemo();
 int importIndex=Array.IndexOf(args,"--import-json");
 if(importIndex>=0){if(importIndex+1>=args.Length)throw new ArgumentException("Supply JSON path after --import-json");store.ImportJson(args[importIndex+1]);}
 Console.WriteLine("Database operation completed.");return;
}
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Home/Error");app.UseHsts();}
// HTTP launch profile remains usable locally; HTTPS deployments keep redirection.
if(app.Configuration["HTTPS_PORT"]!=null||app.Configuration["ASPNETCORE_HTTPS_PORT"]!=null||app.Urls.Any(u=>u.StartsWith("https://")))app.UseHttpsRedirection();
app.UseStaticFiles();app.UseRouting();app.UseRateLimiter();app.UseSession();app.UseAuthentication();app.UseAuthorization();
app.MapControllerRoute(name:"default",pattern:"{controller=Home}/{action=Index}/{id?}");
app.Run();
