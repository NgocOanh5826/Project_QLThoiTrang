# Hướng dẫn đọc code và tìm chức năng QOANH

Tài liệu này chỉ vị trí xử lý và điều kiện đang có trong mã nguồn. Comment tiếng Việt trong từng file dùng các nhãn **VAI TRÒ FILE**, **CHỨC NĂNG**, **ĐIỂM VÀO**, **GIAO DIỆN**, **ĐIỀU KIỆN**, **NHÁNH THÊM/SỬA** và **BƯỚC XÓA** để bạn tìm nhanh bằng Ctrl+F.

## 1. Đọc một chức năng theo thứ tự nào?

```text
Người dùng mở trang (GET)
  → Controller lấy dữ liệu từ StoreService
  → View .cshtml hiển thị form
  → Người dùng bấm nút Lưu/Xóa/Đặt hàng (thường là POST)
  → MVC bind name/asp-for vào tham số hoặc Input model
  → Kiểm tra đăng nhập, vai trò, CSRF và ModelState
  → Controller gọi service hoặc callback StoreService.Write
  → Service kiểm tra điều kiện nghiệp vụ
  → EF ghi SQL trong transaction, rồi Commit
  → Controller trả lại form có lỗi hoặc chuyển hướng sau thành công
```

Các kiểm tra được thực hiện tại những vị trí khác nhau trong pipeline hoặc action; không phải mọi action đều có một lệnh `ModelState.IsValid`. Xem comment và code của action cụ thể để biết kiểm tra nào áp dụng.

| Phần | Vị trí | Dùng để làm gì? |
| --- | --- | --- |
| Khởi động và route | [Program.cs](Project_QLThoiTrang/Program.cs) | Đăng ký MVC, SQL, cookie, Session, CSRF, middleware; route mặc định `/{controller}/{action}/{id?}` |
| Nhận yêu cầu | [Controllers](Project_QLThoiTrang/Controllers) | Nhận GET/POST, đọc form, gọi service, trả view/JSON/file/chuyển hướng |
| Điều kiện và lưu nghiệp vụ | [Services](Project_QLThoiTrang/Services) | Kiểm tra quyền, tồn, tiền, trạng thái, chống gửi trùng và quản lý transaction |
| Dữ liệu/mẫu nhập | [Models](Project_QLThoiTrang/Models) | Thuộc tính, công thức tiền, Required/Range/StringLength/Compare... |
| Form và nút | [Views](Project_QLThoiTrang/Views) | `asp-for` gắn ô với model; `asp-action`/`asp-controller` chọn nơi nhận |
| Truy cập database | [StoreDbContext.cs](Project_QLThoiTrang/Data/StoreDbContext.cs) | DbSet và mapping EF tới SQL |
| Tên bảng/cột SQL gốc | [Mapping.cs](Project_QLThoiTrang/Data/Native/Mapping.cs) | Bảng, khóa và quan hệ của các entity `*Row` |
| Hành vi trong trình duyệt | [site.js](Project_QLThoiTrang/wwwroot/js/site.js), [chat.js](Project_QLThoiTrang/wwwroot/js/chat.js) | Chọn biến thể, tự gửi form, lọc, địa chỉ, xem trước tệp, chat |
| Giao diện | [site.css](Project_QLThoiTrang/wwwroot/css/site.css), [screens.css](Project_QLThoiTrang/wwwroot/css/screens.css) | Bố cục, màu, kích thước và responsive |

Ví dụ: `GET /Admin/Product` mở form thêm; `GET /Admin/Product?id=5` mở form sửa sản phẩm 5. Cả hai gửi `POST /Admin/Product`, nhưng overload POST nhận `Product input` và phân biệt bằng `input.Id == 0`. Khi mở form thêm, **không truyền id**; overload GET hiện tại phân biệt `id == null`, không phải `id == 0`.

## 2. Quyền truy cập và các kiểu điều kiện

| Quyền | Nhóm thao tác quản trị |
| --- | --- |
| Admin | Qua bộ lọc action quản trị; quản lý nhân viên, cấu hình, lịch sử và các nhóm nghiệp vụ |
| Catalog | Sản phẩm, danh mục, thương hiệu/size/màu, biến thể, ưu đãi |
| Inventory | Nhập kho, kiểm kê, phiếu nhập, kho/nhà cung cấp; giá vốn ở ValuationController |
| Sales | Đơn, thu tiền, trả hàng, báo cáo; tài chính và vận đơn ở controller riêng |
| Support | Hồ sơ khách, đánh giá, chat hỗ trợ |
| Customer | Mua hàng, hồ sơ/đơn của mình, yêu thích, đánh giá sau mua, chat của mình |

Điểm kiểm tra action quản trị là `AdminExtensions.OnActionExecuting`. Các controller Finance, Shipping, Valuation, Chat, Documents có `[Authorize]` riêng. Service tiếp tục kiểm tra quyền trong các thao tác có `Permission`/`NativePermission`; không được suy ra rằng ẩn nút trên view đã đủ để bảo vệ dữ liệu.

- `ModelState.IsValid`: dữ liệu bind từ form và các attribute trên model có hợp lệ hay không; sai thường trả lại form hoặc báo lỗi.
- `Require(điều_kiện, thông_báo)`: điều kiện **phải đúng** để chạy tiếp. Sai thì ném `InvalidOperationException`; các controller có `catch` sẽ đưa lỗi ra form hoặc TempData. Không phải mọi action đều có cùng cách bắt lỗi.
- `expected`: số tồn khi mở form, dùng đối chiếu tồn hiện tại để tránh ghi đè thao tác người khác.
- `CheckoutToken`/`ClientToken`/`Reference`: các mã phục vụ chống tạo đơn, tin hoặc chứng từ trùng, theo từng luồng.
- `NotFound()` = HTTP 404; `Forbid()` = từ chối quyền, có thể được cookie handler chuyển tới trang Denied; `BadRequest()` = HTTP 400; thiếu schema có thể nhận HTTP 503.

## 3. Thêm, sửa, xóa sản phẩm nằm ở đâu?

**Form:** [Views/Admin/Product.cshtml](Project_QLThoiTrang/Views/Admin/Product.cshtml). **Danh sách/nút thêm-sửa:** [Products.cshtml](Project_QLThoiTrang/Views/Admin/Products.cshtml). **Xác nhận ẩn:** [DeleteProduct.cshtml](Project_QLThoiTrang/Views/Admin/DeleteProduct.cshtml).

1. `AdminController.Product(int? id)` lấy thương hiệu/danh mục, tạo `Product` mới nếu không có id hoặc lấy sản phẩm cũ nếu có id.
2. Người dùng gửi form tới `AdminController.Product(Product input)` có `[HttpPost]`.
3. POST kiểm tra danh mục có trong dữ liệu, ảnh thuộc 5 ảnh thư viện và `ModelState` hợp lệ. Các giới hạn tên/giá/màu/tồn nằm trong [Models/Product.cs](Project_QLThoiTrang/Models/Product.cs).
4. **Thêm:** `input.Id == 0` → cấp mã → ép `Stock = 0` → `d.Products.Add(input)`.
5. **Sửa:** tìm vị trí bằng `FindIndex` → không có trả 404 → giữ `Stock` cũ → `d.Products[index] = input`.
6. `StoreService.Write` chạy chuẩn hóa/kiểm tra/tính giá vốn rồi `PersistNative` đồng bộ `SanPham`, biến thể/tồn liên quan trong transaction.
7. **Xóa trên giao diện:** GET `DeleteProduct` chỉ hỏi xác nhận. POST `DeleteProductConfirmed` có `ActionName("DeleteProduct")` đặt `Active = false`. Bản ghi sản phẩm vẫn còn; Shop lọc `Active` để ẩn khỏi cửa hàng.

Tồn kho phải thay tại màn hình nhập kho/kiểm kê. Việc form Product hiển thị ô tồn không có nghĩa POST sẽ cho sửa trực tiếp tồn.

## 4. Các luồng thêm/sửa/xóa và điều kiện quan trọng khác

