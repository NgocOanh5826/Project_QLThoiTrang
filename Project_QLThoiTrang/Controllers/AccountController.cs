using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
public partial class AccountController(StoreService store):Controller
{
 int CustomerId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public IActionResult Login(string? returnUrl)=>View(new LoginInput{ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl:null});
 [HttpPost,EnableRateLimiting("login")] public async Task<IActionResult> Login(LoginInput input)
 {
  input.Email=input.Email?.Trim()??"";input.ReturnUrl=Url.IsLocalUrl(input.ReturnUrl)?input.ReturnUrl:null;
  if(!ModelState.IsValid)return View(input);var customer=store.Authenticate(input.Email,input.Password,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());if(customer==null){ModelState.AddModelError("","Email hoặc mật khẩu chưa đúng. Kiểm tra thông tin và thử lại.");return View(input);}await SignIn(customer,input.RememberMe,true);return LocalRedirect(input.ReturnUrl??(customer.Role!="Customer"?"/Admin":"/Account"));
 }
 public IActionResult Register(string? returnUrl)=>View(new RegisterInput{ReturnUrl=Url.IsLocalUrl(returnUrl)?returnUrl:null});
 [HttpPost,EnableRateLimiting("login")] public async Task<IActionResult> Register(RegisterInput input)
 {
  input.Name=input.Name?.Trim()??"";input.Email=input.Email?.Trim()??"";input.ReturnUrl=Url.IsLocalUrl(input.ReturnUrl)?input.ReturnUrl:null;
  if(!ModelState.IsValid)return View(input);try{var customer=store.Register(input);await SignIn(customer,mergeShopping:true);return LocalRedirect(input.ReturnUrl??Url.Action(nameof(Index))!);}catch(InvalidOperationException ex){ModelState.AddModelError(nameof(input.Email),ex.Message);return View(input);}
 }
 async Task SignIn(Customer c,bool rememberMe=false,bool mergeShopping=false)
 {
  if(c.LoginHistoryId==0)c.LoginHistoryId=store.LogLogin(c,true,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());
  await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,c.Id.ToString()),new Claim(ClaimTypes.Name,c.Name),new Claim(ClaimTypes.Email,c.Email),new Claim(ClaimTypes.Role,c.Role),new Claim("login_history",c.LoginHistoryId.ToString()),new Claim("security_stamp",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(c.PasswordHash))))},CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=rememberMe,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(rememberMe?72:8)});
  if(c.Role=="Customer"&&mergeShopping)store.MergeGuestShopping(c.Id,SessionStore.Get<List<CartItem>>(HttpContext.Session,"cart"),SessionStore.Get<List<int>>(HttpContext.Session,"wishlist"));
 }
 [Authorize,HttpPost] public async Task<IActionResult> Logout(){if(long.TryParse(User.FindFirstValue("login_history"),out var history))store.EndLogin(history,CustomerId);HttpContext.Session.Clear();await HttpContext.SignOutAsync();return RedirectToAction("Index","Home");}
 [Authorize] public IActionResult Index(){var c=store.Read(d=>d.Customers.First(c=>c.Id==CustomerId));ViewBag.Email=c.Email;return View(new ProfileInput{Name=c.Name,Phone=c.Phone,Address=c.Address});}
 [Authorize,HttpPost] public async Task<IActionResult> Index(ProfileInput input)
 {
  ViewBag.Email=User.FindFirstValue(ClaimTypes.Email);if(!ModelState.IsValid)return View(input);
  try
  {
   var c=store.Write(d=>{var c=d.Customers.First(c=>c.Id==CustomerId);c.Name=input.Name.Trim();c.Phone=input.Phone?.Trim()??"";c.Address=input.Address?.Trim()??"";return c;});
   await SignIn(c);TempData["Success"]="Đã cập nhật hồ sơ.";return RedirectToAction(nameof(Index));
  }
  catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);return View(input);}
 }
 [Authorize] public IActionResult Orders()=>View(store.Read(d=>d.Orders.Where(o=>o.CustomerId==CustomerId).OrderByDescending(o=>o.Created).ToList()));
 [Authorize] public IActionResult Order(string id){var o=store.Read(d=>d.Orders.FirstOrDefault(o=>o.Id==id&&o.CustomerId==CustomerId));ViewBag.Return=store.Read(d=>d.Returns.FirstOrDefault(r=>r.OrderId==id&&r.CustomerId==CustomerId));return o==null?NotFound():View(o);}
 [Authorize,HttpPost] public IActionResult Cancel(string id)
 {
  try{store.ChangeOrderStatus(id,"Đã hủy",CustomerId,true);TempData["Success"]="Đã hủy đơn hàng.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Order),new{id});
 }
 [Authorize,HttpPost] public IActionResult Return(string id,string reason,int lineId=0,int quantity=0)
 {try{store.RequestReturn(id,reason,CustomerId,lineId,quantity);TempData["Success"]="Đã gửi yêu cầu trả hàng.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Order),new{id});}
 [Authorize] public IActionResult Password()=>View(new PasswordInput());
 [Authorize,HttpPost] public IActionResult Password(PasswordInput input)
 {if(!ModelState.IsValid)return View(input);try{store.ChangePassword(CustomerId,input);TempData["Success"]="Đã đổi mật khẩu. Vui lòng đăng nhập lại.";return RedirectToAction(nameof(Index));}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);return View(input);}}
 public IActionResult Denied()=>View();
}
