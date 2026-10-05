using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
[Authorize(Roles="Admin")]
public class AdminController(StoreService store):Controller
{
 public IActionResult Index()=>View(store.Read(d=>d));
 public IActionResult Products(string? q){ViewBag.Query=q;return View(store.Read(d=>d.Products.Where(p=>string.IsNullOrWhiteSpace(q)||p.Name.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList()));}
 public IActionResult Product(int? id){ViewBag.Categories=store.Read(d=>d.Categories);var p=id==null?new Product():store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id));return p==null?NotFound():View(p);}
 [HttpPost] public IActionResult Product(Product input)
 {
  ViewBag.Categories=store.Read(d=>d.Categories);if(!((List<string>)ViewBag.Categories).Contains(input.Category))ModelState.AddModelError("Category","Danh mục không hợp lệ.");if(!Enumerable.Range(1,5).Any(i=>input.Image==$"/images/products/aothun{i:00}.jpg"))ModelState.AddModelError("Image","Chọn ảnh có sẵn trong thư viện.");if(!ModelState.IsValid)return View(input);
  bool saved=store.Write(d=>{if(input.Id==0){input.Id=d.Products.Select(p=>p.Id).DefaultIfEmpty().Max()+1;d.Products.Add(input);return true;}var index=d.Products.FindIndex(p=>p.Id==input.Id);if(index<0)return false;d.Products[index]=input;return true;});if(!saved)return NotFound();TempData["Success"]="Đã lưu sản phẩm.";return RedirectToAction(nameof(Products));
 }
 public IActionResult DeleteProduct(int id){var p=store.Read(d=>d.Products.FirstOrDefault(p=>p.Id==id));return p==null?NotFound():View(p);}
 [HttpPost,ActionName("DeleteProduct")] public IActionResult DeleteProductConfirmed(int id){store.Write(d=>{var p=d.Products.FirstOrDefault(p=>p.Id==id);if(p!=null)p.Active=false;return 0;});TempData["Success"]="Đã ẩn sản phẩm khỏi cửa hàng; lịch sử đơn hàng được giữ lại.";return RedirectToAction(nameof(Products));}
 public IActionResult Categories()=>View(store.Read(d=>d));
 [HttpPost] public IActionResult AddCategory(string name){name=(name??"").Trim();if(name.Length is <2 or >50){TempData["Error"]="Tên danh mục cần 2–50 ký tự.";}else store.Write(d=>{if(d.Categories.Contains(name,StringComparer.OrdinalIgnoreCase))TempData["Error"]="Danh mục đã tồn tại.";else d.Categories.Add(name);return 0;});return RedirectToAction(nameof(Categories));}
 [HttpPost] public IActionResult DeleteCategory(string name){store.Write(d=>{if(d.Products.Any(p=>p.Category==name))TempData["Error"]="Danh mục đang chứa sản phẩm; chuyển sản phẩm trước khi xóa.";else d.Categories.Remove(name);return 0;});return RedirectToAction(nameof(Categories));}
 public IActionResult Orders(string? status){ViewBag.Status=status;return View(store.Read(d=>d.Orders.Where(o=>string.IsNullOrEmpty(status)||o.Status==status).OrderByDescending(o=>o.Created).ToList()));}
 public IActionResult Order(string id){var o=store.Read(d=>d.Orders.FirstOrDefault(o=>o.Id==id));return o==null?NotFound():View(o);}
 [HttpPost] public IActionResult OrderStatus(string id,string status)
 {
  store.Write(d=>{var o=d.Orders.FirstOrDefault(o=>o.Id==id);if(o==null)return 0;var allowed=o.Status switch{"Chờ xác nhận"=>new[]{"Đã xác nhận","Đã hủy"},"Đã xác nhận"=>new[]{"Đang giao","Đã hủy"},"Đang giao"=>new[]{"Đã giao"},_=>Array.Empty<string>()};if(!allowed.Contains(status)){TempData["Error"]="Không thể chuyển sang trạng thái này.";return 0;}o.Status=status;if(status=="Đã hủy")foreach(var line in o.Lines){var p=d.Products.FirstOrDefault(p=>p.Id==line.ProductId);if(p!=null)p.Stock+=line.Quantity;}TempData["Success"]="Đã cập nhật trạng thái đơn hàng.";return 0;});return RedirectToAction(nameof(Order),new{id});
 }
 public IActionResult Customers(string? q){ViewBag.Query=q;return View(store.Read(d=>d.Customers.Where(c=>c.Role!="Admin"&&(string.IsNullOrWhiteSpace(q)||c.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||c.Email.Contains(q,StringComparison.OrdinalIgnoreCase))).ToList()));}
 public IActionResult Customer(int id){var c=store.Read(d=>d.Customers.FirstOrDefault(c=>c.Id==id));if(c==null)return NotFound();ViewBag.Orders=store.Read(d=>d.Orders.Where(o=>o.CustomerId==id).ToList());return View(c);}
 public IActionResult Inventory()=>View(store.Read(d=>d.Products));
 [HttpPost] public IActionResult Inventory(int id,int quantity){if(quantity is <0 or >100000)TempData["Error"]="Tồn kho phải từ 0 đến 100.000.";else store.Write(d=>{var p=d.Products.FirstOrDefault(p=>p.Id==id);if(p!=null)p.Stock=quantity;return 0;});return RedirectToAction(nameof(Inventory));}
 public IActionResult Promotions()=>View(store.Read(d=>d.Promotions));
 public IActionResult Promotion(int? id){var p=id==null?new Promotion():store.Read(d=>d.Promotions.FirstOrDefault(p=>p.Id==id));return p==null?NotFound():View(p);}
 [HttpPost] public IActionResult Promotion(Promotion input)
 {
  input.Code=input.Code?.Trim().ToUpperInvariant()??"";if(input.Until.Date<DateTime.Today)ModelState.AddModelError("Until","Ngày kết thúc không được ở quá khứ.");if(store.Read(d=>d.Promotions.Any(p=>p.Id!=input.Id&&p.Code==input.Code)))ModelState.AddModelError("Code","Mã đã tồn tại.");if(!ModelState.IsValid)return View(input);store.Write(d=>{if(input.Id==0){input.Id=d.Promotions.Select(p=>p.Id).DefaultIfEmpty().Max()+1;d.Promotions.Add(input);}else{var index=d.Promotions.FindIndex(p=>p.Id==input.Id);if(index>=0)d.Promotions[index]=input;}return 0;});TempData["Success"]="Đã lưu chương trình. Mã ưu đãi là dữ liệu quản lý, chưa áp dụng vào thanh toán demo.";return RedirectToAction(nameof(Promotions));
 }
 public IActionResult Reports(DateTime? from,DateTime? to){var start=from?.Date??DateTime.Today.AddDays(-30);var end=to?.Date??DateTime.Today;ViewBag.From=start;ViewBag.To=end;if(start>end){TempData["Error"]="Khoảng ngày không hợp lệ.";return View(new List<Order>());}return View(store.Read(d=>d.Orders.Where(o=>o.Created.Date>=start&&o.Created.Date<=end).ToList()));}
 public IActionResult Settings()=>View(store.Read(d=>d.Settings));
 [HttpPost] public IActionResult Settings(StoreSettings input){if(!ModelState.IsValid)return View(input);store.Write(d=>{d.Settings=input;return 0;});TempData["Success"]="Đã lưu thông tin cửa hàng.";return RedirectToAction(nameof(Settings));}
 public IActionResult Export(){var csv=store.Read(d=>"Mã đơn,Ngày,Trạng thái,Tổng tiền\n"+string.Join("\n",d.Orders.Select(o=>$"{o.Id},{o.Created:yyyy-MM-dd},{o.Status},{o.Total}")));return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(),"text/csv","qoanh-orders.csv");}
}