| Chức năng | Điểm nhận và nơi lưu | Điều kiện/nhánh xử lý |
| --- | --- | --- |
| Thêm danh mục | `AdminController.Category(string name)` → `Write` | Trim, tên 2–50 ký tự, không trùng không phân biệt hoa/thường |
| Xóa danh mục | `AdminController.DeleteCategory` → `d.Categories.Remove` → `PersistNative` | Chặn nếu bất kỳ sản phẩm nào còn thuộc danh mục; sau khi bỏ khỏi StoreData, adapter xóa dòng `DanhMuc` không còn tham chiếu |
| Sửa tên danh mục sản phẩm | Chưa có action đổi tên riêng trong form `Category` hiện tại | `Category(string name)` là thêm; muốn hỗ trợ đổi tên cần xử lý đồng bộ các sản phẩm tham chiếu |
| Thương hiệu/size/màu/kho/NCC | `AdminExtensions.Reference` → `NativeManagement.SaveReference` | `kind` chọn bảng; Id = 0 thêm, khác 0 sửa; quyền theo loại, DataAnnotations và điều kiện tên trùng theo từng nhánh |
| Biến thể | `AdminExtensions.Variant` → `SaveVariant` | Sản phẩm/size/màu tồn tại, SKU/tổ hợp không trùng; sửa không đổi định danh sản phẩm/size/màu |
| Nhân viên | `AdminExtensions.StaffMember` → `SaveStaff` | Chỉ Admin; email duy nhất; không tự khóa/hạ quyền Admin, phải còn Admin hoạt động; mật khẩu mới được băm |
| Khách hàng | `AdminExtensions.EditCustomer` → `UpdateCustomer` | Support/Admin, đúng Role Customer, sửa hồ sơ/trạng thái; không đổi email/vai trò/mật khẩu tại form này |
| Ưu đãi | `AdminController.Promotion` → `Write` | Ngày bắt đầu/kết thúc, mã duy nhất, loại/giá trị/hạn mức/lượt và danh sách sản phẩm hợp lệ; Id = 0 thêm, khác 0 cập nhật bản ghi tìm được |
| Thêm giỏ | `ShopController.Add` → Session; `ShoppingSession.OnActionExecuted` → `SaveShopping` cho Customer | Sản phẩm/biến thể hoạt động, lượng 1–20 và tổng cùng biến thể không vượt tồn/20 |
| Sửa/xóa giỏ | `ShopController.UpdateCart` | `remove` hoặc lượng 0 xóa dòng; đổi sang biến thể đã có thì gộp; kiểm tra lượng tổng trước lưu; không xóa sản phẩm |
| Nhập kho | `AdminController.Receive` hoặc `AdminExtensions.ReceiveMany` → `ReceiveBatch` | Inventory/Admin là nhân viên; 1–100 dòng, mã chưa dùng, cùng kho/NCC, biến thể không trùng, dữ liệu và tồn không vượt giới hạn |
| Kiểm kê tổng | `AdminController.Inventory` → `AdjustStock` | Quyền Inventory/Admin, lý do 5–300 ký tự, tồn 0–100.000, khớp expected; ghi nhật ký và phân bổ tồn |
| Kiểm kê biến thể/kho | `AdminExtensions.AdjustVariant` → `AdjustVariantStock` | Kho hoạt động, biến thể tồn tại, khớp expected; sửa tồn chi tiết và cộng chênh lệch vào tổng |
| Thu tiền | `AdminController.Payment` → `RecordPayment` | Sales/Admin, trạng thái hợp lệ, không đang gửi GHN, số tiền đúng toàn bộ phần còn thu và biên nhận chưa dùng |
| Yêu cầu trả | `AccountController.Return` → `RequestReturn` | Đơn của mình đã giao/hoàn thành, không có phiếu Pending, lượng không vượt phần còn được trả, lý do hợp lệ |
| Duyệt/từ chối trả | `AdminController.ResolveReturn` → `StoreOperations.ResolveReturn` | Sales/Admin, phiếu Pending và đơn đủ điều kiện; duyệt cần đủ tiền gốc, tiền hoàn hợp lệ/chứng từ mới, rồi hoàn tồn và ghi Refund; từ chối chỉ đổi trạng thái |
| Đánh giá/tệp | `ShopController.Review` → `NativeManagement.Review` → `ReviewUploads.Validate` | Khách hoạt động, đã mua/đã giao, chưa đánh giá, sao 1–5, nội dung 5–1.000; ≤5 tệp, ≤1 video, ảnh ≤5 MB/video ≤20 MB/tổng ≤30 MB, đúng chữ ký định dạng |
| Ẩn/hiện đánh giá | `AdminExtensions.Moderate` → `ModerateReview` | Support/Admin; đổi trạng thái, giữ đánh giá và tệp; URL media tiếp tục kiểm tra quyền/trạng thái |
| Giá vốn | `ValuationController.Edit` → `SetInventoryValue` | Inventory/Admin, biến thể tồn tại, cost 0–100 triệu, lý do và expected hợp lệ; không sửa ngược giá vốn đã chụp trên đơn cũ |
| Công nợ/chi NCC | `FinanceController.Reconcile/Pay` → `ReconcileSupplier/PaySupplier` | Sales/Admin; đối soát trước khi chi, không đổi số phải trả sau phát sinh chi, chứng từ duy nhất, chi trong phần còn nợ |

Các loại danh mục dùng chung, biến thể, nhân viên và ưu đãi hiện dùng màn hình thêm/sửa với trạng thái nếu model hỗ trợ; không mặc định có một action DELETE riêng cho mọi bảng. Tìm `Remove`, `RemoveRange` hoặc phép gán trạng thái để xác định xóa thật, xóa mềm hay thay danh sách.

## 5. Đặt hàng, trạng thái và cách lưu

**Đặt hàng:** GET `Shop.Checkout` chụp token và báo giá → POST kiểm tra token/giỏ/mẫu → `PlaceOrder` kiểm tra token đã có, khách hoạt động, phương thức, giỏ/giá hiện tại và tồn các kho hoạt động → trừ tồn theo kho → chụp phí vận chuyển/QR chuyển khoản nếu chọn → tính ưu đãi → thêm đơn trong `Write`. Thành công mới xóa giỏ và mở `Success` của đúng khách.

**Chuyển trạng thái** trong `ChangeOrderStatus`:

```text
Chờ xác nhận → Đã xác nhận / Đã hủy
Đã xác nhận → Đang chuẩn bị / Đang giao / Đã hủy
Đang chuẩn bị → Đang giao / Đã hủy
Đang giao → Đã giao
Đã giao → Hoàn thành
```

Khách chỉ được hủy đơn chờ xác nhận của chính mình. Nhân viên đổi trạng thái cần Sales/Admin. Đánh dấu đã giao/hoàn thành cần thu đủ tiền; chuyển khoản phải thu đủ trước khi giao. Hủy đơn cần không còn vận đơn chưa hủy/đối soát và chưa thu tiền, sau đó hoàn tồn cùng lượt ưu đãi. Khi duyệt trả hết hàng, service có thể đưa đơn sang `Đã hoàn hàng`.

**Hai cách ghi dữ liệu:**

- `Write`: `Load` → callback sửa `StoreData` → `PrepareStock` → `Validate` → `UpdateCosting` → `PersistNative` → `SaveChanges` → `Commit`.
- `NativeWrite`: callback sửa trực tiếp `DbSet` → `SaveChanges` → `Commit`; mỗi hàm nghiệp vụ tự gọi các kiểm tra cần thiết.

Cả hai dùng transaction Serializable và khóa ứng dụng `QOANH.Store.Write`. `SaveChanges` đẩy SQL nhưng chưa thay thế `Commit` của transaction ngoài. Lỗi trước Commit khiến transaction chưa hoàn tất bị hủy khi dispose. View/JavaScript không trực tiếp ghi database.

## 6. Tài khoản, OTP, chat, PDF và GHN

- **Đăng ký/đăng nhập:** `AccountController` → `StoreService.Register/Authenticate` → `SignIn` tạo cookie/claims. Id khách dương, nhân viên âm. `Program.cs` đối chiếu trạng thái/quyền/security stamp để loại cookie cũ khi tài khoản bị đổi.
- **Đổi mật khẩu:** `Account.Password` → `ChangePassword`, xác minh mật khẩu hiện tại, băm mật khẩu mới.
- **Quên mật khẩu:** `Recovery.Forgot` → `PasswordRecovery.Request` → `IssueReset` → SMTP → `ResetDelivered`. OTP 6 số, 10 phút, tối đa 5 lần thử; yêu cầu phát mã có giới hạn 1 phút/5 lần giờ. `Reset` → `ResetPassword` kiểm tra HMAC, stamp và trạng thái mã rồi băm mật khẩu mới. Thông báo không tiết lộ email có tồn tại.
- **Chat:** `Views/Chat/Index.cshtml` → `chat.js` → `Chat.Messages/Send/Read` → `CustomerFeatures`. Khách chỉ hội thoại của mình; Support/Admin hỗ trợ khách. Một clientToken dùng lại trả tin cũ nếu cùng người/nội dung; service giới hạn tốc độ.
- **PDF:** `DocumentsController` kiểm tra chủ chứng từ/quyền → `PdfDocuments.Create` sinh byte → trả `File`. In PDF không đổi trạng thái đơn hoặc xác nhận thu tiền.
- **GHN:** `Shipping.Create` → `ShippingService.Create` → `ClaimShipment` lưu request hash → gọi API → `ShipmentResult`. Có mã rồi thì đồng bộ; timeout/lỗi chưa rõ được ghi `Unknown` và cần đối soát. `Sync` cập nhật vận đơn, không tự đổi trạng thái bán hàng trong `ChangeOrderStatus`.

## 7. Mở đúng file khi muốn thay đổi một chức năng

1. Tìm tên trang/nút trong `Views`, xem comment **FORM** để biết đường dẫn nhận và phương thức.
2. Tìm đúng controller/action và đúng overload GET/POST; với Admin nhớ kiểm tra file `AdminExtensions.cs` vì đây vẫn là cùng lớp `AdminController`.
3. Xem Input model và comment **RÀNG BUỘC MẪU NHẬP** để hiểu trường bắt buộc, min/max, biểu thức và xác nhận.
4. Theo hàm service được gọi, đọc các comment **ĐIỀU KIỆN**, **NHÁNH THÊM/SỬA**, **BƯỚC XÓA**, **GHI SQL**.
5. Nếu cần biết bảng nào được sửa, mở mapping và `NativeStoreAdapter.PersistNative`; với `NativeWrite`, xem trực tiếp DbSet trong hàm nghiệp vụ.
6. Nếu thay UI tự cập nhật/lọc/biến thể, tìm `data-*` tương ứng trong `site.js` hoặc `chat.js`. Quy tắc cuối cùng về quyền, tồn và tiền vẫn phải nằm ở server.

Các file `scripts/` và `tests/` là công cụ kiểm tra/bàn giao, không phải action xử lý form. Script SQL trong `delivery/` là triển khai/nâng cấp; các ZIP và thư mục `delivery/verify-extracted` là bản đóng gói/đối chiếu, không phải nơi sửa source đang chạy.

## 8. Chỉ mục vị trí hàm trong source

Bảng bên dưới được lập sau khi thêm comment. Số dòng có thể thay đổi khi chỉnh code tiếp; tên hàm và chữ ký vẫn là cách tìm ổn định bằng Ctrl+F. Liên kết trỏ tới dòng khai báo hoặc attribute đầu của hàm.

<!-- FUNCTION_INDEX -->

### Controller: URL và xử lý form

