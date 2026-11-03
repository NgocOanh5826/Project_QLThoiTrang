# Báo cáo bàn giao QOANH tích hợp database gốc

Ngày 02/11/2026 (Asia/Saigon). Đã đọc ZIP gốc, QL_CuaHangThoiTrang.sql, Insert_QLThoiTrang.sql và báo cáo Word theo các đường dẫn người dùng cung cấp. Hash đầu vào ở `input-inventory.json`. Đối chiếu 126 tệp nguồn của ZIP với mã nền workspace: **không có khác biệt** (`original-zip-comparison.json`).

## Chức năng đã hoàn thành

| Phạm vi | Kết quả |
|---|---|
| SQL Server/EF Core | StoreService đọc/ghi trực tiếp 27 bảng tiếng Việt của SQL gốc, thêm 3 bảng QOANH cho cấu hình và audit; không dùng bộ bảng Products/Customers thay thế |
| Triển khai hai SQL | Tách schema/dữ liệu/view/procedure, áp dụng cả dữ liệu bổ sung Insert_QLThoiTrang; không DROP DB, script schema từ chối DB đã có bảng; script extension chạy lặp an toàn |
| Bảo toàn dữ liệu | Giữ dữ liệu SQL mẫu; nhập thêm JSON trong transaction, lưu LegacyId, ánh xạ FK, giữ mã QH, giá/phí/tổng và hash; JSON không bị sửa; không trừ lại kho lịch sử |
| Tài khoản | KhachHang và NhanVien riêng, đăng ký/đăng nhập/hồ sơ/đổi mật khẩu; hash Identity có salt; lịch sử login/logout; giới hạn thử đăng nhập |
| Phân quyền | Admin/Catalog/Inventory/Sales/Support/Customer; gán/thu hồi/khóa nhân viên; kiểm tra quyền tại controller/nghiệp vụ; thay quyền/khóa/đổi mật khẩu vô hiệu cookie cũ |
| Khách hàng | Tra cứu, lịch sử đơn, Admin/Support sửa hồ sơ và khóa; khách tự sửa hồ sơ |
| Danh mục mở rộng | Thương hiệu, size, màu, kho, nhà cung cấp: thêm/sửa; ngừng hoạt động các thực thể có cờ TrangThai; giữ lịch sử liên quan |
| Sản phẩm/biến thể | CRUD/ẩn sản phẩm, liên kết thương hiệu; SKU, size, màu, giá riêng, trạng thái biến thể; chặn SKU/tổ hợp trùng; không đổi định danh biến thể đã có |
| Giỏ/yêu thích | Khách đăng nhập lưu vào GioHang/ChiTietGioHang/YeuThich; hợp nhất giỏ vãng lai khi login; xóa giỏ DB cùng transaction tạo đơn |
| Đặt hàng | Kiểm tra chủ thể, giỏ, size/màu/biến thể, lượng mua, giá báo checkout và tồn kho hoạt động; phân bổ kho, snapshot giá/phí/ưu đãi, token chống gửi lặp |
| Kho/nhập | Tồn theo MaKho/MaBienThe; phiếu một/nhiều dòng có NCC/nhân viên/giá nhập, tổng từ chi tiết; lỗi bất kỳ dòng rollback toàn phiếu; kiểm kê có lý do và số dư kỳ vọng |
| Đơn hàng | Trạng thái gốc gồm chuẩn bị/hoàn thành, lịch sử trạng thái; giữ lối tắt xác nhận → giao của giao diện cũ; hủy hoàn đúng kho và lượt ưu đãi, chống hoàn lặp |
| Thanh toán | Ghi nhận COD thực tế vào ThanhToan và ledger; đúng số tiền còn phải thu; chứng từ duy nhất cả dữ liệu gốc và mới; đối soát đủ tiền trước hoàn tất giao |
| Trả hàng | Từng phần/toàn bộ, nhiều đợt sau khi đợt trước xử lý; chống vượt số mua và yêu cầu trùng đang chờ; phân bổ giảm giá, hoàn phần còn lại khi trả nốt; duyệt nhận hàng/hoàn kho/hoàn tiền nguyên tử |
| Đánh giá | Chỉ khách đã mua và đã giao được đánh giá; một khách/sản phẩm; Support/Admin ẩn/hiện |
| Khuyến mãi | Phần trăm/số tiền, ngày hiệu lực, đơn tối thiểu, giới hạn giảm, số lượt, sản phẩm áp dụng; áp dụng ở server và lưu snapshot |
| Báo cáo | Theo ngày đặt, tiền hàng sau giảm/hoàn; trừ số lượng/giá trị trả khi thống kê sản phẩm; hỗ trợ trạng thái Đã giao/Hoàn thành; CSV hiện có |
| QOANH | Giữ nguyên toàn bộ CSS/JS/ảnh wwwroot, trang chủ/catalog/login; các trang/field bổ sung dùng layout/classes cũ; manifest bảo toàn đính kèm |

