using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
[Authorize(Roles="Admin,Inventory")]
public class ValuationController(StoreService store):Controller
{
 public IActionResult Index(){ViewBag.Values=store.NativeRead(db=>db.InventoryValues.ToList());return View(store.Read(d=>d));}
 public IActionResult Edit(int id){var d=store.Read(d=>d);if(!d.Variants.Any(v=>v.Id==id))return NotFound();ViewBag.VariantId=id;return View(d);}
 [HttpPost] public IActionResult Edit(int variantId,int expected,decimal cost,string note){try{if(!ModelState.IsValid)throw new InvalidOperationException("Giá vốn không hợp lệ.");store.SetInventoryValue(variantId,expected,cost,note,int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));TempData["Success"]="Đã ghi giá vốn cho các giao dịch phát sinh tiếp theo.";return RedirectToAction(nameof(Index));}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Edit),new{id=variantId});}}
}
