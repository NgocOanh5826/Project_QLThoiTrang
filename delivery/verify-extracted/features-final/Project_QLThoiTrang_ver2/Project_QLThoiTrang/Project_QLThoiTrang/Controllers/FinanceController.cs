using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
[Authorize(Roles="Admin,Sales"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class FinanceController(StoreService store):Controller
{
 int Actor=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public IActionResult Index(DateTime? from,DateTime? to){ViewBag.From=from?.Date??DateTime.Today.AddDays(-30);ViewBag.To=to?.Date??DateTime.Today;if((DateTime)ViewBag.From>(DateTime)ViewBag.To){ModelState.AddModelError("","Khoảng ngày không hợp lệ.");return View(new List<Models.ProfitLine>());}return View(store.Profit(ViewBag.From,ViewBag.To));}
 public IActionResult Suppliers(){ViewBag.Receipts=store.NativeRead(db=>db.PhieuNhap.OrderByDescending(p=>p.NgayNhap).ToList());ViewBag.Suppliers=store.Read(d=>d.Suppliers);ViewBag.Accounts=store.NativeRead(db=>db.SupplierAccounts.ToList());return View(store.NativeRead(db=>db.SupplierPayments.ToList()));}
 public IActionResult Receipt(int id){var p=store.NativeRead(db=>db.PhieuNhap.SingleOrDefault(p=>p.MaPhieuNhap==id));if(p==null)return NotFound();ViewBag.Account=store.NativeRead(db=>db.SupplierAccounts.SingleOrDefault(a=>a.ReceiptId==id));ViewBag.Payments=store.NativeRead(db=>db.SupplierPayments.Where(p=>p.ReceiptId==id).ToList());ViewBag.DetailTotal=store.NativeRead(db=>db.ChiTietPhieuNhap.Where(l=>l.MaPhieuNhap==id).Sum(l=>(decimal?)l.SoLuong*l.GiaNhap)??0);return View(p);}
 [HttpPost] public IActionResult Reconcile(int receiptId,decimal amount,string note){try{if(!ModelState.IsValid)throw new InvalidOperationException("Dữ liệu không hợp lệ.");store.ReconcileSupplier(receiptId,amount,note,Actor);TempData["Success"]="Đã đối soát số phải trả.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Receipt),new{id=receiptId});}
 [HttpPost] public IActionResult Pay(int receiptId,decimal amount,string reference){try{if(!ModelState.IsValid)throw new InvalidOperationException("Dữ liệu không hợp lệ.");store.PaySupplier(receiptId,amount,reference,Actor);TempData["Success"]="Đã ghi nhận chứng từ chi.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Receipt),new{id=receiptId});}
}
