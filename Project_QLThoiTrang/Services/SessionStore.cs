using System.Text.Json;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public static class SessionStore
{
    // Get: Đọc và giải mã dữ liệu JSON từ Session.
    public static T Get<T>(ISession session, string key)
        where T : new() =>
        JsonSerializer.Deserialize<T>(session.GetString(key) ?? "null") ?? new T();

    // Set: Mã hóa dữ liệu JSON và lưu vào Session.
    public static void Set<T>(ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));

    // Cart: Dựng giỏ hàng với thông tin và giá hiện tại.
    public static CartViewModel Cart(ISession session, StoreService store)
    {
        var items = Get<List<CartItem>>(session, "cart");
        return store.Read(d => new CartViewModel
        {
            Lines = items
                .Select(i =>
                {
                    var p = d.Products.FirstOrDefault(p => p.Id == i.ProductId);
                    var v = d.Variants.FirstOrDefault(v =>
                        v.ProductId == i.ProductId
                        && (i.VariantId > 0 ? v.Id == i.VariantId : v.Size == i.Size)
                    );
                    return p == null || v == null
                        ? null
                        : new OrderLine
                        {
                            ProductId = p.Id,
                            VariantId = v.Id,
                            Color = v.Color,
                            Name = p.Name,
                            Image = p.Image,
                            Price = v.Price ?? p.Price,
                            Size = v.Size,
                            Quantity = i.Quantity,
                        };
                })
                .OfType<OrderLine>()
                .ToList(),
        });
    }
}
