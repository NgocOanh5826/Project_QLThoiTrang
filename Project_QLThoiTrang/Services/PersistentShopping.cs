using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // UserCart: Lấy giỏ hàng đã lưu của khách.
    public List<CartItem> UserCart(int customerId) =>
        NativeRead(db =>
            (
                from g in db.GioHang
                join l in db.ChiTietGioHang on g.MaGioHang equals l.MaGioHang
                join v in db.BienTheSanPham on l.MaBienThe equals v.MaBienThe
                join size in db.KichThuoc on v.MaKichThuoc equals size.MaKichThuoc
                join color in db.MauSac on v.MaMauSac equals color.MaMauSac
                where g.MaKH == customerId
                select new CartItem
                {
                    ProductId = v.MaSP,
                    VariantId = v.MaBienThe,
                    Size = size.TenKichThuoc,
                    Color = color.TenMauSac,
                    Quantity = l.SoLuong,
                }
            ).ToList()
        );

    // UserWishlist: Lấy danh sách sản phẩm yêu thích của khách.
    public List<int> UserWishlist(int customerId) =>
        NativeRead(db => db.YeuThich.Where(x => x.MaKH == customerId).Select(x => x.MaSP).ToList());

    // SaveShopping: Lưu giỏ hàng và danh sách yêu thích của khách.
    public void SaveShopping(int customerId, List<CartItem> items, List<int> wishlist) =>
        NativeWrite(db =>
        {
            Require(
                customerId > 0 && db.KhachHang.Any(c => c.MaKH == customerId && c.TrangThai),
                "Tài khoản không hoạt động."
            );

            Require(
                items.Count <= 100 && items.All(i => i.Quantity is >= 1 and <= 20),
                "Giỏ hàng không hợp lệ."
            );
            var cart = db.GioHang.SingleOrDefault(c => c.MaKH == customerId);
            if (cart == null)
            {
                cart = new() { MaKH = customerId, NgayTao = DateTime.Now };
                db.Add(cart);

                db.SaveChanges();
            }

            db.ChiTietGioHang.RemoveRange(
                db.ChiTietGioHang.Where(l => l.MaGioHang == cart.MaGioHang)
            );

            db.SaveChanges();
            foreach (var group in items.GroupBy(i => i.VariantId))
            {
                Require(
                    group.Key > 0
                        && db.BienTheSanPham.Any(v => v.MaBienThe == group.Key)
                        && group.Sum(i => i.Quantity) <= 20,
                    "Biến thể giỏ không hợp lệ."
                );
                db.ChiTietGioHang.Add(
                    new()
                    {
                        MaGioHang = cart.MaGioHang,
                        MaBienThe = group.Key,
                        SoLuong = group.Sum(i => i.Quantity),
                    }
                );
            }

            db.YeuThich.RemoveRange(db.YeuThich.Where(w => w.MaKH == customerId));

            db.SaveChanges();
            foreach (int id in wishlist.Distinct())
            {
                Require(db.SanPham.Any(p => p.MaSP == id), "Sản phẩm yêu thích không tồn tại.");
                db.YeuThich.Add(
                    new()
                    {
                        MaKH = customerId,
                        MaSP = id,
                        NgayThem = DateTime.Now,
                    }
                );
            }

            return 0;
        });

    // MergeGuestShopping: Gộp giỏ và yêu thích vãng lai vào tài khoản.
    public void MergeGuestShopping(int customerId, List<CartItem> guest, List<int> wishes)
    {
        var cart = UserCart(customerId);
        var products = Read(d => d.Products);
        foreach (var i in guest)
        {
            var p = products.FirstOrDefault(p => p.Id == i.ProductId && p.Active);
            var v = p?.Variants.FirstOrDefault(v =>
                v.Active && (i.VariantId > 0 ? v.Id == i.VariantId : v.Size == i.Size)
            );
            if (v == null)
                continue;
            var existing = cart.FirstOrDefault(x => x.VariantId == v.Id);
            if (existing == null)
                cart.Add(
                    new()
                    {
                        ProductId = v.ProductId,
                        VariantId = v.Id,
                        Size = v.Size,
                        Color = v.Color,
                        Quantity = Math.Clamp(i.Quantity, 1, 20),
                    }
                );
            else
                existing.Quantity = Math.Min(20, existing.Quantity + Math.Max(0, i.Quantity));
        }

        SaveShopping(
            customerId,
            cart,
            UserWishlist(customerId)
                .Concat(wishes.Where(id => products.Any(p => p.Id == id)))
                .Distinct()
                .ToList()
        );
    }
}
