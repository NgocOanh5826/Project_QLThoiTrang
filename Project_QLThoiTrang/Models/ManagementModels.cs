using System.ComponentModel.DataAnnotations;
namespace Project_QLThoiTrang.Models;
public class ReferenceInput
{
 public int Id { get; set; }
 public string Kind { get; set; }="brands";
 [Required,StringLength(100)] public string Name { get; set; }="";
 [StringLength(500)] public string? Description { get; set; }
 [StringLength(255)] public string? Address { get; set; }
 [StringLength(15)] public string? Phone { get; set; }
 [EmailAddress,StringLength(150)] public string? Email { get; set; }
 public bool Active { get; set; }=true;
}
public class ReferencePage { public ReferenceInput Input { get; set; }=new(); public List<ReferenceInput> Rows { get; set; }=new(); }
public class StaffInput
{
 public int Id { get; set; }
 [Required,StringLength(100)] public string Name { get; set; }="";
 [Required,EmailAddress,StringLength(150)] public string Email { get; set; }="";
 [StringLength(15)] public string? Phone { get; set; }
 public string Role { get; set; }="Support";
 [StringLength(100)] public string? Password { get; set; }
 public bool Active { get; set; }=true;
}
public class VariantInput
{
 public int Id { get; set; }
 public int ProductId { get; set; }
 public int SizeId { get; set; }
 public int ColorId { get; set; }
 [Required,StringLength(50)] public string Sku { get; set; }="";
 [Range(typeof(decimal),"1000","100000000")] public decimal? Price { get; set; }
 public bool Active { get; set; }=true;
}
public class ReviewItem { public int Id { get; set; } public int ProductId { get; set; } public string Customer { get; set; }=""; public int Stars { get; set; } public string Text { get; set; }=""; public bool Active { get; set; } public DateTime Created { get; set; } }
public class PasswordInput
{
 [Required] public string CurrentPassword { get; set; }="";
 [Required,MinLength(8),StringLength(100),RegularExpression(@"^(?=.*\p{L})(?=.*\d).+$")] public string NewPassword { get; set; }="";
 [Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; }="";
}
