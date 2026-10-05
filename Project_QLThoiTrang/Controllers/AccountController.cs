using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
public class AccountController(StoreService store):Controller
{
 int CustomerId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public IActionResult Login(string? returnUrl)=>View(new LoginInput{ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl:null});
 [HttpPost] public async Task<IActionResult> Login(LoginInput input)
 {
  input.Email=input.Email?.Trim()??"";input.ReturnUrl=Url.IsLocalUrl(input.ReturnUrl)?input.ReturnUrl:null;
  if(!ModelState.IsValid)return View(input);var customer=store.Authenticate(input.Email,input.Password);if(customer==null){ModelState.AddModelError("","Email hoặc mật khẩu chưa đúng. Kiểm tra thông tin và thử lại.");return View(input);}await SignIn(customer,input.RememberMe);return LocalRedirect(input.ReturnUrl??(customer.Role=="Admin"?"/Admin":"/Account"));
 }
 public IActionResult Register(string? returnUrl)=>View(new RegisterInput{ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl:null});
 [HttpPost] public async Task<IActionResult> Register(RegisterInput input)
 {
  input.Name=input.Name?.Trim()??"";input.Email=input.Email?.Trim()??"";input.ReturnUrl=Url.IsLocalUrl(input.ReturnUrl)?input.ReturnUrl:null;
  if(!ModelState.IsValid)return View(input);try{var customer=store.Register(input);await SignIn(customer);return LocalRedirect(input.ReturnUrl??Url.Action(nameof(Index))!);}catch(InvalidOperationException ex){ModelState.AddModelError(nameof(input.Email),ex.Message);return View(input);}
 }
 async Task SignIn(Customer c,bool rememberMe=false)=>await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,c.Id.ToString()),new Claim(ClaimTypes.Name,c.Name),new Claim(ClaimTypes.Email,c.Email),new Claim(ClaimTypes.Role,c.Role)},CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=rememberMe,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(rememberMe?72:8)});
 [Authorize,HttpPost] public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync();return RedirectToAction("Index","Home");}
 [Authorize] public IActionResult Index(){var c=store.Read(d=>d.Customers.First(c=>c.Id==CustomerId));ViewBag.Email=c.Email;return View(new ProfileInput{Name=c.Name,Phone=c.Phone,Address=c.Address});}
 [Authorize,HttpPost] public async Task<IActionResult> Index(ProfileInput input)
 {
  ViewBag.Email=User.FindFirstValue(ClaimTypes.Email);if(!ModelState.IsValid)return View(input);var c=store.Write(d=>{var c=d.Customers.First(c=>c.Id==CustomerId);c.Name=input.Name.Trim();c.Phone=input.Phone.Trim();c.Address=input.Address.Trim();return c;});await SignIn(c);TempData["Success"]="Đã cập nhật hồ sơ.";return RedirectToAction(nameof(Index));
 }
 [Authorize] public IActionResult Orders()=>View(store.Read(d=>d.Orders.Where(o=>o.CustomerId==CustomerId).OrderByDescending(o=>o.Created).ToList()));
 [Authorize] public IActionResult Order(string id){var o=store.Read(d=>d.Orders.FirstOrDefault(o=>o.Id==id&&o.CustomerId==CustomerId));return o==null?NotFound():View(o);}
 [Authorize,HttpPost] public IActionResult Cancel(string id)
 {
  store.Write(d=>{var o=d.Orders.FirstOrDefault(o=>o.Id==id&&o.CustomerId==CustomerId);if(o?.Status=="Chờ xác nhận"){o.Status="Đã hủy";foreach(var line in o.Lines){var p=d.Products.FirstOrDefault(p=>p.Id==line.ProductId);if(p!=null)p.Stock+=line.Quantity;}}return 0;});TempData["Success"]="Đã xử lý yêu cầu hủy; chỉ đơn chờ xác nhận được hủy.";return RedirectToAction(nameof(Order),new{id});
 }
 public IActionResult Denied()=>View();
}