**[AccountController.cs](Project_QLThoiTrang/Controllers/AccountController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Login(string? returnUrl)` | [26](Project_QLThoiTrang/Controllers/AccountController.cs#L26) | Mở form đăng nhập; chỉ giữ returnUrl nội bộ để tránh chuyển hướng ra trang bên ngoài. |
| POST `Login(LoginInput input)` | [36](Project_QLThoiTrang/Controllers/AccountController.cs#L36) | Kiểm tra email/mẫu nhập và xác thực mật khẩu bằng StoreService.Authenticate; thành công tạo cookie, gộp giỏ và chuyển tới returnUrl hoặc trang theo vai trò. |
| GET `Register(string? returnUrl)` | [71](Project_QLThoiTrang/Controllers/AccountController.cs#L71) | Mở form đăng ký khách hàng; chỉ nhận đường dẫn quay về nội bộ. |
| POST `Register(RegisterInput input)` | [80](Project_QLThoiTrang/Controllers/AccountController.cs#L80) | Chuẩn hóa tên/email, kiểm tra mẫu rồi Register để thêm khách và băm mật khẩu; tạo cookie và gộp giỏ khi thành công, email trùng trả lại form có lỗi. |
| Nội bộ `SignIn(Customer c, bool rememberMe = false, bool mergeShopping = false)` | [108](Project_QLThoiTrang/Controllers/AccountController.cs#L108) | Helper tạo cookie với claim id/tên/email/quyền, lịch sử và security_stamp từ hash mật khẩu. |
| POST `Logout()` | [159](Project_QLThoiTrang/Controllers/AccountController.cs#L159) | Nhận POST của tài khoản đã đăng nhập: ghi thời điểm đăng xuất, xóa Session và cookie rồi quay về trang chủ. |
| GET `Index()` | [174](Project_QLThoiTrang/Controllers/AccountController.cs#L174) | Tài khoản đã đăng nhập xem hồ sơ của chính mình; lấy id từ claim, không lấy id người khác từ form. |
| POST `Index(ProfileInput input)` | [195](Project_QLThoiTrang/Controllers/AccountController.cs#L195) | Sửa tên/điện thoại/địa chỉ của chính tài khoản sau khi ModelState hợp lệ; Write kiểm tra dữ liệu rồi cấp lại cookie để tên hiển thị được cập nhật. |
| GET `Orders()` | [230](Project_QLThoiTrang/Controllers/AccountController.cs#L230) | Chỉ lấy các đơn có CustomerId bằng id trong cookie hiện tại; sắp xếp mới nhất trước. |
| GET `Order(string id)` | [246](Project_QLThoiTrang/Controllers/AccountController.cs#L246) | Mở đơn và yêu cầu trả hàng thuộc đúng tài khoản đang đăng nhập; không được xem đơn chỉ bằng cách đoán mã của khách khác. |
| POST `Cancel(string id)` | [263](Project_QLThoiTrang/Controllers/AccountController.cs#L263) | Gửi yêu cầu hủy đơn của khách tới ChangeOrderStatus với customer = true; service chỉ chấp nhận đơn của mình còn Chờ xác nhận. |
| POST `Return(string id, string reason, int lineId = 0, int quantity = 0)` | [285](Project_QLThoiTrang/Controllers/AccountController.cs#L285) | Gửi yêu cầu trả toàn bộ phần còn lại hoặc một dòng với số lượng cụ thể; RequestReturn kiểm tra chủ đơn, trạng thái và lượng còn được trả. |
| GET `Password()` | [304](Project_QLThoiTrang/Controllers/AccountController.cs#L304) | Mở form đổi mật khẩu cho tài khoản đã đăng nhập. |
| POST `Password(PasswordInput input)` | [313](Project_QLThoiTrang/Controllers/AccountController.cs#L313) | Kiểm tra mẫu và gọi ChangePassword để xác minh mật khẩu cũ, băm mật khẩu mới; cookie cũ sẽ bị kiểm tra lại qua security_stamp. |
| GET `Denied()` | [337](Project_QLThoiTrang/Controllers/AccountController.cs#L337) | Hiển thị trang báo tài khoản không có quyền truy cập. |

**[AdminController.cs](Project_QLThoiTrang/Controllers/AdminController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index()` | [25](Project_QLThoiTrang/Controllers/AdminController.cs#L25) | Trang tổng quan quản trị; lấy StoreData để view tổng hợp sản phẩm, đơn, khách và tồn. |
| GET `Products(string? q, string? category, int? brandId, string? state)` | [34](Project_QLThoiTrang/Controllers/AdminController.cs#L34) | Danh sách sản phẩm; lọc theo từ khóa tên/màu, danh mục, thương hiệu và trạng thái; chuẩn bị các lựa chọn bộ lọc trong ViewBag. |
| GET `Product(int? id)` | [69](Project_QLThoiTrang/Controllers/AdminController.cs#L69) | Mở form thêm khi không có id, hoặc tìm sản phẩm để sửa khi có id; chuẩn bị thương hiệu/danh mục, không tìm thấy trả 404. |
| POST `Product(Product input)` | [87](Project_QLThoiTrang/Controllers/AdminController.cs#L87) | Nhận form thêm/sửa sản phẩm. |
| GET `DeleteProduct(int id)` | [146](Project_QLThoiTrang/Controllers/AdminController.cs#L146) | Mở trang xác nhận ẩn sản phẩm theo id; chưa thay đổi dữ liệu ở yêu cầu GET. |
| POST `DeleteProductConfirmed(int id)` | [157](Project_QLThoiTrang/Controllers/AdminController.cs#L157) | Xử lý nút xác nhận bằng POST /Admin/DeleteProduct: đặt Active = false trong Write. |
| GET `Categories()` | [179](Project_QLThoiTrang/Controllers/AdminController.cs#L179) | Mở danh sách danh mục và dữ liệu liên quan để kiểm tra danh mục đang được sản phẩm sử dụng. |
| GET `Category()` | [184](Project_QLThoiTrang/Controllers/AdminController.cs#L184) | Mở form thêm danh mục với tên ban đầu rỗng. |
| POST `AddCategory(string name)` | [191](Project_QLThoiTrang/Controllers/AdminController.cs#L191) | Action tương thích chuyển yêu cầu POST thêm danh mục sang Category(name); dùng chung kiểm tra và cách lưu. |
| POST `Category(string name)` | [200](Project_QLThoiTrang/Controllers/AdminController.cs#L200) | Thêm danh mục sau khi trim, yêu cầu tên 2–50 ký tự và không trùng không phân biệt hoa/thường; hợp lệ thì thêm vào d.Categories trong Write. |
| POST `DeleteCategory(string name)` | [234](Project_QLThoiTrang/Controllers/AdminController.cs#L234) | Xóa danh mục bằng POST; nếu có bất kỳ sản phẩm nào đang thuộc danh mục thì báo lỗi và giữ lại, ngược lại Remove khỏi d.Categories để PersistNative đồng bộ. |
| GET `Orders(string? status, string? q, string? payment, string? period)` | [258](Project_QLThoiTrang/Controllers/AdminController.cs#L258) | Lọc danh sách đơn theo trạng thái, từ khóa mã/tên/điện thoại, phương thức thanh toán và khoảng ngày nhanh; trả đơn mới nhất trước. |
| GET `Order(string id)` | [294](Project_QLThoiTrang/Controllers/AdminController.cs#L294) | Mở chi tiết đơn theo mã công khai, không có thì trả 404; dữ liệu hiển thị không được sửa ngay tại GET. |
| POST `OrderStatus(string id, string status)` | [303](Project_QLThoiTrang/Controllers/AdminController.cs#L303) | Nhận trạng thái mới, chuyển điều kiện chuyển bước/thu tiền/hoàn tồn sang StoreService.ChangeOrderStatus; báo kết quả bằng TempData rồi quay lại chi tiết đơn. |
| GET `Customers(string? q, string? state, string? hasOrders)` | [325](Project_QLThoiTrang/Controllers/AdminController.cs#L325) | Danh sách tài khoản Role Customer; lọc từ khóa, trạng thái và có/chưa có đơn, không đưa nhân viên vào danh sách này. |
| GET `Customer(int id)` | [350](Project_QLThoiTrang/Controllers/AdminController.cs#L350) | Hiển thị thông tin một tài khoản và danh sách đơn của tài khoản đó; id không tồn tại trả 404. |
| GET `Inventory()` | [364](Project_QLThoiTrang/Controllers/AdminController.cs#L364) | Mở kiểm kê tồn tổng theo sản phẩm, kèm 200 biến động kho gần nhất. |
| POST `Inventory(int id, int quantity, int expected, string reason)` | [378](Project_QLThoiTrang/Controllers/AdminController.cs#L378) | Nhận số tồn thực tế và tồn expected lúc mở form; AdjustStock kiểm tra quyền, chống ghi đè và ghi nhật ký thay đổi. |
| GET `Receive(string tab = "single")` | [400](Project_QLThoiTrang/Controllers/AdminController.cs#L400) | Mở form nhập kho một hoặc nhiều dòng theo tab; chỉ cho chọn kho/NCC đang hoạt động qua ReceiveOptions. |
| POST `Receive(ReceiveInput input)` | [413](Project_QLThoiTrang/Controllers/AdminController.cs#L413) | Nhận phiếu nhập một dòng; kiểm tra DataAnnotations rồi gọi Receive/ReceiveBatch. |
| POST `Payment(string id, decimal amount, string reference)` | [440](Project_QLThoiTrang/Controllers/AdminController.cs#L440) | Ghi nhận khoản tiền đã thu cho đơn; RecordPayment kiểm tra đúng số còn thu, trạng thái đơn và mã biên nhận chưa trùng. |
| GET `Returns()` | [459](Project_QLThoiTrang/Controllers/AdminController.cs#L459) | Hiển thị danh sách phiếu trả hàng mới nhất trước. |
| POST `ResolveReturn(int id, bool approve, string resolution, string reference)` | [469](Project_QLThoiTrang/Controllers/AdminController.cs#L469) | Nhận nút duyệt/từ chối, ghi chú và chứng từ hoàn tiền; ResolveReturn ở service kiểm tra phiếu Pending, hoàn tồn và ghi tiền hoàn khi được duyệt. |
| GET `Promotions()` | [488](Project_QLThoiTrang/Controllers/AdminController.cs#L488) | Mở danh sách chương trình ưu đãi. |
| GET `Promotion(int? id)` | [496](Project_QLThoiTrang/Controllers/AdminController.cs#L496) | Mở form thêm khi id trống hoặc form sửa theo id; nạp danh sách sản phẩm áp dụng, không tìm thấy trả 404. |
| POST `Promotion(Promotion input)` | [512](Project_QLThoiTrang/Controllers/AdminController.cs#L512) | Thêm/sửa ưu đãi: kiểm tra ngày, loại PHANTRAM/SOTIEN, giá trị/hạn mức/lượt, sản phẩm áp dụng và mã chưa trùng; chuẩn hóa mã chữ hoa. |
| GET `Reports(DateTime? from, DateTime? to, string? period)` | [578](Project_QLThoiTrang/Controllers/AdminController.cs#L578) | Chọn khoảng ngày báo cáo từ bộ lọc nhanh hoặc from/to; ngày bắt đầu sau ngày kết thúc thì trả danh sách rỗng kèm lỗi. |
| GET `Settings()` | [609](Project_QLThoiTrang/Controllers/AdminController.cs#L609) | Mở thông tin cửa hàng hiện tại để chỉnh sửa. |
| POST `Settings(StoreSettings input)` | [617](Project_QLThoiTrang/Controllers/AdminController.cs#L617) | Kiểm tra mẫu tên/email/điện thoại/địa chỉ rồi thay d.Settings trong Write; thông báo và tải lại trang sau lưu. |
| GET `Export()` | [638](Project_QLThoiTrang/Controllers/AdminController.cs#L638) | Xuất toàn bộ đơn thành CSV UTF-8 có BOM với mã đơn, ngày, trạng thái và tổng tiền. |

**[AdminExtensions.cs](Project_QLThoiTrang/Controllers/AdminExtensions.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| Nội bộ `OnActionExecuting(ActionExecutingContext context)` | [19](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L19) | Bộ lọc quyền trước mọi action quản trị: Admin đi tiếp; Catalog quản lý danh mục/sản phẩm, Inventory quản lý kho, Sales quản lý đơn, Support quản lý khách/đánh giá. |
| GET `References(string kind = "brands", int id = 0)` | [82](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L82) | Mở danh sách danh mục theo kind hoặc chuyển sang form sửa khi id > 0; tính tập id đang được sử dụng để view thể hiện trạng thái liên quan. |
| GET `Reference(string kind = "brands", int id = 0)` | [127](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L127) | Mở form thêm danh mục khi id = 0 hoặc form sửa bản ghi cùng kind; loại sai trả 400, id không có trả 404. |
| POST `References(ReferenceInput input)` | [146](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L146) | Action tương thích nhận ReferenceInput rồi chuyển sang Reference(input) để dùng chung xử lý. |
| POST `Reference(ReferenceInput input)` | [155](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L155) | Nhận form thương hiệu/size/màu/kho/NCC, kiểm tra kind và ModelState rồi SaveReference; lỗi giữ dữ liệu nhập và hiển thị lại form. |
| Nội bộ `ReceiveOptions()` | [187](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L187) | Chuẩn bị ViewBag.Products/Variants và danh sách kho/NCC hoạt động cho form nhập kho; đây là helper nội bộ, không phải URL công khai. |
| Nội bộ `VariantOptions()` | [200](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L200) | Chuẩn bị danh sách sản phẩm, kích thước và màu để dùng chung khi mở hoặc trả lại form biến thể bị lỗi. |
| GET `Variants(int? id = null)` | [213](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L213) | Mở danh sách biến thể; có id thì chuyển sang trang sửa riêng, không có thì nạp các lựa chọn và danh sách biến thể. |
| GET `Variant(int id = 0)` | [228](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L228) | Mở form thêm khi id = 0, hoặc đọc BienTheSanPham để dựng VariantInput khi sửa; không có bản ghi trả 404. |
| POST `Variants(VariantInput input)` | [252](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L252) | Action tương thích chuyển VariantInput sang Variant(input). |
| POST `Variant(VariantInput input)` | [261](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L261) | Nhận form biến thể, kiểm tra ModelState rồi SaveVariant; service kiểm tra SKU/tổ hợp không trùng và cấm sửa định danh của biến thể cũ. |
| GET `VariantStock()` | [284](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L284) | Mở bảng tồn chi tiết theo biến thể và kho từ StoreData. |
| POST `AdjustVariant( int variantId, int warehouseId, int quantity, int expected, string reason )` | [293](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L293) | Nhận kiểm kê biến thể/kho, số tồn expected và lý do; service kiểm tra rồi lưu, sau đó quay lại StockCount đúng biến thể/kho. |
| GET `Receipts()` | [320](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L320) | Mở danh sách phiếu nhập từ SQL, bổ sung bảng tra tên kho/NCC/nhân viên trong ViewBag. |
| GET `Receipt(int id)` | [339](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L339) | Mở một phiếu nhập và các dòng ChiTietPhieuNhap; không có đầu phiếu trả 404. |
| POST `ReceiveMany( List<ReceiveInput> lines, string reference, int warehouseId, int supplierId )` | [361](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L361) | Gán chung mã phiếu/kho/NCC cho các dòng form rồi gọi ReceiveBatch; lỗi kiểm tra dữ liệu được chuyển thành thông báo, quay lại tab nhiều dòng. |
| GET `Staff(int id = 0)` | [403](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L403) | Mở danh sách nhân viên hoặc chuyển sang StaffMember nếu có id cần sửa; chỉ Admin được qua bộ lọc quyền action này. |
| GET `StaffMember(int id = 0)` | [417](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L417) | Mở form thêm/sửa nhân viên; đổi id dương của form sang Id âm trong StoreData để tìm đúng nhân viên. |
| POST `Staff(StaffInput input)` | [439](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L439) | Action tương thích chuyển form nhân viên sang StaffMember(input). |
| POST `StaffMember(StaffInput input)` | [448](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L448) | Admin lưu nhân viên qua SaveStaff; nếu lỗi thì xóa mật khẩu khỏi input/ModelState trước khi hiển thị lại form để không điền lại giá trị nhạy cảm. |
| GET `StockCount(int variantId, int warehouseId)` | [474](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L474) | Mở form kiểm kê một biến thể tại kho; bắt buộc biến thể tồn tại và kho đang hoạt động. |
| GET `ReturnRequest(int id)` | [494](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L494) | Mở chi tiết một phiếu trả hàng, bọc thành danh sách để dùng chung model/view hiển thị. |
| GET `EditCustomer(int id)` | [505](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L505) | Mở form sửa hồ sơ tài khoản Role Customer, kèm id/trạng thái; không tìm thấy đúng khách trả 404. |
| POST `EditCustomer(int id, ProfileInput input, bool active)` | [532](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L532) | Nhận hồ sơ và trạng thái mới, gọi UpdateCustomer với quyền Support/Admin; thành công quay về chi tiết khách, lỗi giữ form. |
| GET `Reviews()` | [558](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L558) | Mở danh sách đánh giá kể cả đánh giá ẩn để Support/Admin kiểm duyệt; nạp tên sản phẩm cho view. |
| POST `Moderate(int id, bool active)` | [566](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L566) | Nhận nút ẩn/hiện đánh giá và gọi ModerateReview; không xóa đánh giá hay tệp. |
| GET `LoginHistory()` | [584](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L584) | Admin xem tối đa 300 nhật ký đăng nhập gần nhất trực tiếp từ SQL. |

**[ChatController.cs](Project_QLThoiTrang/Controllers/ChatController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| Nội bộ `Conversation(int customerId)` | [20](Project_QLThoiTrang/Controllers/ChatController.cs#L20) | Khách luôn dùng id của chính mình; Admin/Support mới dùng customerId được chọn để mở hội thoại khách cần hỗ trợ. |
| GET `Index(int customerId = 0)` | [29](Project_QLThoiTrang/Controllers/ChatController.cs#L29) | Nhân viên chưa chọn khách thì mở Inbox; đã chọn khách hoặc tài khoản Customer thì kiểm tra khách còn hoạt động rồi mở trang chat. |
| GET `Messages(int customerId, long before = 0, long after = 0)` | [47](Project_QLThoiTrang/Controllers/ChatController.cs#L47) | API JSON lấy tin cũ/tin mới theo before/after sau kiểm tra quyền; trả id, nội dung, cờ người gửi và thời gian UTC cho chat.js. |
| POST `Send(int customerId, string text, Guid clientToken)` | [75](Project_QLThoiTrang/Controllers/ChatController.cs#L75) | API POST gửi tin với clientToken chống gửi trùng; service kiểm tra nội dung/quyền/tốc độ, lỗi nghiệp vụ trả HTTP 400 kèm thông báo. |
| POST `Read(int customerId, long last)` | [93](Project_QLThoiTrang/Controllers/ChatController.cs#L93) | API POST đánh dấu đã đọc tới mã last trong đúng hội thoại; không đủ quyền trả Forbid. |

**[DocumentsController.cs](Project_QLThoiTrang/Controllers/DocumentsController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Order(string id)` | [20](Project_QLThoiTrang/Controllers/DocumentsController.cs#L20) | Xuất PDF đơn hàng: chỉ Admin/Sales hoặc Customer sở hữu đơn được tải; chứa dòng hàng, giảm giá, vận chuyển, tổng thu và hoàn tiền. |
| GET `Receipt(int id)` | [76](Project_QLThoiTrang/Controllers/DocumentsController.cs#L76) | Inventory/Admin tải PDF phiếu nhập và tổng các dòng; không tìm thấy phiếu trả 404. |
| GET `Return(int id)` | [124](Project_QLThoiTrang/Controllers/DocumentsController.cs#L124) | Xuất PDF phiếu trả cho Admin/Sales hoặc chính khách sở hữu, kèm lý do, tình trạng, số lượng và tiền hoàn. |

**[FinanceController.cs](Project_QLThoiTrang/Controllers/FinanceController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index(DateTime? from, DateTime? to, string? period)` | [22](Project_QLThoiTrang/Controllers/FinanceController.cs#L22) | Mở báo cáo doanh thu/giá vốn/lợi nhuận theo ngày; kiểm tra from <= to rồi gọi Profit. |
| GET `Suppliers()` | [50](Project_QLThoiTrang/Controllers/FinanceController.cs#L50) | Ghép dữ liệu phiếu nhập, nhà cung cấp, số phải trả và các chứng từ chi để hiển thị công nợ. |
| GET `Receipt(int id)` | [66](Project_QLThoiTrang/Controllers/FinanceController.cs#L66) | Mở một phiếu nhập, công nợ đã đối soát, các lần chi và tổng chi tiết tính lại; phiếu không tồn tại trả 404. |
| POST `Reconcile(int receiptId, decimal amount, string note)` | [92](Project_QLThoiTrang/Controllers/FinanceController.cs#L92) | Nhận số phải trả/căn cứ đối soát rồi gọi ReconcileSupplier; không được đổi số phải trả sau khi đã phát sinh chi tiền. |
| POST `Pay(int receiptId, decimal amount, string reference)` | [118](Project_QLThoiTrang/Controllers/FinanceController.cs#L118) | Nhận chứng từ chi và số tiền; PaySupplier kiểm tra phiếu đã đối soát, chứng từ duy nhất và số tiền không vượt phần còn nợ. |

**[HomeController.cs](Project_QLThoiTrang/Controllers/HomeController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index()` | [15](Project_QLThoiTrang/Controllers/HomeController.cs#L15) | Trang chủ nhận danh sách sản phẩm Active từ StoreService để hiển thị các khu vực giới thiệu hàng hóa. |
| GET `Info(string id = "about")` | [23](Project_QLThoiTrang/Controllers/HomeController.cs#L23) | Tra nội dung trang thông tin theo id trong bảng pages; id không được hỗ trợ trả 404, có thì ghép thông tin cửa hàng cho view. |
| GET `Privacy()` | [95](Project_QLThoiTrang/Controllers/HomeController.cs#L95) | Chuyển URL tương thích sang Info với id privacy. |
| GET `Error()` | [99](Project_QLThoiTrang/Controllers/HomeController.cs#L99) | Dựng trang lỗi với mã request để hỗ trợ tra log; không cache nội dung lỗi. |

**[RecoveryController.cs](Project_QLThoiTrang/Controllers/RecoveryController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Forgot()` | [13](Project_QLThoiTrang/Controllers/RecoveryController.cs#L13) | Mở form quên mật khẩu, ViewBag.Ready cho biết SMTP/khóa OTP đã cấu hình hay chưa. |
| POST `Forgot(ForgotInput input, CancellationToken cancellationToken)` | [26](Project_QLThoiTrang/Controllers/RecoveryController.cs#L26) | Kiểm tra email rồi yêu cầu gửi OTP qua PasswordRecovery; thông báo chung không xác nhận email có trong hệ thống. |
| GET `Reset()` | [46](Project_QLThoiTrang/Controllers/RecoveryController.cs#L46) | Mở form nhập email, OTP và mật khẩu mới. |
| POST `Reset(ResetInput input)` | [54](Project_QLThoiTrang/Controllers/RecoveryController.cs#L54) | Kiểm tra mẫu và hoàn tất khôi phục; OTP không hợp lệ/hết hạn/hết số lần thử thì trả lại form, thành công chuyển sang đăng nhập. |

**[ShippingController.cs](Project_QLThoiTrang/Controllers/ShippingController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index(string id)` | [20](Project_QLThoiTrang/Controllers/ShippingController.cs#L20) | Admin/Sales mở thông tin gửi hàng của đơn, trạng thái cấu hình GHN và vận đơn đã lưu nếu có; id không tồn tại trả 404. |
| POST `Create(ShipmentInput input, CancellationToken token)` | [40](Project_QLThoiTrang/Controllers/ShippingController.cs#L40) | Nhận tỉnh/phường, khối lượng và kích thước; ShippingService.Create kiểm tra điều kiện rồi gửi GHN. |
| POST `Sync(string id, CancellationToken token)` | [76](Project_QLThoiTrang/Controllers/ShippingController.cs#L76) | Lấy lại trạng thái vận đơn bằng mã đơn đã gửi qua ShippingService.Sync; cập nhật kết quả trong SQL rồi quay lại trang vận chuyển. |

**[ShopController.cs](Project_QLThoiTrang/Controllers/ShopController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index( string? q, string? category, string sort = "new", string? collection = null )` | [26](Project_QLThoiTrang/Controllers/ShopController.cs#L26) | Lọc sản phẩm đang bán theo từ khóa tên/màu, danh mục, bộ sưu tập mới/ưu đãi và thứ tự giá/tên/mã; đóng gói CatalogViewModel cho view. |
| GET `Detail(int id)` | [73](Project_QLThoiTrang/Controllers/ShopController.cs#L73) | Mở chi tiết sản phẩm đang bán, nạp đánh giá và tối đa 4 sản phẩm liên quan cùng danh mục; sản phẩm không tồn tại hoặc đã ẩn trả 404. |
| GET `Wishlist()` | [95](Project_QLThoiTrang/Controllers/ShopController.cs#L95) | Hiển thị sản phẩm đang hoạt động trong danh sách yêu thích ở Session; khách đăng nhập được nạp danh sách SQL trước action bởi ShoppingSession. |
| POST `Favorite(int id, string? returnUrl)` | [108](Project_QLThoiTrang/Controllers/ShopController.cs#L108) | Bật/tắt yêu thích sản phẩm: đã có thì Remove id, chưa có thì Add id; sản phẩm phải còn hoạt động. |
| GET `Cart()` | [129](Project_QLThoiTrang/Controllers/ShopController.cs#L129) | Dựng giỏ từ Session và giá/biến thể hiện tại trên server bằng SessionStore.Cart; ShoppingSession chặn tài khoản nhân viên mua hàng. |
| POST `Add(int id, string size = "M", int quantity = 1, int variantId = 0)` | [137](Project_QLThoiTrang/Controllers/ShopController.cs#L137) | Thêm sản phẩm vào giỏ: sản phẩm và biến thể phải hoạt động, lượng 1–20 và tổng cùng biến thể không quá min(20, tồn). |
| POST `UpdateCart( int id, string? size, int quantity, bool remove = false, int variantId = 0, int newVariantId = 0 )` | [187](Project_QLThoiTrang/Controllers/ShopController.cs#L187) | Sửa màu/size/số lượng hoặc xóa dòng giỏ. |
| POST `Review(int id, int stars, string text, List<IFormFile>? media)` | [266](Project_QLThoiTrang/Controllers/ShopController.cs#L266) | Nhận đánh giá và tối đa 5 tệp từ khách; giới hạn request, đọc byte vào bộ nhớ rồi chuyển kiểm tra đã mua/số sao/định dạng tệp sang StoreService.Review. |
| GET `ReviewMedia(Guid id)` | [309](Project_QLThoiTrang/Controllers/ShopController.cs#L309) | Trả ảnh/video theo Guid; chỉ nội dung của đánh giá đang hiển thị hoặc người xem Admin/Support được đọc. |
| GET `Checkout()` | [335](Project_QLThoiTrang/Controllers/ShopController.cs#L335) | Khách mở thanh toán khi giỏ có hàng; điền hồ sơ, tạo checkout-token và chụp checkout-quote trong Session để phát hiện giỏ/giá đổi khi gửi form. |
| POST `Checkout(CheckoutInput input)` | [366](Project_QLThoiTrang/Controllers/ShopController.cs#L366) | Kiểm tra giỏ không rỗng, ModelState và checkout-token khớp Session; PlaceOrder đối chiếu báo giá/trừ tồn/lưu đơn. |
| GET `Success(string id)` | [414](Project_QLThoiTrang/Controllers/ShopController.cs#L414) | Chỉ hiển thị xác nhận đơn thuộc đúng tài khoản đang đăng nhập; mã đơn không khớp chủ trả 404. |

**[ShoppingSession.cs](Project_QLThoiTrang/Controllers/ShoppingSession.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| Nội bộ `OnActionExecuting(ActionExecutingContext context)` | [18](Project_QLThoiTrang/Controllers/ShoppingSession.cs#L18) | Trước action mua hàng: chặn nhân viên vào Cart/Add/UpdateCart/Checkout/Success; nếu là Customer thì nạp giỏ và yêu thích từ SQL vào Session. |
| Nội bộ `OnActionExecuted(ActionExecutedContext context)` | [43](Project_QLThoiTrang/Controllers/ShoppingSession.cs#L43) | Sau Add/UpdateCart/Favorite không có ngoại lệ chưa xử lý, đồng bộ giỏ/yêu thích Session xuống SQL cho Customer. |

**[ValuationController.cs](Project_QLThoiTrang/Controllers/ValuationController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index()` | [16](Project_QLThoiTrang/Controllers/ValuationController.cs#L16) | Inventory/Admin xem tồn và giá vốn hiện có theo biến thể; dữ liệu đọc từ InventoryValues và StoreData. |
| GET `Edit(int id)` | [27](Project_QLThoiTrang/Controllers/ValuationController.cs#L27) | Mở form khai báo giá vốn cho biến thể tồn tại; đưa id vào ViewBag để xác định dòng cần cập nhật. |
| POST `Edit(int variantId, int expected, decimal cost, string note)` | [44](Project_QLThoiTrang/Controllers/ValuationController.cs#L44) | Nhận giá vốn, tồn expected và lý do; SetInventoryValue kiểm tra chống ghi đè, lưu giá vốn cho giao dịch tiếp theo, lỗi quay lại form cùng biến thể. |

### Service: điều kiện và lưu nghiệp vụ

**[CustomerFeatures.cs](Project_QLThoiTrang/Services/CustomerFeatures.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ChatAccess(Data.StoreDbContext db, int actor, int customer)` | [22](Project_QLThoiTrang/Services/CustomerFeatures.cs#L22) | Cho phép khách đọc/gửi trong cuộc hội thoại của chính mình; tài khoản khác phải là nhân viên Support/Admin. |
| `Messages(int customer, int actor, long before = 0, long after = 0)` | [39](Project_QLThoiTrang/Services/CustomerFeatures.cs#L39) | Đọc tối đa 100 tin trong đúng hội thoại sau khi kiểm tra quyền. |
| `SendMessage(int customer, int actor, string text, Guid clientToken)` | [60](Project_QLThoiTrang/Services/CustomerFeatures.cs#L60) | Thêm tin nhắn 1–2.000 ký tự: kiểm tra quyền, clientToken khác rỗng và giới hạn dưới 20 tin/phút. |
| `MarkChatRead(int customer, int actor, long last)` | [114](Project_QLThoiTrang/Services/CustomerFeatures.cs#L114) | Cập nhật mốc đã đọc cho khách hoặc nhân viên; ép last vào khoảng 0..mã tin lớn nhất và chỉ tăng mốc đọc để việc tải lại không làm tin đã đọc thành chưa đọc. |
| `ChatInbox(int actor)` | [137](Project_QLThoiTrang/Services/CustomerFeatures.cs#L137) | Support/Admin lấy tối đa 200 hội thoại mới cập nhật nhất, kèm tên khách và số tin khách gửi sau mốc nhân viên đã đọc. |
| `IssueReset(string email, string secret)` | [166](Project_QLThoiTrang/Services/CustomerFeatures.cs#L166) | Tạo OTP 6 số cho khách đang hoạt động: tối đa 1 lần/phút và 5 lần/giờ, hết hạn sau 10 phút. |
| `ResetDelivered(Guid id)` | [203](Project_QLThoiTrang/Services/CustomerFeatures.cs#L203) | Đánh dấu OTP đã gửi email thành công; ResetPassword chỉ chấp nhận mã có Sent = true. |
| `ResetPassword(ResetInput input, string secret)` | [218](Project_QLThoiTrang/Services/CustomerFeatures.cs#L218) | Đổi mật khẩu bằng OTP: kiểm tra mẫu và khóa HMAC, khách hoạt động, mã đã gửi/chưa dùng/chưa hết 10 phút/chưa thử đủ 5 lần và stamp chưa đổi. |
| `PasswordStamp(string hash)` | [263](Project_QLThoiTrang/Services/CustomerFeatures.cs#L263) | Tạo SHA-256 của hash mật khẩu hiện tại; dùng để vô hiệu OTP nếu mật khẩu đã đổi từ lúc phát mã. |
| `OtpHash(Guid id, string code, string secret)` | [270](Project_QLThoiTrang/Services/CustomerFeatures.cs#L270) | Tạo HMAC-SHA256 từ mã challenge và OTP bằng khóa cấu hình; database chỉ giữ kết quả này, không lưu OTP rõ. |
| `SendCode(string email, string code, CancellationToken cancellationToken)` | [284](Project_QLThoiTrang/Services/CustomerFeatures.cs#L284) | Gửi OTP qua SMTP TLS tới email được truyền vào; triển khai thật dùng SmtpMailSender, kiểm thử có thể thay bằng IMailSender giả lập. |
| `SendCode(string email, string code, CancellationToken cancellationToken)` | [292](Project_QLThoiTrang/Services/CustomerFeatures.cs#L292) | Gửi OTP qua SMTP TLS tới email được truyền vào; triển khai thật dùng SmtpMailSender, kiểm thử có thể thay bằng IMailSender giả lập. |
| `Request(string email, CancellationToken cancellationToken)` | [329](Project_QLThoiTrang/Services/CustomerFeatures.cs#L329) | Chỉ phát/gửi OTP khi SMTP và khóa HMAC đã sẵn sàng. |
| `Complete(ResetInput input)` | [361](Project_QLThoiTrang/Services/CustomerFeatures.cs#L361) | Chỉ cho hoàn tất đặt lại mật khẩu khi cấu hình khôi phục đã sẵn sàng; sau đó chuyển kiểm tra OTP sang StoreService.ResetPassword. |

**[DatabaseSchemaGuard.cs](Project_QLThoiTrang/Services/DatabaseSchemaGuard.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `IsReady()` | [24](Project_QLThoiTrang/Services/DatabaseSchemaGuard.cs#L24) | Kiểm tra kết nối và các cột/bảng cần thiết của SQL; khóa gate ngăn kiểm tra đồng thời, cache kết quả trong 10 giây. |
| `InvokeAsync( HttpContext context, DatabaseSchemaStatus status, IWebHostEnvironment environment )` | [86](Project_QLThoiTrang/Services/DatabaseSchemaGuard.cs#L86) | Middleware cho tài nguyên tĩnh đi tiếp; yêu cầu động chỉ chạy tiếp khi database sẵn sàng. |

**[Finance.cs](Project_QLThoiTrang/Services/Finance.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `UpdateCosting( StoreDbContext db, StoreData data, Dictionary<int, int> before, Dictionary<string, string> oldStatuses, HashSet<int> approved, int movementCount )` | [17](Project_QLThoiTrang/Services/Finance.cs#L17) | Chụp giá vốn cho dòng đơn mới, rồi tính giá vốn bình quân di động khi nhập kho/nhận hàng hủy/nhận hàng trả. |
| `SetInventoryValue( int variantId, int expected, decimal cost, string note, int actor )` | [89](Project_QLThoiTrang/Services/Finance.cs#L89) | Inventory/Admin khai báo giá vốn cho biến thể; cost 0–100 triệu, lý do 5–300 ký tự và tổng tồn phải bằng expected. |
| `Profit(DateTime from, DateTime to)` | [136](Project_QLThoiTrang/Services/Finance.cs#L136) | Lập dòng doanh thu/giá vốn theo ngày tạo đơn, chỉ xét đơn đã giao/hoàn thành/đã hoàn hàng. |
| `ReconcileSupplier(int receiptId, decimal amount, string note, int actor)` | [202](Project_QLThoiTrang/Services/Finance.cs#L202) | Sales/Admin đối soát số phải trả của phiếu nhập trước khi chi tiền; yêu cầu phiếu tồn tại, số tiền hợp lệ, có căn cứ đối soát và chưa có SupplierPayment. |
| `PaySupplier(int receiptId, decimal amount, string reference, int actor)` | [237](Project_QLThoiTrang/Services/Finance.cs#L237) | Sales/Admin thêm chứng từ chi cho phiếu đã đối soát; mã chứng từ chưa dùng, amount > 0 và không vượt công nợ còn lại sau các lần chi trước. |

**[JsonMigration.cs](Project_QLThoiTrang/Services/JsonMigration.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ImportJson(string path)` | [17](Project_QLThoiTrang/Services/JsonMigration.cs#L17) | Đọc JSON cũ, tính SHA-256 và chuyển dữ liệu qua MergeLegacy để nhập có kiểm soát. |
| `SeedDemo()` | [29](Project_QLThoiTrang/Services/JsonMigration.cs#L29) | Nạp bộ dữ liệu mẫu qua MergeLegacy; chỉ thực hiện khi người vận hành chủ động dùng --seed-demo. |
| `MergeLegacy(StoreData source, string hash)` | [36](Project_QLThoiTrang/Services/JsonMigration.cs#L36) | Ghép dữ liệu cũ/mẫu vào StoreData, kiểm tra dấu vết nhập và ánh xạ mã liên quan trước khi Write lưu SQL; xem các Require bên dưới để biết điều kiện được phép nhập. |

**[NativeManagement.cs](Project_QLThoiTrang/Services/NativeManagement.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `CurrentAccount(int id)` | [22](Project_QLThoiTrang/Services/NativeManagement.cs#L22) | Đọc tài khoản phục vụ xác thực cookie: id dương tra KhachHang, id âm tra NhanVien với mã -id; trả null nếu không tồn tại. |
| `NativeRead(Func<StoreDbContext, T> read)` | [58](Project_QLThoiTrang/Services/NativeManagement.cs#L58) | Đọc trực tiếp các DbSet SQL, không qua StoreData; dùng NoTracking và transaction Serializable. |
| `NativeWrite(Func<StoreDbContext, T> write)` | [75](Project_QLThoiTrang/Services/NativeManagement.cs#L75) | Ghi trực tiếp các entity SQL: mở transaction Serializable và khóa QOANH.Store.Write, chạy callback, SaveChanges rồi Commit. |
| `NativePermission(StoreDbContext db, int actor, params string[] roles)` | [103](Project_QLThoiTrang/Services/NativeManagement.cs#L103) | Kiểm tra actor là nhân viên (id âm), còn hoạt động và có quyền Admin hoặc một vai trò được truyền vào; không đạt thì chặn trước khi sửa SQL. |
| `References(string kind)` | [121](Project_QLThoiTrang/Services/NativeManagement.cs#L121) | Đọc danh mục theo kind: brands, sizes, colors, warehouses hoặc suppliers; đổi các bảng tương ứng thành ReferenceInput. |
| `SaveReference(ReferenceInput input, int actor)` | [185](Project_QLThoiTrang/Services/NativeManagement.cs#L185) | Thêm/sửa thương hiệu, size, màu, kho hoặc nhà cung cấp theo kind. |
| `SaveVariant(VariantInput input, int actor)` | [306](Project_QLThoiTrang/Services/NativeManagement.cs#L306) | Thêm/sửa biến thể sản phẩm: kiểm tra quyền Catalog, sản phẩm/size/màu tồn tại và SKU/tổ hợp không trùng. |
| `Staff()` | [370](Project_QLThoiTrang/Services/NativeManagement.cs#L370) | Lấy danh sách tài khoản nhân viên từ StoreData theo quy ước Id âm; dữ liệu này được Load từ bảng NhanVien. |
| `SaveStaff(StaffInput input, int actor)` | [379](Project_QLThoiTrang/Services/NativeManagement.cs#L379) | Admin thêm/sửa nhân viên và quyền: email không trùng khách hoặc nhân viên khác; không tự khóa/hạ quyền Admin đang dùng, phải còn ít nhất một Admin hoạt động. |
| `UpdateCustomer(int id, ProfileInput input, bool active, int actor)` | [455](Project_QLThoiTrang/Services/NativeManagement.cs#L455) | Nhân viên Support/Admin sửa tên, điện thoại, địa chỉ và trạng thái của tài khoản có Role Customer; không sửa email, vai trò hay mật khẩu tại đây. |
| `Reviews(int? product = null, bool includeHidden = false)` | [479](Project_QLThoiTrang/Services/NativeManagement.cs#L479) | Đọc đánh giá kèm tên khách và danh sách mã ảnh/video. |
| `Review( int customerId, int productId, int stars, string text, IReadOnlyList<ReviewUpload>? uploads = null )` | [524](Project_QLThoiTrang/Services/NativeManagement.cs#L524) | Thêm đánh giá sau mua: khách đang hoạt động, 1–5 sao, nội dung 5–1.000 ký tự, đã mua sản phẩm trong đơn đã giao/hoàn thành và chưa từng đánh giá sản phẩm đó. |
| `ModerateReview(int id, bool active, int actor)` | [592](Project_QLThoiTrang/Services/NativeManagement.cs#L592) | Support/Admin ẩn hoặc hiện đánh giá bằng TrangThai = active; không xóa bản ghi hay tệp đính kèm. |
| `LogLogin(Customer c, bool success, string? ip, string? device)` | [608](Project_QLThoiTrang/Services/NativeManagement.cs#L608) | Thêm nhật ký đăng nhập gồm khách/nhân viên, thời gian, IP và thiết bị đã cắt theo giới hạn cột. |
| `EndLogin(long id, int actor)` | [633](Project_QLThoiTrang/Services/NativeManagement.cs#L633) | Ghi thời điểm đăng xuất cho bản ghi lịch sử thuộc đúng actor; không cập nhật nhật ký của tài khoản khác. |
| `ChangePassword(int actor, PasswordInput input)` | [650](Project_QLThoiTrang/Services/NativeManagement.cs#L650) | Kiểm tra mẫu đổi mật khẩu, xác minh mật khẩu hiện tại của tài khoản đang hoạt động, rồi băm mật khẩu mới trong Write. |
| `HashLegacyPasswords()` | [675](Project_QLThoiTrang/Services/NativeManagement.cs#L675) | Lệnh quản trị chuyển mật khẩu cũ trong KhachHang/NhanVien sang Identity hash; bỏ qua bản ghi đã đúng định dạng. |

**[NativeStoreAdapter.cs](Project_QLThoiTrang/Services/NativeStoreAdapter.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `StaffRole(string title)` | [19](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L19) | Đổi chức vụ SQL sang vai trò ứng dụng theo switch bên dưới; các nhánh tương thích xử lý cả tên chức vụ tiếng Việt của dữ liệu cũ. |
| `PublicId(DonHangRow o)` | [38](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L38) | Lấy QPublicId của đơn; nếu dữ liệu cũ chưa có mã công khai thì tạo mã hiển thị QH-SQL-{MaDonHang} để vẫn truy cập được đơn. |
| `Load(StoreDbContext db)` | [45](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L45) | Bộ chuyển đổi SQL → StoreData: đọc các bảng gốc, ghép sản phẩm/biến thể/tồn/đơn/thanh toán/trả hàng và cấu hình cho controller/view hiện có. |
| `SafeImage(string? image, int id)` | [309](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L309) | Chỉ dùng 5 ảnh sản phẩm có sẵn trong thư viện; đường dẫn ngoài danh sách được thay bằng ảnh mặc định tính theo id. |
| `Row(StoreDbContext db, Func<T, bool> match, Func<T> create)` | [319](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L319) | Tìm entity đã nạp trong DbSet.Local theo match; chưa có thì gọi create và đánh dấu thêm mới. |
| `Flush(StoreDbContext db)` | [334](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L334) | Lưu một nhóm entity theo thứ tự bảng; khi cần thêm mã tường minh vào cột IDENTITY thì bật/tắt IDENTITY_INSERT quanh SaveChanges. |
| `PrepareStock(StoreData d)` | [373](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L373) | Đồng bộ tồn tổng, biến thể và tồn từng kho; tạo dữ liệu biến thể/tồn còn thiếu và phân bổ chênh lệch theo các nhánh bên dưới. |
| `PersistNative(StoreDbContext db, StoreData d)` | [449](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L449) | Bộ chuyển đổi StoreData → các bảng SQL gốc: cập nhật/thêm danh mục, tài khoản, sản phẩm, biến thể, tồn, phiếu nhập, ưu đãi, đơn, thanh toán, trả hàng và nhật ký. |

**[PasswordStorage.cs](Project_QLThoiTrang/Services/PasswordStorage.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `IsIdentityHash(string? value)` | [17](Project_QLThoiTrang/Services/PasswordStorage.cs#L17) | Kiểm tra chuỗi Base64 có cấu trúc hash ASP.NET Identity hợp lệ theo phiên bản và độ dài; không xác minh mật khẩu người dùng ở bước này. |

**[PdfDocuments.cs](Project_QLThoiTrang/Services/PdfDocuments.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ResolveTypeface(string familyName, bool bold, bool italic)` | [17](Project_QLThoiTrang/Services/PdfDocuments.cs#L17) | Chọn Arial thường hoặc đậm cho PDF theo yêu cầu bold; mã font được GetFont dùng để đọc tệp thực tế. |
| `GetFont(string faceName)` | [23](Project_QLThoiTrang/Services/PdfDocuments.cs#L23) | Đọc arial.ttf hoặc arialbd.ttf từ thư mục font đã cấu hình để PDF hỗ trợ tiếng Việt. |
| `Create( string title, string code, IEnumerable<string> info, string[] headers, List<string[]> rows, IEnumerable<string> totals )` | [35](Project_QLThoiTrang/Services/PdfDocuments.cs#L35) | Tạo PDF từ tiêu đề, thông tin, bảng và tổng tiền; các hàm nội bộ đo chiều rộng, xuống dòng và sang trang khi thiếu chỗ. |

**[PersistentShopping.cs](Project_QLThoiTrang/Services/PersistentShopping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `UserCart(int customerId)` | [16](Project_QLThoiTrang/Services/PersistentShopping.cs#L16) | Đọc giỏ lâu dài của khách từ GioHang/ChiTietGioHang, join biến thể, size và màu thành CartItem. |
| `UserWishlist(int customerId)` | [38](Project_QLThoiTrang/Services/PersistentShopping.cs#L38) | Đọc danh sách MaSP yêu thích thuộc khách từ bảng YeuThich. |
| `SaveShopping(int customerId, List<CartItem> items, List<int> wishlist)` | [47](Project_QLThoiTrang/Services/PersistentShopping.cs#L47) | Đồng bộ giỏ và yêu thích của khách đang hoạt động trong một NativeWrite: tạo đầu giỏ nếu thiếu, xóa chi tiết cũ rồi thêm danh sách đã gom theo VariantId; thay danh sách yêu thích bằng các mã không trùng. |
| `MergeGuestShopping(int customerId, List<CartItem> guest, List<int> wishes)` | [132](Project_QLThoiTrang/Services/PersistentShopping.cs#L132) | Sau đăng nhập, ghép giỏ khách vãng lai vào giỏ SQL: bỏ sản phẩm/biến thể không còn bán, cộng lượng cùng biến thể tối đa 20, hợp nhất yêu thích rồi SaveShopping. |

**[ReviewUploads.cs](Project_QLThoiTrang/Services/ReviewUploads.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Validate(IReadOnlyList<ReviewUpload>? uploads)` | [20](Project_QLThoiTrang/Services/ReviewUploads.cs#L20) | Kiểm tra tối đa 5 tệp/tổng 30 MB; nhận ảnh JPG/PNG/GIF/WebP tối đa 5 MB và video MP4/WebM tối đa 20 MB, nhiều nhất 1 video. |

**[SessionStore.cs](Project_QLThoiTrang/Services/SessionStore.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Get(ISession session, string key)` | [16](Project_QLThoiTrang/Services/SessionStore.cs#L16) | Đọc JSON từ Session theo key và giải tuần tự thành T; chưa có giá trị thì trả new T để giỏ/yêu thích có thể khởi tạo rỗng. |
| `Set(ISession session, string key, T value)` | [24](Project_QLThoiTrang/Services/SessionStore.cs#L24) | Tuần tự dữ liệu thành JSON rồi lưu Session theo key; khách đăng nhập còn được đồng bộ giỏ/yêu thích xuống SQL bởi ShoppingSession. |
| `Cart(ISession session, StoreService store)` | [31](Project_QLThoiTrang/Services/SessionStore.cs#L31) | Ghép các dòng Session với sản phẩm/biến thể hiện có, lấy giá server và loại dòng không tìm thấy sản phẩm/biến thể để dựng CartViewModel hiển thị. |

**[Shipping.cs](Project_QLThoiTrang/Services/Shipping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ClaimShipment( string id, string requestHash, decimal expectedOutstanding, int actor )` | [22](Project_QLThoiTrang/Services/Shipping.cs#L22) | Khóa/lưu yêu cầu tạo vận đơn trước khi gọi mạng: kiểm tra quyền, trạng thái đơn, số còn thu và thanh toán chuyển khoản. |
| `ShipmentResult(int orderId, string? code, string status, string error, int actor)` | [96](Project_QLThoiTrang/Services/Shipping.cs#L96) | Lưu mã, trạng thái, thông báo lỗi và thời điểm cập nhật vận đơn trong SQL; giữ mã cũ nếu lần này không có mã mới. |
| `Options()` | [116](Project_QLThoiTrang/Services/Shipping.cs#L116) | Đọc cấu hình Ghn, bắt buộc đủ token/shop và chỉ chấp nhận URL gateway GHN trong danh sách cố định. |
| `Send( HttpMethod method, string route, object? body, CancellationToken token )` | [136](Project_QLThoiTrang/Services/Shipping.cs#L136) | Gửi HTTP tới GHN với Token/ShopId; 4xx thành GhnRejectedException, lỗi/kết quả không rõ thành InvalidOperationException. |
| `Create(ShipmentInput input, int actor, CancellationToken token)` | [173](Project_QLThoiTrang/Services/Shipping.cs#L173) | Kiểm tra quyền Sales/Admin, địa chỉ/kích thước và số COD nguyên không quá 50 triệu. |
| `Sync(string id, int actor, CancellationToken token)` | [276](Project_QLThoiTrang/Services/Shipping.cs#L276) | Lấy vận đơn GHN theo ClientCode đã lưu, kiểm tra GHN trả đúng mã đơn rồi cập nhật mã vận đơn/trạng thái; không tự đổi trạng thái giao hàng của đơn trong StoreData. |

**[StoreOperations.cs](Project_QLThoiTrang/Services/StoreOperations.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `FindOrder(StoreData d, string id)` | [16](Project_QLThoiTrang/Services/StoreOperations.cs#L16) | Tìm đơn theo mã hiển thị trong StoreData; nếu không có thì ném lỗi nghiệp vụ thay vì tiếp tục dùng giá trị null. |
| `Permission(StoreData d, int actor, params string[] roles)` | [24](Project_QLThoiTrang/Services/StoreOperations.cs#L24) | Kiểm tra tài khoản actor còn hoạt động và có quyền Admin hoặc một vai trò cho phép. |
| `Text(string? text, int min, int max, string field)` | [36](Project_QLThoiTrang/Services/StoreOperations.cs#L36) | Trim chuỗi đầu vào rồi bắt buộc độ dài nằm trong min..max; field được ghép vào thông báo lỗi để biết ô nhập nào chưa hợp lệ. |
| `GetVariant(StoreData d, CartItem c)` | [47](Project_QLThoiTrang/Services/StoreOperations.cs#L47) | Đối chiếu dòng giỏ với sản phẩm đang bán và biến thể đang hoạt động; ưu tiên VariantId, dữ liệu cũ có thể tra theo size. |
| `StockRow(StoreData d, int variant, int warehouse)` | [69](Project_QLThoiTrang/Services/StoreOperations.cs#L69) | Tìm tồn của một biến thể tại một kho; nếu chưa có thì tạo dòng WarehouseStock với số lượng mặc định. |
| `Restore(StoreData d, OrderLine l, int quantity)` | [83](Project_QLThoiTrang/Services/StoreOperations.cs#L83) | Hoàn số lượng về biến thể/kho của dòng đơn và tăng tồn tổng sản phẩm; dùng khi hủy đơn hoặc duyệt nhận hàng trả. |
| `PlaceOrder( int customerId, CheckoutInput input, List<CartItem> cart, List<OrderLine>? quote = null )` | [99](Project_QLThoiTrang/Services/StoreOperations.cs#L99) | Đặt hàng trong một Write: kiểm tra checkout token và trả lại đơn cũ nếu gửi lại cùng token → xác thực khách/giỏ/phương thức → đối chiếu báo giá → trừ tồn kho đang hoạt động → tính phí/ưu đãi → thêm đơn. |
| `ChangeOrderStatus(string id, string status, int actor, bool customer = false)` | [308](Project_QLThoiTrang/Services/StoreOperations.cs#L308) | Chuyển trạng thái đơn theo danh sách allowed bên dưới. |
| `Receive(ReceiveInput input, int actor)` | [389](Project_QLThoiTrang/Services/StoreOperations.cs#L389) | Lối nhập kho một dòng: đóng gói ReceiveInput thành danh sách rồi dùng ReceiveBatch để áp dụng cùng quy tắc và transaction. |
| `ReceiveBatch(List<ReceiveInput> inputs, int actor)` | [398](Project_QLThoiTrang/Services/StoreOperations.cs#L398) | Thêm phiếu nhập 1–100 dòng trong một Write: cần nhân viên Inventory/Admin, mã phiếu chưa dùng, cùng kho/NCC, biến thể không trùng. |
| `AdjustStock(int id, int quantity, int expected, string reason, int actor)` | [504](Project_QLThoiTrang/Services/StoreOperations.cs#L504) | Sửa tồn tổng bằng kiểm kê: quyền Inventory/Admin, lý do 5–300 ký tự, quantity 0–100.000 và tồn hiện tại phải bằng expected. |
| `AdjustVariantStock( int variantId, int warehouseId, int quantity, int expected, string reason, int actor )` | [549](Project_QLThoiTrang/Services/StoreOperations.cs#L549) | Kiểm kê một biến thể tại một kho hoạt động. |
| `RecordPayment(string id, decimal amount, string reference, int actor)` | [610](Project_QLThoiTrang/Services/StoreOperations.cs#L610) | Sales/Admin ghi nhận tiền đã thu: đơn ở trạng thái cho phép, không đang gửi GHN, amount phải đúng toàn bộ phần còn phải thu và mã biên nhận chưa dùng. |
| `Returned(StoreData d, string orderId, int lineId)` | [674](Project_QLThoiTrang/Services/StoreOperations.cs#L674) | Tính tổng số lượng đã trả của một dòng đơn, chỉ cộng phiếu có trạng thái Approved; phiếu Pending/Rejected không làm giảm số lượng còn được trả. |
| `RequestReturn( string id, string reason, int customerId, int lineId = 0, int quantity = 0 )` | [688](Project_QLThoiTrang/Services/StoreOperations.cs#L688) | Khách tạo yêu cầu trả cho đơn của mình đã giao/hoàn thành và chưa có phiếu Pending. |
| `ResolveReturn( int returnId, bool approve, string resolution, string reference, int actor )` | [772](Project_QLThoiTrang/Services/StoreOperations.cs#L772) | Sales/Admin duyệt hoặc từ chối phiếu Pending. |

**[StoreSeed.cs](Project_QLThoiTrang/Services/StoreSeed.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Seed()` | [14](Project_QLThoiTrang/Services/StoreSeed.cs#L14) | Tạo đối tượng dữ liệu mẫu trong bộ nhớ cho SeedDemo; việc lưu database thuộc MergeLegacy/Write. |

**[StoreService.cs](Project_QLThoiTrang/Services/StoreService.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Read(Func<StoreData, T> read)` | [31](Project_QLThoiTrang/Services/StoreService.cs#L31) | Đọc một ảnh dữ liệu StoreData từ SQL trong transaction Serializable; NoTracking dùng cho truy vấn chỉ đọc. |
| `Write( Func<StoreData, T> write, int? actorId = null, string reason = "Điều chỉnh dữ liệu" )` | [53](Project_QLThoiTrang/Services/StoreService.cs#L53) | Cổng ghi nghiệp vụ dùng chung: mở transaction → lấy khóa ứng dụng độc quyền → Load dữ liệu → chạy callback thêm/sửa/xóa → chuẩn hóa tồn → Validate → tính giá vốn → PersistNative → SaveChanges → Commit. |
| `Next(IEnumerable<int> ids)` | [123](Project_QLThoiTrang/Services/StoreService.cs#L123) | Sinh mã nghiệp vụ bằng mã lớn nhất hiện có cộng 1; tập rỗng cho kết quả 1. |
| `Require(bool condition, string message)` | [127](Project_QLThoiTrang/Services/StoreService.cs#L127) | Chốt kiểm tra nghiệp vụ: condition phải đúng để chạy tiếp; nếu sai thì ném InvalidOperationException với message để tầng gọi hiển thị hoặc xử lý lỗi. |
| `Validate(StoreData d)` | [139](Project_QLThoiTrang/Services/StoreService.cs#L139) | Kiểm tra toàn bộ StoreData trước khi ghi: tồn 0–100.000, email/mã ưu đãi/danh mục không trùng, số điện thoại không trùng trong từng nhóm khách/nhân viên, vai trò hợp lệ, mật khẩu là Identity hash và số tiền/số lượng đơn hợp lệ. |
| `Authenticate( string email, string password, string? ip = null, string? device = null )` | [235](Project_QLThoiTrang/Services/StoreService.cs#L235) | Tìm tài khoản đang hoạt động theo email, kiểm tra định dạng hash và mật khẩu bằng Identity; sai thì ghi nhận thất bại khi đã tìm thấy tài khoản. |
| `Register(RegisterInput input)` | [281](Project_QLThoiTrang/Services/StoreService.cs#L281) | Thêm khách hàng mới: kiểm tra DataAnnotations và email chưa dùng, chuẩn hóa tên/email, băm mật khẩu rồi thêm vào d.Customers trong Write. |

### Data: mapping SQL

**[FeatureMapping.cs](Project_QLThoiTrang/Data/FeatureMapping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `MapFeatures(ModelBuilder b)` | [26](Project_QLThoiTrang/Data/FeatureMapping.cs#L26) | Ánh xạ chat, OTP, giá vốn, công nợ và GHN sang bảng QOANH_*; cấu hình khóa ngoại Restrict và chỉ mục duy nhất để chống trùng token/chứng từ/mã vận đơn. |

**[ReviewMediaMapping.cs](Project_QLThoiTrang/Data/ReviewMediaMapping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `MapReviewMedia(ModelBuilder b)` | [18](Project_QLThoiTrang/Data/ReviewMediaMapping.cs#L18) | Ánh xạ ReviewMedia sang QOANH_ReviewMedia, khóa Guid tự cấp, ContentType ASCII tối đa 30 ký tự và khóa ngoại đến DanhGia với Restrict. |

**[StoreDbContext.cs](Project_QLThoiTrang/Data/StoreDbContext.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `OnModelCreating(ModelBuilder b)` | [22](Project_QLThoiTrang/Data/StoreDbContext.cs#L22) | Đăng ký mapping bảng gốc và tính năng; đặt khóa/chỉ mục/ràng buộc nhật ký tồn và tiền, hạn chế xóa quan hệ và chuẩn hóa decimal(18,2). |

**[Mapping.cs](Project_QLThoiTrang/Data/Native/Mapping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `MapNative(ModelBuilder b)` | [46](Project_QLThoiTrang/Data/Native/Mapping.cs#L46) | Ánh xạ các lớp *Row sang bảng/cột SQL gốc và các khóa/chỉ mục/quan hệ; đây là nơi tra tên bảng thật khi cần hiểu dữ liệu được lưu ở đâu. |

### Models: helper lọc

**[AdminListFilter.cs](Project_QLThoiTrang/Models/AdminListFilter.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Choice( string key, string label, params (string Value, string Text)[] choices )` | [20](Project_QLThoiTrang/Models/AdminListFilter.cs#L20) | Tạo cấu hình một ô lọc và các cặp giá trị/nhãn hiển thị dùng chung cho thanh công cụ danh sách quản trị. |
| `Matches(string text, string? query)` | [29](Project_QLThoiTrang/Models/AdminListFilter.cs#L29) | So khớp từ khóa với chuỗi tìm kiếm theo biểu thức bên dưới; từ khóa rỗng cho phép giữ lại bản ghi. |

### Models và entity: mở phần định nghĩa dữ liệu

| File | Các kiểu dữ liệu được giải thích trong code |
| --- | --- |
| [AdminListFilter.cs](Project_QLThoiTrang/Models/AdminListFilter.cs) | `AdminFilterOption`, `AdminListFilter` |
| [ErrorViewModel.cs](Project_QLThoiTrang/Models/ErrorViewModel.cs) | `ErrorViewModel` |
| [FeatureModels.cs](Project_QLThoiTrang/Models/FeatureModels.cs) | `ChatThread`, `ChatMessage`, `ResetCode`, `InventoryValue`, `SupplierAccount`, `SupplierPayment`, `Shipment`, `ForgotInput`, `ResetInput`, `ShipmentInput`, `BankTransferOptions`, `SmtpOptions`, `GhnOptions`, `ProfitLine` |
| [ManagementModels.cs](Project_QLThoiTrang/Models/ManagementModels.cs) | `ReferenceInput`, `ReferencePage`, `StaffInput`, `VariantInput`, `ReviewItem`, `PasswordInput` |
| [Operations.cs](Project_QLThoiTrang/Models/Operations.cs) | `Category`, `StoreConfiguration`, `StockMovement`, `PaymentRecord`, `ReturnRecord`, `ReturnLine`, `Variant`, `WarehouseStock`, `Warehouse`, `OrderHistory`, `ReceiveInput`, `Supplier` |
| [Product.cs](Project_QLThoiTrang/Models/Product.cs) | `Product` |
| [ReviewMedia.cs](Project_QLThoiTrang/Models/ReviewMedia.cs) | `ReviewMedia`, `ReviewMediaInfo`, `ReviewUpload` |
| [StoreModels.cs](Project_QLThoiTrang/Models/StoreModels.cs) | `Customer`, `Order`, `OrderLine`, `CartItem`, `Promotion`, `StoreSettings`, `StoreData`, `CatalogViewModel`, `CartViewModel`, `CheckoutInput`, `LoginInput`, `RegisterInput`, `ProfileInput` |
| [Entities.cs](Project_QLThoiTrang/Data/Native/Entities.cs) | `KhachHangRow`, `NhanVienRow`, `LichSuDangNhapRow`, `DanhMucRow`, `ThuongHieuRow`, `SanPhamRow`, `KichThuocRow`, `MauSacRow`, `BienTheSanPhamRow`, `HinhAnhSanPhamRow`, `KhoRow`, `TonKhoRow`, `NhaCungCapRow`, `PhieuNhapRow`, `ChiTietPhieuNhapRow`, `GioHangRow`, `ChiTietGioHangRow`, `YeuThichRow`, `KhuyenMaiRow`, `ChiTietKhuyenMaiRow`, `DonHangRow`, `ChiTietDonHangRow`, `ThanhToanRow`, `LichSuTrangThaiDonHangRow`, `PhieuTraHangRow`, `ChiTietTraHangRow`, `DanhGiaRow` |
