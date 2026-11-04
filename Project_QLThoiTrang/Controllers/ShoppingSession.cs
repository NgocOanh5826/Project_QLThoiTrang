using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

public partial class ShopController
{
    // OnActionExecuting: Nạp giỏ khách và kiểm tra quyền mua hàng.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (
            User.Identity?.IsAuthenticated == true
            && !User.IsInRole("Customer")
            && new[] { "Cart", "Add", "UpdateCart", "Checkout", "Success" }.Contains(
                context.ActionDescriptor.RouteValues["action"]
            )
        )
        {
            context.Result = Forbid();
            return;
        }

        if (User.IsInRole("Customer"))
        {
            SessionStore.Set(HttpContext.Session, "cart", store.UserCart(CustomerId));
            SessionStore.Set(HttpContext.Session, "wishlist", store.UserWishlist(CustomerId));
        }
    }

    // OnActionExecuted: Lưu giỏ và yêu thích sau khi khách cập nhật.
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (
            context.Exception == null
            && User.IsInRole("Customer")
            && new[] { "Add", "UpdateCart", "Favorite" }.Contains(
                context.ActionDescriptor.RouteValues["action"]
            )
        )
            store.SaveShopping(
                CustomerId,
                SessionStore.Get<List<CartItem>>(HttpContext.Session, "cart"),
                SessionStore.Get<List<int>>(HttpContext.Session, "wishlist")
            );
    }
}
