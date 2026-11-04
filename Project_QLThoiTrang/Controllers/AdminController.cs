// VAI TRÒ FILE: Nhận yêu cầu quản trị sản phẩm, danh mục, đơn, kho, khách, ưu đãi và cấu hình. Form
// ở Views/Admin;
// Write/StoreOperations/NativeManagement chịu phần lưu. Quyền từng action nằm trong
// AdminExtensions.OnActionExecuting.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
[Authorize(Roles = "Admin,Catalog,Inventory,Sales,Support")]
public partial class AdminController(StoreService store) : Controller
{
    // THUỘC TÍNH AdminController.ActorId (int): id lấy từ claim của người đang đăng nhập; có thể âm
    // với nhân viên, dương với khách; không lấy từ ô input do người dùng tự chọn.
    int ActorId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    // HÀM Index: Trang tổng quan quản trị; lấy StoreData để view tổng hợp sản phẩm, đơn, khách và
    // tồn.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Admin/Index. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Index.cshtml.
    public IActionResult Index() => View(store.Read(d => d));

    // HÀM Products: Danh sách sản phẩm; lọc theo từ khóa tên/màu, danh mục, thương hiệu và trạng
    // thái; chuẩn bị các lựa chọn bộ lọc trong ViewBag.
    // THAM SỐ q (string?): từ khóa tìm kiếm; rỗng nghĩa là không lọc theo từ khóa.
    // THAM SỐ category (string?): tên danh mục cần lọc; bỏ trống để lấy mọi danh mục.
    // THAM SỐ brandId (int?): mã thương hiệu lọc: null không lọc, 0 chọn sản phẩm chưa có thương
    // hiệu, số dương chọn đúng thương hiệu.
    // THAM SỐ state (string?): trạng thái lọc: active là đang hoạt động, inactive là ngừng; giá trị
    // khác không áp bộ lọc này.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (string): chuỗi ký tự; tên này chỉ có nghĩa trong lambda tương ứng, không phải
    // biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Products. Tham số hàm: (string? q, string? category, int? brandId,
    // string? state).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Products.cshtml.
    public IActionResult Products(string? q, string? category, int? brandId, string? state)
    {
        // BIẾN d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng.
        var d = store.Read(d => d);
        ViewBag.Query = q;
        ViewBag.Category = category;
        ViewBag.BrandId = brandId;
        ViewBag.State = state;
        ViewBag.Categories = d
            .Categories.Concat(d.Products.Select(p => p.Category))
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        ViewBag.Brands = store.References("brands");
        ViewBag.Total = d.Products.Count;
        return View(
            d.Products.Where(p =>
                    AdminListFilter.Matches(p.Name + " " + p.Color, q)
                    && (string.IsNullOrEmpty(category) || p.Category == category)
                    && (
                        !brandId.HasValue
                        || (brandId == 0 ? !p.BrandId.HasValue : p.BrandId == brandId)
                    )
                    && (state != "active" || p.Active)
                    && (state != "inactive" || !p.Active)
                )
                .ToList()
        );
    }

    // HÀM Product: Mở form thêm khi không có id, hoặc tìm sản phẩm để sửa khi có id; chuẩn bị
    // thương hiệu/danh mục, không tìm thấy trả 404.
    // THAM SỐ id (int?): mã sản phẩm; GET không truyền id mở form thêm, có id tìm sản phẩm để sửa.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Product. Tham số hàm: (int? id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Product.cshtml.
    public IActionResult Product(int? id)
    {
        ViewBag.Brands = store.References("brands");
        ViewBag.Categories = store.Read(d => d.Categories);
        // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến thể;
        // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var p =
            id == null
                ? new Product()
                : store.Read(d => d.Products.FirstOrDefault(p => p.Id == id));
        return p == null ? NotFound() : View(p);
    }

