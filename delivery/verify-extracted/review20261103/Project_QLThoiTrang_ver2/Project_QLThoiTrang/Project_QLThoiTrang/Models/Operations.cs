using System.ComponentModel.DataAnnotations;
namespace Project_QLThoiTrang.Models;

public class Category { [MaxLength(50)] public string Name { get; set; } = ""; }
public class StoreConfiguration
{
 public int Id { get; set; } = 1;
 public StoreSettings Settings { get; set; } = new();
 public string? ImportedFileHash { get; set; }
}
public class StockMovement
{
 public int? VariantId { get; set; }
 public int? WarehouseId { get; set; }
 public int? SupplierId { get; set; }
 public string? ReceiptReference { get; set; }
 public int Id { get; set; }
 public int ProductId { get; set; }
 public int Before { get; set; }
 public int Delta { get; set; }
 public int After { get; set; }
 public string Reason { get; set; } = "";
 public string Reference { get; set; } = "";
 public string Supplier { get; set; } = "";
 public decimal UnitCost { get; set; }
 public int? ActorId { get; set; }
 public DateTime Created { get; set; } = DateTime.UtcNow;
}
public class PaymentRecord
{
 public int Id { get; set; }
 public string OrderId { get; set; } = "";
 public string Kind { get; set; } = "Receipt";
 public decimal Amount { get; set; }
 public string Reference { get; set; } = "";
 public int ActorId { get; set; }
 public DateTime Created { get; set; } = DateTime.UtcNow;
}
public class ReturnRecord
{
 public int Id { get; set; }
 public string OrderId { get; set; } = "";
 public int CustomerId { get; set; }
 public string Reason { get; set; } = "";
 public string Status { get; set; } = "Pending";
 public string Resolution { get; set; } = "";
 public int? ActorId { get; set; }
 public DateTime Created { get; set; } = DateTime.UtcNow;
 public DateTime? Resolved { get; set; }
 public List<ReturnLine> Lines { get; set; }=new();
 public decimal Refund { get; set; }
 public string Reference { get; set; }="";
}
public class ReturnLine { public int Id { get; set; } public int OrderLineId { get; set; } public int Quantity { get; set; } public decimal Refund { get; set; } public string Condition { get; set; }=""; }
public class Variant { public int Id { get; set; } public int ProductId { get; set; } public string Size { get; set; }=""; public string Color { get; set; }=""; public string Sku { get; set; }=""; public decimal? Price { get; set; } public bool Active { get; set; }=true; public int Stock { get; set; } }
public class WarehouseStock { public int WarehouseId { get; set; } public int VariantId { get; set; } public int Quantity { get; set; } }
public class Warehouse { public int Id { get; set; } public string Name { get; set; }=""; public bool Active { get; set; }=true; }
public class OrderHistory { public long Id { get; set; } public string OrderId { get; set; }=""; public string Status { get; set; }=""; public DateTime Created { get; set; } public string Note { get; set; }=""; }
public class ReceiveInput
{
 public int VariantId { get; set; }
 public int WarehouseId { get; set; }
 public int SupplierId { get; set; }
 public int ProductId { get; set; }
 [Range(1,100000)] public int Quantity { get; set; }
 [Range(typeof(decimal),"0.01","100000000")] public decimal UnitCost { get; set; }
 [StringLength(150)] public string Supplier { get; set; } = "";
 [Required,StringLength(100)] public string Reference { get; set; } = "";
}
public class Supplier { public int Id { get; set; } public string Name { get; set; }=""; public bool Active { get; set; }=true; }
