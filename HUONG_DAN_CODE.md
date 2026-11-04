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

**Cập nhật quên/đổi mật khẩu:** chỉ gửi qua email API Resend. OTP ở `Services/PasswordRecovery.cs`, API ở `Services/ResendMailSender.cs`, mẫu nhập ở `Models/PasswordRecoveryModels.cs`. Hướng dẫn cấu hình và điều kiện trong [PASSWORD_RECOVERY.md](delivery/PASSWORD_RECOVERY.md).


- **Đăng ký/đăng nhập:** `AccountController` → `StoreService.Register/Authenticate` → `SignIn` tạo cookie/claims. Id khách dương, nhân viên âm. `Program.cs` đối chiếu trạng thái/quyền/security stamp để loại cookie cũ khi tài khoản bị đổi.
- **Đổi mật khẩu:** `Account.Password` → `ChangePassword`, xác minh mật khẩu hiện tại, băm mật khẩu mới.
- **Quên mật khẩu:** `Recovery.Forgot` → `PasswordRecovery.Request` → cấp OTP → Resend API → đánh dấu đã gửi. `Recovery.Reset` → kiểm tra HMAC gắn challenge/email, hạn 10 phút, tối đa 5 lần thử → băm mật khẩu mới → đăng xuất. Chỉ dùng email khách đã đăng ký, không cần sửa schema OTP.
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
| GET `Login(string? returnUrl)` | [17](Project_QLThoiTrang/Controllers/AccountController.cs#L17) | Hiển thị form đăng nhập. |
| POST `Login(LoginInput input)` | [21](Project_QLThoiTrang/Controllers/AccountController.cs#L21) | Xác thực tài khoản và tạo phiên đăng nhập. |
| GET `Register(string? returnUrl)` | [52](Project_QLThoiTrang/Controllers/AccountController.cs#L52) | Hiển thị form đăng ký tài khoản. |
| POST `Register(RegisterInput input)` | [56](Project_QLThoiTrang/Controllers/AccountController.cs#L56) | Tạo tài khoản khách hàng và đăng nhập. |
| Nội bộ `SignIn(Customer c, bool rememberMe = false, bool mergeShopping = false)` | [79](Project_QLThoiTrang/Controllers/AccountController.cs#L79) | Tạo cookie và thông tin xác thực tài khoản. |
| POST `Logout()` | [126](Project_QLThoiTrang/Controllers/AccountController.cs#L126) | Kết thúc phiên và đăng xuất tài khoản. |
| GET `Index()` | [137](Project_QLThoiTrang/Controllers/AccountController.cs#L137) | Hiển thị thông tin tài khoản. |
| POST `Index(ProfileInput input)` | [153](Project_QLThoiTrang/Controllers/AccountController.cs#L153) | Cập nhật hồ sơ khách hàng. |
| GET `Orders()` | [182](Project_QLThoiTrang/Controllers/AccountController.cs#L182) | Hiển thị lịch sử đơn của khách hàng. |
| GET `Order(string id)` | [193](Project_QLThoiTrang/Controllers/AccountController.cs#L193) | Hiển thị chi tiết đơn thuộc khách hàng. |
| POST `Cancel(string id)` | [206](Project_QLThoiTrang/Controllers/AccountController.cs#L206) | Hủy đơn đang chờ xác nhận của khách hàng. |
| POST `Return(string id, string reason, int lineId = 0, int quantity = 0)` | [223](Project_QLThoiTrang/Controllers/AccountController.cs#L223) | Gửi yêu cầu trả hàng. |
| GET `Password()` | [240](Project_QLThoiTrang/Controllers/AccountController.cs#L240) | Hiển thị form đổi mật khẩu. |
| POST `Password(PasswordInput input)` | [244](Project_QLThoiTrang/Controllers/AccountController.cs#L244) | Đổi mật khẩu và đăng xuất tài khoản. |
| GET `Denied()` | [268](Project_QLThoiTrang/Controllers/AccountController.cs#L268) | Hiển thị thông báo không đủ quyền truy cập. |

**[AdminController.cs](Project_QLThoiTrang/Controllers/AdminController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index()` | [15](Project_QLThoiTrang/Controllers/AdminController.cs#L15) | Hiển thị Dashboard quản trị. |
| GET `Products(string? q, string? category, int? brandId, string? state)` | [18](Project_QLThoiTrang/Controllers/AdminController.cs#L18) | Tìm kiếm và lọc danh sách sản phẩm. |
| GET `Product(int? id)` | [48](Project_QLThoiTrang/Controllers/AdminController.cs#L48) | Hiển thị form thêm hoặc sửa sản phẩm. |
| POST `Product(Product input)` | [60](Project_QLThoiTrang/Controllers/AdminController.cs#L60) | Lưu sản phẩm mới hoặc cập nhật sản phẩm. |
| GET `DeleteProduct(int id)` | [102](Project_QLThoiTrang/Controllers/AdminController.cs#L102) | Hiển thị trang xác nhận xóa sản phẩm. |
| POST `DeleteProductConfirmed(int id)` | [109](Project_QLThoiTrang/Controllers/AdminController.cs#L109) | Xóa hoặc ngừng bán sản phẩm theo điều kiện sử dụng. |
| GET `Categories()` | [124](Project_QLThoiTrang/Controllers/AdminController.cs#L124) | Hiển thị danh sách danh mục. |
| GET `Category()` | [127](Project_QLThoiTrang/Controllers/AdminController.cs#L127) | Hiển thị form thêm danh mục. |
| POST `AddCategory(string name)` | [130](Project_QLThoiTrang/Controllers/AdminController.cs#L130) | Chuyển yêu cầu thêm danh mục tới hàm xử lý chung. |
| POST `Category(string name)` | [134](Project_QLThoiTrang/Controllers/AdminController.cs#L134) | Thêm danh mục sản phẩm. |
| POST `DeleteCategory(string name)` | [164](Project_QLThoiTrang/Controllers/AdminController.cs#L164) | Xóa danh mục chưa được sử dụng. |
| GET `Orders(string? status, string? q, string? payment, string? period)` | [179](Project_QLThoiTrang/Controllers/AdminController.cs#L179) | Tìm kiếm và lọc danh sách đơn hàng. |
| GET `Order(string id)` | [210](Project_QLThoiTrang/Controllers/AdminController.cs#L210) | Hiển thị chi tiết đơn hàng cho quản trị. |
| POST `OrderStatus(string id, string status)` | [217](Project_QLThoiTrang/Controllers/AdminController.cs#L217) | Cập nhật trạng thái đơn hàng. |
| GET `Customers(string? q, string? state, string? hasOrders)` | [234](Project_QLThoiTrang/Controllers/AdminController.cs#L234) | Tìm kiếm và lọc danh sách khách hàng. |
| GET `Customer(int id)` | [255](Project_QLThoiTrang/Controllers/AdminController.cs#L255) | Hiển thị hồ sơ và các đơn của khách hàng. |
| GET `Inventory()` | [266](Project_QLThoiTrang/Controllers/AdminController.cs#L266) | Hiển thị tồn kho và biến động gần đây. |
| POST `Inventory(int id, int quantity, int expected, string reason)` | [275](Project_QLThoiTrang/Controllers/AdminController.cs#L275) | Lưu kết quả kiểm kê tồn kho sản phẩm. |
| GET `Receive(string tab = "single")` | [292](Project_QLThoiTrang/Controllers/AdminController.cs#L292) | Hiển thị form nhập kho. |
| POST `Receive(ReceiveInput input)` | [300](Project_QLThoiTrang/Controllers/AdminController.cs#L300) | Lưu phiếu nhập kho một dòng. |
| POST `Payment(string id, decimal amount, string reference)` | [321](Project_QLThoiTrang/Controllers/AdminController.cs#L321) | Ghi nhận tiền đã thu của đơn hàng. |
| GET `Returns()` | [338](Project_QLThoiTrang/Controllers/AdminController.cs#L338) | Hiển thị danh sách yêu cầu trả hàng. |
| POST `ResolveReturn(int id, bool approve, string resolution, string reference)` | [342](Project_QLThoiTrang/Controllers/AdminController.cs#L342) | Duyệt hoặc từ chối yêu cầu trả hàng. |
| GET `Promotions()` | [359](Project_QLThoiTrang/Controllers/AdminController.cs#L359) | Hiển thị danh sách chương trình khuyến mãi. |
| GET `Promotion(int? id)` | [362](Project_QLThoiTrang/Controllers/AdminController.cs#L362) | Hiển thị form thêm hoặc sửa khuyến mãi. |
| POST `Promotion(Promotion input)` | [373](Project_QLThoiTrang/Controllers/AdminController.cs#L373) | Lưu chương trình và điều kiện khuyến mãi. |
| GET `Reports(DateTime? from, DateTime? to, string? period)` | [424](Project_QLThoiTrang/Controllers/AdminController.cs#L424) | Hiển thị báo cáo bán hàng theo khoảng ngày. |
| GET `Settings()` | [453](Project_QLThoiTrang/Controllers/AdminController.cs#L453) | Hiển thị cấu hình cửa hàng. |
| POST `Settings(StoreSettings input)` | [456](Project_QLThoiTrang/Controllers/AdminController.cs#L456) | Lưu cấu hình cửa hàng. |
| GET `Export()` | [471](Project_QLThoiTrang/Controllers/AdminController.cs#L471) | Xuất toàn bộ đơn hàng ra CSV. |

**[AdminExtensions.cs](Project_QLThoiTrang/Controllers/AdminExtensions.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| Nội bộ `OnActionExecuting(ActionExecutingContext context)` | [10](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L10) | Kiểm tra quyền truy cập chức năng quản trị. |
| GET `References(string kind = "brands", int id = 0)` | [68](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L68) | Hiển thị danh sách hoặc form danh mục dùng chung. |
| GET `Reference(string kind = "brands", int id = 0)` | [108](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L108) | Hiển thị form thêm hoặc sửa danh mục dùng chung. |
| POST `References(ReferenceInput input)` | [124](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L124) | Lưu danh mục dùng chung và trở về danh sách. |
| POST `Reference(ReferenceInput input)` | [128](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L128) | Lưu thương hiệu, size, màu, kho hoặc nhà cung cấp. |
| Nội bộ `ReceiveOptions()` | [156](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L156) | Nạp lựa chọn kho và nhà cung cấp cho phiếu nhập. |
| Nội bộ `VariantOptions()` | [166](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L166) | Nạp lựa chọn sản phẩm, size và màu cho biến thể. |
| GET `Variants(int? id = null)` | [174](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L174) | Hiển thị danh sách biến thể sản phẩm. |
| GET `Variant(int id = 0)` | [184](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L184) | Hiển thị form thêm hoặc sửa biến thể. |
| POST `Variants(VariantInput input)` | [207](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L207) | Lưu biến thể và trở về danh sách. |
| POST `Variant(VariantInput input)` | [211](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L211) | Lưu biến thể sản phẩm. |
| GET `VariantStock()` | [231](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L231) | Hiển thị tồn kho theo biến thể và kho. |
| POST `AdjustVariant( int variantId, int warehouseId, int quantity, int expected, string reason )` | [234](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L234) | Lưu kết quả kiểm kê biến thể. |
| GET `Receipts()` | [257](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L257) | Hiển thị danh sách phiếu nhập kho. |
| GET `Receipt(int id)` | [274](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L274) | Hiển thị chi tiết phiếu nhập kho. |
| POST `ReceiveMany( List<ReceiveInput> lines, string reference, int warehouseId, int supplierId )` | [289](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L289) | Lưu phiếu nhập kho nhiều dòng. |
| GET `Staff(int id = 0)` | [326](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L326) | Hiển thị danh sách nhân viên. |
| GET `StaffMember(int id = 0)` | [335](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L335) | Hiển thị form thêm hoặc sửa nhân viên. |
| POST `Staff(StaffInput input)` | [356](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L356) | Lưu nhân viên và trở về danh sách. |
| POST `StaffMember(StaffInput input)` | [360](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L360) | Lưu tài khoản và quyền nhân viên. |
| GET `StockCount(int variantId, int warehouseId)` | [381](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L381) | Hiển thị form kiểm kê sản phẩm hoặc biến thể. |
| GET `ReturnRequest(int id)` | [396](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L396) | Hiển thị chi tiết yêu cầu trả hàng. |
| GET `EditCustomer(int id)` | [403](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L403) | Hiển thị form sửa hồ sơ khách hàng. |
| POST `EditCustomer(int id, ProfileInput input, bool active)` | [424](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L424) | Lưu hồ sơ và trạng thái khách hàng. |
| GET `Reviews()` | [446](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L446) | Hiển thị danh sách đánh giá sản phẩm. |
| POST `Moderate(int id, bool active)` | [453](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L453) | Ẩn hoặc hiện đánh giá sản phẩm. |
| GET `LoginHistory()` | [469](Project_QLThoiTrang/Controllers/AdminExtensions.cs#L469) | Hiển thị lịch sử đăng nhập. |

**[ChatController.cs](Project_QLThoiTrang/Controllers/ChatController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| Nội bộ `Conversation(int customerId)` | [17](Project_QLThoiTrang/Controllers/ChatController.cs#L17) | Xác định khách hàng của cuộc hội thoại. |
| GET `Index(int customerId = 0)` | [20](Project_QLThoiTrang/Controllers/ChatController.cs#L20) | Hiển thị trang chat hỗ trợ. |
| GET `Messages(int customerId, long before = 0, long after = 0)` | [33](Project_QLThoiTrang/Controllers/ChatController.cs#L33) | Lấy danh sách tin nhắn hội thoại. |
| POST `Send(int customerId, string text, Guid clientToken)` | [57](Project_QLThoiTrang/Controllers/ChatController.cs#L57) | Gửi tin nhắn hỗ trợ. |
| POST `Read(int customerId, long last)` | [72](Project_QLThoiTrang/Controllers/ChatController.cs#L72) | Đánh dấu tin nhắn đã đọc. |

**[DocumentsController.cs](Project_QLThoiTrang/Controllers/DocumentsController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Order(string id)` | [16](Project_QLThoiTrang/Controllers/DocumentsController.cs#L16) | Xuất PDF đơn hàng được phép xem. |
| GET `Receipt(int id)` | [68](Project_QLThoiTrang/Controllers/DocumentsController.cs#L68) | Xuất PDF phiếu nhập kho. |
| GET `Return(int id)` | [111](Project_QLThoiTrang/Controllers/DocumentsController.cs#L111) | Xuất PDF phiếu trả hàng. |

**[FinanceController.cs](Project_QLThoiTrang/Controllers/FinanceController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index(DateTime? from, DateTime? to, string? period)` | [17](Project_QLThoiTrang/Controllers/FinanceController.cs#L17) | Hiển thị báo cáo doanh thu, giá vốn và lợi nhuận. |
| GET `Suppliers()` | [41](Project_QLThoiTrang/Controllers/FinanceController.cs#L41) | Hiển thị công nợ nhà cung cấp. |
| GET `Receipt(int id)` | [52](Project_QLThoiTrang/Controllers/FinanceController.cs#L52) | Hiển thị công nợ và chứng từ của phiếu nhập. |
| POST `Reconcile(int receiptId, decimal amount, string note)` | [73](Project_QLThoiTrang/Controllers/FinanceController.cs#L73) | Lưu kết quả đối soát công nợ phiếu nhập. |
| POST `Pay(int receiptId, decimal amount, string reference)` | [92](Project_QLThoiTrang/Controllers/FinanceController.cs#L92) | Ghi nhận thanh toán cho nhà cung cấp. |

**[HomeController.cs](Project_QLThoiTrang/Controllers/HomeController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index()` | [11](Project_QLThoiTrang/Controllers/HomeController.cs#L11) | Hiển thị trang chủ cửa hàng. |
| GET `Info(string id = "about")` | [14](Project_QLThoiTrang/Controllers/HomeController.cs#L14) | Hiển thị trang thông tin theo mục được chọn. |
| GET `Privacy()` | [84](Project_QLThoiTrang/Controllers/HomeController.cs#L84) | Hiển thị chính sách quyền riêng tư. |
| GET `Error()` | [87](Project_QLThoiTrang/Controllers/HomeController.cs#L87) | Hiển thị trang báo lỗi. |

**[RecoveryController.cs](Project_QLThoiTrang/Controllers/RecoveryController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Forgot()` | [13](Project_QLThoiTrang/Controllers/RecoveryController.cs#L13) | Hiển thị form gửi OTP qua email và đặt lại mật khẩu. |
| POST `Forgot(ForgotInput input, CancellationToken cancellationToken)` | [26](Project_QLThoiTrang/Controllers/RecoveryController.cs#L26) | Yêu cầu gửi OTP tới email đã đăng ký. |
| GET `Reset()` | [62](Project_QLThoiTrang/Controllers/RecoveryController.cs#L62) | Hiển thị form xác minh OTP và mật khẩu mới. |
| POST `Reset(ResetInput input, bool inline = false)` | [72](Project_QLThoiTrang/Controllers/RecoveryController.cs#L72) | Xác minh OTP, đặt mật khẩu mới và đăng xuất. |
| Nội bộ `SetChannelAvailability()` | [97](Project_QLThoiTrang/Controllers/RecoveryController.cs#L97) | Cập nhật trạng thái gửi email và thời gian chờ gửi lại. |
| Nội bộ `ResetForm(ResetInput input, bool inline)` | [109](Project_QLThoiTrang/Controllers/RecoveryController.cs#L109) | Hiển thị lại form tương ứng khi xác minh thất bại. |
| Nội bộ `WantsJson()` | [118](Project_QLThoiTrang/Controllers/RecoveryController.cs#L118) | Kiểm tra yêu cầu có cần phản hồi JSON. |

**[ShippingController.cs](Project_QLThoiTrang/Controllers/ShippingController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index(string id)` | [15](Project_QLThoiTrang/Controllers/ShippingController.cs#L15) | Hiển thị thông tin vận chuyển đơn hàng. |
| POST `Create(ShipmentInput input, CancellationToken token)` | [30](Project_QLThoiTrang/Controllers/ShippingController.cs#L30) | Yêu cầu tạo vận đơn GHN. |
| POST `Sync(string id, CancellationToken token)` | [59](Project_QLThoiTrang/Controllers/ShippingController.cs#L59) | Đồng bộ trạng thái vận đơn GHN. |

**[ShopController.cs](Project_QLThoiTrang/Controllers/ShopController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index( string? q, string? category, string sort = "new", string? collection = null )` | [14](Project_QLThoiTrang/Controllers/ShopController.cs#L14) | Lọc và hiển thị sản phẩm đang bán. |
| GET `Detail(int id)` | [56](Project_QLThoiTrang/Controllers/ShopController.cs#L56) | Hiển thị sản phẩm, đánh giá và sản phẩm liên quan. |
| GET `Wishlist()` | [72](Project_QLThoiTrang/Controllers/ShopController.cs#L72) | Hiển thị danh sách sản phẩm yêu thích. |
| POST `Favorite(int id, string? returnUrl)` | [81](Project_QLThoiTrang/Controllers/ShopController.cs#L81) | Thêm hoặc bỏ sản phẩm yêu thích. |
| GET `Cart()` | [95](Project_QLThoiTrang/Controllers/ShopController.cs#L95) | Hiển thị giỏ hàng và giá hiện tại. |
| POST `Add(int id, string size = "M", int quantity = 1, int variantId = 0)` | [98](Project_QLThoiTrang/Controllers/ShopController.cs#L98) | Thêm sản phẩm hoặc biến thể vào giỏ hàng. |
| POST `UpdateCart( int id, string? size, int quantity, bool remove = false, int variantId = 0, int newVariantId = 0 )` | [141](Project_QLThoiTrang/Controllers/ShopController.cs#L141) | Sửa biến thể, số lượng hoặc xóa dòng giỏ hàng. |
| POST `Review(int id, int stars, string text, List<IFormFile>? media)` | [207](Project_QLThoiTrang/Controllers/ShopController.cs#L207) | Gửi đánh giá sản phẩm kèm ảnh hoặc video. |
| GET `ReviewMedia(Guid id)` | [246](Project_QLThoiTrang/Controllers/ShopController.cs#L246) | Trả tệp ảnh hoặc video của đánh giá được phép xem. |
| GET `Checkout()` | [266](Project_QLThoiTrang/Controllers/ShopController.cs#L266) | Hiển thị form đặt hàng và chụp báo giá hiện tại. |
| POST `Checkout(CheckoutInput input)` | [292](Project_QLThoiTrang/Controllers/ShopController.cs#L292) | Tạo đơn hàng và xóa giỏ sau khi thành công. |
| GET `Success(string id)` | [332](Project_QLThoiTrang/Controllers/ShopController.cs#L332) | Hiển thị xác nhận đặt hàng của khách. |

**[ShoppingSession.cs](Project_QLThoiTrang/Controllers/ShoppingSession.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| Nội bộ `OnActionExecuting(ActionExecutingContext context)` | [11](Project_QLThoiTrang/Controllers/ShoppingSession.cs#L11) | Nạp giỏ khách và kiểm tra quyền mua hàng. |
| Nội bộ `OnActionExecuted(ActionExecutedContext context)` | [33](Project_QLThoiTrang/Controllers/ShoppingSession.cs#L33) | Lưu giỏ và yêu thích sau khi khách cập nhật. |

**[ValuationController.cs](Project_QLThoiTrang/Controllers/ValuationController.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| GET `Index()` | [12](Project_QLThoiTrang/Controllers/ValuationController.cs#L12) | Hiển thị giá trị tồn kho theo biến thể. |
| GET `Edit(int id)` | [19](Project_QLThoiTrang/Controllers/ValuationController.cs#L19) | Hiển thị form khai báo giá vốn tồn kho. |
| POST `Edit(int variantId, int expected, decimal cost, string note)` | [30](Project_QLThoiTrang/Controllers/ValuationController.cs#L30) | Lưu giá vốn tồn kho của biến thể. |

### Service: điều kiện và lưu nghiệp vụ

**[CustomerFeatures.cs](Project_QLThoiTrang/Services/CustomerFeatures.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ChatAccess(Data.StoreDbContext db, int actor, int customer)` | [11](Project_QLThoiTrang/Services/CustomerFeatures.cs#L11) | Kiểm tra quyền truy cập hội thoại. |
| `Messages(int customer, int actor, long before = 0, long after = 0)` | [23](Project_QLThoiTrang/Services/CustomerFeatures.cs#L23) | Lấy các tin nhắn theo mốc thời gian. |
| `SendMessage(int customer, int actor, string text, Guid clientToken)` | [38](Project_QLThoiTrang/Services/CustomerFeatures.cs#L38) | Lưu tin nhắn và ngăn gửi trùng. |
| `MarkChatRead(int customer, int actor, long last)` | [82](Project_QLThoiTrang/Services/CustomerFeatures.cs#L82) | Đánh dấu tin nhắn hội thoại đã đọc. |
| `ChatInbox(int actor)` | [101](Project_QLThoiTrang/Services/CustomerFeatures.cs#L101) | Lấy danh sách hội thoại hỗ trợ. |

**[DatabaseSchemaGuard.cs](Project_QLThoiTrang/Services/DatabaseSchemaGuard.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `IsReady()` | [16](Project_QLThoiTrang/Services/DatabaseSchemaGuard.cs#L16) | Kiểm tra kết nối và schema database cần thiết. |
| `InvokeAsync( HttpContext context, DatabaseSchemaStatus status, IWebHostEnvironment environment )` | [75](Project_QLThoiTrang/Services/DatabaseSchemaGuard.cs#L75) | Chặn yêu cầu động khi database chưa sẵn sàng. |

**[Finance.cs](Project_QLThoiTrang/Services/Finance.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `UpdateCosting( StoreDbContext db, StoreData data, Dictionary<int, int> before, Dictionary<string, string> oldStatuses, HashSet<int> approved, int movementCount )` | [9](Project_QLThoiTrang/Services/Finance.cs#L9) | Tính giá vốn tồn kho và chụp giá vốn dòng đơn. |
| `SetInventoryValue( int variantId, int expected, decimal cost, string note, int actor )` | [76](Project_QLThoiTrang/Services/Finance.cs#L76) | Lưu giá vốn kiểm kê của biến thể. |
| `Profit(DateTime from, DateTime to)` | [116](Project_QLThoiTrang/Services/Finance.cs#L116) | Tính doanh thu, giá vốn và lợi nhuận sau trả hàng. |
| `ReconcileSupplier(int receiptId, decimal amount, string note, int actor)` | [176](Project_QLThoiTrang/Services/Finance.cs#L176) | Đối soát số tiền phải trả của phiếu nhập. |
| `PaySupplier(int receiptId, decimal amount, string reference, int actor)` | [205](Project_QLThoiTrang/Services/Finance.cs#L205) | Ghi chứng từ thanh toán công nợ nhà cung cấp. |

**[JsonMigration.cs](Project_QLThoiTrang/Services/JsonMigration.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ImportJson(string path)` | [10](Project_QLThoiTrang/Services/JsonMigration.cs#L10) | Nhập dữ liệu JSON cũ vào SQL có kiểm soát. |
| `SeedDemo()` | [20](Project_QLThoiTrang/Services/JsonMigration.cs#L20) | Lưu bộ dữ liệu mẫu vào database. |
| `MergeLegacy(StoreData source, string hash)` | [23](Project_QLThoiTrang/Services/JsonMigration.cs#L23) | Ghép dữ liệu cũ và ánh xạ các mã liên quan. |

**[NativeManagement.cs](Project_QLThoiTrang/Services/NativeManagement.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `CurrentAccount(int id)` | [14](Project_QLThoiTrang/Services/NativeManagement.cs#L14) | Lấy tài khoản khách hoặc nhân viên theo mã đăng nhập. |
| `NativeRead(Func<StoreDbContext, T> read)` | [47](Project_QLThoiTrang/Services/NativeManagement.cs#L47) | Đọc trực tiếp entity SQL trong transaction. |
| `NativeWrite(Func<StoreDbContext, T> write)` | [59](Project_QLThoiTrang/Services/NativeManagement.cs#L59) | Lưu trực tiếp entity SQL trong transaction. |
| `NativePermission(StoreDbContext db, int actor, params string[] roles)` | [80](Project_QLThoiTrang/Services/NativeManagement.cs#L80) | Kiểm tra quyền và trạng thái nhân viên. |
| `References(string kind)` | [92](Project_QLThoiTrang/Services/NativeManagement.cs#L92) | Lấy danh mục thương hiệu, size, màu, kho hoặc nhà cung cấp. |
| `SaveReference(ReferenceInput input, int actor)` | [150](Project_QLThoiTrang/Services/NativeManagement.cs#L150) | Thêm hoặc sửa danh mục dùng chung. |
| `SaveVariant(VariantInput input, int actor)` | [254](Project_QLThoiTrang/Services/NativeManagement.cs#L254) | Thêm hoặc sửa biến thể sản phẩm. |
| `Staff()` | [306](Project_QLThoiTrang/Services/NativeManagement.cs#L306) | Lấy danh sách tài khoản nhân viên. |
| `SaveStaff(StaffInput input, int actor)` | [309](Project_QLThoiTrang/Services/NativeManagement.cs#L309) | Lưu tài khoản, mật khẩu và quyền nhân viên. |
| `UpdateCustomer(int id, ProfileInput input, bool active, int actor)` | [369](Project_QLThoiTrang/Services/NativeManagement.cs#L369) | Cập nhật hồ sơ và trạng thái khách hàng. |
| `Reviews(int? product = null, bool includeHidden = false)` | [389](Project_QLThoiTrang/Services/NativeManagement.cs#L389) | Lấy đánh giá sản phẩm kèm thông tin media. |
| `Review( int customerId, int productId, int stars, string text, IReadOnlyList<ReviewUpload>? uploads = null )` | [428](Project_QLThoiTrang/Services/NativeManagement.cs#L428) | Lưu đánh giá của khách đã mua sản phẩm. |
| `ModerateReview(int id, bool active, int actor)` | [486](Project_QLThoiTrang/Services/NativeManagement.cs#L486) | Ẩn hoặc hiện đánh giá sản phẩm. |
| `LogLogin(Customer c, bool success, string? ip, string? device)` | [498](Project_QLThoiTrang/Services/NativeManagement.cs#L498) | Ghi lịch sử đăng nhập thành công hoặc thất bại. |
| `EndLogin(long id, int actor)` | [517](Project_QLThoiTrang/Services/NativeManagement.cs#L517) | Ghi thời điểm đăng xuất của tài khoản. |
| `ChangePassword(int actor, PasswordInput input)` | [529](Project_QLThoiTrang/Services/NativeManagement.cs#L529) | Xác minh mật khẩu cũ và lưu mật khẩu mới đã băm. |
| `HashLegacyPasswords()` | [558](Project_QLThoiTrang/Services/NativeManagement.cs#L558) | Chuyển mật khẩu cũ sang hash ASP.NET Identity. |

**[NativeStoreAdapter.cs](Project_QLThoiTrang/Services/NativeStoreAdapter.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `StaffRole(string title)` | [12](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L12) | Ánh xạ chức vụ nhân viên sang quyền ứng dụng. |
| `PublicId(DonHangRow o)` | [28](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L28) | Lấy mã công khai của đơn hàng. |
| `Load(StoreDbContext db)` | [31](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L31) | Chuyển dữ liệu SQL sang mô hình cửa hàng. |
| `SafeImage(string? image, int id)` | [292](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L292) | Chọn ảnh sản phẩm hợp lệ hoặc ảnh mặc định. |
| `Row(StoreDbContext db, Func<T, bool> match, Func<T> create)` | [299](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L299) | Tìm entity đã nạp hoặc tạo entity mới. |
| `Flush(StoreDbContext db)` | [311](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L311) | Đồng bộ các bản ghi entity với dữ liệu cần lưu. |
| `PrepareStock(StoreData d)` | [341](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L341) | Chuẩn bị dữ liệu tồn kho trước khi lưu. |
| `PersistNative(StoreDbContext db, StoreData d)` | [409](Project_QLThoiTrang/Services/NativeStoreAdapter.cs#L409) | Đồng bộ thay đổi cửa hàng vào các bảng SQL. |

**[PasswordRecovery.cs](Project_QLThoiTrang/Services/PasswordRecovery.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `FindRecoveryCustomer(StoreDbContext db, string email)` | [13](Project_QLThoiTrang/Services/PasswordRecovery.cs#L13) | Tìm khách đang hoạt động theo email. |
| `IssueReset( string contact, string secret, string channel = "email" )` | [17](Project_QLThoiTrang/Services/PasswordRecovery.cs#L17) | Tạo OTP có thời hạn và vô hiệu mã cũ. |
| `ResetDelivered(Guid id)` | [61](Project_QLThoiTrang/Services/PasswordRecovery.cs#L61) | Đánh dấu OTP đã được dịch vụ gửi chấp nhận. |
| `ResetPassword(ResetInput input, string secret)` | [69](Project_QLThoiTrang/Services/PasswordRecovery.cs#L69) | Kiểm tra OTP và lưu mật khẩu mới. |
| `PasswordStamp(string hash)` | [114](Project_QLThoiTrang/Services/PasswordRecovery.cs#L114) | Tạo dấu xác minh từ hash mật khẩu hiện tại. |
| `OtpHash( Guid id, string code, string secret, string channel, string destination )` | [118](Project_QLThoiTrang/Services/PasswordRecovery.cs#L118) | Ký HMAC gắn OTP với yêu cầu và email nhận. |
| `SendCode(string email, string code, CancellationToken cancellationToken)` | [136](Project_QLThoiTrang/Services/PasswordRecovery.cs#L136) | Gửi mã OTP qua email. |
| `Request(string email, CancellationToken cancellationToken)` | [152](Project_QLThoiTrang/Services/PasswordRecovery.cs#L152) | Cấp OTP và yêu cầu gửi qua email API. |
| `Request(ForgotInput input, CancellationToken cancellationToken)` | [156](Project_QLThoiTrang/Services/PasswordRecovery.cs#L156) | Cấp OTP và yêu cầu gửi qua email API. |
| `Complete(ResetInput input)` | [197](Project_QLThoiTrang/Services/PasswordRecovery.cs#L197) | Xác minh OTP và hoàn tất đặt lại mật khẩu. |

**[PasswordStorage.cs](Project_QLThoiTrang/Services/PasswordStorage.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `IsIdentityHash(string? value)` | [8](Project_QLThoiTrang/Services/PasswordStorage.cs#L8) | Kiểm tra cấu trúc hash mật khẩu ASP.NET Identity. |

**[PdfDocuments.cs](Project_QLThoiTrang/Services/PdfDocuments.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ResolveTypeface(string familyName, bool bold, bool italic)` | [10](Project_QLThoiTrang/Services/PdfDocuments.cs#L10) | Chọn font chữ thường hoặc đậm cho PDF. |
| `GetFont(string faceName)` | [14](Project_QLThoiTrang/Services/PdfDocuments.cs#L14) | Đọc dữ liệu font dùng để xuất PDF. |
| `Create( string title, string code, IEnumerable<string> info, string[] headers, List<string[]> rows, IEnumerable<string> totals )` | [23](Project_QLThoiTrang/Services/PdfDocuments.cs#L23) | Tạo nội dung PDF với bảng, tổng tiền và phân trang. |

**[PersistentShopping.cs](Project_QLThoiTrang/Services/PersistentShopping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `UserCart(int customerId)` | [9](Project_QLThoiTrang/Services/PersistentShopping.cs#L9) | Lấy giỏ hàng đã lưu của khách. |
| `UserWishlist(int customerId)` | [30](Project_QLThoiTrang/Services/PersistentShopping.cs#L30) | Lấy danh sách sản phẩm yêu thích của khách. |
| `SaveShopping(int customerId, List<CartItem> items, List<int> wishlist)` | [34](Project_QLThoiTrang/Services/PersistentShopping.cs#L34) | Lưu giỏ hàng và danh sách yêu thích của khách. |
| `MergeGuestShopping(int customerId, List<CartItem> guest, List<int> wishes)` | [98](Project_QLThoiTrang/Services/PersistentShopping.cs#L98) | Gộp giỏ và yêu thích vãng lai vào tài khoản. |

**[ResendMailSender.cs](Project_QLThoiTrang/Services/ResendMailSender.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `SendCode(string email, string code, CancellationToken cancellationToken)` | [11](Project_QLThoiTrang/Services/ResendMailSender.cs#L11) | Gửi OTP qua Resend API và kiểm tra mã thư trả về. |

**[ReviewUploads.cs](Project_QLThoiTrang/Services/ReviewUploads.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Validate(IReadOnlyList<ReviewUpload>? uploads)` | [12](Project_QLThoiTrang/Services/ReviewUploads.cs#L12) | Kiểm tra số lượng, dung lượng và định dạng tệp đánh giá. |

**[SessionStore.cs](Project_QLThoiTrang/Services/SessionStore.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Get(ISession session, string key)` | [9](Project_QLThoiTrang/Services/SessionStore.cs#L9) | Đọc và giải mã dữ liệu JSON từ Session. |
| `Set(ISession session, string key, T value)` | [14](Project_QLThoiTrang/Services/SessionStore.cs#L14) | Mã hóa dữ liệu JSON và lưu vào Session. |
| `Cart(ISession session, StoreService store)` | [18](Project_QLThoiTrang/Services/SessionStore.cs#L18) | Dựng giỏ hàng với thông tin và giá hiện tại. |

**[Shipping.cs](Project_QLThoiTrang/Services/Shipping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `ClaimShipment( string id, string requestHash, decimal expectedOutstanding, int actor )` | [13](Project_QLThoiTrang/Services/Shipping.cs#L13) | Lưu và khóa yêu cầu tạo vận đơn để tránh gửi trùng. |
| `ShipmentResult(int orderId, string? code, string status, string error, int actor)` | [75](Project_QLThoiTrang/Services/Shipping.cs#L75) | Lưu kết quả và trạng thái vận đơn. |
| `Options()` | [93](Project_QLThoiTrang/Services/Shipping.cs#L93) | Đọc và kiểm tra cấu hình GHN. |
| `Send( HttpMethod method, string route, object? body, CancellationToken token )` | [110](Project_QLThoiTrang/Services/Shipping.cs#L110) | Gửi yêu cầu API GHN và kiểm tra phản hồi. |
| `Create(ShipmentInput input, int actor, CancellationToken token)` | [142](Project_QLThoiTrang/Services/Shipping.cs#L142) | Tạo vận đơn GHN và lưu kết quả. |
| `Sync(string id, int actor, CancellationToken token)` | [242](Project_QLThoiTrang/Services/Shipping.cs#L242) | Đồng bộ vận đơn GHN theo mã đơn đã lưu. |

**[StoreOperations.cs](Project_QLThoiTrang/Services/StoreOperations.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `FindOrder(StoreData d, string id)` | [9](Project_QLThoiTrang/Services/StoreOperations.cs#L9) | Tìm đơn hàng theo mã công khai. |
| `Permission(StoreData d, int actor, params string[] roles)` | [14](Project_QLThoiTrang/Services/StoreOperations.cs#L14) | Kiểm tra quyền thực hiện thao tác nghiệp vụ. |
| `Text(string? text, int min, int max, string field)` | [23](Project_QLThoiTrang/Services/StoreOperations.cs#L23) | Chuẩn hóa và kiểm tra độ dài nội dung nhập. |
| `GetVariant(StoreData d, CartItem c)` | [31](Project_QLThoiTrang/Services/StoreOperations.cs#L31) | Tìm và kiểm tra biến thể còn được bán. |
| `StockRow(StoreData d, int variant, int warehouse)` | [49](Project_QLThoiTrang/Services/StoreOperations.cs#L49) | Tìm hoặc tạo dòng tồn biến thể tại kho. |
| `Restore(StoreData d, OrderLine l, int quantity)` | [60](Project_QLThoiTrang/Services/StoreOperations.cs#L60) | Hoàn số lượng hàng về kho xuất ban đầu. |
| `PlaceOrder( int customerId, CheckoutInput input, List<CartItem> cart, List<OrderLine>? quote = null )` | [70](Project_QLThoiTrang/Services/StoreOperations.cs#L70) | Tạo đơn, áp ưu đãi và trừ tồn kho trong transaction. |
| `ChangeOrderStatus(string id, string status, int actor, bool customer = false)` | [249](Project_QLThoiTrang/Services/StoreOperations.cs#L249) | Chuyển trạng thái đơn và hoàn kho khi hủy hợp lệ. |
| `Receive(ReceiveInput input, int actor)` | [311](Project_QLThoiTrang/Services/StoreOperations.cs#L311) | Lập phiếu nhập kho một dòng. |
| `ReceiveBatch(List<ReceiveInput> inputs, int actor)` | [314](Project_QLThoiTrang/Services/StoreOperations.cs#L314) | Lưu phiếu nhập nhiều dòng và cập nhật tồn kho. |
| `AdjustStock(int id, int quantity, int expected, string reason, int actor)` | [406](Project_QLThoiTrang/Services/StoreOperations.cs#L406) | Điều chỉnh tồn sản phẩm theo kết quả kiểm kê. |
| `AdjustVariantStock( int variantId, int warehouseId, int quantity, int expected, string reason, int actor )` | [444](Project_QLThoiTrang/Services/StoreOperations.cs#L444) | Điều chỉnh tồn biến thể tại kho. |
| `RecordPayment(string id, decimal amount, string reference, int actor)` | [496](Project_QLThoiTrang/Services/StoreOperations.cs#L496) | Ghi nhận đủ tiền đã thu và ngăn trùng chứng từ. |
| `Returned(StoreData d, string orderId, int lineId)` | [551](Project_QLThoiTrang/Services/StoreOperations.cs#L551) | Tính số lượng đã được trả của dòng đơn. |
| `RequestReturn( string id, string reason, int customerId, int lineId = 0, int quantity = 0 )` | [559](Project_QLThoiTrang/Services/StoreOperations.cs#L559) | Tạo yêu cầu trả hàng của khách. |
| `ResolveReturn( int returnId, bool approve, string resolution, string reference, int actor )` | [630](Project_QLThoiTrang/Services/StoreOperations.cs#L630) | Xử lý trả hàng, hoàn kho và ghi nhận hoàn tiền. |

**[StoreSeed.cs](Project_QLThoiTrang/Services/StoreSeed.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Seed()` | [8](Project_QLThoiTrang/Services/StoreSeed.cs#L8) | Tạo bộ dữ liệu mẫu trong bộ nhớ. |

**[StoreService.cs](Project_QLThoiTrang/Services/StoreService.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Read(Func<StoreData, T> read)` | [20](Project_QLThoiTrang/Services/StoreService.cs#L20) | Đọc dữ liệu cửa hàng từ SQL. |
| `Write( Func<StoreData, T> write, int? actorId = null, string reason = "Điều chỉnh dữ liệu" )` | [32](Project_QLThoiTrang/Services/StoreService.cs#L32) | Kiểm tra và lưu thay đổi cửa hàng trong transaction. |
| `Next(IEnumerable<int> ids)` | [95](Project_QLThoiTrang/Services/StoreService.cs#L95) | Tạo mã tiếp theo cho dữ liệu nghiệp vụ. |
| `Require(bool condition, string message)` | [98](Project_QLThoiTrang/Services/StoreService.cs#L98) | Báo lỗi khi điều kiện nghiệp vụ không đạt. |
| `Validate(StoreData d)` | [105](Project_QLThoiTrang/Services/StoreService.cs#L105) | Kiểm tra tính hợp lệ của dữ liệu. |
| `Authenticate( string email, string password, string? ip = null, string? device = null )` | [185](Project_QLThoiTrang/Services/StoreService.cs#L185) | Xác thực email và mật khẩu tài khoản. |
| `Register(RegisterInput input)` | [227](Project_QLThoiTrang/Services/StoreService.cs#L227) | Tạo tài khoản khách hàng với mật khẩu đã băm. |

### Data: mapping SQL

**[FeatureMapping.cs](Project_QLThoiTrang/Data/FeatureMapping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `MapFeatures(ModelBuilder b)` | [18](Project_QLThoiTrang/Data/FeatureMapping.cs#L18) | Ánh xạ các bảng chức năng mở rộng. |

**[ReviewMediaMapping.cs](Project_QLThoiTrang/Data/ReviewMediaMapping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `MapReviewMedia(ModelBuilder b)` | [12](Project_QLThoiTrang/Data/ReviewMediaMapping.cs#L12) | Ánh xạ bảng ảnh và video đánh giá. |

**[StoreDbContext.cs](Project_QLThoiTrang/Data/StoreDbContext.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `OnModelCreating(ModelBuilder b)` | [14](Project_QLThoiTrang/Data/StoreDbContext.cs#L14) | Thiết lập ánh xạ entity và ràng buộc SQL. |

**[Mapping.cs](Project_QLThoiTrang/Data/Native/Mapping.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `MapNative(ModelBuilder b)` | [38](Project_QLThoiTrang/Data/Native/Mapping.cs#L38) | Ánh xạ các bảng SQL gốc. |

### Models: helper lọc

**[AdminListFilter.cs](Project_QLThoiTrang/Models/AdminListFilter.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Choice( string key, string label, params (string Value, string Text)[] choices )` | [12](Project_QLThoiTrang/Models/AdminListFilter.cs#L12) | Tạo bộ lọc với danh sách lựa chọn. |
| `Matches(string text, string? query)` | [19](Project_QLThoiTrang/Models/AdminListFilter.cs#L19) | Kiểm tra dữ liệu khớp từ khóa tìm kiếm. |

**[PasswordRecoveryModels.cs](Project_QLThoiTrang/Models/PasswordRecoveryModels.cs)**

| Hàm / phương thức | Vị trí | Chức năng |
| --- | --- | --- |
| `Validate(ValidationContext validationContext)` | [25](Project_QLThoiTrang/Models/PasswordRecoveryModels.cs#L25) | Kiểm tra email và từ chối kênh nhận mã không hỗ trợ. |
| `TryNormalize(string? contact, string channel, out string normalized)` | [76](Project_QLThoiTrang/Models/PasswordRecoveryModels.cs#L76) | Kiểm tra và chuẩn hóa email nhận OTP. |

### Models và entity: mở phần định nghĩa dữ liệu

| File | Các kiểu dữ liệu được giải thích trong code |
| --- | --- |
| [AdminListFilter.cs](Project_QLThoiTrang/Models/AdminListFilter.cs) | `AdminFilterOption`, `AdminListFilter` |
| [ErrorViewModel.cs](Project_QLThoiTrang/Models/ErrorViewModel.cs) | `ErrorViewModel` |
| [FeatureModels.cs](Project_QLThoiTrang/Models/FeatureModels.cs) | `ChatThread`, `ChatMessage`, `ResetCode`, `InventoryValue`, `SupplierAccount`, `SupplierPayment`, `Shipment`, `ShipmentInput`, `BankTransferOptions`, `GhnOptions`, `ProfitLine` |
| [ManagementModels.cs](Project_QLThoiTrang/Models/ManagementModels.cs) | `ReferenceInput`, `ReferencePage`, `StaffInput`, `VariantInput`, `ReviewItem`, `PasswordInput` |
| [Operations.cs](Project_QLThoiTrang/Models/Operations.cs) | `Category`, `StoreConfiguration`, `StockMovement`, `PaymentRecord`, `ReturnRecord`, `ReturnLine`, `Variant`, `WarehouseStock`, `Warehouse`, `OrderHistory`, `ReceiveInput`, `Supplier` |
| [PasswordRecoveryModels.cs](Project_QLThoiTrang/Models/PasswordRecoveryModels.cs) | `ForgotInput`, `ResetInput`, `EmailApiOptions`, `RecoveryContact` |
| [Product.cs](Project_QLThoiTrang/Models/Product.cs) | `Product` |
| [ReviewMedia.cs](Project_QLThoiTrang/Models/ReviewMedia.cs) | `ReviewMedia`, `ReviewMediaInfo`, `ReviewUpload` |
| [StoreModels.cs](Project_QLThoiTrang/Models/StoreModels.cs) | `Customer`, `Order`, `OrderLine`, `CartItem`, `Promotion`, `StoreSettings`, `StoreData`, `CatalogViewModel`, `CartViewModel`, `CheckoutInput`, `LoginInput`, `RegisterInput`, `ProfileInput` |
| [Entities.cs](Project_QLThoiTrang/Data/Native/Entities.cs) | `KhachHangRow`, `NhanVienRow`, `LichSuDangNhapRow`, `DanhMucRow`, `ThuongHieuRow`, `SanPhamRow`, `KichThuocRow`, `MauSacRow`, `BienTheSanPhamRow`, `HinhAnhSanPhamRow`, `KhoRow`, `TonKhoRow`, `NhaCungCapRow`, `PhieuNhapRow`, `ChiTietPhieuNhapRow`, `GioHangRow`, `ChiTietGioHangRow`, `YeuThichRow`, `KhuyenMaiRow`, `ChiTietKhuyenMaiRow`, `DonHangRow`, `ChiTietDonHangRow`, `ThanhToanRow`, `LichSuTrangThaiDonHangRow`, `PhieuTraHangRow`, `ChiTietTraHangRow`, `DanhGiaRow` |
