using System.ComponentModel.DataAnnotations;
namespace Project_QLThoiTrang.Models;
public class Customer
{
 public int Id { get; set; }
 public string Name { get; set; }="";
 public string Email { get; set; }="";
 public string Phone { get; set; }="";
 public string Address { get; set; }="";
 public string PasswordHash { get; set; }="";
 public string Role { get; set; }="Customer";
}
public class Order
{
 public string Id { get; set; }="";
 public int CustomerId { get; set; }
 public DateTime Created { get; set; }=DateTime.Now;
 public string Name { get; set; }="";
 public string Phone { get; set; }="";
 public string Address { get; set; }="";
 public string Note { get; set; }="";
 public string Status { get; set; }="Chờ xác nhận";
 public string Payment { get; set; }="Thanh toán khi nhận hàng";
 public List<OrderLine> Lines { get; set; }=new();
 public decimal Subtotal=>Lines.Sum(x=>x.Price*x.Quantity);
 public decimal Shipping=>Subtotal>=700000?0:30000;
 public decimal Total=>Subtotal+Shipping;
}
public class OrderLine
{
 public int ProductId { get; set; }
 public string Name { get; set; }="";
 public string Image { get; set; }="";
 public string Size { get; set; }="M";
 public int Quantity { get; set; }
 public decimal Price { get; set; }
}
public class CartItem { public int ProductId { get; set; } public string Size { get; set; }="M"; public int Quantity { get; set; }=1; }
public class Promotion
{
 public int Id { get; set; }
 [Required,StringLength(120)] public string Name { get; set; }="";
 [Required,StringLength(30)] public string Code { get; set; }="";
 [Range(1,70)] public int Percent { get; set; }=10;
 public DateTime Until { get; set; }=DateTime.Today.AddDays(30);
 public bool Active { get; set; }=true;
}
public class StoreSettings
{
 [Required,StringLength(80)] public string Name { get; set; }="QOANH";
 [Required,EmailAddress] public string Email { get; set; }="hello@qoanh.example";
 [Required,StringLength(30)] public string Phone { get; set; }="0900 123 456";
 [Required,StringLength(250)] public string Address { get; set; }="TP. Hồ Chí Minh";
}
public class StoreData
{
 public List<Product> Products { get; set; }=new();
 public List<Customer> Customers { get; set; }=new();
 public List<Order> Orders { get; set; }=new();
 public List<Promotion> Promotions { get; set; }=new();
 public List<string> Categories { get; set; }=new(){"Nữ","Nam","Trẻ em","Phụ kiện"};
 public StoreSettings Settings { get; set; }=new();
}
public class CatalogViewModel
{
 public List<Product> Products { get; set; }=new();
 public List<string> Categories { get; set; }=new();
 public string? Query { get; set; }
 public string? Category { get; set; }
 public string Sort { get; set; }="new";
 public string? Collection { get; set; }
}
public class CartViewModel
{
 public List<OrderLine> Lines { get; set; }=new();
 public decimal Subtotal=>Lines.Sum(x=>x.Price*x.Quantity);
 public decimal Shipping=>Lines.Count==0||Subtotal>=700000?0:30000;
 public decimal Total=>Subtotal+Shipping;
}
public class CheckoutInput
{
 [Required(ErrorMessage="Nhập họ tên"),StringLength(100)] public string Name { get; set; }="";
 [Required(ErrorMessage="Nhập số điện thoại"),RegularExpression(@"[0-9+ ()-]{9,20}",ErrorMessage="Số điện thoại chưa hợp lệ")] public string Phone { get; set; }="";
 [Required(ErrorMessage="Nhập địa chỉ giao hàng"),StringLength(300)] public string Address { get; set; }="";
 [StringLength(500)] public string Note { get; set; }="";
}
public class LoginInput
{
 [Required(ErrorMessage="Nhập email của bạn"),EmailAddress(ErrorMessage="Email chưa đúng định dạng")] public string Email { get; set; }="";
 [Required(ErrorMessage="Nhập mật khẩu")] public string Password { get; set; }="";
 public bool RememberMe { get; set; }
 public string? ReturnUrl { get; set; }
}
public class RegisterInput
{
 [Required(ErrorMessage="Nhập họ tên"),StringLength(100,MinimumLength=2,ErrorMessage="Họ tên cần từ 2 đến 100 ký tự")] public string Name { get; set; }="";
 [Required(ErrorMessage="Nhập email"),EmailAddress(ErrorMessage="Email chưa đúng định dạng"),StringLength(254)] public string Email { get; set; }="";
 [Required(ErrorMessage="Tạo mật khẩu"),MinLength(8,ErrorMessage="Mật khẩu cần ít nhất 8 ký tự"),StringLength(100,ErrorMessage="Mật khẩu không được quá 100 ký tự"),RegularExpression(@"^(?=.*\p{L})(?=.*\d).+$",ErrorMessage="Mật khẩu cần có ít nhất một chữ cái và một chữ số")] public string Password { get; set; }="";
 [Required(ErrorMessage="Nhập lại mật khẩu"),Compare("Password",ErrorMessage="Mật khẩu xác nhận chưa khớp")] public string ConfirmPassword { get; set; }="";
 public string? ReturnUrl { get; set; }
}
public class ProfileInput
{
 [Required,StringLength(100)] public string Name { get; set; }="";
 [StringLength(30)] public string Phone { get; set; }="";
 [StringLength(300)] public string Address { get; set; }="";
}
