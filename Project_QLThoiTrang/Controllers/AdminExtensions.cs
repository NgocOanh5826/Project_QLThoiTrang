// VAI TRÒ FILE: Phần bổ sung của cùng AdminController (partial): phân quyền, danh mục dùng chung,
// biến thể, nhập
// nhiều dòng, nhân viên, khách và kiểm duyệt. Id = 0 thường là thêm; id đã có là sửa.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Controllers;

public partial class AdminController
{
    // HÀM OnActionExecuting: Bộ lọc quyền trước mọi action quản trị: Admin đi tiếp; Catalog quản lý
    // danh mục/sản phẩm, Inventory quản lý kho, Sales quản lý đơn, Support quản lý khách/đánh giá.
    // Action không có trong bảng quyền bị chặn với Forbid.
    // THAM SỐ context (ActionExecutingContext): ngữ cảnh yêu cầu hoặc bộ lọc MVC đang chạy, chứa
    // route, tham số, User và/hoặc phản hồi theo kiểu cụ thể.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (User.IsInRole("Admin"))
            return;
        // BIẾN action (string): tên action MVC lấy từ route của request, dùng chọn nhóm quyền truy
        // cập.
        string action = context.ActionDescriptor.RouteValues["action"] ?? "";
        // BIẾN allowed (string[]): các vai trò được phép vào action hiện tại, tính từ switch phân
        // quyền quản trị.
        string[] allowed = action switch
        {
            "Index" => new[] { "Catalog", "Inventory", "Sales", "Support" },
            "Products"
            or "Product"
            or "DeleteProduct"
            or "Categories"
            or "AddCategory"
            or "DeleteCategory"
            or "Promotions"
            or "Promotion"
            or "Variants"
            or "Variant"
            or "Category" => new[] { "Catalog" },
            "Inventory"
            or "Receive"
            or "ReceiveMany"
            or "VariantStock"
            or "AdjustVariant"
            or "Receipts"
            or "Receipt"
            or "StockCount" => new[] { "Inventory" },
            "Orders"
            or "Order"
            or "OrderStatus"
            or "Payment"
            or "Returns"
            or "ResolveReturn"
            or "Reports"
            or "Export"
            or "ReturnRequest" => new[] { "Sales" },
            "Customers" or "Customer" or "EditCustomer" or "Reviews" or "Moderate" => new[]
            {
                "Support",
            },
            "References" or "Reference" => new[]
            {
                (
                    context.ActionArguments.Values.OfType<ReferenceInput>().FirstOrDefault()?.Kind
                    ?? Request.Query["kind"].ToString()
                )
                    is "warehouses"
                        or "suppliers"
                    ? "Inventory"
                    : "Catalog",
            },
            _ => Array.Empty<string>(),
        };
        if (!allowed.Any(User.IsInRole))
            context.Result = Forbid();
    }

    // HÀM References: Mở danh sách danh mục theo kind hoặc chuyển sang form sửa khi id > 0; tính
    // tập id đang được sử dụng để view thể hiện trạng thái liên quan.
    // THAM SỐ kind (string): loại danh mục: brands, sizes, colors, warehouses hoặc suppliers; quyết
    // định bảng SQL và nhóm quyền cần dùng. Mặc định khi không truyền: "brands".
    // THAM SỐ id (int): mã bản ghi của danh mục kind đang chọn; 0 mở form thêm, khác 0 mở bản ghi
    // sửa. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA p (SanPhamRow): entity của bảng SanPham, thông tin sản phẩm gốc; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
    // và SKU; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (TonKhoRow): entity của bảng TonKho, tồn của từng biến thể tại từng kho; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/References. Tham số hàm: (string kind = "brands", int id = 0).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/References.cshtml.
    public IActionResult References(string kind = "brands", int id = 0)
    {
        if (id > 0)
            return RedirectToAction(nameof(Reference), new { kind, id });
        try
        {
            // BIẾN rows (List<ReferenceInput>): danh sách/truy vấn các phần tử: dữ liệu danh mục
            // dùng chung; Kind chọn loại, Id phân biệt thêm với sửa.
            var rows = store.References(kind);
            ViewBag.ReferenceUsage = store.NativeRead(db =>
                kind switch
                {
                    "brands" => db
                        .SanPham.Where(p => p.MaThuongHieu.HasValue)
                        .Select(p => p.MaThuongHieu!.Value)
                        .ToHashSet(),
                    "sizes" => db.BienTheSanPham.Select(v => v.MaKichThuoc).ToHashSet(),
                    "colors" => db.BienTheSanPham.Select(v => v.MaMauSac).ToHashSet(),
                    "warehouses" => db
                        .TonKho.Where(s => s.SoLuongTon > 0)
                        .Select(s => s.MaKho)
                        .Concat(db.PhieuNhap.Select(p => p.MaKho))
                        .ToHashSet(),
                    "suppliers" => db.PhieuNhap.Select(p => p.MaNCC).ToHashSet(),
                    _ => new HashSet<int>(),
                }
            );
            return View(
                new ReferencePage
                {
                    Input = new() { Kind = kind },
                    Rows = rows,
                }
            );
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }

    // HÀM Reference: Mở form thêm danh mục khi id = 0 hoặc form sửa bản ghi cùng kind; loại sai trả
    // 400, id không có trả 404.
    // THAM SỐ kind (string): loại danh mục: brands, sizes, colors, warehouses hoặc suppliers; quyết
    // định bảng SQL và nhóm quyền cần dùng. Mặc định khi không truyền: "brands".
    // THAM SỐ id (int): mã bản ghi của danh mục kind đang chọn; 0 mở form thêm, khác 0 mở bản ghi
    // sửa. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA r (ReferenceInput): dữ liệu danh mục dùng chung; Kind chọn loại, Id phân biệt
    // thêm với sửa; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Reference. Tham số hàm: (string kind = "brands", int id = 0).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Reference.cshtml.
    public IActionResult Reference(string kind = "brands", int id = 0)
    {
        try
        {
            // BIẾN rows (List<ReferenceInput>): danh sách/truy vấn các phần tử: dữ liệu danh mục
            // dùng chung; Kind chọn loại, Id phân biệt thêm với sửa.
            var rows = store.References(kind);
            // BIẾN input (ReferenceInput): dữ liệu danh mục dùng chung; Kind chọn loại, Id phân
            // biệt thêm với sửa; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra
            // hoặc ?? ngay sau.
            var input =
                id == 0 ? new ReferenceInput { Kind = kind } : rows.FirstOrDefault(r => r.Id == id);
            return input == null ? NotFound() : View(input);
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }

    // HÀM References: Action tương thích nhận ReferenceInput rồi chuyển sang Reference(input) để
    // dùng chung xử lý.
    // THAM SỐ input (ReferenceInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/References. Tham số hàm: (ReferenceInput input).
    [HttpPost]
    public IActionResult References(ReferenceInput input) => Reference(input);

    // HÀM Reference: Nhận form thương hiệu/size/màu/kho/NCC, kiểm tra kind và ModelState rồi
    // SaveReference; lỗi giữ dữ liệu nhập và hiển thị lại form.
    // THAM SỐ input (ReferenceInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Reference. Tham số hàm: (ReferenceInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Reference.cshtml.
    [HttpPost]
    public IActionResult Reference(ReferenceInput input)
    {
        try
        {
            store.References(input.Kind);
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }

        // DỮ LIỆU FORM: chỉ đi vào phần lưu khi binding và các DataAnnotations đều hợp lệ.
        if (ModelState.IsValid)
            try
            {
                store.SaveReference(input, ActorId);
                TempData["Success"] = "Đã lưu danh mục.";
                return RedirectToAction(nameof(References), new { kind = input.Kind });
            }
            // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
            // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        return View("Reference", input);
    }

    // HÀM ReceiveOptions: Chuẩn bị ViewBag.Products/Variants và danh sách kho/NCC hoạt động cho
    // form nhập kho; đây là helper nội bộ, không phải URL công khai.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
    // nhập/xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (Supplier): nhà cung cấp dùng cho các dòng nhập hàng; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    void ReceiveOptions()
    {
        // BIẾN d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng.
        var d = store.Read(d => d);
        ViewBag.Products = d.Products;
        ViewBag.Variants = d.Variants;
        ViewBag.Warehouses = d.Warehouses.Where(w => w.Active).ToList();
        ViewBag.Suppliers = d.Suppliers.Where(s => s.Active).ToList();
    }

    // HÀM VariantOptions: Chuẩn bị danh sách sản phẩm, kích thước và màu để dùng chung khi mở hoặc
    // trả lại form biến thể bị lỗi.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    void VariantOptions()
    {
        ViewBag.Products = store.Read(d => d.Products);
        ViewBag.Sizes = store.References("sizes");
        ViewBag.Colors = store.References("colors");
    }

    // HÀM Variants: Mở danh sách biến thể; có id thì chuyển sang trang sửa riêng, không có thì nạp
    // các lựa chọn và danh sách biến thể.
    // THAM SỐ id (int?): mã biến thể cần xem/sửa; form Variant dùng 0 cho thêm mới. Mặc định khi
    // không truyền: null.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Admin/Variants. Tham số hàm: (int? id = null).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Variants.cshtml.
    public IActionResult Variants(int? id = null)
    {
        if (id.HasValue)
            return RedirectToAction(nameof(Variant), new { id });
        VariantOptions();
        ViewBag.Variants = store.Read(d => d.Variants);
        return View();
    }

    // HÀM Variant: Mở form thêm khi id = 0, hoặc đọc BienTheSanPham để dựng VariantInput khi sửa;
    // không có bản ghi trả 404.
    // THAM SỐ id (int): mã biến thể cần xem/sửa; form Variant dùng 0 cho thêm mới. Mặc định khi
    // không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA v (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
    // và SKU; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Variant. Tham số hàm: (int id = 0).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Variant.cshtml.
    public IActionResult Variant(int id = 0)
    {
        VariantOptions();
        if (id == 0)
            return View(new VariantInput());
        // BIẾN r (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu và
        // SKU; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var r = store.NativeRead(db => db.BienTheSanPham.SingleOrDefault(v => v.MaBienThe == id));
        return r == null
            ? NotFound()
            : View(
                new VariantInput
                {
                    Id = r.MaBienThe,
                    ProductId = r.MaSP,
                    SizeId = r.MaKichThuoc,
                    ColorId = r.MaMauSac,
                    Sku = r.SKU,
                    Price = r.GiaBanRieng,
                    Active = r.TrangThai,
                }
            );
    }

    // HÀM Variants: Action tương thích chuyển VariantInput sang Variant(input).
    // THAM SỐ input (VariantInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Variants. Tham số hàm: (VariantInput input).
    [HttpPost]
    public IActionResult Variants(VariantInput input) => Variant(input);

    // HÀM Variant: Nhận form biến thể, kiểm tra ModelState rồi SaveVariant; service kiểm tra SKU/tổ
    // hợp không trùng và cấm sửa định danh của biến thể cũ.
    // THAM SỐ input (VariantInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Variant. Tham số hàm: (VariantInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Variant.cshtml.
    [HttpPost]
    public IActionResult Variant(VariantInput input)
    {
        // DỮ LIỆU FORM: chỉ đi vào phần lưu khi binding và các DataAnnotations đều hợp lệ.
        if (ModelState.IsValid)
            try
            {
                store.SaveVariant(input, ActorId);
                TempData["Success"] = "Đã lưu biến thể.";
                return RedirectToAction(nameof(Variants));
            }
            // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
            // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        VariantOptions();
        return View("Variant", input);
    }

    // HÀM VariantStock: Mở bảng tồn chi tiết theo biến thể và kho từ StoreData.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Admin/VariantStock. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/VariantStock.cshtml.
    public IActionResult VariantStock() => View(store.Read(d => d));

    // HÀM AdjustVariant: Nhận kiểm kê biến thể/kho, số tồn expected và lý do; service kiểm tra rồi
    // lưu, sau đó quay lại StockCount đúng biến thể/kho.
    // THAM SỐ variantId (int): mã biến thể sản phẩm, dùng phân biệt đúng tổ hợp size/màu thay vì
    // chỉ mã sản phẩm.
    // THAM SỐ warehouseId (int): mã kho cần nhập/kiểm kê; service đối chiếu kho tồn tại và trạng
    // thái hoạt động.
    // THAM SỐ quantity (int): số tồn thực tế muốn ghi sau kiểm kê (giá trị tuyệt đối), không phải
    // số tăng/giảm; phải 0–100.000.
    // THAM SỐ expected (int): số tồn người dùng đã thấy lúc mở form; so với tồn hiện tại để phát
    // hiện thay đổi và tránh ghi đè.
    // THAM SỐ reason (string): lý do nghiệp vụ do người dùng nhập; Text/attribute kiểm tra độ dài
    // trước khi ghi nhật ký hoặc phiếu.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/AdjustVariant. Tham số hàm: (int variantId, int warehouseId, int
    // quantity, int expected, string reason).
    [HttpPost]
    public IActionResult AdjustVariant(
        int variantId,
        int warehouseId,
        int quantity,
        int expected,
        string reason
    )
    {
        try
        {
            store.AdjustVariantStock(variantId, warehouseId, quantity, expected, reason, ActorId);
            TempData["Success"] = "Đã kiểm kê biến thể.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StockCount), new { variantId, warehouseId });
    }

    // HÀM Receipts: Mở danh sách phiếu nhập từ SQL, bổ sung bảng tra tên kho/NCC/nhân viên trong
    // ViewBag.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA w (KhoRow): entity của bảng Kho, thông tin kho; tên này chỉ có nghĩa trong lambda
    // tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung cấp hàng;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Receipts. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Receipts.cshtml.
    public IActionResult Receipts()
    {
        ViewBag.WarehouseNames = store.NativeRead(db =>
            db.Kho.ToDictionary(w => w.MaKho, w => w.TenKho)
        );
        ViewBag.SupplierNames = store.NativeRead(db =>
            db.NhaCungCap.ToDictionary(s => s.MaNCC, s => s.TenNCC)
        );
        ViewBag.StaffNames = store.NativeRead(db =>
            db.NhanVien.ToDictionary(s => s.MaNV, s => s.HoTen)
        );
        return View(
            store.NativeRead(db => db.PhieuNhap.OrderByDescending(p => p.NgayNhap).ToList())
        );
    }

    // HÀM Receipt: Mở một phiếu nhập và các dòng ChiTietPhieuNhap; không có đầu phiếu trả 404.
    // THAM SỐ id (int): khóa SQL của phiếu nhập kho cần xem/in, không phải mã đơn bán hàng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ChiTietPhieuNhapRow): entity của bảng ChiTietPhieuNhap, số lượng và giá nhập
    // từng biến thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Receipt. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Receipt.cshtml.
    public IActionResult Receipt(int id)
    {
        // BIẾN receipt (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; kết quả tra
        // cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var receipt = store.NativeRead(db =>
            db.PhieuNhap.SingleOrDefault(p => p.MaPhieuNhap == id)
        );
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (receipt == null)
            return NotFound();
        ViewBag.Lines = store.NativeRead(db =>
            db.ChiTietPhieuNhap.Where(l => l.MaPhieuNhap == id).ToList()
        );
        return View(receipt);
    }

    // HÀM ReceiveMany: Gán chung mã phiếu/kho/NCC cho các dòng form rồi gọi ReceiveBatch; lỗi kiểm
    // tra dữ liệu được chuyển thành thông báo, quay lại tab nhiều dòng.
    // THAM SỐ lines (List<ReceiveInput>): các dòng ReceiveInput gửi từ form nhập nhiều; controller
    // gán chung mã phiếu/kho/NCC trước ReceiveBatch.
    // THAM SỐ reference (string): mã chứng từ/biên nhận do người dùng cung cấp; các luồng tiền/nhập
    // kho kiểm tra chưa dùng để chống ghi trùng.
    // THAM SỐ warehouseId (int): mã kho cần nhập/kiểm kê; service đối chiếu kho tồn tại và trạng
    // thái hoạt động.
    // THAM SỐ supplierId (int): mã nhà cung cấp của phiếu; 0 dùng nhánh tương thích, số dương phải
    // tìm được NCC hợp lệ.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/ReceiveMany. Tham số hàm: (List<ReceiveInput> lines, string reference,
    // int warehouseId, int supplierId).
    [HttpPost]
    public IActionResult ReceiveMany(
        List<ReceiveInput> lines,
        string reference,
        int warehouseId,
        int supplierId
    )
    {
        try
        {
            // BIẾN LẶP line (ReceiveInput): một dòng phiếu nhập gồm biến thể, kho, nhà cung cấp, số
            // lượng, giá nhập và mã phiếu; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng
            // trong vòng foreach này.
            foreach (var line in lines)
            {
                line.Reference = reference;
                line.WarehouseId = warehouseId;
                line.SupplierId = supplierId;
            }

            try
            {
                store.ReceiveBatch(lines, ActorId);
            }
            // BIẾN LỖI ex (System.ComponentModel.DataAnnotations.ValidationException): ngoại lệ bắt
            // từ khối try; Message chứa thông báo, còn loại lỗi dùng quyết định cách báo/ghi log ở
            // catch.
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }

            TempData["Success"] = "Đã nhập toàn bộ phiếu.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Receive), new { tab = "many" });
    }

    // HÀM Staff: Mở danh sách nhân viên hoặc chuyển sang StaffMember nếu có id cần sửa; chỉ Admin
    // được qua bộ lọc quyền action này.
    // THAM SỐ id (int): mã nhân viên dương từ form/URL; khi tìm trong StoreData phải đối chiếu Id
    // âm tương ứng. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Admin/Staff. Tham số hàm: (int id = 0).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Staff.cshtml.
    public IActionResult Staff(int id = 0)
    {
        if (id > 0)
            return RedirectToAction(nameof(StaffMember), new { id });
        ViewBag.Staff = store.Staff();
        return View();
    }

    // HÀM StaffMember: Mở form thêm/sửa nhân viên; đổi id dương của form sang Id âm trong StoreData
    // để tìm đúng nhân viên.
    // THAM SỐ id (int): mã nhân viên dương từ form/URL; khi tìm trong StoreData phải đối chiếu Id
    // âm tương ứng. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA s (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/StaffMember. Tham số hàm: (int id = 0).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/StaffMember.cshtml.
    public IActionResult StaffMember(int id = 0)
    {
        // BIẾN s (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
        // trò và trạng thái; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
        // ngay sau.
        var s = store.Staff().FirstOrDefault(s => -s.Id == id);
        return id > 0 && s == null
            ? NotFound()
            : View(
                s == null
                    ? new StaffInput()
                    : new StaffInput
                    {
                        Id = -s.Id,
                        Name = s.Name,
                        Email = s.Email,
                        Phone = s.Phone,
                        Role = s.Role,
                        Active = s.Active,
                    }
            );
    }

    // HÀM Staff: Action tương thích chuyển form nhân viên sang StaffMember(input).
    // THAM SỐ input (StaffInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Staff. Tham số hàm: (StaffInput input).
    [HttpPost]
    public IActionResult Staff(StaffInput input) => StaffMember(input);

    // HÀM StaffMember: Admin lưu nhân viên qua SaveStaff; nếu lỗi thì xóa mật khẩu khỏi
    // input/ModelState trước khi hiển thị lại form để không điền lại giá trị nhạy cảm.
    // THAM SỐ input (StaffInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/StaffMember. Tham số hàm: (StaffInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/StaffMember.cshtml.
    [HttpPost]
    public IActionResult StaffMember(StaffInput input)
    {
        // DỮ LIỆU FORM: chỉ đi vào phần lưu khi binding và các DataAnnotations đều hợp lệ.
        if (ModelState.IsValid)
            try
            {
                store.SaveStaff(input, ActorId);
                TempData["Success"] = "Đã lưu nhân viên và quyền.";
                return RedirectToAction(nameof(Staff));
            }
            // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
            // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        input.Password = null;
        ModelState.Remove(nameof(input.Password));
        return View("StaffMember", input);
    }

    // HÀM StockCount: Mở form kiểm kê một biến thể tại kho; bắt buộc biến thể tồn tại và kho đang
    // hoạt động.
    // THAM SỐ variantId (int): mã biến thể sản phẩm, dùng phân biệt đúng tổ hợp size/màu thay vì
    // chỉ mã sản phẩm.
    // THAM SỐ warehouseId (int): mã kho cần nhập/kiểm kê; service đối chiếu kho tồn tại và trạng
    // thái hoạt động.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
    // nhập/xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/StockCount. Tham số hàm: (int variantId, int warehouseId).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/StockCount.cshtml.
    public IActionResult StockCount(int variantId, int warehouseId)
    {
        // BIẾN data (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng.
        var data = store.Read(d => d);
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (
            !data.Variants.Any(v => v.Id == variantId)
            || !data.Warehouses.Any(w => w.Id == warehouseId && w.Active)
        )
            return NotFound();
        ViewBag.VariantId = variantId;
        ViewBag.WarehouseId = warehouseId;
        return View(data);
    }

    // HÀM ReturnRequest: Mở chi tiết một phiếu trả hàng, bọc thành danh sách để dùng chung
    // model/view hiển thị.
    // THAM SỐ id (int): mã phiếu yêu cầu trả hàng cần xem/in.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/ReturnRequest. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/ReturnRequest.cshtml.
    public IActionResult ReturnRequest(int id)
    {
        // BIẾN r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
        // hoàn; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var r = store.Read(d => d.Returns.FirstOrDefault(r => r.Id == id));
        return r == null ? NotFound() : View(new List<ReturnRecord> { r });
    }

    // HÀM EditCustomer: Mở form sửa hồ sơ tài khoản Role Customer, kèm id/trạng thái; không tìm
    // thấy đúng khách trả 404.
    // THAM SỐ id (int): mã khách hàng cần xem/sửa; service chỉ sửa tài khoản Role Customer theo
    // quyền Support/Admin.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/EditCustomer. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/EditCustomer.cshtml.
    public IActionResult EditCustomer(int id)
    {
        // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
        // trò và trạng thái; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
        // ngay sau.
        var c = store.Read(d =>
            d.Customers.FirstOrDefault(c => c.Id == id && c.Role == "Customer")
        );
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (c == null)
            return NotFound();
        ViewBag.Id = id;
        ViewBag.Active = c.Active;
        return View(
            new ProfileInput
            {
                Name = c.Name,
                Phone = c.Phone,
                Address = c.Address,
            }
        );
    }

    // HÀM EditCustomer: Nhận hồ sơ và trạng thái mới, gọi UpdateCustomer với quyền Support/Admin;
    // thành công quay về chi tiết khách, lỗi giữ form.
    // THAM SỐ id (int): mã khách hàng cần xem/sửa; service chỉ sửa tài khoản Role Customer theo
    // quyền Support/Admin.
    // THAM SỐ input (ProfileInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ active (bool): trạng thái mới: true để hoạt động/hiển thị, false để khóa hoặc ẩn theo
    // đối tượng của hàm.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/EditCustomer. Tham số hàm: (int id, ProfileInput input, bool active).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/EditCustomer.cshtml.
    [HttpPost]
    public IActionResult EditCustomer(int id, ProfileInput input, bool active)
    {
        ViewBag.Id = id;
        ViewBag.Active = active;
        // DỮ LIỆU FORM: chỉ đi vào phần lưu khi binding và các DataAnnotations đều hợp lệ.
        if (ModelState.IsValid)
            try
            {
                store.UpdateCustomer(id, input, active, ActorId);
                TempData["Success"] = "Đã cập nhật khách hàng.";
                return RedirectToAction(nameof(Customer), new { id });
            }
            // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
            // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        return View(input);
    }

    // HÀM Reviews: Mở danh sách đánh giá kể cả đánh giá ẩn để Support/Admin kiểm duyệt; nạp tên sản
    // phẩm cho view.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/Reviews. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/Reviews.cshtml.
    public IActionResult Reviews()
    {
        ViewBag.ProductNames = store.Read(d => d.Products.ToDictionary(p => p.Id, p => p.Name));
        return View(store.Reviews(includeHidden: true));
    }

    // HÀM Moderate: Nhận nút ẩn/hiện đánh giá và gọi ModerateReview; không xóa đánh giá hay tệp.
    // THAM SỐ id (int): mã đánh giá cần ẩn/hiện.
    // THAM SỐ active (bool): trạng thái mới: true để hoạt động/hiển thị, false để khóa hoặc ẩn theo
    // đối tượng của hàm.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Admin/Moderate. Tham số hàm: (int id, bool active).
    [HttpPost]
    public IActionResult Moderate(int id, bool active)
    {
        try
        {
            store.ModerateReview(id, active, ActorId);
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Reviews));
    }

    // HÀM LoginHistory: Admin xem tối đa 300 nhật ký đăng nhập gần nhất trực tiếp từ SQL.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA x (LichSuDangNhapRow): entity của bảng LichSuDangNhap, nhật ký đăng nhập/đăng
    // xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Admin/LoginHistory. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Admin/LoginHistory.cshtml.
    public IActionResult LoginHistory() =>
        View(
            store.NativeRead(db =>
                db.LichSuDangNhap.OrderByDescending(x => x.MaLichSu).Take(300).ToList()
            )
        );
}
