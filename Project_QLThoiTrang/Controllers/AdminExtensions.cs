using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Controllers;

public partial class AdminController
{
    // OnActionExecuting: Kiểm tra quyền truy cập chức năng quản trị.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (User.IsInRole("Admin"))
            return;
        string action = context.ActionDescriptor.RouteValues["action"] ?? "";
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

    // References: Hiển thị danh sách hoặc form danh mục dùng chung.
    public IActionResult References(string kind = "brands", int id = 0)
    {
        if (id > 0)
            return RedirectToAction(nameof(Reference), new { kind, id });
        try
        {
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

    // Reference: Hiển thị form thêm hoặc sửa danh mục dùng chung.
    public IActionResult Reference(string kind = "brands", int id = 0)
    {
        try
        {
            var rows = store.References(kind);
            var input =
                id == 0 ? new ReferenceInput { Kind = kind } : rows.FirstOrDefault(r => r.Id == id);
            return input == null ? NotFound() : View(input);
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }

    // References: Lưu danh mục dùng chung và trở về danh sách.
    [HttpPost]
    public IActionResult References(ReferenceInput input) => Reference(input);

    // Reference: Lưu thương hiệu, size, màu, kho hoặc nhà cung cấp.
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

        if (ModelState.IsValid)
            try
            {
                store.SaveReference(input, ActorId);
                TempData["Success"] = "Đã lưu danh mục.";
                return RedirectToAction(nameof(References), new { kind = input.Kind });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        return View("Reference", input);
    }

    // ReceiveOptions: Nạp lựa chọn kho và nhà cung cấp cho phiếu nhập.
    void ReceiveOptions()
    {
        var d = store.Read(d => d);
        ViewBag.Products = d.Products;
        ViewBag.Variants = d.Variants;
        ViewBag.Warehouses = d.Warehouses.Where(w => w.Active).ToList();
        ViewBag.Suppliers = d.Suppliers.Where(s => s.Active).ToList();
    }

    // VariantOptions: Nạp lựa chọn sản phẩm, size và màu cho biến thể.
    void VariantOptions()
    {
        ViewBag.Products = store.Read(d => d.Products);
        ViewBag.Sizes = store.References("sizes");
        ViewBag.Colors = store.References("colors");
    }

    // Variants: Hiển thị danh sách biến thể sản phẩm.
    public IActionResult Variants(int? id = null)
    {
        if (id.HasValue)
            return RedirectToAction(nameof(Variant), new { id });
        VariantOptions();
        ViewBag.Variants = store.Read(d => d.Variants);
        return View();
    }

    // Variant: Hiển thị form thêm hoặc sửa biến thể.
    public IActionResult Variant(int id = 0)
    {
        VariantOptions();
        if (id == 0)
            return View(new VariantInput());
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

    // Variants: Lưu biến thể và trở về danh sách.
    [HttpPost]
    public IActionResult Variants(VariantInput input) => Variant(input);

    // Variant: Lưu biến thể sản phẩm.
    [HttpPost]
    public IActionResult Variant(VariantInput input)
    {
        if (ModelState.IsValid)
            try
            {
                store.SaveVariant(input, ActorId);
                TempData["Success"] = "Đã lưu biến thể.";
                return RedirectToAction(nameof(Variants));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        VariantOptions();
        return View("Variant", input);
    }

    // VariantStock: Hiển thị tồn kho theo biến thể và kho.
    public IActionResult VariantStock() => View(store.Read(d => d));

    // AdjustVariant: Lưu kết quả kiểm kê biến thể.
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
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StockCount), new { variantId, warehouseId });
    }

    // Receipts: Hiển thị danh sách phiếu nhập kho.
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

    // Receipt: Hiển thị chi tiết phiếu nhập kho.
    public IActionResult Receipt(int id)
    {
        var receipt = store.NativeRead(db =>
            db.PhieuNhap.SingleOrDefault(p => p.MaPhieuNhap == id)
        );

        if (receipt == null)
            return NotFound();
        ViewBag.Lines = store.NativeRead(db =>
            db.ChiTietPhieuNhap.Where(l => l.MaPhieuNhap == id).ToList()
        );
        return View(receipt);
    }

    // ReceiveMany: Lưu phiếu nhập kho nhiều dòng.
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
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }

            TempData["Success"] = "Đã nhập toàn bộ phiếu.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Receive), new { tab = "many" });
    }

