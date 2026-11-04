// VAI TRÒ FILE: Đăng nhập/đăng ký/đăng xuất và hồ sơ/đơn của tài khoản hiện tại. Form ở
// Views/Account; id lấy từ
// claim; mật khẩu được kiểm tra/băm trong service.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
public partial class AccountController(StoreService store) : Controller
{
    // THUỘC TÍNH AccountController.CustomerId (int): id lấy từ claim của người đang đăng nhập; có
    // thể âm với nhân viên, dương với khách; không lấy từ ô input do người dùng tự chọn.
    int CustomerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // HÀM Login: Mở form đăng nhập; chỉ giữ returnUrl nội bộ để tránh chuyển hướng ra trang bên
    // ngoài.
    // THAM SỐ returnUrl (string?): đường dẫn quay về sau thao tác; controller chỉ giữ URL nội bộ để
    // tránh chuyển hướng ra ngoài.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Account/Login. Tham số hàm: (string? returnUrl).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Login.cshtml.
    public IActionResult Login(string? returnUrl) =>
        View(new LoginInput { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });

    // HÀM Login: Kiểm tra email/mẫu nhập và xác thực mật khẩu bằng StoreService.Authenticate; thành
    // công tạo cookie, gộp giỏ và chuyển tới returnUrl hoặc trang theo vai trò. Policy login giới
    // hạn số lần yêu cầu.
    // THAM SỐ input (LoginInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // ĐIỂM VÀO: POST /Account/Login. Tham số hàm: (LoginInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Login.cshtml.
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        input.Email = input.Email?.Trim() ?? "";
        input.ReturnUrl = Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl : null;
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        // BIẾN customer (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên,
        // email, vai trò và trạng thái.
        var customer = store.Authenticate(
            input.Email,
            input.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()
        );
        // KIỂM TRA FORM: nhánh này thêm lỗi ", "; ModelState sẽ ngăn bước lưu bên dưới.
        if (customer == null)
        {
            ModelState.AddModelError(
                "",
                "Email hoặc mật khẩu chưa đúng. Kiểm tra thông tin và thử lại."
            );
            return View(input);
        }

