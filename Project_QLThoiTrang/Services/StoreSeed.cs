using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;
public sealed partial class StoreService
{
 StoreData Seed()
 {
  var d=new StoreData();
  string[] names={"Áo thun Cotton Basic","Áo thun Essential","Áo thun Premium","Áo thun Unisex","Áo thun Everyday","Áo thun Junior","Áo thun Weekend","Áo thun Relaxed"};
  for(int i=0;i<names.Length;i++)d.Products.Add(new Product{Id=i+1,Name=names[i],Category=i==5?"Trẻ em":i%2==0?"Nữ":"Nam",Price=299000+i*50000,Image=$"/images/products/aothun{i%5+1:00}.jpg",Color=i%2==0?"Trắng":"Đen",Stock=15+i*3,OnSale=i==2,IsNew=i>=4});
  var admin=new Customer{Id=1,Name="Quản trị cửa hàng",Email="admin@qoanh.local",Role="Admin"};admin.PasswordHash="AQAAAAIAAYagAAAAEPf4HCeGeaRarlp8AHMXzK9Pzj6bHPeuK0bTCMvZWdF+E3oeI1/eLTpTEbIZbbifcw==";
  var customer=new Customer{Id=2,Name="Khách hàng demo",Email="khach@qoanh.local",Phone="0901234567",Address="TP. Hồ Chí Minh"};customer.PasswordHash="AQAAAAIAAYagAAAAEOjmujh1ybT/DhQChP3kvV7Z+4s/Q8ACNyRvPNtsdbwvHHFwFj8+6SSKKU0U3H3Ckw==";d.Customers.AddRange(new[]{admin,customer});
  d.Orders.Add(new Order{Id="QH-DEMO01",CustomerId=2,Name=customer.Name,Phone=customer.Phone,Address=customer.Address,Status="Đã giao",Created=DateTime.Now.AddDays(-3),Lines=new(){new OrderLine{ProductId=1,Name=names[0],Image=d.Products[0].Image,Price=d.Products[0].Price,Quantity=2}}});
  d.Promotions.Add(new Promotion{Id=1,Name="Ưu đãi bộ sưu tập mới",Code="HELLO10",Percent=10});return d;
 }
}
