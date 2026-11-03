using System.Security.Cryptography;
using System.Text.Json;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;
public sealed partial class StoreService
{
 public void ImportJson(string path)
 {
  var bytes=File.ReadAllBytes(path);var source=JsonSerializer.Deserialize<StoreData>(bytes)??throw new InvalidDataException("JSON trống.");
  MergeLegacy(source,Convert.ToHexString(SHA256.HashData(bytes)));
 }
 public void SeedDemo()=>MergeLegacy(Seed(),"DEMO-SEED");
 void MergeLegacy(StoreData source,string hash)=>Write(d=>
 {
  Require(d.ImportedFileHash==null,"Database đã nhập JSON; từ chối nhập lặp hoặc ghi đè.");
  var accounts=new Dictionary<int,int>();var products=new Dictionary<int,int>();
  foreach(var c in source.Customers)
  {
   int legacy=c.Id;var existing=d.Customers.FirstOrDefault(x=>x.Email.Equals(c.Email.Trim(),StringComparison.OrdinalIgnoreCase));
   if(existing!=null){Require(existing.Role==c.Role&&existing.PasswordHash==c.PasswordHash,"Email trùng dữ liệu SQL nhưng khác tài khoản: "+c.Email);accounts[legacy]=existing.Id;continue;}
   c.LegacyId=legacy;c.Email=c.Email.Trim().ToLowerInvariant();c.Id=c.Role=="Customer"?Next(d.Customers.Where(x=>x.Id>0).Select(x=>x.Id)):-Next(d.Customers.Where(x=>x.Id<0).Select(x=>-x.Id));accounts[legacy]=c.Id;d.Customers.Add(c);
  }
  foreach(string cat in source.Categories)if(!d.Categories.Contains(cat,StringComparer.OrdinalIgnoreCase))d.Categories.Add(cat);
  foreach(var p in source.Products){int legacy=p.Id;p.Id=Next(d.Products.Select(x=>x.Id));p.LegacyId=legacy;p.Variants.Clear();products[legacy]=p.Id;d.Products.Add(p);}
  PrepareStock(d);
  foreach(var o in source.Orders)
  {
   Require(!d.Orders.Any(x=>x.Id==o.Id),"Mã đơn JSON trùng SQL: "+o.Id);o.DatabaseId=0;o.CustomerId=accounts[o.CustomerId];o.ShippingSnapshot=o.Shipping;
   foreach(var l in o.Lines){l.Id=0;l.ProductId=products[l.ProductId];var v=d.Variants.Single(v=>v.ProductId==l.ProductId&&v.Size==l.Size);l.VariantId=v.Id;l.Color=v.Color;}
   d.Orders.Add(o);
  }
  foreach(var p in source.Promotions){Require(!d.Promotions.Any(x=>x.Code.Equals(p.Code,StringComparison.OrdinalIgnoreCase)),"Mã ưu đãi JSON trùng SQL: "+p.Code);p.Id=Next(d.Promotions.Select(x=>x.Id));if(p.From>p.Until)p.From=p.Until.Date;d.Promotions.Add(p);}
  foreach(var p in source.Products)d.StockMovements.Add(new StockMovement{Id=Next(d.StockMovements.Select(x=>x.Id)),ProductId=p.Id,Before=0,Delta=p.Stock,After=p.Stock,Reason="JSON opening balance SHA256 "+hash,Reference="IMPORT-"+p.Id});
  d.Settings=source.Settings;d.ImportedFileHash=hash;return 0;
 });
}
