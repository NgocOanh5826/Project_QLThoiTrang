using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
[Authorize(Roles="Customer,Admin,Support"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class ChatController(StoreService store):Controller
{
 int Actor=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 int Conversation(int customerId)=>User.IsInRole("Customer")?Actor:customerId;
 public IActionResult Index(int customerId=0){if(!User.IsInRole("Customer")&&customerId==0)return View("Inbox",store.ChatInbox(Actor));int target=Conversation(customerId);if(store.NativeRead(db=>db.KhachHang.Any(c=>c.MaKH==target&&c.TrangThai))==false)return NotFound();ViewBag.CustomerId=target;return View();}
 public IActionResult Messages(int customerId,long before=0,long after=0){try{return Json(store.Messages(Conversation(customerId),Actor,before,after).Select(m=>new{id=m.Id,text=m.Text,mine=m.SenderId==Actor,staff=m.SenderId<0,created=DateTime.SpecifyKind(m.CreatedUtc,DateTimeKind.Utc)}));}catch(InvalidOperationException){return Forbid();}}
 [HttpPost] public IActionResult Send(int customerId,string text,Guid clientToken){try{var m=store.SendMessage(Conversation(customerId),Actor,text,clientToken);return Json(new{id=m.Id});}catch(InvalidOperationException ex){return BadRequest(new{error=ex.Message});}}
 [HttpPost] public IActionResult Read(int customerId,long last){try{store.MarkChatRead(Conversation(customerId),Actor,last);return Ok();}catch(InvalidOperationException){return Forbid();}}
}