    // HÀM Product: Nhận form thêm/sửa sản phẩm. Kiểm tra danh mục có thật, ảnh thuộc thư viện và
    // ModelState; Id = 0 cấp mã, ép tồn ban đầu về 0 rồi Add; Id khác 0 tìm bản ghi và giữ nguyên
    // tồn cũ khi thay thông tin.
    // THAM SỐ input (Product): đối tượng nhận dữ liệu form; thuộc tính được bind theo name/asp-for,
    // không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA i (int): số nguyên 32 bit; tên này chỉ có nghĩa trong lambda tương ứng, không
    // phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Admin/Product. Tham số hàm: (Product input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Product.cshtml.
    [HttpPost]
    public IActionResult Product(Product input)
    {
        ViewBag.Brands = store.References("brands");
        ViewBag.Categories = store.Read(d => d.Categories);
        // KIỂM TRA FORM: nhánh này thêm lỗi "Danh mục không hợp lệ."; ModelState sẽ ngăn bước lưu
        // bên dưới.
        if (!((List<string>)ViewBag.Categories).Contains(input.Category))
            ModelState.AddModelError("Category", "Danh mục không hợp lệ.");
        // KIỂM TRA FORM: nhánh này thêm lỗi "Chọn ảnh có sẵn trong thư viện."; ModelState sẽ ngăn
        // bước lưu bên
        // dưới.
        if (!Enumerable.Range(1, 5).Any(i => input.Image == $"/images/products/aothun{i:00}.jpg"))
            ModelState.AddModelError("Image", "Chọn ảnh có sẵn trong thư viện.");
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        // BIẾN saved (bool): kết quả callback lưu sản phẩm: true đã thêm/cập nhật, false không tìm
        // thấy mã cần sửa.
        bool saved = store.Write(d =>
        {
            // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh còn lại xử
            // lý bản ghi đã
            // tồn tại nếu có.
            if (input.Id == 0)
            {
                input.Id = d.Products.Select(p => p.Id).DefaultIfEmpty().Max() + 1;
                input.Stock = 0;
                // BƯỚC THÊM SẢN PHẨM: thêm vào ảnh StoreData; Write sẽ kiểm tra và PersistNative
                // ghi sang bảng
                // SanPham.
                d.Products.Add(input);
                return true;
            }

            // BIẾN index (int): vị trí dòng trong List tìm bằng FindIndex, không phải mã SQL; -1
            // nghĩa là không tìm thấy.
            var index = d.Products.FindIndex(p => p.Id == input.Id);
            // KHÔNG TÌM THẤY: FindIndex trả -1 khi mã cần sửa không còn trong danh sách; không được
            // ghi đè dòng
            // khác.
            if (index < 0)
                return false;
            input.Stock = d.Products[index].Stock;
            // BƯỚC SỬA SẢN PHẨM: thay thông tin đúng vị trí tìm được; Stock đã được giữ từ bản ghi
            // cũ để form không
            // tự sửa tồn.
            d.Products[index] = input;
            return true;
        });
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (!saved)
            return NotFound();
        TempData["Success"] = "Đã lưu sản phẩm.";
        return RedirectToAction(nameof(Products));
    }

    // HÀM DeleteProduct: Mở trang xác nhận ẩn sản phẩm theo id; chưa thay đổi dữ liệu ở yêu cầu
    // GET.
    // THAM SỐ id (int): mã sản phẩm cần tải để hiện trang xác nhận ẩn.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/DeleteProduct. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/DeleteProduct.cshtml.
    public IActionResult DeleteProduct(int id)
    {
        // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến thể;
        // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var p = store.Read(d => d.Products.FirstOrDefault(p => p.Id == id));
        return p == null ? NotFound() : View(p);
    }

