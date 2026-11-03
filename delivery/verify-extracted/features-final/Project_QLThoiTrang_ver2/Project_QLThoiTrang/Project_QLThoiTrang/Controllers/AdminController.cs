using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
[Authorize(Roles="Admin,Catalog,Inventory,Sales,Support")]
public partial class AdminController(StoreService store):Controller
{
 int ActorId=>int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
 public IActionResult Index()=>View(store.Read(d=>d));
 public IActionResult Products(string? q){ViewBag.Query=q;return View(store.Read(d=>d.Products.Where(p=>string.IsNullOrWhiteSpace(q)||p.Name.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList()));}
 public IActionResult Product(int? id){ViewBag.Brands=store.References("brands");ViewBag.Categories=store.Read(d=>d.Categories);var p=id==null?new Product():store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id));return p==null?NotFound():View(p);}
 [HttpPost] public IActionResult Product(Product input)
 {
  ViewBag.Brands=store.References("brands");ViewBag.Categories=store.Read(d=>d.Categories);if(!((List<string>)ViewBag.Categories).Contains(input.Category))ModelState.AddModelError("Category","Danh mục không hợp lệ.");if(!Enumerable.Range(1,5).Any(i=>input.Image==$"/images/products/aothun{i:00}.jpg"))ModelState.AddModelError("Image","Chọn ảnh có sẵn trong thư viện.");if(!ModelState.IsValid)return View(input);
  bool saved=store.Write(d=>{if(input.Id==0){input.Id=d.Products.Select(p=>p.Id).DefaultIfEmpty().Max()+1;input.Stock=0;d.Products.Add(input);return true;}var index=d.Products.FindIndex(p=>p.Id==input.Id);if(index<0)return false;input.Stock=d.Products[index].Stock;d.Products[index]=input;return true;});if(!saved)return NotFound();TempData["Success"]="Đã lưu sản phẩm.";return RedirectToAction(nameof(Products));
 }
 public IActionResult DeleteProduct(int id){var p=store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id));return p==null?NotFound():View(p);}
 [HttpPost,ActionName("DeleteProduct")] public IActionResult DeleteProductConfirmed(int id){store.Write(d=>{var p=d.Products.FirstOrDefault(p=>p.Id==id);if(p!=null)p.Active=false;return 0;});TempData["Success"]="Đã ẩn sản phẩm khỏi cửa hàng; lịch sử đơn hàng được giữ lại.";return RedirectToAction(nameof(Products));}
 public IActionResult Categories()=>View(store.Read(d=>d));
 public IActionResult Category()=>View("Category", "");
 [HttpPost] public IActionResult AddCategory(string name)=>Category(name);
 [HttpPost] public IActionResult Category(string name)
 {name=(name??"").Trim();if(name.Length is <2 or >50)ModelState.AddModelError("","Tên danh mục cần 2–50 ký tự.");if(ModelState.IsValid){bool added=store.Write(d=>{if(d.Categories.Contains(name,StringComparer.OrdinalIgnoreCase))return false;d.Categories.Add(name);return true;});if(added){TempData["Success"]="Đã thêm danh mục.";return RedirectToAction(nameof(Categories));}ModelState.AddModelError("","Danh mục đã tồn tại.");}return View("Category",name);}
 [HttpPost] public IActionResult DeleteCategory(string name){store.Write(d=>{if(d.Products.Any(p=>p.Category==name))TempData["Error"]="Danh mục đang chứa sản phẩm; chuyển sản phẩm trước khi xóa.";else d.Categories.Remove(name);return 0;});return RedirectToAction(nameof(Categories));}
 public IActionResult Orders(string? status){ViewBag.Status=status;return View(store.Read(d=>d.Orders.Where(o=>string.IsNullOrEmpty(status)||o.Status==status).OrderByDescending(o=>o.Created).ToList()));}
 public IActionResult Order(string id){var o=store.Read(d=>d.Orders.FirstOrDefault(o=>o.Id==id));return o==null?NotFound():View(o);}
 [HttpPost] public IActionResult OrderStatus(string id,string status)
 {
  try{store.ChangeOrderStatus(id,status,ActorId);TempData["Success"]="Đã cập nhật đơn hàng.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Order),new{id});
 }
 public IActionResult Customers(string? q){ViewBag.Query=q;return View(store.Read(d=>d.Customers.Where(c=>c.Role=="Customer"&&(string.IsNullOrWhiteSpace(q)||c.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||c.Email.Contains(q,StringComparison.OrdinalIgnoreCase))).ToList()));}
 public IActionResult Customer(int id){var c=store.Read(d=>d.Customers.FirstOrDefault(c=>c.Id==id));if(c==null)return NotFound();ViewBag.Orders=store.Read(d=>d.Orders.Where(o=>o.CustomerId==id).ToList());return View(c);}
 public IActionResult Inventory(){ViewBag.Movements=store.Read(d=>d.StockMovements.OrderByDescending(m=>m.Id).Take(200).ToList());return View(store.Read(d=>d.Products));}
 [HttpPost] public IActionResult Inventory(int id,int quantity,int expected,string reason)
 {try{store.AdjustStock(id,quantity,expected,reason,ActorId);TempData["Success"]="Đã ghi kiểm kê.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Inventory));}
 public IActionResult Receive(string tab="single"){ViewBag.ReceiveTab=tab=="many"?"many":"single";ReceiveOptions();return View(new ReceiveInput());}
 [HttpPost] public IActionResult Receive(ReceiveInput input)
 {ReceiveOptions();if(!ModelState.IsValid)return View(input);try{store.Receive(input,ActorId);TempData["Success"]="Đã nhập kho.";return RedirectToAction(nameof(Inventory));}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);return View(input);}}
 [HttpPost] public IActionResult Payment(string id,decimal amount,string reference)
 {try{store.RecordPayment(id,amount,reference,ActorId);TempData["Success"]="Đã ghi nhận tiền thanh toán.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Order),new{id});}
 public IActionResult Returns()=>View(store.Read(d=>d.Returns.OrderByDescending(r=>r.Id).ToList()));
 [HttpPost] public IActionResult ResolveReturn(int id,bool approve,string resolution,string reference)
 {try{store.ResolveReturn(id,approve,resolution,reference,ActorId);TempData["Success"]="Đã xử lý trả hàng.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(ReturnRequest),new{id});}
 public IActionResult Promotions()=>View(store.Read(d=>d.Promotions));
 public IActionResult Promotion(int? id){ViewBag.Products=store.Read(d=>d.Products);var p=id==null?new Promotion():store.Read(d=>d.Promotions.FirstOrDefault(p=>p.Id==id));return p==null?NotFound():View(p);}
 [HttpPost] public IActionResult Promotion(Promotion input)
 {
  ViewBag.Products=store.Read(d=>d.Products);if(input.From>input.Until)ModelState.AddModelError("From","Invalid date range.");if(input.Kind is not ("PHANTRAM" or "SOTIEN")||input.Value<=0||(input.Kind=="PHANTRAM"&&input.Value>100)||input.Minimum<0||input.Maximum<0||input.Remaining<0)ModelState.AddModelError("Value","Invalid promotion conditions.");if(input.ProductIds.Any(id=>!((List<Product>)ViewBag.Products).Any(p=>p.Id==id)))ModelState.AddModelError("ProductIds","Invalid products.");input.Code=input.Code?.Trim().ToUpperInvariant()??"";if(input.Until.Date<DateTime.Today)ModelState.AddModelError("Until","Ngày kết thúc không được ở quá khứ.");if(store.Read(d=>d.Promotions.Any(p=>p.Id!=input.Id&&p.Code==input.Code)))ModelState.AddModelError("Code","Mã đã tồn tại.");if(!ModelState.IsValid)return View(input);store.Write(d=>{if(input.Id==0){input.Id=d.Promotions.Select(p=>p.Id).DefaultIfEmpty().Max()+1;d.Promotions.Add(input);}else{var index=d.Promotions.FindIndex(p=>p.Id==input.Id);if(index>=0)d.Promotions[index]=input;}return 0;});TempData["Success"]="Đã lưu chương trình. Mã ưu đãi áp dụng tại bước đặt hàng.";return RedirectToAction(nameof(Promotions));
 }
 public IActionResult Reports(DateTime? from,DateTime? to){var start=from?.Date??DateTime.Today.AddDays(-30);var end=to?.Date??DateTime.Today;ViewBag.From=start;ViewBag.To=end;if(start>end){TempData["Error"]="Khoảng ngày không hợp lệ.";return View(new List<Order>());}return View(store.Read(d=>d.Orders.Where(o=>o.Created.Date>=start&&o.Created.Date<=end).ToList()));}
 public IActionResult Settings()=>View(store.Read(d=>d.Settings));
 [HttpPost] public IActionResult Settings(StoreSettings input){if(!ModelState.IsValid)return View(input);store.Write(d=>{d.Settings=input;return 0;});TempData["Success"]="Đã lưu thông tin cửa hàng.";return RedirectToAction(nameof(Settings));}
 public IActionResult Export(){var csv=store.Read(d=>"Mã đơn,Ngày,Trạng thái,Tổng tiền\n"+string.Join("\n",d.Orders.Select(o=>$"{o.Id},{o.Created:yyyy-MM-dd},{o.Status},{o.Total}")));return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(),"text/csv","qoanh-orders.csv");}
}
