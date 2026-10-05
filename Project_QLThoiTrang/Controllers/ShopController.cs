using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
public class ShopController(StoreService store):Controller
{
 int CustomerId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public IActionResult Index(string? q,string? category,string sort="new",string? collection=null)
 {
  var vm=store.Read(d=>{var products=d.Products.Where(p=>p.Active);if(!string.IsNullOrWhiteSpace(q))products=products.Where(p=>p.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||p.Color.Contains(q,StringComparison.OrdinalIgnoreCase));if(!string.IsNullOrEmpty(category))products=products.Where(p=>p.Category==category);if(collection=="new")products=products.Where(p=>p.IsNew);if(collection=="sale")products=products.Where(p=>p.OnSale);products=sort switch{"price-asc"=>products.OrderBy(p=>p.Price),"price-desc"=>products.OrderByDescending(p=>p.Price),"name"=>products.OrderBy(p=>p.Name),_=>products.OrderByDescending(p=>p.Id)};return new CatalogViewModel{Products=products.ToList(),Categories=d.Categories,Query=q,Category=category,Sort=sort,Collection=collection};});return View(vm);
 }
 public IActionResult Detail(int id){var p=store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id&&p.Active));if(p==null)return NotFound();ViewBag.Related=store.Read(d=>d.Products.Where(x=>x.Active&&x.Id!=id&&x.Category==p.Category).Take(4).ToList());return View(p);}
 public IActionResult Wishlist(){var ids=SessionStore.Get<List<int>>(HttpContext.Session,"wishlist");return View(store.Read(d=>d.Products.Where(p=>p.Active&&ids.Contains(p.Id)).ToList()));}
 [HttpPost] public IActionResult Favorite(int id,string? returnUrl)
 {
  if(!store.Read(d=>d.Products.Any(p=>p.Id==id&&p.Active)))return NotFound();var ids=SessionStore.Get<List<int>>(HttpContext.Session,"wishlist");if(!ids.Remove(id))ids.Add(id);SessionStore.Set(HttpContext.Session,"wishlist",ids);TempData["Success"]="Đã cập nhật danh sách yêu thích.";return LocalRedirect(Url.IsLocalUrl(returnUrl)?returnUrl!:"/Shop/Wishlist");
 }
 public IActionResult Cart()=>View(SessionStore.Cart(HttpContext.Session,store));
 [HttpPost] public IActionResult Add(int id,string size="M",int quantity=1)
 {
  var p=store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id&&p.Active));if(p==null)return NotFound();var items=SessionStore.Get<List<CartItem>>(HttpContext.Session,"cart");var existing=items.FirstOrDefault(i=>i.ProductId==id&&i.Size==size);
  if(!new[]{"S","M","L","XL"}.Contains(size)||quantity<1||quantity>20||items.Where(i=>i.ProductId==id).Sum(i=>i.Quantity)+quantity>p.Stock){TempData["Error"]="Kích cỡ hoặc số lượng không hợp lệ; hãy kiểm tra tồn kho.";return RedirectToAction(nameof(Detail),new{id});}
  if(existing==null)items.Add(new CartItem{ProductId=id,Size=size,Quantity=quantity});else existing.Quantity+=quantity;SessionStore.Set(HttpContext.Session,"cart",items);TempData["Success"]="Đã thêm sản phẩm vào giỏ.";return RedirectToAction(nameof(Cart));
 }
 [HttpPost] public IActionResult UpdateCart(int id,string size,int quantity,bool remove=false)
 {
  var items=SessionStore.Get<List<CartItem>>(HttpContext.Session,"cart");var item=items.FirstOrDefault(i=>i.ProductId==id&&i.Size==size);if(item==null)return RedirectToAction(nameof(Cart));var p=store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id&&p.Active));
  if(remove||quantity==0)items.Remove(item);else if(p==null||quantity<1||quantity>20||quantity+items.Where(i=>i.ProductId==id&&i!=item).Sum(i=>i.Quantity)>p.Stock)TempData["Error"]="Số lượng vượt tồn kho hoặc giới hạn 20 sản phẩm mỗi dòng.";else item.Quantity=quantity;SessionStore.Set(HttpContext.Session,"cart",items);return RedirectToAction(nameof(Cart));
 }
 [Authorize] public IActionResult Checkout()
 {
  if(SessionStore.Cart(HttpContext.Session,store).Lines.Count==0)return RedirectToAction(nameof(Cart));var c=store.Read(d=>d.Customers.First(c=>c.Id==CustomerId));ViewBag.Cart=SessionStore.Cart(HttpContext.Session,store);return View(new CheckoutInput{Name=c.Name,Phone=c.Phone,Address=c.Address});
 }
 [Authorize,HttpPost] public IActionResult Checkout(CheckoutInput input)
 {
  var cart=SessionStore.Cart(HttpContext.Session,store);ViewBag.Cart=cart;if(cart.Lines.Count==0){ModelState.AddModelError("","Giỏ hàng đang trống.");return View(input);}if(!ModelState.IsValid)return View(input);
  try{var order=store.Write(d=>{foreach(var group in cart.Lines.GroupBy(l=>l.ProductId)){var p=d.Products.FirstOrDefault(p=>p.Id==group.Key&&p.Active);if(p==null||group.Sum(l=>l.Quantity)>p.Stock)throw new InvalidOperationException("Có sản phẩm hết hàng hoặc không đủ tồn kho. Kiểm tra lại giỏ hàng.");if(group.Any(l=>l.Price!=p.Price))throw new InvalidOperationException("Giá sản phẩm đã thay đổi. Quay lại giỏ để kiểm tra giá mới.");}var o=new Order{Id="QH-"+Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),CustomerId=CustomerId,Name=input.Name.Trim(),Phone=input.Phone.Trim(),Address=input.Address.Trim(),Note=input.Note.Trim(),Lines=cart.Lines};foreach(var line in o.Lines)d.Products.First(p=>p.Id==line.ProductId).Stock-=line.Quantity;d.Orders.Add(o);return o;});SessionStore.Set(HttpContext.Session,"cart",new List<CartItem>());return RedirectToAction(nameof(Success),new{id=order.Id});}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);return View(input);}
 }
 [Authorize] public IActionResult Success(string id){var o=store.Read(d=>d.Orders.FirstOrDefault(o=>o.Id==id&&o.CustomerId==CustomerId));return o==null?NotFound():View(o);}
}

