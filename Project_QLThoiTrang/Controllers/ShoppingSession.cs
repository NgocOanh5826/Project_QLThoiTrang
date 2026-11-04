// VAI TRÒ FILE: Bộ lọc trước/sau action của ShopController: đồng bộ giỏ/yêu thích và chặn nhân viên
// thực hiện luồng
// mua hàng. Đây là cùng lớp partial với ShopController.cs.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

public partial class ShopController
{
    // HÀM OnActionExecuting: Trước action mua hàng: chặn nhân viên vào
    // Cart/Add/UpdateCart/Checkout/Success; nếu là Customer thì nạp giỏ và yêu thích từ SQL vào
    // Session.
    // THAM SỐ context (ActionExecutingContext): ngữ cảnh yêu cầu hoặc bộ lọc MVC đang chạy, chứa
    // route, tham số, User và/hoặc phản hồi theo kiểu cụ thể.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
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

    // HÀM OnActionExecuted: Sau Add/UpdateCart/Favorite không có ngoại lệ chưa xử lý, đồng bộ
    // giỏ/yêu thích Session xuống SQL cho Customer. Tách khỏi action để ba thao tác dùng chung cơ
    // chế lưu.
    // THAM SỐ context (ActionExecutedContext): ngữ cảnh yêu cầu hoặc bộ lọc MVC đang chạy, chứa
    // route, tham số, User và/hoặc phản hồi theo kiểu cụ thể.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
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
