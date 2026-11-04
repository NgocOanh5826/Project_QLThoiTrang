using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

public partial class ShopController(StoreService store) : Controller
{
    int CustomerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Index: Lọc và hiển thị sản phẩm đang bán.
    public IActionResult Index(
        string? q,
        string? category,
        string sort = "new",
        string? collection = null
    )
    {
        category = category?.Trim();
        q = q?.Trim();
        collection = collection is "new" or "sale" ? collection : null;
        var vm = store.Read(d =>
        {
            var products = d.Products.Where(p => p.Active);
            if (!string.IsNullOrWhiteSpace(q))
                products = products.Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || p.Color.Contains(q, StringComparison.OrdinalIgnoreCase)
                );
            if (!string.IsNullOrEmpty(category))
                products = products.Where(p =>
                    string.Equals(p.Category.Trim(), category, StringComparison.OrdinalIgnoreCase)
                );
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

    // Detail: Hiển thị sản phẩm, đánh giá và sản phẩm liên quan.
    public IActionResult Detail(int id)
    {
        var p = store.Read(d => d.Products.FirstOrDefault(p => p.Id == id && p.Active));

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

    // Wishlist: Hiển thị danh sách sản phẩm yêu thích.
    public IActionResult Wishlist()
    {
        var ids = SessionStore.Get<List<int>>(HttpContext.Session, "wishlist");
        return View(
            store.Read(d => d.Products.Where(p => p.Active && ids.Contains(p.Id)).ToList())
        );
    }

    // Favorite: Thêm hoặc bỏ sản phẩm yêu thích.
    [HttpPost]
    public IActionResult Favorite(int id, string? returnUrl)
    {
        if (!store.Read(d => d.Products.Any(p => p.Id == id && p.Active)))
            return NotFound();
        var ids = SessionStore.Get<List<int>>(HttpContext.Session, "wishlist");
        if (!ids.Remove(id))
            ids.Add(id);
        SessionStore.Set(HttpContext.Session, "wishlist", ids);
        TempData["Success"] = "Đã cập nhật danh sách yêu thích.";
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Shop/Wishlist");
    }

    // Cart: Hiển thị giỏ hàng và giá hiện tại.
    public IActionResult Cart() => View(SessionStore.Cart(HttpContext.Session, store));

    // Add: Thêm sản phẩm hoặc biến thể vào giỏ hàng.
    [HttpPost]
    public IActionResult Add(int id, string size = "M", int quantity = 1, int variantId = 0)
    {
        var product = store.Read(d => d.Products.FirstOrDefault(p => p.Id == id && p.Active));

        if (product == null)
            return NotFound();
        var variant = product.Variants.FirstOrDefault(v =>
            v.Active && (variantId > 0 ? v.Id == variantId : v.Size == size)
        );
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

    // UpdateCart: Sửa biến thể, số lượng hoặc xóa dòng giỏ hàng.
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
        var items = SessionStore.Get<List<CartItem>>(HttpContext.Session, "cart");
        var item = items.FirstOrDefault(i =>
            i.ProductId == id && (variantId > 0 ? i.VariantId == variantId : i.Size == size)
        );
        if (item == null)
            return RedirectToAction(nameof(Cart));

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Thông tin giỏ hàng không hợp lệ.";
            return RedirectToAction(nameof(Cart));
        }

        if (remove || quantity == 0)
            items.Remove(item);
        else
        {
            var targetId = newVariantId > 0 ? newVariantId : item.VariantId;
            var variant = store.Read(d =>
                d.Products.Any(p => p.Id == id && p.Active)
                    ? d.Variants.FirstOrDefault(v =>
                        v.Id == targetId && v.ProductId == id && v.Active
                    )
                    : null
            );
            var existing = items.FirstOrDefault(i => i != item && i.VariantId == targetId);
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

    // Review: Gửi đánh giá sản phẩm kèm ảnh hoặc video.
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
            var uploads = new List<ReviewUpload>();
            foreach (var file in media)
            {
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, HttpContext.RequestAborted);
                uploads.Add(new(file.FileName, buffer.ToArray()));
            }

            store.Review(CustomerId, id, stars, text, uploads);
            TempData["Success"] = "Đã lưu đánh giá.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ReviewMedia: Trả tệp ảnh hoặc video của đánh giá được phép xem.
    public IActionResult ReviewMedia(Guid id)
    {
        bool staff = User.IsInRole("Admin") || User.IsInRole("Support");
        var media = store.NativeRead(db =>
            (
                from m in db.ReviewMedia
                join r in db.DanhGia on m.ReviewId equals r.MaDanhGia
                where m.Id == id && (r.TrangThai || staff)
                select m
            ).FirstOrDefault()
        );

        if (media == null)
            return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(media.Content, media.ContentType, enableRangeProcessing: true);
    }

    // Checkout: Hiển thị form đặt hàng và chụp báo giá hiện tại.
    [Authorize(Roles = "Customer")]
    public IActionResult Checkout()
    {
        if (SessionStore.Cart(HttpContext.Session, store).Lines.Count == 0)
            return RedirectToAction(nameof(Cart));
        var c = store.Read(d => d.Customers.First(c => c.Id == CustomerId));
        ViewBag.Cart = SessionStore.Cart(HttpContext.Session, store);
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

    // Checkout: Tạo đơn hàng và xóa giỏ sau khi thành công.
    [Authorize(Roles = "Customer"), HttpPost]
    public IActionResult Checkout(CheckoutInput input)
    {
        var cart = SessionStore.Cart(HttpContext.Session, store);
        ViewBag.Cart = cart;

        if (cart.Lines.Count == 0)
        {
            ModelState.AddModelError("", "Giỏ hàng đang trống.");
            return View(input);
        }

        if (!ModelState.IsValid)
            return View(input);

        if (input.CheckoutToken != HttpContext.Session.GetString("checkout-token"))
        {
            ModelState.AddModelError("", "Phiên đặt hàng đã thay đổi; mở lại trang thanh toán.");
            return View(input);
        }

        try
        {
            var order = store.PlaceOrder(
                CustomerId,
                input,
                SessionStore.Get<List<CartItem>>(HttpContext.Session, "cart"),
                SessionStore.Get<List<OrderLine>>(HttpContext.Session, "checkout-quote")
            );
            SessionStore.Set(HttpContext.Session, "cart", new List<CartItem>());
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(input);
        }
    }

    // Success: Hiển thị xác nhận đặt hàng của khách.
    [Authorize]
    public IActionResult Success(string id)
    {
        var o = store.Read(d =>
            d.Orders.FirstOrDefault(o => o.Id == id && o.CustomerId == CustomerId)
        );
        return o == null ? NotFound() : View(o);
    }
}