    // HÀM DeleteProductConfirmed: Xử lý nút xác nhận bằng POST /Admin/DeleteProduct: đặt Active =
    // false trong Write. Đây là xóa mềm để giữ lịch sử đơn hàng; không Remove sản phẩm khỏi SQL.
    // THAM SỐ id (int): mã sản phẩm cần đặt Active = false sau khi người dùng xác nhận.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Admin/DeleteProduct. Tham số hàm: (int id).
    [HttpPost, ActionName("DeleteProduct")]
    public IActionResult DeleteProductConfirmed(int id)
    {
        store.Write(d =>
        {
            // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
            // thể; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var p = d.Products.FirstOrDefault(p => p.Id == id);
            if (p != null)
                // BƯỚC ẨN SẢN PHẨM: Active = false làm sản phẩm biến mất khỏi danh sách bán; bản
                // ghi và lịch sử
                // được giữ.
                p.Active = false;
            return 0;
        });
        TempData["Success"] = "Đã ẩn sản phẩm khỏi cửa hàng; lịch sử đơn hàng được giữ lại.";
        return RedirectToAction(nameof(Products));
    }

    // HÀM Categories: Mở danh sách danh mục và dữ liệu liên quan để kiểm tra danh mục đang được sản
    // phẩm sử dụng.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Admin/Categories. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Categories.cshtml.
    public IActionResult Categories() => View(store.Read(d => d));

    // HÀM Category: Mở form thêm danh mục với tên ban đầu rỗng.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Admin/Category. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Category.cshtml.
    public IActionResult Category() => View("Category", "");

    // HÀM AddCategory: Action tương thích chuyển yêu cầu POST thêm danh mục sang Category(name);
    // dùng chung kiểm tra và cách lưu.
    // THAM SỐ name (string): tên danh mục/đối tượng được thêm hoặc tìm; nhánh xử lý bên dưới quyết
    // định trim và giới hạn độ dài.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/AddCategory. Tham số hàm: (string name).
    [HttpPost]
    public IActionResult AddCategory(string name) => Category(name);

    // HÀM Category: Thêm danh mục sau khi trim, yêu cầu tên 2–50 ký tự và không trùng không phân
    // biệt hoa/thường; hợp lệ thì thêm vào d.Categories trong Write.
    // THAM SỐ name (string): tên danh mục/đối tượng được thêm hoặc tìm; nhánh xử lý bên dưới quyết
    // định trim và giới hạn độ dài.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: POST /Admin/Category. Tham số hàm: (string name).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Category.cshtml.
    [HttpPost]
    public IActionResult Category(string name)
    {
        name = (name ?? "").Trim();
        // KIỂM TRA FORM: nhánh này thêm lỗi ", "; ModelState sẽ ngăn bước lưu bên dưới.
        if (name.Length is < 2 or > 50)
            ModelState.AddModelError("", "Tên danh mục cần 2–50 ký tự.");
        // DỮ LIỆU FORM: chỉ đi vào phần lưu khi binding và các DataAnnotations đều hợp lệ.
        if (ModelState.IsValid)
        {
            // BIẾN added (bool): true khi tên mới đã được thêm; false khi phát hiện tên trùng và
            // phải trả lỗi về form.
            bool added = store.Write(d =>
            {
                if (d.Categories.Contains(name, StringComparer.OrdinalIgnoreCase))
                    return false;
                d.Categories.Add(name);
                return true;
            });
            if (added)
            {
                TempData["Success"] = "Đã thêm danh mục.";
                return RedirectToAction(nameof(Categories));
            }

            ModelState.AddModelError("", "Danh mục đã tồn tại.");
        }

        return View("Category", name);
    }

    // HÀM DeleteCategory: Xóa danh mục bằng POST; nếu có bất kỳ sản phẩm nào đang thuộc danh mục
    // thì báo lỗi và giữ lại, ngược lại Remove khỏi d.Categories để PersistNative đồng bộ.
    // THAM SỐ name (string): tên danh mục/đối tượng được thêm hoặc tìm; nhánh xử lý bên dưới quyết
    // định trim và giới hạn độ dài.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Admin/DeleteCategory. Tham số hàm: (string name).
    [HttpPost]
    public IActionResult DeleteCategory(string name)
    {
        store.Write(d =>
        {
            if (d.Products.Any(p => p.Category == name))
                TempData["Error"] = "Danh mục đang chứa sản phẩm; chuyển sản phẩm trước khi xóa.";
            else
                // BƯỚC XÓA DANH MỤC: chỉ tới đây khi không còn sản phẩm tham chiếu; PersistNative
                // gọi Remove trên
                // DanhMuc để xóa bản ghi SQL.
                d.Categories.Remove(name);
            return 0;
        });
        return RedirectToAction(nameof(Categories));
    }

