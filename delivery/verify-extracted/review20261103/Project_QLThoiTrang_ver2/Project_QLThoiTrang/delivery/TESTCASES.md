# Danh sách testcase đầy đủ QOANH

## Môi trường và phương pháp

SQL Server Developer instance mặc định, .NET SDK 10.0.202, target .NET 8. Database thử được tạo bằng delivery/Deploy.sql từ cả hai SQL gốc đã sửa an toàn. Chưa chạy trên SQLEXPRESS. Lệnh chạy ở README.

Mỗi ca nghiệp vụ chạy đúng thứ tự fixture trong tests/BusinessChecks/Program.cs; cần DB mới, đã có dữ liệu mẫu của cả hai SQL. Ca nhập JSON là một phần của runner, không import trước khi chạy. Runner giữ DB sau kiểm tra, không xóa/reset.

HTTP checks dùng cookie riêng, lấy CSRF token từ form và gọi thật các route MVC. Các ca PAGE kiểm tra HTTP 200/Razor render, không chứng minh JavaScript hoặc bố cục pixel. Ca chức năng kiểm tra phản hồi sau POST/redirect. Dữ liệu chi tiết trong scripts/http_checks.py.

## 80 testcase nghiệp vụ — 0 lỗi

Chạy UTC: 2026-11-03T07:14:27.4161579Z. Database: `QOANH_Verification_20261103_Review01`.