## Quy tắc transaction và bảo mật

Mọi ghi qua StoreService dùng Serializable và khóa `QOANH.Store.Write` bằng sp_getapplock, owner Transaction. Dữ liệu được xác minh trước khi lưu; các lần SaveChanges giữa những bảng gốc vẫn ở trong **cùng transaction**. Bất kỳ lỗi nào trước commit phải rollback cả các lần SaveChanges trước đó. Khóa giải phóng khi commit/rollback; timeout khóa 15 giây. Không tự chạy lại giao dịch tiền bên ngoài.

Giữ FK/check/unique của schema gốc; thêm unique checkout token, public order code, chứng từ thu tiền và mã phiếu nhập. Khóa SQL phối hợp các connection ứng dụng; chưa chứng minh khả năng tải cao/multi-host session bằng kiểm thử tải.

Script bàn giao không chứa mật khẩu mẫu dạng rõ ở cột MatKhau: đã đổi sang Identity V3 hash, vẫn đăng nhập được mật khẩu mẫu gốc. Hash JSON được giữ nguyên, rehash khi cần. Thông tin mật khẩu demo trong README và dữ liệu nhập của testcase không phải dữ liệu mật khẩu lưu trong database. Không có endpoint nhận PasswordHash/Role của khách để nâng quyền.

## Kiểm chứng

Kết quả cuối: **77/77 testcase nghiệp vụ và 62/62 HTTP đạt**, tổng 139 ca tự động. Database cuối: `QOANH_Verification_Original_20261102_D`.

- Log build/publish Release và kết quả kiểm tra ZIP đi kèm. Không coi build thành công là chứng minh đầy đủ nghiệp vụ.
- `business-check-results.json` / `review-business-check.log`: database thử mới được tạo từ **cả hai script gốc đã điều chỉnh**, rồi nhập JSON trong test. Có kiểm tra dữ liệu/mật khẩu gốc, ánh xạ JSON, quyền nhân viên, variant/warehouse, nhập nhiều dòng, trả từng phần, rollback, chứng từ trùng, ngưỡng phí và thao tác đồng thời.
- `http-check-results.json` / `review-http-check.log`: bản MVC Release; render các trang cũ/mới, login/CSRF, E2E đặt → thu COD → giao → trả/hoàn, nhập kho, quyền các nhóm nhân viên, khóa cookie đang dùng, giỏ qua logout và rate limit 429.
- SQLCMD triển khai và chạy lặp extension trên database thử; app EF đọc/ghi được chính schema này. Database thật `QL_CuaHangThoiTrang` chưa bị tạo/ghi trong quá trình xác minh; các database thử được giữ, không xóa.
- Có hai SQL connection song song mua đơn vị cuối và thu tiền cùng đơn: chỉ một thành công. Chưa chạy hai web host độc lập.
- Testcase chi tiết và các bước **chưa chạy** nằm ở `TESTCASES.md`; số PASS chính thức lấy từ JSON cuối cùng.

