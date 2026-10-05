using System.Text.Json;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;
public static class SessionStore
{
 public static T Get<T>(ISession session,string key) where T:new()=>JsonSerializer.Deserialize<T>(session.GetString(key)??"null")??new T();
 public static void Set<T>(ISession session,string key,T value)=>session.SetString(key,JsonSerializer.Serialize(value));
 public static CartViewModel Cart(ISession session,StoreService store)
 {
  var items=Get<List<CartItem>>(session,"cart");return store.Read(d=>new CartViewModel{Lines=items.Select(i=>{var p=d.Products.FirstOrDefault(p=>p.Id==i.ProductId);return p==null?null:new OrderLine{ProductId=p.Id,Name=p.Name,Image=p.Image,Price=p.Price,Size=i.Size,Quantity=i.Quantity};}).OfType<OrderLine>().ToList()});
 }
}
