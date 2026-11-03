using System.ComponentModel.DataAnnotations;
namespace Project_QLThoiTrang.Models;
public class Product
{
 public int? BrandId { get; set; }
 public int? LegacyId { get; set; }
 public List<Variant> Variants { get; set; }=new();
 public int AvailableStock=>Variants.Where(v=>v.Active).Sum(v=>v.Stock);
 public int Id { get; set; }
 [Required(ErrorMessage="Nhập tên sản phẩm"),StringLength(120)] public string Name { get; set; }="";
 [Required] public string Category { get; set; }="Nữ";
 [Range(1000,100000000,ErrorMessage="Giá phải từ 1.000 đến 100.000.000 đ")] public decimal Price { get; set; }
 public string Image { get; set; }="/images/products/aothun01.jpg";
 [Required,StringLength(60)] public string Color { get; set; }="Trắng";
 [StringLength(2000)] public string Description { get; set; }="Cotton mềm mại, phom dễ mặc. Phù hợp với những ngày bạn muốn thoải mái mà vẫn chỉn chu.";
 [Range(0,100000)] public int Stock { get; set; }=20;
 public bool Active { get; set; }=true;
 public bool IsNew { get; set; }=true;
 public bool OnSale { get; set; }
}
