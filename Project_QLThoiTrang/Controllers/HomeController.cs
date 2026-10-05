using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;
namespace Project_QLThoiTrang.Controllers;
public class HomeController(StoreService store):Controller
{
 public IActionResult Index()=>View(store.Read(d=>d.Products.Where(p=>p.Active).ToList()));
 public IActionResult Info(string id="about")
 {
  var pages=new Dictionary<string,(string Title,string Text)>{
   ["about"]=("Về QOANH","Những thiết kế đơn giản, chất liệu dễ chịu và phom dáng dành cho mỗi ngày. QOANH mang đến một tủ đồ dễ phối, bền với thời gian."),
   ["contact"]=("Liên hệ","Cần tư vấn kích cỡ hoặc hỗ trợ đơn hàng? Liên hệ cửa hàng theo thông tin dưới đây."),
   ["returns"]=("Chính sách đổi trả","Trong bản demo, bạn có thể yêu cầu hỗ trợ đổi trả qua thông tin liên hệ. Chính sách kinh doanh thực tế cần được cửa hàng xác nhận trước khi triển khai."),
   ["guide"]=("Hướng dẫn mua hàng","Chọn sản phẩm và kích cỡ → thêm vào giỏ → kiểm tra đơn → đăng nhập → nhập địa chỉ → xác nhận đặt hàng. Bản demo dùng thanh toán khi nhận hàng, không thu tiền trực tuyến."),
   ["stores"]=("Hệ thống cửa hàng","Thông tin cửa hàng mẫu được hiển thị dưới đây. Bạn có thể cập nhật trong phần cài đặt quản trị."),
   ["terms"]=("Điều khoản sử dụng","Đây là website demo quản lý thời trang. Đơn hàng và tài khoản phục vụ thử nghiệm; không có giao dịch thanh toán thật."),
   ["privacy"]=("Quyền riêng tư","Dữ liệu demo được lưu trong tệp JSON trên máy chạy ứng dụng. Không nhập thông tin cá nhân nhạy cảm trong môi trường thử nghiệm.")};
  if(!pages.TryGetValue(id,out var info))return NotFound();ViewData["Title"]=info.Title;ViewData["Text"]=info.Text;return View(store.Read(d=>d.Settings));
 }
 public IActionResult Privacy()=>RedirectToAction(nameof(Info),new{id="privacy"});
 [ResponseCache(Duration=0,Location=ResponseCacheLocation.None,NoStore=true)]
 public IActionResult Error()=>View(new ErrorViewModel{RequestId=Activity.Current?.Id??HttpContext.TraceIdentifier});
}
