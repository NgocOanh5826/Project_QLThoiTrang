using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
public partial class ShopController(StoreService store):Controller
{
 int CustomerId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public IActionResult Index(string? q,string? category,string sort="new",string? collection=null)
 {
  var vm=store.Read(d=>{var products=d.Products.Where(p=>p.Active);if(!string.IsNullOrWhiteSpace(q))products=products.Where(p=>p.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||p.Color.Contains(q,StringComparison.OrdinalIgnoreCase));if(!string.IsNullOrEmpty(category))products=products.Where(p=>p.Category==category);if(collection=="new")products=products.Where(p=>p.IsNew);if(collection=="sale")products=products.Where(p=>p.OnSale);products=sort switch{"price-asc"=>products.OrderBy(p=>p.Price),"price-desc"=>products.OrderByDescending(p=>p.Price),"name"=>products.OrderBy(p=>p.Name),_=>products.OrderByDescending(p=>p.Id)};return new CatalogViewModel{Products=products.ToList(),Categories=d.Categories,Query=q,Category=category,Sort=sort,Collection=collection};});return View(vm);
 }
 public IActionResult Detail(int id){var p=store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id&&p.Active));if(p==null)return NotFound();ViewBag.Reviews=store.Reviews(id);ViewBag.Related=store.Read(d=>d.Products.Where(x=>x.Active&&x.Id!=id&&x.Category==p.Category).Take(4).ToList());return View(p);}
 public IActionResult Wishlist(){var ids=SessionStore.Get<List<int>>(HttpContext.Session,"wishlist");return View(store.Read(d=>d.Products.Where(p=>p.Active&&ids.Contains(p.Id)).ToList()));}
 [HttpPost] public IActionResult Favorite(int id,string? returnUrl)
 {
  if(!store.Read(d=>d.Products.Any(p=>p.Id==id&&p.Active)))return NotFound();var ids=SessionStore.Get<List<int>>(HttpContext.Session,"wishlist");if(!ids.Remove(id))ids.Add(id);SessionStore.Set(HttpContext.Session,"wishlist",ids);TempData["Success"]="Đã cập nhật danh sách yêu thích.";return LocalRedirect(Url.IsLocalUrl(returnUrl)?returnUrl!:"/Shop/Wishlist");
 }
 public IActionResult Cart()=>View(SessionStore.Cart(HttpContext.Session,store));
 [HttpPost] public IActionResult Add(int id,string size="M",int quantity=1,int variantId=0)
 {
  var product=store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id&&p.Active));if(product==null)return NotFound();
  var variant=product.Variants.FirstOrDefault(v=>v.Active&&(variantId>0?v.Id==variantId:v.Size==size));
  var items=SessionStore.Get<List<CartItem>>(HttpContext.Session,"cart");
  if(variant==null||quantity<1||quantity>20||items.Where(i=>i.VariantId==variant.Id).Sum(i=>i.Quantity)+quantity>Math.Min(20,variant.Stock)){TempData["Error"]="Biến thể hoặc số lượng không hợp lệ; hãy kiểm tra tồn kho.";return RedirectToAction(nameof(Detail),new{id});}
  var existing=items.FirstOrDefault(i=>i.VariantId==variant.Id);if(existing==null)items.Add(new(){ProductId=id,VariantId=variant.Id,Size=variant.Size,Color=variant.Color,Quantity=quantity});else existing.Quantity+=quantity;
  SessionStore.Set(HttpContext.Session,"cart",items);TempData["Success"]="Đã thêm sản phẩm vào giỏ.";return RedirectToAction(nameof(Cart));
 }
 [HttpPost] public IActionResult UpdateCart(int id,string size,int quantity,bool remove=false,int variantId=0)
 {
  var items=SessionStore.Get<List<CartItem>>(HttpContext.Session,"cart");var item=items.FirstOrDefault(i=>i.ProductId==id&&(variantId>0?i.VariantId==variantId:i.Size==size));if(item==null)return RedirectToAction(nameof(Cart));
  var variant=store.Read(d=>d.Variants.FirstOrDefault(v=>v.Id==item.VariantId&&v.Active));
  if(remove||quantity==0)items.Remove(item);else if(variant==null||quantity<1||quantity>20||quantity>variant.Stock)TempData["Error"]="Số lượng không hợp lệ hoặc vượt tồn kho.";else item.Quantity=quantity;
  SessionStore.Set(HttpContext.Session,"cart",items);return RedirectToAction(nameof(Cart));
 }
 [Authorize(Roles="Customer"),HttpPost] public IActionResult Review(int id,int stars,string text)
 {try{store.Review(CustomerId,id,stars,text);TempData["Success"]="Đã lưu đánh giá.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Detail),new{id});}
 [Authorize(Roles="Customer")] public IActionResult Checkout()
 {
  if(SessionStore.Cart(HttpContext.Session,store).Lines.Count==0)return RedirectToAction(nameof(Cart));var c=store.Read(d=>d.Customers.First(c=>c.Id==CustomerId));ViewBag.Cart=SessionStore.Cart(HttpContext.Session,store);var token=Guid.NewGuid().ToString("N");HttpContext.Session.SetString("checkout-token",token);SessionStore.Set(HttpContext.Session,"checkout-quote",((CartViewModel)ViewBag.Cart).Lines);return View(new CheckoutInput{Name=c.Name,Phone=c.Phone,Address=c.Address,CheckoutToken=token});
 }
 [Authorize(Roles="Customer"),HttpPost] public IActionResult Checkout(CheckoutInput input)
 {
  var cart=SessionStore.Cart(HttpContext.Session,store);ViewBag.Cart=cart;if(cart.Lines.Count==0){ModelState.AddModelError("","Giỏ hàng đang trống.");return View(input);}if(!ModelState.IsValid)return View(input);
  if(input.CheckoutToken!=HttpContext.Session.GetString("checkout-token")){ModelState.AddModelError("","Phiên đặt hàng đã thay đổi; mở lại trang thanh toán.");return View(input);}
  try{var order=store.PlaceOrder(CustomerId,input,SessionStore.Get<List<CartItem>>(HttpContext.Session,"cart"),SessionStore.Get<List<OrderLine>>(HttpContext.Session,"checkout-quote"));SessionStore.Set(HttpContext.Session,"cart",new List<CartItem>());return RedirectToAction(nameof(Success),new{id=order.Id});}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);return View(input);}
 }
 [Authorize] public IActionResult Success(string id){var o=store.Read(d=>d.Orders.FirstOrDefault(o=>o.Id==id&&o.CustomerId==CustomerId));return o==null?NotFound():View(o);}
}