| Mã | Thao tác và kết quả mong đợi | Kết quả |
|---|---|---|
| SQL01 | 27 bảng gốc + 3 bảng mở rộng; không có bảng Products/Customers thay thế | PASS |
| SQL02 | Hai script cung cấp tạo đủ 20 khách và 20 nhân viên | PASS |
| SQL03 | Dữ liệu gốc sản phẩm/đơn/trả hàng/phiếu nhập giữ đủ | PASS |
| AU01 | Hash từ bản SQL an toàn xác minh được mật khẩu gốc | PASS |
| AU02 | Tài khoản kho được ánh xạ đúng quyền Inventory | PASS |
| AU03 | Sai mật khẩu ghi nhận thất bại; không xác thực | PASS |
| DB01 | Nhập JSON cùng dữ liệu SQL: giữ cả hai tập dữ liệu | PASS |
| DB02 | Legacy ID ánh xạ đúng; giữ tồn và hash JSON | PASS |
| DB03 | Tổng tiền đơn JSON không đổi; không trừ lại kho lịch sử | PASS |
| DB04 | Chặn nhập lại JSON, giữ nguyên số bản ghi | PASS |
| AU04 | Đăng ký lưu Identity hash, đăng nhập được | PASS |
| AU05 | Email trùng không phân biệt hoa/thường bị chặn | PASS |
| AU06 | Mật khẩu yếu bị chặn | PASS |
| AU07 | Ghi mật khẩu rõ bị rollback | PASS |
| AU08 | Khách không được nhập kho hoặc quản lý danh mục | PASS |
| MG01 | Nhân viên Catalog thêm thương hiệu | PASS |
| MG02 | Nhân viên kho không được quản lý thương hiệu | PASS |
| MG03 | Thêm size/màu; chặn trùng | PASS |
| MG04 | Tạo biến thể, chặn SKU/tổ hợp trùng | PASS |
| MG05 | Admin tạo nhân viên có hash và quyền; nhân viên không tự cấp quyền | PASS |
| MG06 | Admin không được tự khóa/hạ quyền | PASS |
| MG07 | Khóa nhân viên chặn đăng nhập | PASS |
| OR01 | Giỏ rỗng bị chặn | PASS |
| OR02 | Số lượng âm hoặc >20 bị chặn | PASS |
| OR03 | Biến thể không tồn tại bị chặn | PASS |
| OR04 | Đặt theo biến thể, trừ đúng kho và snapshot giá | PASS |
| OR05 | Token chống gửi đơn hai lần | PASS |
| OR06 | Tài khoản không tồn tại bị chặn | PASS |
| OR07 | Mã hết hạn không trừ kho | PASS |
| OR08 | Giảm phần trăm có giới hạn và sản phẩm áp dụng | PASS |
| OR09 | Giảm tiền cố định đúng điều kiện | PASS |
| OR10 | Mã không áp dụng sản phẩm bị chặn | PASS |
| OR11 | Giá báo checkout cũ bị chặn và rollback | PASS |
| OR12 | Khách khác không được hủy | PASS |
| OR13 | Hủy hoàn kho đúng biến thể/kho một lần | PASS |
| OR14 | Không nhảy thẳng sang đã giao | PASS |
| OR15 | Khách không được chuyển trạng thái quản trị | PASS |
| OR16 | Xác nhận/chuẩn bị/đang giao không trừ kho lần nữa | PASS |
| PA01 | Chặn giao xong khi chưa thu tiền | PASS |
| PA02 | Thu sai tiền bị chặn | PASS |
| PA03 | Thu đủ tiền ghi ThanhToan gốc và ledger | PASS |
| PA04 | Thu lặp bị chặn | PASS |
| PA05 | Mã chứng từ đã dùng bị chặn | PASS |
| PA06 | Chặn hủy trực tiếp đơn đã thu tiền | PASS |
| PA07 | Giao/hoàn thành hợp lệ, có lịch sử trạng thái | PASS |
| IN01 | Phiếu nhập nhiều dòng cập nhật kho, header và chi tiết gốc | PASS |
| IN02 | Phiếu nhập trùng bị chặn | PASS |
| IN03 | Dòng cuối sai làm rollback cả phiếu nhập | PASS |
| IN04 | Kiểm kê biến thể chống ghi đè tồn cũ | PASS |
| IN05 | Tồn vượt giới hạn bị rollback | PASS |
| IN06 | Kiểm kê cần lý do | PASS |
| RT01 | Khách khác không được yêu cầu trả | PASS |
| RT02 | Trả một phần, phân bổ giảm giá | PASS |
| RT03 | Chặn yêu cầu trả trùng đang chờ | PASS |
| RT04 | Duyệt trả phần đầu hoàn đúng tiền và kho | PASS |
| RT05 | Duyệt lặp không hoàn lần nữa | PASS |
| RT06 | Trả vượt phần còn lại bị chặn | PASS |
| RT07 | Trả nốt hoàn phần còn lại, trạng thái đầy đủ | PASS |
| RT08 | Từ chối không đổi kho hoặc tiền | PASS |
| RT09 | Không hoàn khi chưa đối soát tiền gốc | PASS |
| RV01 | Chặn đánh giá khi chưa mua/giao | PASS |
| RV02 | Đã mua/giao được đánh giá; chặn lặp | PASS |
| RV03 | Ẩn/hiện đánh giá theo quyền Support | PASS |
| SH01 | Giỏ và yêu thích lưu qua connection mới | PASS |
| SH02 | Tạo đơn xóa giỏ lưu DB cùng transaction | PASS |
| TX01 | Giỏ nhiều dòng sai rollback toàn bộ | PASS |
| TX02 | Hai connection mua đơn vị cuối chỉ một thành công | PASS |
| TX03 | Hai connection thu tiền chỉ một thành công | PASS |
| TX04 | SQL check constraint ngăn tồn âm | PASS |
| TX05 | FK ngăn xóa khách đã có đơn | PASS |
| TX06 | Sổ kho cân bằng với tổng tồn tất cả sản phẩm | PASS |
| AU09 | Đổi mật khẩu kiểm tra mật khẩu hiện tại | PASS |
| AU10 | Đăng xuất cập nhật lịch sử đúng chủ thể | PASS |
| PA08 | Không tái dùng chứng từ có sẵn trong ThanhToan gốc | PASS |
| RT10 | Vượt tồn khi nhận trả rollback cả tiền, phiếu và trạng thái | PASS |
| OR17 | Shipping boundary 699000/700000 uses effective variant price | PASS |
| TX07 | Cân bằng sổ kho sau rollback và kiểm thử ngưỡng | PASS |
| AU11 | Hash giả có marker Identity nhưng header sai bị từ chối và rollback | PASS |
| AU12 | Mật khẩu cũ là chuỗi Base64 dài vẫn được băm; chạy lặp không đổi hash | PASS |
| AU13 | Admin cập nhật hồ sơ khách với điện thoại/địa chỉ trống | PASS |

