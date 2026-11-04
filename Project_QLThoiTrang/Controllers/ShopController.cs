// VAI TRÒ FILE: Danh mục bán hàng, yêu thích, giỏ, đánh giá và đặt hàng. Form ở Views/Shop và
// partial đánh giá; giỏ
// đồng bộ qua ShoppingSession, lưu đơn trong StoreOperations.PlaceOrder.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
public partial class ShopController(StoreService store) : Controller
{
    // THUỘC TÍNH ShopController.CustomerId (int): id lấy từ claim của người đang đăng nhập; có thể
    // âm với nhân viên, dương với khách; không lấy từ ô input do người dùng tự chọn.
    int CustomerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // HÀM Index: Lọc sản phẩm đang bán theo từ khóa tên/màu, danh mục, bộ sưu tập mới/ưu đãi và thứ
    // tự giá/tên/mã; đóng gói CatalogViewModel cho view.
    // THAM SỐ q (string?): từ khóa tìm kiếm; rỗng nghĩa là không lọc theo từ khóa.
    // THAM SỐ category (string?): tên danh mục cần lọc; bỏ trống để lấy mọi danh mục.
    // THAM SỐ sort (string): kiểu sắp xếp: price-asc, price-desc, name; mặc định new sắp theo mã
    // sản phẩm giảm dần. Mặc định khi không truyền: "new".
    // THAM SỐ collection (string?): bộ sưu tập lọc: new chỉ hàng mới, sale chỉ hàng ưu đãi; null
    // không lọc bộ sưu tập. Mặc định khi không truyền: null.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Shop/Index. Tham số hàm: (string? q, string? category, string sort = "new",
    // string? collection = null).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shop/Index.cshtml.
    public IActionResult Index(
        string? q,
        string? category,
        string sort = "new",
        string? collection = null
    )
    {
        // BIẾN vm (CatalogViewModel): CatalogViewModel cuối cùng chứa sản phẩm đã lọc cùng lựa chọn
        // lọc/sắp xếp để trả về view.
        var vm = store.Read(d =>
        {
            // BIẾN products (IEnumerable<Product>): danh sách/truy vấn các phần tử: sản phẩm gồm
            // thông tin bán, giá mặc định, trạng thái và các biến thể.
            var products = d.Products.Where(p => p.Active);
            if (!string.IsNullOrWhiteSpace(q))
                products = products.Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || p.Color.Contains(q, StringComparison.OrdinalIgnoreCase)
                );
            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category == category);
            if (collection == "new")
                products = products.Where(p => p.IsNew);
            if (collection == "sale")
                products = products.Where(p => p.OnSale);
            products = sort switch
            {
                "price-asc" => products.OrderBy(p => p.Price),
                "price-desc" => products.OrderByDescending(p => p.Price),
                "name" => products.OrderBy(p => p.Name),
                _ => products.OrderByDescending(p => p.Id),
            };
            return new CatalogViewModel
            {
                Products = products.ToList(),
                Categories = d.Categories,
                Query = q,
                Category = category,
                Sort = sort,
                Collection = collection,
            };
        });
        return View(vm);
    }

    // HÀM Detail: Mở chi tiết sản phẩm đang bán, nạp đánh giá và tối đa 4 sản phẩm liên quan cùng
    // danh mục; sản phẩm không tồn tại hoặc đã ẩn trả 404.
    // THAM SỐ id (int): mã sản phẩm đang bán cần hiện trang chi tiết, đánh giá và gợi ý liên quan.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Shop/Detail. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shop/Detail.cshtml.
    public IActionResult Detail(int id)
    {
        // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến thể;
        // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var p = store.Read(d => d.Products.FirstOrDefault(p => p.Id == id && p.Active));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (p == null)
            return NotFound();
        ViewBag.Reviews = store.Reviews(id);
        ViewBag.Related = store.Read(d =>
            d.Products.Where(x => x.Active && x.Id != id && x.Category == p.Category)
                .Take(4)
                .ToList()
        );
        return View(p);
    }

    // HÀM Wishlist: Hiển thị sản phẩm đang hoạt động trong danh sách yêu thích ở Session; khách
    // đăng nhập được nạp danh sách SQL trước action bởi ShoppingSession.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Shop/Wishlist. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shop/Wishlist.cshtml.
    public IActionResult Wishlist()
    {
        // BIẾN ids (List<int>): các mã sản phẩm yêu thích lấy từ Session, dùng lọc danh sách sản
        // phẩm vẫn đang hoạt động.
        var ids = SessionStore.Get<List<int>>(HttpContext.Session, "wishlist");
        return View(
            store.Read(d => d.Products.Where(p => p.Active && ids.Contains(p.Id)).ToList())
        );
    }

    // HÀM Favorite: Bật/tắt yêu thích sản phẩm: đã có thì Remove id, chưa có thì Add id; sản phẩm
    // phải còn hoạt động. Chỉ chuyển hướng tới returnUrl nội bộ.
    // THAM SỐ id (int): mã sản phẩm cần thêm vào hoặc bỏ khỏi danh sách yêu thích.
    // THAM SỐ returnUrl (string?): đường dẫn quay về sau thao tác; controller chỉ giữ URL nội bộ để
    // tránh chuyển hướng ra ngoài.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Shop/Favorite. Tham số hàm: (int id, string? returnUrl).
    [HttpPost]
    public IActionResult Favorite(int id, string? returnUrl)
    {
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (!store.Read(d => d.Products.Any(p => p.Id == id && p.Active)))
            return NotFound();
        // BIẾN ids (List<int>): danh sách mã yêu thích trong Session; Remove thành công là bỏ
        // thích, chưa có thì Add để thích.
        var ids = SessionStore.Get<List<int>>(HttpContext.Session, "wishlist");
        if (!ids.Remove(id))
            ids.Add(id);
        SessionStore.Set(HttpContext.Session, "wishlist", ids);
        TempData["Success"] = "Đã cập nhật danh sách yêu thích.";
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Shop/Wishlist");
    }

    // HÀM Cart: Dựng giỏ từ Session và giá/biến thể hiện tại trên server bằng SessionStore.Cart;
    // ShoppingSession chặn tài khoản nhân viên mua hàng.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Shop/Cart. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shop/Cart.cshtml.
    public IActionResult Cart() => View(SessionStore.Cart(HttpContext.Session, store));

    // HÀM Add: Thêm sản phẩm vào giỏ: sản phẩm và biến thể phải hoạt động, lượng 1–20 và tổng cùng
    // biến thể không quá min(20, tồn). Chưa có dòng thì Add, đã có thì tăng Quantity, rồi lưu
    // Session.
    // THAM SỐ id (int): mã sản phẩm gốc cần thêm vào giỏ, còn variantId/size chọn biến thể của sản
    // phẩm đó.
    // THAM SỐ size (string): tên kích thước, dùng tra biến thể khi không truyền mã biến thể trong
    // nhánh tương thích. Mặc định khi không truyền: "M".
    // THAM SỐ quantity (int): lượng cần thêm/cập nhật trong giỏ, từ 1–20; UpdateCart nhận 0 để xóa
    // dòng. Mặc định khi không truyền: 1.
    // THAM SỐ variantId (int): mã biến thể sản phẩm, dùng phân biệt đúng tổ hợp size/màu thay vì
    // chỉ mã sản phẩm. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA i (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
    // quyết định giá bán; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Shop/Add. Tham số hàm: (int id, string size = "M", int quantity = 1, int
    // variantId = 0).
    [HttpPost]
    public IActionResult Add(int id, string size = "M", int quantity = 1, int variantId = 0)
    {
        // BIẾN product (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
        // thể; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var product = store.Read(d => d.Products.FirstOrDefault(p => p.Id == id && p.Active));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (product == null)
            return NotFound();
        // BIẾN variant (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; kết
        // quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var variant = product.Variants.FirstOrDefault(v =>
            v.Active && (variantId > 0 ? v.Id == variantId : v.Size == size)
        );
        // BIẾN items (List<CartItem>): danh sách/truy vấn các phần tử: dòng giỏ chứa mã sản
        // phẩm/biến thể, size/màu và số lượng; không tự quyết định giá bán.
        var items = SessionStore.Get<List<CartItem>>(HttpContext.Session, "cart");
        if (
            variant == null
            || quantity < 1
            || quantity > 20
            || items.Where(i => i.VariantId == variant.Id).Sum(i => i.Quantity) + quantity
                > Math.Min(20, variant.Stock)
        )
        {
            TempData["Error"] = "Biến thể hoặc số lượng không hợp lệ; hãy kiểm tra tồn kho.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // BIẾN existing (CartItem): bản ghi/dòng đã tồn tại để gộp hoặc tránh tạo trùng; dòng giỏ
        // chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự quyết định giá bán.
        var existing = items.FirstOrDefault(i => i.VariantId == variant.Id);
        if (existing == null)
            items.Add(
                new()
                {
                    ProductId = id,
                    VariantId = variant.Id,
                    Size = variant.Size,
                    Color = variant.Color,
                    Quantity = quantity,
                }
            );
        else
            existing.Quantity += quantity;
        SessionStore.Set(HttpContext.Session, "cart", items);
        TempData["Success"] = "Đã thêm sản phẩm vào giỏ.";
        return RedirectToAction(nameof(Cart));
    }

    // HÀM UpdateCart: Sửa màu/size/số lượng hoặc xóa dòng giỏ. remove = true hoặc quantity = 0 thì
    // Remove; khi đổi biến thể trùng dòng khác thì gộp lượng. Kiểm tra tổng gộp không vượt 20/tồn
    // trước khi lưu Session.
    // THAM SỐ id (int): mã sản phẩm gốc dùng cùng variantId/size xác định đúng dòng giỏ cần sửa
    // hoặc bỏ.
    // THAM SỐ size (string?): tên kích thước, dùng tra biến thể khi không truyền mã biến thể trong
    // nhánh tương thích.
    // THAM SỐ quantity (int): lượng cần thêm/cập nhật trong giỏ, từ 1–20; UpdateCart nhận 0 để xóa
    // dòng.
    // THAM SỐ remove (bool): true để bỏ dòng giỏ; false tiếp tục nhánh đổi biến thể hoặc cập nhật
    // số lượng. Mặc định khi không truyền: false.
    // THAM SỐ variantId (int): mã biến thể sản phẩm, dùng phân biệt đúng tổ hợp size/màu thay vì
    // chỉ mã sản phẩm. Mặc định khi không truyền: 0.
    // THAM SỐ newVariantId (int): mã biến thể mới khi đổi màu/size trong giỏ; 0 giữ biến thể hiện
    // tại. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA i (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
    // quyết định giá bán; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Shop/UpdateCart. Tham số hàm: (int id, string? size, int quantity, bool
    // remove = false, int variantId = 0, int newVariantId = 0).
    [HttpPost]
    public IActionResult UpdateCart(
        int id,
        string? size,
        int quantity,
        bool remove = false,
        int variantId = 0,
        int newVariantId = 0
    )
    {
        // BIẾN items (List<CartItem>): danh sách/truy vấn các phần tử: dòng giỏ chứa mã sản
        // phẩm/biến thể, size/màu và số lượng; không tự quyết định giá bán.
        var items = SessionStore.Get<List<CartItem>>(HttpContext.Session, "cart");
        // BIẾN item (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
        // quyết định giá bán; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc
        // ?? ngay sau.
        var item = items.FirstOrDefault(i =>
            i.ProductId == id && (variantId > 0 ? i.VariantId == variantId : i.Size == size)
        );
        if (item == null)
            return RedirectToAction(nameof(Cart));
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Thông tin giỏ hàng không hợp lệ.";
            return RedirectToAction(nameof(Cart));
        }

        // NHÁNH XÓA GIỎ: nút xóa hoặc số lượng bằng 0 chỉ bỏ dòng giỏ, không xóa sản phẩm khỏi cửa

        // hàng.
        if (remove || quantity == 0)
            // XÓA DÒNG GIỎ: bỏ phần tử khỏi danh sách trong bộ nhớ; SessionStore.Set phía dưới lưu
            // giỏ mới.
            items.Remove(item);
        else
        {
            // BIẾN targetId (int): mã biến thể đích khi sửa giỏ: lấy newVariantId nếu > 0, ngược
            // lại giữ mã biến thể của dòng cũ.
            var targetId = newVariantId > 0 ? newVariantId : item.VariantId;
            // BIẾN variant (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn;
            // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var variant = store.Read(d =>
                d.Products.Any(p => p.Id == id && p.Active)
                    ? d.Variants.FirstOrDefault(v =>
                        v.Id == targetId && v.ProductId == id && v.Active
                    )
                    : null
            );
            // BIẾN existing (CartItem): bản ghi/dòng đã tồn tại để gộp hoặc tránh tạo trùng; dòng
            // giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự quyết định giá bán.
            var existing = items.FirstOrDefault(i => i != item && i.VariantId == targetId);
            // BIẾN total (long): lượng cuối sau gộp vào biến thể đích, dùng long để tránh tràn khi
            // cộng dữ liệu số người dùng gửi.
            var total = (long)quantity + (existing?.Quantity ?? 0);
            if (variant == null || quantity < 1 || total > Math.Min(20, variant.Stock))
            {
                TempData["Error"] = (
                    "Màu, kích cỡ hoặc số lượng không hợp lệ; tổng trong giỏ "
                    + "không được vượt tồn kho và 20 sản phẩm mỗi phiên bản."
                );
                return RedirectToAction(nameof(Cart));
            }

            if (existing != null)
            {
                existing.Quantity = (int)total;
                // XÓA DÒNG GIỎ: bỏ phần tử khỏi danh sách trong bộ nhớ; SessionStore.Set phía dưới
                // lưu giỏ mới.
                items.Remove(item);
            }
            else
            {
                item.VariantId = variant.Id;
                item.Size = variant.Size;
                item.Color = variant.Color;
                item.Quantity = quantity;
            }
        }

        SessionStore.Set(HttpContext.Session, "cart", items);
        return RedirectToAction(nameof(Cart));
    }

    // HÀM Review: Nhận đánh giá và tối đa 5 tệp từ khách; giới hạn request, đọc byte vào bộ nhớ rồi
    // chuyển kiểm tra đã mua/số sao/định dạng tệp sang StoreService.Review.
    // THAM SỐ id (int): mã sản phẩm khách muốn đánh giá; service kiểm tra đã mua và đơn đã giao.
    // THAM SỐ stars (int): số sao đánh giá, bắt buộc trong khoảng 1 đến 5.
    // THAM SỐ text (string): nội dung đánh giá sau Trim, cần 5–1.000 ký tự.
    // THAM SỐ media (List<IFormFile>?): các IFormFile đính kèm đánh giá; controller đọc byte rồi
    // service kiểm tra loại và dung lượng.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // BIẾN LAMBDA f (IFormFile): IFormFile; tên này chỉ có nghĩa trong lambda tương ứng, không phải
    // biến toàn cục.
    // ĐIỂM VÀO: POST /Shop/Review. Tham số hàm: (int id, int stars, string text, List<IFormFile>?
    // media).
    [
        Authorize(Roles = "Customer"),
        HttpPost,
        RequestSizeLimit(33554432),
        RequestFormLimits(MultipartBodyLengthLimit = 33554432)
    ]
    public async Task<IActionResult> Review(int id, int stars, string text, List<IFormFile>? media)
    {
        try
        {
            media ??= [];
            if (
                media.Count > 5
                || media.Sum(f => f.Length) > ReviewUploads.TotalLimit
                || media.Any(f => f.Length <= 0 || f.Length > ReviewUploads.VideoLimit)
            )
                throw new InvalidOperationException(
                    "Tối đa 5 tệp, mỗi ảnh 5 MB, video 20 MB và tổng 30 MB."
                );
            // BIẾN uploads (List<ReviewUpload>): danh sách tệp đã đọc thành byte từ IFormFile, còn
            // phải được service kiểm tra trước lưu.
            var uploads = new List<ReviewUpload>();
            // BIẾN LẶP file (IFormFile): IFormFile; mỗi lượt nhận một phần tử của nguồn sau in, chỉ
            // dùng trong vòng foreach này.
            foreach (var file in media)
            {
                // BIẾN buffer (MemoryStream): MemoryStream giữ byte một tệp upload trong RAM trước
                // khi chuyển sang ReviewUpload.
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, HttpContext.RequestAborted);
                uploads.Add(new(file.FileName, buffer.ToArray()));
            }

            store.Review(CustomerId, id, stars, text, uploads);
            TempData["Success"] = "Đã lưu đánh giá.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // HÀM ReviewMedia: Trả ảnh/video theo Guid; chỉ nội dung của đánh giá đang hiển thị hoặc người
    // xem Admin/Support được đọc. Dùng nosniff, no-store và hỗ trợ range cho video.
    // THAM SỐ id (Guid): mã Guid tệp đánh giá cần đọc.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // ĐIỂM VÀO: GET /Shop/ReviewMedia. Tham số hàm: (Guid id).
    public IActionResult ReviewMedia(Guid id)
    {
        // BIẾN staff (bool): cờ cho phép xem media đánh giá bị ẩn vì người dùng là Admin hoặc
        // Support.
        bool staff = User.IsInRole("Admin") || User.IsInRole("Support");
        // BIẾN media (ReviewMedia): tệp đánh giá đã xác định loại nội dung, chứa mã Guid, ReviewId
        // và byte tệp; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay
        // sau.
        var media = store.NativeRead(db =>
            (
                from m in db.ReviewMedia
                join r in db.DanhGia on m.ReviewId equals r.MaDanhGia
                where m.Id == id && (r.TrangThai || staff)
                select m
            ).FirstOrDefault()
        );
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (media == null)
            return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(media.Content, media.ContentType, enableRangeProcessing: true);
    }

    // HÀM Checkout: Khách mở thanh toán khi giỏ có hàng; điền hồ sơ, tạo checkout-token và chụp
    // checkout-quote trong Session để phát hiện giỏ/giá đổi khi gửi form.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Shop/Checkout. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shop/Checkout.cshtml.
    [Authorize(Roles = "Customer")]
    public IActionResult Checkout()
    {
        if (SessionStore.Cart(HttpContext.Session, store).Lines.Count == 0)
            return RedirectToAction(nameof(Cart));
        // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
        // trò và trạng thái.
        var c = store.Read(d => d.Customers.First(c => c.Id == CustomerId));
        ViewBag.Cart = SessionStore.Cart(HttpContext.Session, store);
        // BIẾN token (string): Guid dạng N dùng nhận diện lần checkout trong Session; POST phải gửi
        // đúng token này.
        var token = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("checkout-token", token);
        SessionStore.Set(
            HttpContext.Session,
            "checkout-quote",
            ((CartViewModel)ViewBag.Cart).Lines
        );
        return View(
            new CheckoutInput
            {
                Name = c.Name,
                Phone = c.Phone,
                Address = c.Address,
                CheckoutToken = token,
            }
        );
    }

    // HÀM Checkout: Kiểm tra giỏ không rỗng, ModelState và checkout-token khớp Session; PlaceOrder
    // đối chiếu báo giá/trừ tồn/lưu đơn. Chỉ xóa giỏ sau khi đặt thành công rồi chuyển trang
    // Success.
    // THAM SỐ input (CheckoutInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Shop/Checkout. Tham số hàm: (CheckoutInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shop/Checkout.cshtml.
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult Checkout(CheckoutInput input)
    {
        // BIẾN cart (CartViewModel): giỏ đã ghép giá server, các dòng hàng, phí vận chuyển và tổng
        // hiển thị.
        var cart = SessionStore.Cart(HttpContext.Session, store);
        ViewBag.Cart = cart;
        // KIỂM TRA FORM: nhánh này thêm lỗi ", "; ModelState sẽ ngăn bước lưu bên dưới.
        if (cart.Lines.Count == 0)
        {
            ModelState.AddModelError("", "Giỏ hàng đang trống.");
            return View(input);
        }

        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để

        // không lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        // KIỂM TRA FORM: nhánh này thêm lỗi ", "; ModelState sẽ ngăn bước lưu bên dưới.
        if (input.CheckoutToken != HttpContext.Session.GetString("checkout-token"))
        {
            ModelState.AddModelError("", "Phiên đặt hàng đã thay đổi; mở lại trang thanh toán.");
            return View(input);
        }

        try
        {
            // BIẾN order (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
            // khoản tiền.
            var order = store.PlaceOrder(
                CustomerId,
                input,
                SessionStore.Get<List<CartItem>>(HttpContext.Session, "cart"),
                SessionStore.Get<List<OrderLine>>(HttpContext.Session, "checkout-quote")
            );
            SessionStore.Set(HttpContext.Session, "cart", new List<CartItem>());
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(input);
        }
    }

    // HÀM Success: Chỉ hiển thị xác nhận đơn thuộc đúng tài khoản đang đăng nhập; mã đơn không khớp
    // chủ trả 404.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Shop/Success. Tham số hàm: (string id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Shop/Success.cshtml.
    [Authorize]
    public IActionResult Success(string id)
    {
        // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var o = store.Read(d =>
            d.Orders.FirstOrDefault(o => o.Id == id && o.CustomerId == CustomerId)
        );
        return o == null ? NotFound() : View(o);
    }
}