    // Staff: Hiển thị danh sách nhân viên.
    public IActionResult Staff(int id = 0)
    {
        if (id > 0)
            return RedirectToAction(nameof(StaffMember), new { id });
        ViewBag.Staff = store.Staff();
        return View();
    }

    // StaffMember: Hiển thị form thêm hoặc sửa nhân viên.
    public IActionResult StaffMember(int id = 0)
    {
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

    // Staff: Lưu nhân viên và trở về danh sách.
    [HttpPost]
    public IActionResult Staff(StaffInput input) => StaffMember(input);

    // StaffMember: Lưu tài khoản và quyền nhân viên.
    [HttpPost]
    public IActionResult StaffMember(StaffInput input)
    {
        if (ModelState.IsValid)
            try
            {
                store.SaveStaff(input, ActorId);
                TempData["Success"] = "Đã lưu nhân viên và quyền.";
                return RedirectToAction(nameof(Staff));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        input.Password = null;
        ModelState.Remove(nameof(input.Password));
        return View("StaffMember", input);
    }

    // StockCount: Hiển thị form kiểm kê sản phẩm hoặc biến thể.
    public IActionResult StockCount(int variantId, int warehouseId)
    {
        var data = store.Read(d => d);

        if (
            !data.Variants.Any(v => v.Id == variantId)
            || !data.Warehouses.Any(w => w.Id == warehouseId && w.Active)
        )
            return NotFound();
        ViewBag.VariantId = variantId;
        ViewBag.WarehouseId = warehouseId;
        return View(data);
    }

    // ReturnRequest: Hiển thị chi tiết yêu cầu trả hàng.
    public IActionResult ReturnRequest(int id)
    {
        var r = store.Read(d => d.Returns.FirstOrDefault(r => r.Id == id));
        return r == null ? NotFound() : View(new List<ReturnRecord> { r });
    }

    // EditCustomer: Hiển thị form sửa hồ sơ khách hàng.
    public IActionResult EditCustomer(int id)
    {
        var c = store.Read(d =>
            d.Customers.FirstOrDefault(c => c.Id == id && c.Role == "Customer")
        );

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

    // EditCustomer: Lưu hồ sơ và trạng thái khách hàng.
    [HttpPost]
    public IActionResult EditCustomer(int id, ProfileInput input, bool active)
    {
        ViewBag.Id = id;
        ViewBag.Active = active;

        if (ModelState.IsValid)
            try
            {
                store.UpdateCustomer(id, input, active, ActorId);
                TempData["Success"] = "Đã cập nhật khách hàng.";
                return RedirectToAction(nameof(Customer), new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

        return View(input);
    }

    // Reviews: Hiển thị danh sách đánh giá sản phẩm.
    public IActionResult Reviews()
    {
        ViewBag.ProductNames = store.Read(d => d.Products.ToDictionary(p => p.Id, p => p.Name));
        return View(store.Reviews(includeHidden: true));
    }

    // Moderate: Ẩn hoặc hiện đánh giá sản phẩm.
    [HttpPost]
    public IActionResult Moderate(int id, bool active)
    {
        try
        {
            store.ModerateReview(id, active, ActorId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Reviews));
    }

    // LoginHistory: Hiển thị lịch sử đăng nhập.
    public IActionResult LoginHistory() =>
        View(
            store.NativeRead(db =>
                db.LichSuDangNhap.OrderByDescending(x => x.MaLichSu).Take(300).ToList()
            )
        );
}