## 64 testcase HTTP — 0 lỗi

Chạy UTC: 2026-11-03T07:31:32.139150+00:00. Bản MVC Release, database gốc đã chạy runner phía trên.

| Mã | Thao tác và kết quả mong đợi | Kết quả |
|---|---|---|
| HTTP01-home | home | PASS |
| HTTP02-guest-admin-redirect-login | guest admin redirect login | PASS |
| HTTP03-antiforgery-required | antiforgery required | PASS |
| HTTP04-admin-login | admin login | PASS |
| PAGE/Admin | Đăng nhập đúng vai trò và GET /Admin: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Products | Đăng nhập đúng vai trò và GET /Admin/Products: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Product | Đăng nhập đúng vai trò và GET /Admin/Product: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Product/1 | Đăng nhập đúng vai trò và GET /Admin/Product/1: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Categories | Đăng nhập đúng vai trò và GET /Admin/Categories: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Orders | Đăng nhập đúng vai trò và GET /Admin/Orders: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Order/QH-DEMO01 | Đăng nhập đúng vai trò và GET /Admin/Order/QH-DEMO01: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Customers | Đăng nhập đúng vai trò và GET /Admin/Customers: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Customer/2 | Đăng nhập đúng vai trò và GET /Admin/Customer/2: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/EditCustomer/2 | Đăng nhập đúng vai trò và GET /Admin/EditCustomer/2: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Inventory | Đăng nhập đúng vai trò và GET /Admin/Inventory: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Receive | Đăng nhập đúng vai trò và GET /Admin/Receive: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Returns | Đăng nhập đúng vai trò và GET /Admin/Returns: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Promotions | Đăng nhập đúng vai trò và GET /Admin/Promotions: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Promotion | Đăng nhập đúng vai trò và GET /Admin/Promotion: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Promotion/1 | Đăng nhập đúng vai trò và GET /Admin/Promotion/1: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Reports | Đăng nhập đúng vai trò và GET /Admin/Reports: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Settings | Đăng nhập đúng vai trò và GET /Admin/Settings: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Export | Đăng nhập đúng vai trò và GET /Admin/Export: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/References?kind=brands | Đăng nhập đúng vai trò và GET /Admin/References?kind=brands: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/References?kind=sizes | Đăng nhập đúng vai trò và GET /Admin/References?kind=sizes: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/References?kind=colors | Đăng nhập đúng vai trò và GET /Admin/References?kind=colors: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/References?kind=warehouses | Đăng nhập đúng vai trò và GET /Admin/References?kind=warehouses: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/References?kind=suppliers | Đăng nhập đúng vai trò và GET /Admin/References?kind=suppliers: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Variants | Đăng nhập đúng vai trò và GET /Admin/Variants: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/VariantStock | Đăng nhập đúng vai trò và GET /Admin/VariantStock: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Staff | Đăng nhập đúng vai trò và GET /Admin/Staff: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Staff/1 | Đăng nhập đúng vai trò và GET /Admin/Staff/1: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Receipts | Đăng nhập đúng vai trò và GET /Admin/Receipts: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/Reviews | Đăng nhập đúng vai trò và GET /Admin/Reviews: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Admin/LoginHistory | Đăng nhập đúng vai trò và GET /Admin/LoginHistory: HTTP 200, Razor render không lỗi. | PASS |
| HTTP05-customer-login | customer login | PASS |
| HTTP06-customer-admin-denied | customer admin denied | PASS |
| PAGE/Shop | Đăng nhập đúng vai trò và GET /Shop: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Shop/Detail/1 | Đăng nhập đúng vai trò và GET /Shop/Detail/1: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Shop/Cart | Đăng nhập đúng vai trò và GET /Shop/Cart: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Shop/Wishlist | Đăng nhập đúng vai trò và GET /Shop/Wishlist: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Account | Đăng nhập đúng vai trò và GET /Account: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Account/Password | Đăng nhập đúng vai trò và GET /Account/Password: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Account/Orders | Đăng nhập đúng vai trò và GET /Account/Orders: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Account/Order/QH-DEMO01 | Đăng nhập đúng vai trò và GET /Account/Order/QH-DEMO01: HTTP 200, Razor render không lỗi. | PASS |
| PAGE/Home/Info/privacy | Đăng nhập đúng vai trò và GET /Home/Info/privacy: HTTP 200, Razor render không lỗi. | PASS |
| HTTP07-other-customer-order-hidden | other customer order hidden | PASS |
| HTTP08-checkout-COD-deliver-full-return | checkout COD deliver full return | PASS |
| HTTP09-receive-and-reject-duplicate | receive and reject duplicate | PASS |
| HTTP10-inventory-login | inventory login | PASS |
| HTTP11-inventory-read-stock | inventory read stock | PASS |
| HTTP12-inventory-staff-denied | inventory staff denied | PASS |
| HTTP13-inventory-cannot-create-admin | inventory cannot create admin | PASS |
| HTTP14-catalog-login | catalog login | PASS |
| HTTP15-catalog-can-products | catalog can products | PASS |
| HTTP16-catalog-receive-denied | catalog receive denied | PASS |
| HTTP17-sales-login | sales login | PASS |
| HTTP18-sales-can-orders | sales can orders | PASS |
| HTTP19-sales-settings-denied | sales settings denied | PASS |
| HTTP20-lock-account-revokes-existing-cookie | lock account revokes existing cookie | PASS |
| HTTP21-guest-cart-merged-and-survives-logout | guest cart merged and survives logout | PASS |
| HTTP23-profile-empty-optional-fields | profile empty optional fields | PASS |
| HTTP24-profile-duplicate-phone-validation-rollback | profile duplicate phone validation rollback | PASS |
| HTTP22-login-rate-limit-429 | login rate limit 429 | PASS |