## Dữ liệu gốc có điểm chưa nhất quán

Giữ nguyên dữ liệu lịch sử của nguồn và ghi rõ để người dùng quyết định điều chỉnh, không tự sửa chứng từ:

- Tổng phiếu nhập mẫu 1/2/3 trong header lần lượt 15.000.000 / 12.000.000 / 7.000.000 đ; tổng chi tiết tương ứng 16.500.000 / 18.250.000 / 5.600.000 đ. Phiếu mới trong ứng dụng tính tổng từ chi tiết nên không phát sinh kiểu lệch này.
- Đơn mẫu 1 mua hai áo, giảm 10%; nguồn ghi hoàn một áo 350.000 đ, trong khi giá phân bổ ưu đãi là 315.000 đ. Bản cập nhật giữ số đã hoàn 350.000 đ; lần hoàn tiếp theo bị giới hạn bởi số tiền thực còn lại, không vượt tổng đã thu.
- Một số đánh giá mẫu không khớp điều kiện đã giao tại thời điểm ghi; giữ dữ liệu mẫu, nhưng đánh giá mới phải qua kiểm tra đã mua/đã giao.
- Các ảnh SQL chưa có file trong ZIP. Giữ đường dẫn gốc trong HinhAnhSanPham và hiển thị ảnh QOANH có sẵn thay thế; chưa bổ sung ảnh thương hiệu ngoài.
- JSON không biết tồn từng size/kho. Khi nhập, giữ tổng và đưa số dư vào size M tại kho mặc định; cần kiểm kê thực tế trước khi dùng. Không suy diễn dữ liệu size còn thiếu.

## Chưa hoàn thành hoặc chưa thể kiểm chứng

1. **Chưa chạy trên `.\SQLEXPRESS`**: máy kiểm thử hiện có SQL Server Developer instance mặc định. Connection string và hướng dẫn Express có sẵn; cần nghiệm thu lại trên máy đích.
2. Chưa tích hợp ngân hàng/ví/cổng thanh toán, vận chuyển, email/SMS, VAT/hóa đơn điện tử hoặc công nợ NCC. Thanh toán/hoàn tiền là ghi nhận nghiệp vụ đã thực hiện ngoài hệ thống; không chuyển tiền thật.
3. Chưa có quên mật khẩu tự phục vụ qua email; đã có đổi mật khẩu và Admin đặt lại mật khẩu nhân viên. Chưa có quy trình phục hồi tài khoản khách qua kênh xác thực ngoài.
4. Trả hàng chỉ hỗ trợ hàng nhận lại có thể nhập kho; chưa có kho hỏng, đổi size/sản phẩm, tự động tính hạn trả theo ngày giao hoặc phí trả theo chính sách cửa hàng. Chưa có luồng hủy kèm hoàn tiền trước giao; đơn đã thu tiền bị chặn hủy trực tiếp.
5. Chưa có EF migrations nâng cấp mọi phiên bản schema tùy ý. Có script extension chạy lặp cho đúng schema gốc; chưa kiểm chứng nâng cấp trên database sản xuất thực tế. Database thử tiếng Anh của bản trước không được tự chuyển sang 27 bảng gốc.
6. Danh mục size/màu có thêm/sửa, chưa có xóa vật lý qua giao diện; các danh mục có TrangThai dùng ngừng hoạt động. Không cung cấp xóa lịch sử hàng hóa/tài khoản/đơn đã liên kết.
7. Chưa kiểm thử trình duyệt có JavaScript, chọn địa chỉ, responsive hoặc so sánh pixel. Bảo toàn CSS/JS/ảnh được kiểm chứng theo hash/so sánh nguồn; các trang được kiểm tra HTTP render. Nút thêm/bớt dòng nhập và chọn địa chỉ vẫn cần nghiệm thu trên trình duyệt thật.

## Hạn chế và lỗi kỹ thuật còn tồn tại

