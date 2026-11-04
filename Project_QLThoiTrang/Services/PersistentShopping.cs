// VAI TRÒ FILE: Lưu giỏ và yêu thích theo khách vào bảng SQL; gộp giỏ khách vãng lai khi đăng nhập.
// Xóa chi tiết ở
// đây là thay nội dung giỏ/yêu thích, không xóa sản phẩm.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // HÀM UserCart: Đọc giỏ lâu dài của khách từ GioHang/ChiTietGioHang, join biến thể, size và màu
    // thành CartItem.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // TRẢ VỀ (List<CartItem>): danh sách/truy vấn các phần tử: dòng giỏ chứa mã sản phẩm/biến thể,
    // size/màu và số lượng; không tự quyết định giá bán; có thể rỗng khi không có dữ liệu khớp.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // LIÊN KẾT XỬ LÝ: NativeRead; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
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

    // HÀM UserWishlist: Đọc danh sách MaSP yêu thích thuộc khách từ bảng YeuThich.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // TRẢ VỀ (List<int>): danh sách/truy vấn các phần tử: int; có thể rỗng khi không có dữ liệu
    // khớp.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA x (YeuThichRow): entity của bảng YeuThich, liên kết khách với sản phẩm yêu thích;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeRead; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public List<int> UserWishlist(int customerId) =>
        NativeRead(db => db.YeuThich.Where(x => x.MaKH == customerId).Select(x => x.MaSP).ToList());

    // HÀM SaveShopping: Đồng bộ giỏ và yêu thích của khách đang hoạt động trong một NativeWrite:
    // tạo đầu giỏ nếu thiếu, xóa chi tiết cũ rồi thêm danh sách đã gom theo VariantId; thay danh
    // sách yêu thích bằng các mã không trùng.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ items (List<CartItem>): danh sách dòng giỏ hiện tại cần lưu lâu dài cho khách.
    // THAM SỐ wishlist (List<int>): các mã sản phẩm yêu thích của khách cần đồng bộ xuống SQL.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA i (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
    // quyết định giá bán; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (GioHangRow): entity của bảng GioHang, đầu giỏ theo khách; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ChiTietGioHangRow): entity của bảng ChiTietGioHang, các dòng biến thể trong
    // giỏ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
    // và SKU; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA w (YeuThichRow): entity của bảng YeuThich, liên kết khách với sản phẩm yêu thích;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (SanPhamRow): entity của bảng SanPham, thông tin sản phẩm gốc; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void SaveShopping(int customerId, List<CartItem> items, List<int> wishlist) =>
        NativeWrite(db =>
        {
            // ĐIỀU KIỆN: Tài khoản không hoạt động. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(
                customerId > 0 && db.KhachHang.Any(c => c.MaKH == customerId && c.TrangThai),
                "Tài khoản không hoạt động."
            );
            // ĐIỀU KIỆN: Giỏ hàng không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(
                items.Count <= 100 && items.All(i => i.Quantity is >= 1 and <= 20),
                "Giỏ hàng không hợp lệ."
            );
            // BIẾN cart (GioHangRow): entity của bảng GioHang, đầu giỏ theo khách; kết quả tra cứu
            // có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var cart = db.GioHang.SingleOrDefault(c => c.MaKH == customerId);
            if (cart == null)
            {
                cart = new() { MaKH = customerId, NgayTao = DateTime.Now };
                db.Add(cart);
                // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài
                // chỉ hoàn tất khi
                // Commit thành công.
                db.SaveChanges();
            }

            // ĐỒNG BỘ GIỎ: xóa các chi tiết cũ của đúng giỏ trước khi thêm danh sách mới; không

            // đụng bảng sản phẩm hay
            // đơn đã đặt.
            db.ChiTietGioHang.RemoveRange(
                db.ChiTietGioHang.Where(l => l.MaGioHang == cart.MaGioHang)
            );
            // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
            // hoàn tất khi Commit
            // thành công.
            db.SaveChanges();
            // BIẾN LẶP group (IGrouping<int, CartItem>): một nhóm phần tử có cùng Key do GroupBy
            // tạo ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi lượt nhận một phần tử của nguồn
            // sau in, chỉ dùng trong vòng foreach này.
            foreach (var group in items.GroupBy(i => i.VariantId))
            {
                // ĐIỀU KIỆN: Biến thể giỏ không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng
                // xử lý.
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

            // ĐỒNG BỘ YÊU THÍCH: thay danh sách của riêng khách này bằng tập mã mới đã loại trùng.
            db.YeuThich.RemoveRange(db.YeuThich.Where(w => w.MaKH == customerId));
            // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
            // hoàn tất khi Commit
            // thành công.
            db.SaveChanges();
            // BIẾN LẶP id (int): số nguyên 32 bit; mỗi lượt nhận một phần tử của nguồn sau in, chỉ
            // dùng trong vòng foreach này.
            foreach (int id in wishlist.Distinct())
            {
                // ĐIỀU KIỆN: Sản phẩm yêu thích không tồn tại. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
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

    // HÀM MergeGuestShopping: Sau đăng nhập, ghép giỏ khách vãng lai vào giỏ SQL: bỏ sản phẩm/biến
    // thể không còn bán, cộng lượng cùng biến thể tối đa 20, hợp nhất yêu thích rồi SaveShopping.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ guest (List<CartItem>): các dòng giỏ khách vãng lai trước đăng nhập, dùng gộp với giỏ
    // tài khoản.
    // THAM SỐ wishes (List<int>): các mã yêu thích khách vãng lai trước đăng nhập, dùng hợp nhất và
    // loại trùng.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
    // quyết định giá bán; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA id (int): số nguyên 32 bit; tên này chỉ có nghĩa trong lambda tương ứng, không
    // phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Read → SaveShopping; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void MergeGuestShopping(int customerId, List<CartItem> guest, List<int> wishes)
    {
        // BIẾN cart (List<CartItem>): danh sách/truy vấn các phần tử: dòng giỏ chứa mã sản
        // phẩm/biến thể, size/màu và số lượng; không tự quyết định giá bán.
        var cart = UserCart(customerId);
        // BIẾN products (List<Product>): danh sách/truy vấn các phần tử: sản phẩm gồm thông tin
        // bán, giá mặc định, trạng thái và các biến thể.
        var products = Read(d => d.Products);
        // BIẾN LẶP i (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
        // quyết định giá bán; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
        // foreach này.
        foreach (var i in guest)
        {
            // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
            // thể; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var p = products.FirstOrDefault(p => p.Id == i.ProductId && p.Active);
            // BIẾN v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; kết
            // quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var v = p?.Variants.FirstOrDefault(v =>
                v.Active && (i.VariantId > 0 ? v.Id == i.VariantId : v.Size == i.Size)
            );
            if (v == null)
                continue;
            // BIẾN existing (CartItem): bản ghi/dòng đã tồn tại để gộp hoặc tránh tạo trùng; dòng
            // giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự quyết định giá bán.
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
