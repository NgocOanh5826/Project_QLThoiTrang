using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

[Authorize(Roles = "Admin,Sales")]
public class ShippingController(StoreService store, ShippingService shipping) : Controller
{
    int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Index: Hiển thị thông tin vận chuyển đơn hàng.
    public IActionResult Index(string id)
    {
        var o = store.Read(d => d.Orders.FirstOrDefault(o => o.Id == id));

        if (o == null)
            return NotFound();
        ViewBag.Order = o;
        ViewBag.Ready = shipping.Ready;
        ViewBag.Shipment = store.NativeRead(db =>
            db.Shipments.SingleOrDefault(s => s.OrderId == o.DatabaseId)
        );
        return View(new ShipmentInput { Id = id });
    }

    // Create: Yêu cầu tạo vận đơn GHN.
    [HttpPost]
    public async Task<IActionResult> Create(ShipmentInput input, CancellationToken token)
    {
        try
        {
            if (!ModelState.IsValid)
                throw new InvalidOperationException(
                    "Kiểm tra tỉnh/phường, trọng lượng và kích thước."
                );
            await shipping.Create(input, Actor, token);
            TempData["Success"] = "Đã gửi yêu cầu vận đơn GHN.";
        }
        catch (Exception ex)
            when (ex
                    is InvalidOperationException
                        or HttpRequestException
                        or OperationCanceledException
            )
        {
            TempData["Error"] =
                ex is InvalidOperationException
                    ? ex.Message
                    : "Không kết nối được GHN; cần đối soát trước khi thử lại.";
        }

        return RedirectToAction(nameof(Index), new { id = input.Id });
    }

    // Sync: Đồng bộ trạng thái vận đơn GHN.
    [HttpPost]
    public async Task<IActionResult> Sync(string id, CancellationToken token)
    {
        try
        {
            await shipping.Sync(id, Actor, token);
            TempData["Success"] = "Đã đồng bộ trạng thái vận đơn.";
        }
        catch (Exception ex)
            when (ex
                    is InvalidOperationException
                        or HttpRequestException
                        or OperationCanceledException
            )
        {
            TempData["Error"] = "Chưa đồng bộ được. Kiểm tra cấu hình và đối soát GHN.";
        }

        return RedirectToAction(nameof(Index), new { id });
    }
}
