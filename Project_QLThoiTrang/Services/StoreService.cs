using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;
public sealed class StoreService
{
 readonly object gate=new();
 readonly string path;
 readonly PasswordHasher<Customer> hasher=new();
 StoreData data;
 public StoreService(IWebHostEnvironment env,IConfiguration configuration)
 {
  path=configuration["StoreDataPath"]??Path.Combine(env.ContentRootPath,"App_Data","store.json");
  if(File.Exists(path))data=JsonSerializer.Deserialize<StoreData>(File.ReadAllText(path))??throw new InvalidDataException("Không đọc được dữ liệu cửa hàng.");
  else{data=Seed();Save();}
 }
 public T Read<T>(Func<StoreData,T> read){lock(gate){var snapshot=JsonSerializer.Deserialize<StoreData>(JsonSerializer.Serialize(data))!;return read(snapshot);}}
 public T Write<T>(Func<StoreData,T> write){lock(gate){var next=JsonSerializer.Deserialize<StoreData>(JsonSerializer.Serialize(data))!;var result=write(next);var previous=data;data=next;try{Save();}catch{data=previous;throw;}return result;}}
 void Save(){Directory.CreateDirectory(Path.GetDirectoryName(path)!);string temporary=path+".tmp";File.WriteAllText(temporary,JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));File.Move(temporary,path,true);}
 public Customer? Authenticate(string email,string password)=>Read(d=>{var c=d.Customers.FirstOrDefault(c=>c.Email.Equals(email.Trim(),StringComparison.OrdinalIgnoreCase));return c!=null&&hasher.VerifyHashedPassword(c,c.PasswordHash,password)!=PasswordVerificationResult.Failed?c:null;});
 public Customer Register(RegisterInput input)=>Write(d=>{if(d.Customers.Any(c=>c.Email.Equals(input.Email.Trim(),StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Email đã được sử dụng.");var c=new Customer{Id=d.Customers.Select(c=>c.Id).DefaultIfEmpty().Max()+1,Name=input.Name.Trim(),Email=input.Email.Trim().ToLowerInvariant()};c.PasswordHash=hasher.HashPassword(c,input.Password);d.Customers.Add(c);return c;});
 StoreData Seed()
 {
  var d=new StoreData();
  string[] names={"Áo thun Cotton Basic","Áo thun Essential","Áo thun Premium","Áo thun Unisex","Áo thun Everyday","Áo thun Junior","Áo thun Weekend","Áo thun Relaxed"};
  for(int i=0;i<names.Length;i++)d.Products.Add(new Product{Id=i+1,Name=names[i],Category=i==5?"Trẻ em":i%2==0?"Nữ":"Nam",Price=299000+i*50000,Image=$"/images/products/aothun{i%5+1:00}.jpg",Color=i%2==0?"Trắng":"Đen",Stock=15+i*3,OnSale=i==2,IsNew=i>=4});
  var admin=new Customer{Id=1,Name="Quản trị cửa hàng",Email="admin@qoanh.local",Role="Admin"};admin.PasswordHash=hasher.HashPassword(admin,"Admin@12345");
  var customer=new Customer{Id=2,Name="Khách hàng demo",Email="khach@qoanh.local",Phone="0901234567",Address="TP. Hồ Chí Minh"};customer.PasswordHash=hasher.HashPassword(customer,"Khach@12345");d.Customers.AddRange(new[]{admin,customer});
  d.Orders.Add(new Order{Id="QH-DEMO01",CustomerId=2,Name=customer.Name,Phone=customer.Phone,Address=customer.Address,Status="Đã giao",Created=DateTime.Now.AddDays(-3),Lines=new(){new OrderLine{ProductId=1,Name=names[0],Image=d.Products[0].Image,Price=d.Products[0].Price,Quantity=2}}});
  d.Promotions.Add(new Promotion{Id=1,Name="Ưu đãi bộ sưu tập mới",Code="HELLO10",Percent=10});return d;
 }
}