## Kiểm tra build và SQL

| Mã | Tiêu chí | Kết quả |
|---|---|---|
| BUILD01 | Solution build Release, 0 lỗi/cảnh báo | PASS |
| BUILD02 | Publish Release | PASS |
| SQLDEP01 | Deploy hai SQL gốc đã sửa an toàn, 27 bảng + 3 bảng mở rộng | PASS |
| SQLDEP02 | Chạy lặp 04-qoanh-extensions.sql không tạo lại/bổ sung số dư đã có | PASS |
| SOURCE01 | 126 tệp nguồn ZIP gốc khớp mã nền trước sửa | PASS |
| SOURCE02 | CSS/JS/ảnh và các trang bảo toàn khớp nguồn, chuẩn hóa CRLF khi so sánh text | Xem preservation-check.json |
| PACKAGE01 | CRC ZIP và đối chiếu byte từng file | Xem package-verification.json |
| PACKAGE02 | Build mã giải nén từ ZIP | Xem archive-build.log |

## Kiểm thử chấp nhận chưa thực hiện

Các ca dưới đây **CHƯA CHẠY**, không tính trong số PASS. Cần áp dụng phạm vi được hỗ trợ trong BAO_CAO.md.

| Mã | Bước thực hiện / tiêu chí | Trạng thái |
|---|---|---|
| M01 | Máy sạch .\SQLEXPRESS: Deploy/import/build/start; đăng nhập, đặt/thu/giao/trả; dữ liệu giữ sau restart. | CHƯA CHẠY |
| M02 | Nghiệm thu trực tiếp với nhóm theo báo cáo Word và chính sách cửa hàng; xác nhận phân quyền/các nghiệp vụ mở rộng. | CHƯA CHẠY |
| M03 | Browser desktop/mobile: kiểm tra QOANH, các form mới, ảnh, font, cuộn, menu và responsive; so sánh ảnh chụp. | CHƯA CHẠY |
| M04 | Browser JavaScript: chọn tỉnh/xã/đường trong checkout, thêm/bớt nhiều dòng nhập, validation và submit đúng dữ liệu. | CHƯA CHẠY |
| M05 | Dừng SQL/ngắt kết nối giữa những SaveChanges: không commit một phần; khởi động lại và đối chiếu kho/đơn/tiền. | CHƯA CHẠY |
| M06 | Hai web host độc lập cùng DB, session phân tán: đặt/thu/hoàn đồng thời; không oversell/thu/hoàn trùng. | CHƯA CHẠY |
| M07 | Gây deadlock/lock timeout: rollback và kiểm tra trang lỗi; không tự lặp giao dịch tiền bên ngoài. | CHƯA CHẠY |
| M08 | Backup/restore trên Express rồi so sánh toàn bộ bảng và đăng nhập. | CHƯA CHẠY |
| M09 | IIS identity riêng và HTTPS: quyền SQL, cookie, CSRF, Data Protection key persistence. | CHƯA CHẠY |
| M10 | Nâng cấp bản sao DB gốc đã có dữ liệu thực bằng extension và --hash-passwords; kiểm tra hash cũ giữ nguyên, mật khẩu rõ đã được băm. | CHƯA CHẠY |
| M11 | CRUD qua browser: xóa danh mục có SP bị chặn; thương hiệu/size/màu/NCC/kho, SKU và dữ liệu trùng/sai; lưu rồi refresh. | CHƯA CHẠY |
| M12 | Mật khẩu chỉ chữ hoặc chỉ số, xác nhận khác: không đăng ký; đăng nhập quá hạn, đổi/reset mật khẩu làm cookie cũ mất hiệu lực. | CHƯA CHẠY |
| M13 | Giỏ 0/21 sản phẩm, nhiều dòng cùng biến thể, >100 dòng, giá/ẩn SP thay đổi khi checkout: không tạo đơn sai. | CHƯA CHẠY |
| M14 | Mã ưu đãi hết lượt/bắt đầu tương lai/ngày cuối/min/trần/sản phẩm hỗn hợp và hủy hoàn lượt: kiểm tra các biên chưa nằm trong runner. | CHƯA CHẠY |
| M15 | Tamper role/CustomerId/VariantId/giá/tổng tiền qua request; HTML/script trong tên/ghi chú/lý do: không nâng quyền và Razor encode. | CHƯA CHẠY |
| M16 | Báo cáo ngày đảo ngược, giảm giá, trả từng phần/nhiều đợt và dữ liệu mẫu lệch; đối chiếu CSV mở trong Excel. | CHƯA CHẠY |
| M17 | JSON hỏng/FK/email trùng/hash không hợp lệ; import rollback, không xóa/sửa nguồn; dữ liệu thực tế vượt kích thước cột phải được báo rõ. | CHƯA CHẠY |
| M18 | Hoàn từ nhiều nhân viên đồng thời, kho gốc bị ngừng hoạt động, hàng hỏng không nhập lại: xác nhận chính sách ngoài phạm vi. | CHƯA CHẠY |
| M19 | Dữ liệu lớn/tải đồng thời: số truy vấn, thời gian khóa, throughput và dung lượng Express; chưa có cam kết hiệu năng. | CHƯA CHẠY |
| M20 | Đối soát tiền thu/hoàn với chứng từ bên ngoài; hệ thống chỉ ghi nhận COD, không thực chuyển tiền. | CHƯA CHẠY |
| M21 | Chạy lại toàn Deploy trên bản sao DB có dữ liệu: schema phải từ chối; không xóa/ghi đè. | CHƯA CHẠY |

## Ki?m tra b? sung l?n r? so?t

| M? | Ti?u ch? | K?t qu? |
|---|---|---|
| ADMIN01 | L?nh qu?n tr? v?i schema ?? Deploy: ho?n t?t, kh?ng t? t?o b?ng | PASS |
| ADMIN02 | L?nh qu?n tr? v?i database kh?ng c? schema QOANH: t? ch?i, kh?ng t?o b?ng | PASS |
| SOURCE03 | 74 t?p giao di?n/JSON ???c b?o to?n v?i ZIP ??u v?o hi?n t?i | PASS |

Log l?n ch?y m?i mang ti?n t? `review-`. C?c log kh?c l? l?ch s?; s? ca t? ??ng hi?n t?i l?y t? hai JSON k?t qu?.
