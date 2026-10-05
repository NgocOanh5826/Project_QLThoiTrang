using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(options=>options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSingleton<StoreService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options=>{options.Cookie.HttpOnly=true;options.Cookie.IsEssential=true;options.IdleTimeout=TimeSpan.FromHours(2);});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options=>{options.LoginPath="/Account/Login";options.AccessDeniedPath="/Account/Denied";options.ExpireTimeSpan=TimeSpan.FromHours(8);});
builder.Services.AddAuthorization();
var app=builder.Build();
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Home/Error");app.UseHsts();}
// HTTP launch profile remains usable locally; HTTPS deployments keep redirection.
if(app.Configuration["HTTPS_PORT"]!=null||app.Configuration["ASPNETCORE_HTTPS_PORT"]!=null||app.Urls.Any(u=>u.StartsWith("https://")))app.UseHttpsRedirection();
app.UseStaticFiles();app.UseRouting();app.UseSession();app.UseAuthentication();app.UseAuthorization();
app.MapControllerRoute(name:"default",pattern:"{controller=Home}/{action=Index}/{id?}");
app.Run();