        await SignIn(customer, input.RememberMe, true);
        return LocalRedirect(
            input.ReturnUrl ?? (customer.Role != "Customer" ? "/Admin" : "/Account")
        );
    }

    // HÀM Register: Mở form đăng ký khách hàng; chỉ nhận đường dẫn quay về nội bộ.
    // THAM SỐ returnUrl (string?): đường dẫn quay về sau thao tác; controller chỉ giữ URL nội bộ để
    // tránh chuyển hướng ra ngoài.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Account/Register. Tham số hàm: (string? returnUrl).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Register.cshtml.
    public IActionResult Register(string? returnUrl) =>
        View(new RegisterInput { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });

    // HÀM Register: Chuẩn hóa tên/email, kiểm tra mẫu rồi Register để thêm khách và băm mật khẩu;
    // tạo cookie và gộp giỏ khi thành công, email trùng trả lại form có lỗi.
    // THAM SỐ input (RegisterInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // ĐIỂM VÀO: POST /Account/Register. Tham số hàm: (RegisterInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Register.cshtml.
    [HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        input.Name = input.Name?.Trim() ?? "";
        input.Email = input.Email?.Trim() ?? "";
        input.ReturnUrl = Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl : null;
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        try
        {
            // BIẾN customer (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên,
            // email, vai trò và trạng thái.
            var customer = store.Register(input);
            await SignIn(customer, mergeShopping: true);
            return LocalRedirect(input.ReturnUrl ?? Url.Action(nameof(Index))!);
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(input.Email), ex.Message);
            return View(input);
        }
    }

    // HÀM SignIn: Helper tạo cookie với claim id/tên/email/quyền, lịch sử và security_stamp từ hash
    // mật khẩu. RememberMe chọn 72 giờ thay cho 8 giờ; chỉ gộp giỏ khi là khách và mergeShopping =
    // true.
    // THAM SỐ c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
    // trò và trạng thái; đối tượng đầu vào của hàm, không phải một ký tự viết tắt cố định cho mọi
    // file.
    // THAM SỐ rememberMe (bool): true để cookie được lưu lâu hơn (72 giờ ở SignIn); false dùng thời
    // hạn 8 giờ. Mặc định khi không truyền: false.
    // THAM SỐ mergeShopping (bool): true để gộp giỏ/yêu thích của khách vãng lai vào tài khoản
    // Customer sau đăng nhập. Mặc định khi không truyền: false.
    // TRẢ VỀ (Task): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm sau các bước
    // await.
    async Task SignIn(Customer c, bool rememberMe = false, bool mergeShopping = false)
    {
        if (c.LoginHistoryId == 0)
            c.LoginHistoryId = store.LogLogin(
                c,
                true,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()
            );
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, c.Id.ToString()),
                        new Claim(ClaimTypes.Name, c.Name),
                        new Claim(ClaimTypes.Email, c.Email),
                        new Claim(ClaimTypes.Role, c.Role),
                        new Claim("login_history", c.LoginHistoryId.ToString()),
                        new Claim(
                            "security_stamp",
                            Convert.ToHexString(
                                System.Security.Cryptography.SHA256.HashData(
                                    System.Text.Encoding.UTF8.GetBytes(c.PasswordHash)
                                )
                            )
                        ),
                    },
                    CookieAuthenticationDefaults.AuthenticationScheme
                )
            ),
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(rememberMe ? 72 : 8),
            }
        );
        if (c.Role == "Customer" && mergeShopping)
            store.MergeGuestShopping(
                c.Id,
                SessionStore.Get<List<CartItem>>(HttpContext.Session, "cart"),
                SessionStore.Get<List<int>>(HttpContext.Session, "wishlist")
            );
    }

    // HÀM Logout: Nhận POST của tài khoản đã đăng nhập: ghi thời điểm đăng xuất, xóa Session và
    // cookie rồi quay về trang chủ.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // ĐIỂM VÀO: POST /Account/Logout. Tham số hàm: ().
    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        // BIẾN OUT history (long): mã nhật ký đăng nhập đọc từ claim login_history để ghi giờ đăng
        // xuất.
        if (long.TryParse(User.FindFirstValue("login_history"), out var history))
            store.EndLogin(history, CustomerId);
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // HÀM Index: Tài khoản đã đăng nhập xem hồ sơ của chính mình; lấy id từ claim, không lấy id
    // người khác từ form.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Account/Index. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Index.cshtml.
    [Authorize]
    public IActionResult Index()
    {
        // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
        // trò và trạng thái.
        var c = store.Read(d => d.Customers.First(c => c.Id == CustomerId));
        ViewBag.Email = c.Email;
        return View(
            new ProfileInput
            {
                Name = c.Name,
                Phone = c.Phone,
                Address = c.Address,
            }
        );
    }

    // HÀM Index: Sửa tên/điện thoại/địa chỉ của chính tài khoản sau khi ModelState hợp lệ; Write
    // kiểm tra dữ liệu rồi cấp lại cookie để tên hiển thị được cập nhật.
    // THAM SỐ input (ProfileInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (Task<IActionResult>): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi
    // theo các nhánh return; Task bao kết quả vì có thao tác chờ bất đồng bộ.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: POST /Account/Index. Tham số hàm: (ProfileInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Index.cshtml.
    [Authorize, HttpPost]
    public async Task<IActionResult> Index(ProfileInput input)
    {
        ViewBag.Email = User.FindFirstValue(ClaimTypes.Email);
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        try
        {
            // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
            // vai trò và trạng thái.
            var c = store.Write(d =>
            {
                // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên,
                // email, vai trò và trạng thái.
                var c = d.Customers.First(c => c.Id == CustomerId);
                c.Name = input.Name.Trim();
                c.Phone = input.Phone?.Trim() ?? "";
                c.Address = input.Address?.Trim() ?? "";
                return c;
            });
            await SignIn(c);
            TempData["Success"] = "Đã cập nhật hồ sơ.";
            return RedirectToAction(nameof(Index));
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(input);
        }
    }

    // HÀM Orders: Chỉ lấy các đơn có CustomerId bằng id trong cookie hiện tại; sắp xếp mới nhất
    // trước.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Account/Orders. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Orders.cshtml.
    [Authorize]
    public IActionResult Orders() =>
        View(
            store.Read(d =>
                d.Orders.Where(o => o.CustomerId == CustomerId)
                    .OrderByDescending(o => o.Created)
                    .ToList()
            )
        );

    // HÀM Order: Mở đơn và yêu cầu trả hàng thuộc đúng tài khoản đang đăng nhập; không được xem đơn
    // chỉ bằng cách đoán mã của khách khác.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Account/Order. Tham số hàm: (string id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Order.cshtml.
    [Authorize]
    public IActionResult Order(string id)
    {
        // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var o = store.Read(d =>
            d.Orders.FirstOrDefault(o => o.Id == id && o.CustomerId == CustomerId)
        );
        ViewBag.Return = store.Read(d =>
            d.Returns.FirstOrDefault(r => r.OrderId == id && r.CustomerId == CustomerId)
        );
        return o == null ? NotFound() : View(o);
    }

    // HÀM Cancel: Gửi yêu cầu hủy đơn của khách tới ChangeOrderStatus với customer = true; service
    // chỉ chấp nhận đơn của mình còn Chờ xác nhận.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Account/Cancel. Tham số hàm: (string id).
    [Authorize, HttpPost]
    public IActionResult Cancel(string id)
    {
        try
        {
            store.ChangeOrderStatus(id, "Đã hủy", CustomerId, true);
            TempData["Success"] = "Đã hủy đơn hàng.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Order), new { id });
    }

    // HÀM Return: Gửi yêu cầu trả toàn bộ phần còn lại hoặc một dòng với số lượng cụ thể;
    // RequestReturn kiểm tra chủ đơn, trạng thái và lượng còn được trả.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ reason (string): lý do nghiệp vụ do người dùng nhập; Text/attribute kiểm tra độ dài
    // trước khi ghi nhật ký hoặc phiếu.
    // THAM SỐ lineId (int): mã một dòng chi tiết đơn; trong yêu cầu trả, 0 nghĩa là xử lý tất cả
    // dòng còn được trả. Mặc định khi không truyền: 0.
    // THAM SỐ quantity (int): số lượng muốn trả của dòng chọn; khi lineId = 0 service tự lấy toàn
    // bộ lượng còn được trả. Mặc định khi không truyền: 0.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Account/Return. Tham số hàm: (string id, string reason, int lineId = 0, int
    // quantity = 0).
    [Authorize, HttpPost]
    public IActionResult Return(string id, string reason, int lineId = 0, int quantity = 0)
    {
        try
        {
            store.RequestReturn(id, reason, CustomerId, lineId, quantity);
            TempData["Success"] = "Đã gửi yêu cầu trả hàng.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Order), new { id });
    }

    // HÀM Password: Mở form đổi mật khẩu cho tài khoản đã đăng nhập.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Account/Password. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Password.cshtml.
    [Authorize]
    public IActionResult Password() => View(new PasswordInput());

    // HÀM Password: Kiểm tra mẫu và gọi ChangePassword để xác minh mật khẩu cũ, băm mật khẩu mới;
    // cookie cũ sẽ bị kiểm tra lại qua security_stamp.
    // THAM SỐ input (PasswordInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Account/Password. Tham số hàm: (PasswordInput input).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Password.cshtml.
    [Authorize, HttpPost]
    public IActionResult Password(PasswordInput input)
    {
        // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để không
        // lưu dữ liệu
        // sai.
        if (!ModelState.IsValid)
            return View(input);
        try
        {
            store.ChangePassword(CustomerId, input);
            TempData["Success"] = "Đã đổi mật khẩu. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Index));
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(input);
        }
    }

    // HÀM Denied: Hiển thị trang báo tài khoản không có quyền truy cập.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Account/Denied. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Account/Denied.cshtml.
    public IActionResult Denied() => View();
}
