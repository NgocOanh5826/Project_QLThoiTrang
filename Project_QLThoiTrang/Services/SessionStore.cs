// VAI TRÒ FILE: Lưu/đọc JSON Session và dựng giỏ theo dữ liệu server. Session phục vụ khách vãng
// lai; tài khoản
// Customer có thêm bản lưu SQL.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Text.Json;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public static class SessionStore
{
    // HÀM Get: Đọc JSON từ Session theo key và giải tuần tự thành T; chưa có giá trị thì trả new T
    // để giỏ/yêu thích có thể khởi tạo rỗng.
    // KIỂU T: kiểu dữ liệu JSON cần đọc/lưu Session; Get yêu cầu tạo được new T() để có giá trị
    // rỗng khi chưa lưu.
    // THAM SỐ session (ISession): vùng lưu theo phiên trình duyệt; Get/Set chuyển đối tượng qua
    // JSON trong Session.
    // THAM SỐ key (string): tên khóa Session hoặc tên trường bộ lọc; cùng key phải được dùng thống
    // nhất khi đọc/ghi.
    // TRẢ VỀ (T): T đọc từ JSON Session; không có giá trị thì trả đối tượng new T().
    public static T Get<T>(ISession session, string key)
        where T : new() =>
        JsonSerializer.Deserialize<T>(session.GetString(key) ?? "null") ?? new T();

    // HÀM Set: Tuần tự dữ liệu thành JSON rồi lưu Session theo key; khách đăng nhập còn được đồng
    // bộ giỏ/yêu thích xuống SQL bởi ShoppingSession.
    // KIỂU T: kiểu dữ liệu JSON cần đọc/lưu Session; Get yêu cầu tạo được new T() để có giá trị
    // rỗng khi chưa lưu.
    // THAM SỐ session (ISession): vùng lưu theo phiên trình duyệt; Get/Set chuyển đối tượng qua
    // JSON trong Session.
    // THAM SỐ key (string): tên khóa Session hoặc tên trường bộ lọc; cùng key phải được dùng thống
    // nhất khi đọc/ghi.
    // THAM SỐ value (T): đối tượng kiểu T cần tuần tự JSON và lưu vào Session.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    public static void Set<T>(ISession session, string key, T value) =>
        session.SetString(key, JsonSerializer.Serialize(value));

    // HÀM Cart: Ghép các dòng Session với sản phẩm/biến thể hiện có, lấy giá server và loại dòng
    // không tìm thấy sản phẩm/biến thể để dựng CartViewModel hiển thị.
    // THAM SỐ session (ISession): vùng lưu theo phiên trình duyệt; Get/Set chuyển đối tượng qua
    // JSON trong Session.
    // THAM SỐ store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL; được DI
    // cung cấp, không phải dữ liệu form.
    // TRẢ VỀ (CartViewModel): giỏ đã ghép giá server, các dòng hàng, phí vận chuyển và tổng hiển
    // thị; ý nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA i (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
    // quyết định giá bán; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    public static CartViewModel Cart(ISession session, StoreService store)
    {
        // BIẾN items (List<CartItem>): danh sách/truy vấn các phần tử: dòng giỏ chứa mã sản
        // phẩm/biến thể, size/màu và số lượng; không tự quyết định giá bán.
        var items = Get<List<CartItem>>(session, "cart");
        return store.Read(d => new CartViewModel
        {
            Lines = items
                .Select(i =>
                {
                    // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các
                    // biến thể; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc
                    // ?? ngay sau.
                    var p = d.Products.FirstOrDefault(p => p.Id == i.ProductId);
                    // BIẾN v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và
                    // tồn; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
                    // ngay sau.
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