    // HÀM Orders: Lọc danh sách đơn theo trạng thái, từ khóa mã/tên/điện thoại, phương thức thanh
    // toán và khoảng ngày nhanh; trả đơn mới nhất trước.
    // THAM SỐ status (string?): trạng thái đích muốn cập nhật; phải nằm trong tập chuyển trạng thái
    // được phép của luồng tương ứng.
    // THAM SỐ q (string?): từ khóa tìm kiếm; rỗng nghĩa là không lọc theo từ khóa.
    // THAM SỐ payment (string?): phương thức thanh toán dùng làm điều kiện lọc đơn; bỏ trống để
    // không lọc.
    // THAM SỐ period (string?): kỳ thời gian nhanh, như today/last7/last30/month; controller đổi
    // thành mốc ngày ở switch.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Orders. Tham số hàm: (string? status, string? q, string? payment,
    // string? period).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Orders.cshtml.
    public IActionResult Orders(string? status, string? q, string? payment, string? period)
    {
        // BIẾN orders (List<Order>): danh sách/truy vấn các phần tử: đơn hàng gồm chủ đơn, người
        // nhận, trạng thái, dòng hàng và các khoản tiền.
        var orders = store.Read(d => d.Orders);
        ViewBag.Status = status;
        ViewBag.Query = q;
        ViewBag.Payment = payment;
        ViewBag.Period = period;
        ViewBag.Total = orders.Count;
        // BIẾN from (DateTime?): mốc ngày/giờ.
        DateTime? from = period switch
        {
            "today" => DateTime.Today,
            "last7" => DateTime.Today.AddDays(-6),
            "last30" => DateTime.Today.AddDays(-29),
            _ => null,
        };
        return View(
            orders
                .Where(o =>
                    (string.IsNullOrEmpty(status) || o.Status == status)
                    && AdminListFilter.Matches(o.Id + " " + o.Name + " " + o.Phone, q)
                    && (string.IsNullOrEmpty(payment) || o.Payment == payment)
                    && (
                        !from.HasValue || o.Created.Date >= from && o.Created.Date <= DateTime.Today
                    )
                )
                .OrderByDescending(o => o.Created)
                .ToList()
        );
    }

    // HÀM Order: Mở chi tiết đơn theo mã công khai, không có thì trả 404; dữ liệu hiển thị không
    // được sửa ngay tại GET.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Order. Tham số hàm: (string id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Order.cshtml.
    public IActionResult Order(string id)
    {
        // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var o = store.Read(d => d.Orders.FirstOrDefault(o => o.Id == id));
        return o == null ? NotFound() : View(o);
    }

