using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Controllers;
public partial class AdminController
{
 public override void OnActionExecuting(ActionExecutingContext context)
 {
  if(User.IsInRole("Admin"))return;
  string action=context.ActionDescriptor.RouteValues["action"]??"";
  string[] allowed=action switch
  {
   "Index"=>new[]{"Catalog","Inventory","Sales","Support"},
   "Products" or "Product" or "DeleteProduct" or "Categories" or "AddCategory" or "DeleteCategory" or "Promotions" or "Promotion" or "Variants"=>new[]{"Catalog"},
   "Inventory" or "Receive" or "ReceiveMany" or "VariantStock" or "AdjustVariant" or "Receipts"=>new[]{"Inventory"},
   "Orders" or "Order" or "OrderStatus" or "Payment" or "Returns" or "ResolveReturn" or "Reports" or "Export"=>new[]{"Sales"},
   "Customers" or "Customer" or "EditCustomer" or "Reviews" or "Moderate"=>new[]{"Support"},
   "References"=>new[]{(context.ActionArguments.Values.OfType<ReferenceInput>().FirstOrDefault()?.Kind??Request.Query["kind"].ToString()) is "warehouses" or "suppliers"?"Inventory":"Catalog"},
   _=>Array.Empty<string>()
  };
  if(!allowed.Any(User.IsInRole))context.Result=Forbid();
 }
 public IActionResult References(string kind="brands",int id=0)
 {try{var rows=store.References(kind);var input=id==0?new ReferenceInput{Kind=kind}:rows.FirstOrDefault(r=>r.Id==id);return input==null?NotFound():View(new ReferencePage{Input=input,Rows=rows});}catch(InvalidOperationException){return BadRequest();}}
 [HttpPost] public IActionResult References(ReferenceInput input)
 {
  if(ModelState.IsValid)try{store.SaveReference(input,ActorId);TempData["Success"]="Đã lưu danh mục.";return RedirectToAction(nameof(References),new{kind=input.Kind});}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}
  return View(new ReferencePage{Input=input,Rows=store.References(input.Kind)});
 }
 void ReceiveOptions(){var d=store.Read(d=>d);ViewBag.Products=d.Products;ViewBag.Variants=d.Variants;ViewBag.Warehouses=d.Warehouses.Where(w=>w.Active).ToList();ViewBag.Suppliers=d.Suppliers.Where(s=>s.Active).ToList();}
 public IActionResult Variants(int? id=null)
 {var d=store.Read(d=>d);ViewBag.Products=d.Products;ViewBag.Variants=d.Variants;ViewBag.Sizes=store.References("sizes");ViewBag.Colors=store.References("colors");if(id==null)return View(new VariantInput());var r=store.NativeRead(db=>db.BienTheSanPham.SingleOrDefault(v=>v.MaBienThe==id));return r==null?NotFound():View(new VariantInput{Id=r.MaBienThe,ProductId=r.MaSP,SizeId=r.MaKichThuoc,ColorId=r.MaMauSac,Sku=r.SKU,Price=r.GiaBanRieng,Active=r.TrangThai});}
 [HttpPost] public IActionResult Variants(VariantInput input)
 {try{if(ModelState.IsValid){store.SaveVariant(input,ActorId);TempData["Success"]="Đã lưu biến thể.";return RedirectToAction(nameof(Variants));}}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Variants),new{id=input.Id==0?(int?)null:input.Id});}
 public IActionResult VariantStock()=>View(store.Read(d=>d));
 [HttpPost] public IActionResult AdjustVariant(int variantId,int warehouseId,int quantity,int expected,string reason)
 {try{store.AdjustVariantStock(variantId,warehouseId,quantity,expected,reason,ActorId);TempData["Success"]="Đã kiểm kê biến thể.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(VariantStock));}
 public IActionResult Receipts()=>View(store.NativeRead(db=>db.PhieuNhap.OrderByDescending(p=>p.NgayNhap).ToList()));
 [HttpPost] public IActionResult ReceiveMany(List<ReceiveInput> lines,string reference,int warehouseId,int supplierId)
 {try{foreach(var line in lines){line.Reference=reference;line.WarehouseId=warehouseId;line.SupplierId=supplierId;}try{store.ReceiveBatch(lines,ActorId);}catch(System.ComponentModel.DataAnnotations.ValidationException ex){throw new InvalidOperationException(ex.Message);}TempData["Success"]="Đã nhập toàn bộ phiếu.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Receive));}
 public IActionResult Staff(int id=0)
 {var staff=store.Staff();ViewBag.Staff=staff;var s=staff.FirstOrDefault(s=>-s.Id==id);return id>0&&s==null?NotFound():View(s==null?new StaffInput():new StaffInput{Id=-s.Id,Name=s.Name,Email=s.Email,Phone=s.Phone,Role=s.Role,Active=s.Active});}
 [HttpPost] public IActionResult Staff(StaffInput input)
 {if(ModelState.IsValid)try{store.SaveStaff(input,ActorId);TempData["Success"]="Đã lưu nhân viên và quyền.";return RedirectToAction(nameof(Staff));}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}input.Password=null;ViewBag.Staff=store.Staff();return View(input);}
 public IActionResult EditCustomer(int id)
 {var c=store.Read(d=>d.Customers.FirstOrDefault(c=>c.Id==id&&c.Role=="Customer"));if(c==null)return NotFound();ViewBag.Id=id;ViewBag.Active=c.Active;return View(new ProfileInput{Name=c.Name,Phone=c.Phone,Address=c.Address});}
 [HttpPost] public IActionResult EditCustomer(int id,ProfileInput input,bool active)
 {ViewBag.Id=id;ViewBag.Active=active;if(ModelState.IsValid)try{store.UpdateCustomer(id,input,active,ActorId);TempData["Success"]="Đã cập nhật khách hàng.";return RedirectToAction(nameof(Customer),new{id});}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}return View(input);}
 public IActionResult Reviews()=>View(store.Reviews(includeHidden:true));
 [HttpPost] public IActionResult Moderate(int id,bool active){try{store.ModerateReview(id,active,ActorId);}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Reviews));}
 public IActionResult LoginHistory()=>View(store.NativeRead(db=>db.LichSuDangNhap.OrderByDescending(x=>x.MaLichSu).Take(300).ToList()));
}