- Adapter Read/Write đọc nhiều bảng và serialize ghi bằng khóa toàn cửa hàng để giữ controller/view hiện có. Chưa tối ưu paging/truy vấn cho dữ liệu lớn, chưa load test. Không cam kết throughput hoặc SLA.
- Lỗi hạ tầng SQL, timeout hoặc deadlock có thể hiển thị trang lỗi chung; chưa có thông báo/retry riêng cho mọi loại lỗi. Transaction chưa commit được rollback. Không bật tự retry thao tác thu tiền thực tế.
- Session khách vãng lai vẫn trong bộ nhớ, có thể mất khi restart. Giỏ/yêu thích khách đăng nhập lưu SQL; các chỉnh sửa đồng thời cùng tài khoản có thể theo lần lưu cuối, không có giao diện hòa giải xung đột giỏ.
- Token checkout mới thay token cũ khi mở lại trang; tab cũ cần tải lại. Không có payment gateway idempotency/webhook vì chưa tích hợp gateway.
- Đơn SQL/JSON lịch sử không có kho xuất được ghi rõ; khi trả phần hàng lịch sử đó, dùng kho hoạt động mặc định. Đơn mới lưu QWarehouseId để hoàn chính xác kho gốc.
- Báo cáo theo ngày đặt và trạng thái hiện tại, không phải báo cáo thu/chi kế toán theo ngày phát sinh. Không sử dụng số liệu mẫu chưa đối soát làm chứng từ thật.
- Chưa phát hiện lỗi trong các ca tự động cuối cùng ngoài các giới hạn trên; kết luận này chỉ áp dụng phạm vi testcase đã chạy, không chứng nhận hết mọi lỗi.

## Ph?m vi r? so?t l?i ng?y 03/11/2026

ZIP ???c ch? ??nh t?i th?i ?i?m b?t ??u l?n n?y ?? ch?a b?n EF Core t? l?n t?ch h?p tr??c, kh?ng c?n l? b?n JSON ban ??u. `review-input-inventory.json` ghi ??ng hash ??u v?o hi?n t?i; `input-inventory.json` v? c?c ??i chi?u g?c c? ???c gi? l?m l?ch s?. ?? ??c l?i n?i dung b?o c?o Word v? gi? ma tr?n ??i chi?u y?u c?u hi?n c?.

- S?a l?nh qu?n tr?: b? EnsureCreated; y?u c?u database ?? k?t n?i ???c v? c? extension QOANH. Tr?nh t?o schema t? EF thi?u c?c view/procedure/r?ng bu?c c?a b? tri?n khai. X?c minh c? schema h?p l? v? schema thi?u trong log `review-schema-guard-*`.
- B? SQL_Deployment.zip ch? ch?a Deploy.sql v? 00?05, kh?ng k?m script s?a database c?c b? hay m? h?nh SQL sinh t? ??ng.
- Tri?n khai l?i c? hai SQL tr?n database th? m?i, ch?y l?p extension th?nh c?ng. Ch?y l?i 144 ca t? ??ng; build/publish Release kh?ng l?i. Ki?m tra b?o to?n 74 t?p wwwroot/JSON/trang g?c v?i ZIP hi?n t?i trong `review-preservation.json`.
- Kh?ng c?i SQL Express ho?c t?o instance m?i tr?n m?y. M?c ??nh c?u h?nh v?n l? `.\SQLEXPRESS` v? database `QL_CuaHangThoiTrang`; to?n b? l?n ki?m th? n?y d?ng instance Developer `.` v? database ri?ng. Kh?ng d?ng k?t qu? n?y ?? kh?ng ??nh ?? nghi?m thu Express.
- Gi? nguy?n c?c m?c ch?a ho?n th?nh, b?t nh?t d? li?u m?u v? gi?i h?n k? thu?t ? tr?n. ??y l? ph?n c?n ??i so?t/nghi?m thu ti?p, kh?ng t?nh v?o s? ca ??t.