    // HÀM OrderStatus: Nhận trạng thái mới, chuyển điều kiện chuyển bước/thu tiền/hoàn tồn sang
    // StoreService.ChangeOrderStatus; báo kết quả bằng TempData rồi quay lại chi tiết đơn.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ status (string): trạng thái đích muốn cập nhật; phải nằm trong tập chuyển trạng thái
    // được phép của luồng tương ứng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/OrderStatus. Tham số hàm: (string id, string status).
    [HttpPost]
    public IActionResult OrderStatus(string id, string status)
    {
        try
        {
            store.ChangeOrderStatus(id, status, ActorId);
            TempData["Success"] = "Đã cập nhật đơn hàng.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Order), new { id });
    }

    // HÀM Customers: Danh sách tài khoản Role Customer; lọc từ khóa, trạng thái và có/chưa có đơn,
    // không đưa nhân viên vào danh sách này.
    // THAM SỐ q (string?): từ khóa tìm kiếm; rỗng nghĩa là không lọc theo từ khóa.
    // THAM SỐ state (string?): trạng thái lọc: active là đang hoạt động, inactive là ngừng; giá trị
    // khác không áp bộ lọc này.
    // THAM SỐ hasOrders (string?): lọc khách theo yes (đã có đơn), no (chưa có đơn); giá trị khác
    // giữ cả hai nhóm.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Customers. Tham số hàm: (string? q, string? state, string? hasOrders).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Customers.cshtml.
    public IActionResult Customers(string? q, string? state, string? hasOrders)
    {
        // BIẾN d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng.
        var d = store.Read(d => d);
        ViewBag.Query = q;
        ViewBag.State = state;
        ViewBag.HasOrders = hasOrders;
        ViewBag.Total = d.Customers.Count(c => c.Role == "Customer");
        return View(
            d.Customers.Where(c =>
                    c.Role == "Customer"
                    && AdminListFilter.Matches(c.Name + " " + c.Email + " " + c.Phone, q)
                    && (state != "active" || c.Active)
                    && (state != "inactive" || !c.Active)
                    && (hasOrders != "yes" || d.Orders.Any(o => o.CustomerId == c.Id))
                    && (hasOrders != "no" || !d.Orders.Any(o => o.CustomerId == c.Id))
                )
                .ToList()
        );
    }

    // HÀM Customer: Hiển thị thông tin một tài khoản và danh sách đơn của tài khoản đó; id không
    // tồn tại trả 404.
    // THAM SỐ id (int): mã khách hàng cần xem/sửa; service chỉ sửa tài khoản Role Customer theo
    // quyền Support/Admin.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Customer. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Customer.cshtml.
    public IActionResult Customer(int id)
    {
        // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
        // trò và trạng thái; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
        // ngay sau.
        var c = store.Read(d => d.Customers.FirstOrDefault(c => c.Id == id));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (c == null)
            return NotFound();
        ViewBag.Orders = store.Read(d => d.Orders.Where(o => o.CustomerId == id).ToList());
        return View(c);
    }

    // HÀM Inventory: Mở kiểm kê tồn tổng theo sản phẩm, kèm 200 biến động kho gần nhất.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Inventory. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Inventory.cshtml.
    public IActionResult Inventory()
    {
        ViewBag.Movements = store.Read(d =>
            d.StockMovements.OrderByDescending(m => m.Id).Take(200).ToList()
        );
        return View(store.Read(d => d.Products));
    }

    // HÀM Inventory: Nhận số tồn thực tế và tồn expected lúc mở form; AdjustStock kiểm tra quyền,
    // chống ghi đè và ghi nhật ký thay đổi.
    // THAM SỐ id (int): mã bản ghi cần xử lý, được đối chiếu với khóa tương ứng trong thân hàm.
    // THAM SỐ quantity (int): số tồn thực tế muốn ghi sau kiểm kê (giá trị tuyệt đối), không phải
    // số tăng/giảm; phải 0–100.000.
    // THAM SỐ expected (int): số tồn người dùng đã thấy lúc mở form; so với tồn hiện tại để phát
    // hiện thay đổi và tránh ghi đè.
    // THAM SỐ reason (string): lý do nghiệp vụ do người dùng nhập; Text/attribute kiểm tra độ dài
    // trước khi ghi nhật ký hoặc phiếu.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Inventory. Tham số hàm: (int id, int quantity, int expected, string
    // reason).
    [HttpPost]
    public IActionResult Inventory(int id, int quantity, int expected, string reason)
    {
        try
        {
            store.AdjustStock(id, quantity, expected, reason, ActorId);
            TempData["Success"] = "Đã ghi kiểm kê.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Inventory));
    }

    // HÀM Receive: Mở form nhập kho một hoặc nhiều dòng theo tab; chỉ cho chọn kho/NCC đang hoạt
    // động qua ReceiveOptions.
    // THAM SỐ tab (string): tab nhập kho muốn mở: many cho nhiều dòng, giá trị khác về một dòng.
    // Mặc định khi không truyền: "single".
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Admin/Receive. Tham số hàm: (string tab = "single").
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Receive.cshtml.
    public IActionResult Receive(string tab = "single")
    {
        ViewBag.ReceiveTab = tab == "many" ? "many" : "single";
        ReceiveOptions();
        return View(new ReceiveInput());
    }

    // HÀM Receive: Nhận phiếu nhập một dòng; kiểm tra DataAnnotations rồi gọi Receive/ReceiveBatch.
    // Thành công quay về tồn kho; lỗi nghiệp vụ hiển thị lại trên form.
    // THAM SỐ input (ReceiveInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Receive. Tham số hàm: (ReceiveInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Receive.cshtml.
    [HttpPost]
    public IActionResult Receive(ReceiveInput input)
    {
        ReceiveOptions();
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        try
        {
            store.Receive(input, ActorId);
            TempData["Success"] = "Đã nhập kho.";
            return RedirectToAction(nameof(Inventory));
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(input);
        }
    }

    // HÀM Payment: Ghi nhận khoản tiền đã thu cho đơn; RecordPayment kiểm tra đúng số còn thu,
    // trạng thái đơn và mã biên nhận chưa trùng.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ amount (decimal): số tiền VND đã thu, phải dương và đúng toàn bộ phần Total -
    // PaidAmount còn thiếu của đơn.
    // THAM SỐ reference (string): mã chứng từ/biên nhận do người dùng cung cấp; các luồng tiền/nhập
    // kho kiểm tra chưa dùng để chống ghi trùng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Payment. Tham số hàm: (string id, decimal amount, string reference).
    [HttpPost]
    public IActionResult Payment(string id, decimal amount, string reference)
    {
        try
        {
            store.RecordPayment(id, amount, reference, ActorId);
            TempData["Success"] = "Đã ghi nhận tiền thanh toán.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Order), new { id });
    }

    // HÀM Returns: Hiển thị danh sách phiếu trả hàng mới nhất trước.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Returns. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Returns.cshtml.
    public IActionResult Returns() =>
        View(store.Read(d => d.Returns.OrderByDescending(r => r.Id).ToList()));

    // HÀM ResolveReturn: Nhận nút duyệt/từ chối, ghi chú và chứng từ hoàn tiền; ResolveReturn ở
    // service kiểm tra phiếu Pending, hoàn tồn và ghi tiền hoàn khi được duyệt.
    // THAM SỐ id (int): mã bản ghi cần xử lý, được đối chiếu với khóa tương ứng trong thân hàm.
    // THAM SỐ approve (bool): true để duyệt trả hàng và thực hiện hoàn tồn/tiền; false để từ chối
    // yêu cầu.
    // THAM SỐ resolution (string): ghi chú kết quả kiểm tra và xử lý phiếu trả, lưu cùng người
    // duyệt và chứng từ.
    // THAM SỐ reference (string): mã chứng từ/biên nhận do người dùng cung cấp; các luồng tiền/nhập
    // kho kiểm tra chưa dùng để chống ghi trùng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/ResolveReturn. Tham số hàm: (int id, bool approve, string resolution,
    // string reference).
    [HttpPost]
    public IActionResult ResolveReturn(int id, bool approve, string resolution, string reference)
    {
        try
        {
            store.ResolveReturn(id, approve, resolution, reference, ActorId);
            TempData["Success"] = "Đã xử lý trả hàng.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(ReturnRequest), new { id });
    }

    // HÀM Promotions: Mở danh sách chương trình ưu đãi.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Admin/Promotions. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Promotions.cshtml.
    public IActionResult Promotions() => View(store.Read(d => d.Promotions));

    // HÀM Promotion: Mở form thêm khi id trống hoặc form sửa theo id; nạp danh sách sản phẩm áp
    // dụng, không tìm thấy trả 404.
    // THAM SỐ id (int?): mã ưu đãi; GET không có id tạo form mới, có id tải chương trình để sửa.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
    // giới hạn lượt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Promotion. Tham số hàm: (int? id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Promotion.cshtml.
    public IActionResult Promotion(int? id)
    {
        ViewBag.Products = store.Read(d => d.Products);
        // BIẾN p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
        // giới hạn lượt; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
        // ngay sau.
        var p =
            id == null
                ? new Promotion()
                : store.Read(d => d.Promotions.FirstOrDefault(p => p.Id == id));
        return p == null ? NotFound() : View(p);
    }

    // HÀM Promotion: Thêm/sửa ưu đãi: kiểm tra ngày, loại PHANTRAM/SOTIEN, giá trị/hạn mức/lượt,
    // sản phẩm áp dụng và mã chưa trùng; chuẩn hóa mã chữ hoa. Id = 0 thêm mới, khác 0 cập nhật
    // dòng tìm được.
    // THAM SỐ input (Promotion): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA id (int): số nguyên 32 bit; tên này chỉ có nghĩa trong lambda tương ứng, không
    // phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
    // giới hạn lượt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Admin/Promotion. Tham số hàm: (Promotion input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Promotion.cshtml.
    [HttpPost]
    public IActionResult Promotion(Promotion input)
    {
        ViewBag.Products = store.Read(d => d.Products);
        // KIỂM TRA FORM: nhánh này thêm lỗi "Invalid date range."; ModelState sẽ ngăn bước lưu bên
        // dưới.
        if (input.From > input.Until)
            ModelState.AddModelError("From", "Invalid date range.");
        // KIỂM TRA FORM: nhánh này thêm lỗi "Invalid promotion conditions."; ModelState sẽ ngăn
        // bước lưu bên dưới.
        if (
            input.Kind is not ("PHANTRAM" or "SOTIEN")
            || input.Value <= 0
            || (input.Kind == "PHANTRAM" && input.Value > 100)
            || input.Minimum < 0
            || input.Maximum < 0
            || input.Remaining < 0
        )
            ModelState.AddModelError("Value", "Invalid promotion conditions.");
        // KIỂM TRA FORM: nhánh này thêm lỗi "Invalid products."; ModelState sẽ ngăn bước lưu bên
        // dưới.
        if (input.ProductIds.Any(id => !((List<Product>)ViewBag.Products).Any(p => p.Id == id)))
            ModelState.AddModelError("ProductIds", "Invalid products.");
        input.Code = input.Code?.Trim().ToUpperInvariant() ?? "";
        // KIỂM TRA FORM: nhánh này thêm lỗi "Ngày kết thúc không được ở quá khứ."; ModelState sẽ
        // ngăn bước lưu bên
        // dưới.
        if (input.Until.Date < DateTime.Today)
            ModelState.AddModelError("Until", "Ngày kết thúc không được ở quá khứ.");
        // KIỂM TRA FORM: nhánh này thêm lỗi "Mã đã tồn tại."; ModelState sẽ ngăn bước lưu bên dưới.
        if (store.Read(d => d.Promotions.Any(p => p.Id != input.Id && p.Code == input.Code)))
            ModelState.AddModelError("Code", "Mã đã tồn tại.");
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        store.Write(d =>
        {
            // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh còn lại xử
            // lý bản ghi đã
            // tồn tại nếu có.
            if (input.Id == 0)
            {
                input.Id = d.Promotions.Select(p => p.Id).DefaultIfEmpty().Max() + 1;
                d.Promotions.Add(input);
            }
            else
            {
                // BIẾN index (int): vị trí dòng trong List tìm bằng FindIndex, không phải mã SQL;
                // -1 nghĩa là không tìm thấy.
                var index = d.Promotions.FindIndex(p => p.Id == input.Id);
                if (index >= 0)
                    d.Promotions[index] = input;
            }

            return 0;
        });
        TempData["Success"] = "Đã lưu chương trình. Mã ưu đãi áp dụng tại bước đặt hàng.";
        return RedirectToAction(nameof(Promotions));
    }

    // HÀM Reports: Chọn khoảng ngày báo cáo từ bộ lọc nhanh hoặc from/to; ngày bắt đầu sau ngày kết
    // thúc thì trả danh sách rỗng kèm lỗi.
    // THAM SỐ from (DateTime?): ngày bắt đầu kỳ lọc/báo cáo; được lấy phần Date theo quy tắc trong
    // hàm.
    // THAM SỐ to (DateTime?): ngày kết thúc kỳ lọc/báo cáo; phải không trước ngày bắt đầu.
    // THAM SỐ period (string?): kỳ thời gian nhanh, như today/last7/last30/month; controller đổi
    // thành mốc ngày ở switch.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Reports. Tham số hàm: (DateTime? from, DateTime? to, string? period).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Reports.cshtml.
    public IActionResult Reports(DateTime? from, DateTime? to, string? period)
    {
        // BIẾN quick (DateTime?): ngày đầu kỳ suy ra từ lựa chọn nhanh; null nghĩa người dùng không
        // chọn kỳ nhanh hợp lệ.
        var quick = period switch
        {
            "today" => (DateTime?)DateTime.Today,
            "last7" => DateTime.Today.AddDays(-6),
            "last30" => DateTime.Today.AddDays(-29),
            "month" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            _ => null,
        };
        // BIẾN start (DateTime): ngày bắt đầu thực tế sau khi ưu tiên kỳ nhanh, ngày truyền vào và
        // giá trị mặc định.
        var start = quick ?? from?.Date ?? DateTime.Today.AddDays(-30);
        // BIẾN end (DateTime): ngày kết thúc thực tế của kỳ báo cáo, được đối chiếu với start trước
        // khi truy vấn.
        var end = quick.HasValue ? DateTime.Today : to?.Date ?? DateTime.Today;
        ViewBag.Period = period;
        ViewBag.From = start;
        ViewBag.To = end;
        if (start > end)
        {
            TempData["Error"] = "Khoảng ngày không hợp lệ.";
            return View(new List<Order>());
        }

        return View(
            store.Read(d =>
                d.Orders.Where(o => o.Created.Date >= start && o.Created.Date <= end).ToList()
            )
        );
    }

    // HÀM Settings: Mở thông tin cửa hàng hiện tại để chỉnh sửa.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Admin/Settings. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Settings.cshtml.
    public IActionResult Settings() => View(store.Read(d => d.Settings));

    // HÀM Settings: Kiểm tra mẫu tên/email/điện thoại/địa chỉ rồi thay d.Settings trong Write;
    // thông báo và tải lại trang sau lưu.
    // THAM SỐ input (StoreSettings): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: POST /Admin/Settings. Tham số hàm: (StoreSettings input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Settings.cshtml.
    [HttpPost]
    public IActionResult Settings(StoreSettings input)
    {
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        store.Write(d =>
        {
            d.Settings = input;
            return 0;
        });
        TempData["Success"] = "Đã lưu thông tin cửa hàng.";
        return RedirectToAction(nameof(Settings));
    }

    // HÀM Export: Xuất toàn bộ đơn thành CSV UTF-8 có BOM với mã đơn, ngày, trạng thái và tổng
    // tiền.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Export. Tham số hàm: ().
    public IActionResult Export()
    {
        // BIẾN csv (string): toàn bộ nội dung CSV xuất đơn, gồm dòng tiêu đề và các dòng
        // mã/ngày/trạng thái/tổng tiền.
        var csv = store.Read(d =>
            "Mã đơn,Ngày,Trạng thái,Tổng tiền\n"
            + string.Join(
                "\n",
                d.Orders.Select(o => $"{o.Id},{o.Created:yyyy-MM-dd},{o.Status},{o.Total}")
            )
        );
        return File(
            System
                .Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(csv))
                .ToArray(),
            "text/csv",
            "qoanh-orders.csv"
        );
    }
}
