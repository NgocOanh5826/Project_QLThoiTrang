using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;

// Compatibility facade for the existing QOANH views/controllers; SQL is the source of truth.
public sealed partial class StoreService(IDbContextFactory<StoreDbContext> factory)
{
 readonly PasswordHasher<Customer> hasher=new();
 public T Read<T>(Func<StoreData,T> read)
 {
  using var db=factory.CreateDbContext();
  db.ChangeTracker.QueryTrackingBehavior=QueryTrackingBehavior.NoTracking;
  using var tx=db.Database.BeginTransaction(IsolationLevel.Serializable);
  var result=read(Load(db)); tx.Commit(); return result;
 }
 public T Write<T>(Func<StoreData,T> write,int? actorId=null,string reason="Điều chỉnh dữ liệu")
 {
  using var db=factory.CreateDbContext();
  using var tx=db.Database.BeginTransaction(IsolationLevel.Serializable);
  db.Database.ExecuteSqlRaw("DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=N'QOANH.Store.Write', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @result < 0 THROW 51000, N'Không lấy được khóa nghiệp vụ. Vui lòng thử lại.', 1;");
  var data=Load(db);
  var before=data.Products.ToDictionary(p=>p.Id,p=>p.Stock);
  var movementCount=data.StockMovements.Count;
  var result=write(data);
  foreach(var p in data.Products)
  {
   int previous=before.GetValueOrDefault(p.Id);
   if(previous!=p.Stock && !data.StockMovements.Skip(movementCount).Any(m=>m.ProductId==p.Id))
    data.StockMovements.Add(new StockMovement{Id=Next(data.StockMovements.Select(x=>x.Id)),ProductId=p.Id,Before=previous,Delta=p.Stock-previous,After=p.Stock,ActorId=actorId,Reason=reason,Reference="AUTO-"+Guid.NewGuid().ToString("N")});
  }
  PrepareStock(data);
  Validate(data);
  PersistNative(db,data);
  db.SaveChanges();tx.Commit();return result;
 }
 static int Next(IEnumerable<int> ids)=>ids.DefaultIfEmpty().Max()+1;
 static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
 static void Validate(StoreData d)
 {
  Require(d.Products.All(p=>p.Stock is >=0 and <=100000),"Tồn kho phải từ 0 đến 100.000.");
  Require(d.Customers.Select(c=>c.Email.Trim().ToUpperInvariant()).Distinct().Count()==d.Customers.Count,"Email đã được sử dụng.");
  Require(d.Promotions.Select(p=>p.Code.ToUpperInvariant()).Distinct().Count()==d.Promotions.Count,"Mã ưu đãi đã tồn tại.");
  Require(d.Categories.Distinct(StringComparer.OrdinalIgnoreCase).Count()==d.Categories.Count,"Danh mục đã tồn tại.");
  foreach(var group in d.Customers.GroupBy(c=>c.Role=="Customer")){var phones=group.Where(c=>!string.IsNullOrWhiteSpace(c.Phone)).Select(c=>c.Phone.Trim()).ToList();Require(phones.Distinct().Count()==phones.Count,"Duplicate phone number.");}
  foreach(var c in d.Customers)
  {
   Require(c.Phone.Length<=15&&c.Address.Length<=255&&c.Email.Length<=150,"Profile field is too long.");
   Require(c.Role is "Admin" or "Customer" or "Catalog" or "Inventory" or "Sales" or "Support","Vai trò không hợp lệ.");
   Require(PasswordStorage.IsIdentityHash(c.PasswordHash),"Mật khẩu phải là Identity hash; không chấp nhận văn bản rõ hoặc hash sai định dạng.");
  }
  Require(d.Stocks.All(s=>s.Quantity>=0),"Invalid variant stock.");
  foreach(var p in d.Products) Validator.ValidateObject(p,new ValidationContext(p),true);
  foreach(var o in d.Orders)
  {
   Require(o.Lines.Count>0 && o.Lines.All(l=>l.Quantity>0 && l.Price>=0 && !string.IsNullOrWhiteSpace(l.Size)),"Chi tiết đơn hàng không hợp lệ.");
   Require(o.Discount>=0 && o.Discount<=o.Subtotal && o.PaidAmount>=0 && o.PaidAmount<=o.Total && o.RefundedAmount>=0 && o.RefundedAmount<=o.PaidAmount,"Số tiền đơn hàng không hợp lệ.");
  }
 }
 public Customer? Authenticate(string email,string password,string? ip=null,string? device=null)
 {
  var c=Read(d=>d.Customers.FirstOrDefault(c=>c.Active&&c.Email.Equals(email.Trim(),StringComparison.OrdinalIgnoreCase)));
  if(c==null)return null;
  if(!PasswordStorage.IsIdentityHash(c.PasswordHash)){LogLogin(c,false,ip,device);return null;}
  var verified=hasher.VerifyHashedPassword(c,c.PasswordHash,password);
  if(verified==PasswordVerificationResult.Failed){LogLogin(c,false,ip,device);return null;}
  if(verified==PasswordVerificationResult.SuccessRehashNeeded){c.PasswordHash=hasher.HashPassword(c,password);Write(d=>{d.Customers.Single(x=>x.Id==c.Id).PasswordHash=c.PasswordHash;return 0;});}
  c.LoginHistoryId=LogLogin(c,true,ip,device);return c;
 }
 public Customer Register(RegisterInput input)
 {
  Validator.ValidateObject(input,new ValidationContext(input),true);
  return Write(d=>{Require(!d.Customers.Any(c=>c.Email.Equals(input.Email.Trim(),StringComparison.OrdinalIgnoreCase)),"Email đã được sử dụng.");
   var c=new Customer{Id=Next(d.Customers.Select(c=>c.Id)),Name=input.Name.Trim(),Email=input.Email.Trim().ToLowerInvariant()};
   c.PasswordHash=hasher.HashPassword(c,input.Password);d.Customers.Add(c);return c;});
 }
}
