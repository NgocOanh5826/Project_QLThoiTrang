# Đối chiếu phần Nguyễn Phước Ngọc Oanh

Ngày kiểm tra: 04/11/2026. Phạm vi: đọc controller, service, view, mã kiểm thử và báo cáo kết quả
đã lưu. Không tạo đơn/thu tiền trên database cửa hàng, không gọi cổng thanh toán hoặc GHN thật.

**Kết luận:** cả 12 mục có phần triển khai tương ứng. Đủ nhóm chức năng ở mức bán hàng cơ bản;
chưa thể kết luận hoàn chỉnh nếu yêu cầu bao gồm thanh toán trực tuyến tự động, theo dõi hãng
vận chuyển thật hoặc CRUD khuyến mãi có xóa. Các giới hạn và điểm cần bổ sung nằm bên dưới.

## Đối chiếu 12 mục

| STT | Chức năng | Đánh giá | Căn cứ và hành vi hiện có |
| --- | --- | --- | --- |
| 1 | Quản lý giỏ hàng | Có | `ShopController.Cart/Add/UpdateCart`, `Views/Shop/Cart.cshtml`: thêm, sửa số lượng/màu/size, gộp dòng, xóa; kiểm tra biến thể và tồn. `ShoppingSession.cs` và `PersistentShopping.cs` lưu giỏ khách vào SQL, giỏ vãng lai dùng Session. |
| 2 | Sử dụng mã giảm giá | Có | Ô PromotionCode ở checkout; `StoreOperations.PlaceOrder` kiểm tra mã, bật/tắt, thời gian, lượt, đơn tối thiểu, sản phẩm áp dụng; giảm phần trăm/số tiền và trần giảm. Mã sai không được tạo đơn. Chưa có bước áp mã xem trước tổng giảm ngay ở giỏ. |
| 3 | Đặt hàng | Có | GET/POST `ShopController.Checkout`, `PlaceOrder`: thông tin người nhận, địa chỉ, phương thức thanh toán; giá/tồn lấy lại trên server, chống đặt trùng bằng token, lưu snapshot, trừ kho trong transaction, xóa giỏ sau thành công. |
| 4 | Thanh toán | Có, giới hạn | Checkout có COD và chuyển khoản. `_BankInstructions` hiển thị QR/nội dung chuyển; `_PaymentStatus` và `RecordPayment` ghi nhận đủ tiền bằng chứng từ duy nhất. Admin/Sales phải đối soát thủ công. Chưa có gateway, webhook xác nhận tiền hoặc hoàn tiền tự động. |
| 5 | Theo dõi đơn hàng | Có, giới hạn | `AccountController.Order`, `_OrderHistory`: trạng thái và thời điểm; chỉ chủ đơn được xem. `_ShipmentStatus` có mã/trạng thái GHN khi có vận đơn, nhưng Ghn.Enabled hiện false và chưa có token/shop thật. Không có bằng chứng đã đồng bộ vận chuyển thật. |
| 6 | Xem lịch sử đơn hàng | Có | `AccountController.Orders`, `Views/Account/Orders.cshtml`: danh sách đơn riêng của tài khoản, mới nhất trước, ngày/trạng thái/tổng tiền và mở chi tiết. |
| 7 | Hủy đơn hàng | Có điều kiện | POST `AccountController.Cancel` gọi `ChangeOrderStatus`: khách chỉ hủy đơn của mình đang chờ xác nhận; hoàn đúng kho và lượt mã một lần. Chặn đơn đã thu tiền hoặc vận đơn chưa đối soát hủy. Không có luồng tự hoàn tiền khi khách hủy đơn đã thanh toán. |
| 8 | Dashboard | Có, cơ bản | `AdminController.Index`, `Views/Admin/Index.cshtml`: doanh thu hàng đã giao/hoàn thành, đơn chờ xác nhận, số sản phẩm đang bán, hàng sắp hết, 6 đơn gần nhất. Chưa có biểu đồ doanh thu hoặc bộ lọc kỳ ngay trên Dashboard. |
| 9 | Quản lý đơn hàng Admin | Có | `AdminController.Orders/Order/OrderStatus/Payment`: lọc tìm kiếm/trạng thái/thanh toán/thời gian, chi tiết, chuyển bước theo quy tắc, ghi nhận thu tiền, lịch sử; phân quyền Admin/Sales. Có thêm trả hàng và PDF. |
| 10 | Quản lý khuyến mãi | Có, chưa đủ nếu cần CRUD đầy đủ | `AdminController.Promotions/Promotion`, các view tương ứng: danh sách, lọc, thêm, sửa, bật/tắt và cấu hình điều kiện. Chưa có action/nút xóa riêng. Form từ chối mọi lần lưu nếu Until đã qua, kể cả khi chỉ muốn tắt chương trình cũ. |
| 11 | Báo cáo và thống kê | Có, giới hạn | `AdminController.Reports/Export`, `Views/Admin/Reports.cshtml`: lọc ngày, tiền hàng sau giảm/hoàn, đơn theo trạng thái, sản phẩm bán được; CSV đơn. `Finance.Index`/`StoreService.Profit` có doanh thu/giá vốn/lợi nhuận. Báo cáo theo ngày đặt và trạng thái hiện tại, không phải sổ kế toán theo ngày thu/chi; CSV xuất toàn bộ đơn, chưa theo khoảng lọc báo cáo. |
| 12 | Kiểm thử module | Có, cần bổ sung nghiệm thu | Có test nghiệp vụ đặt hàng/khuyến mãi/hủy/thu tiền/phân quyền/đồng thời, HTTP và giỏ hàng. Báo cáo đã lưu cho kết quả đạt; chưa chạy lại toàn bộ bộ bán hàng trong lần rà soát này, chưa xác nhận dịch vụ ngoài thật. Chi tiết bằng chứng bên dưới. |

## Điểm cần xác nhận hoặc bổ sung

1. **Thanh toán:** nếu đồ án yêu cầu chỉ COD/chuyển khoản, đã có luồng cần thiết. Nếu yêu cầu
   VNPay/MoMo/cổng ngân hàng và tự xác nhận/hoàn tiền thì còn thiếu tích hợp thực tế.
2. **Theo dõi vận chuyển:** lịch sử trạng thái nội bộ đã có. Theo dõi GHN thật cần cấu hình tài
   khoản và kiểm thử tích hợp; phản hồi hãng vận chuyển hiện không tự xác nhận thu tiền/trạng thái đơn.
3. **Khuyến mãi:** cần thêm xóa hoặc quy định rõ chỉ vô hiệu để giữ lịch sử, nếu đề bài yêu cầu
   đủ thêm/xóa/sửa. Cần điều chỉnh kiểm tra Until để tắt/chỉnh chương trình đã hết hạn mà không sửa ngày.
4. **Báo cáo:** CSV hiện không nhận bộ lọc from/to; nếu cần xuất đúng báo cáo đang xem thì cần bổ sung.
   Chưa thấy kiểm thử đối soát số liệu đầy đủ cho chính các chỉ số Dashboard và bảng Admin/Reports.
5. **Kiểm thử nghiệm thu:** cần lập bộ test gắn riêng 12 mục của Oanh, bổ sung CRUD khuyến mãi,
   Dashboard/báo cáo, checkout chuyển khoản và điều kiện hủy; chạy lại trên bản bàn giao cuối.

Không coi biểu đồ, xóa đơn hoặc thanh toán qua gateway là bắt buộc chỉ từ tên nhóm chức năng;
cần đối chiếu mô tả use case/tiêu chí chấm trước khi mở rộng phạm vi.

## Bằng chứng kiểm thử đã có trong repository

| Tệp | Bằng chứng |
| --- | --- |
| [business-check-results.json](business-check-results.json) | Báo cáo ngày 03/11/2026: 80 ca tổng hợp, failed = 0; đây không phải 80 ca riêng của Oanh. |
| [http-check-results.json](http-check-results.json) | Báo cáo ngày 03/11/2026: 64 ca, failed = 0; gồm đặt đơn, quyền chủ đơn, quyền quản trị và các trang. |
| [cart-options-http-results.json](cart-options-http-results.json) | 10 ca PASS: sửa lượng/biến thể, gộp, xóa, CSRF, giới hạn và lưu giỏ qua phiên. |
| [features-check-results.json](features-check-results.json) | 31 ca tổng hợp, failed = 0; có chuyển khoản, lợi nhuận/giá vốn/hoàn hàng và GHN giả lập. |
| [page-filter-results.json](page-filter-results.json) | Có kiểm tra bộ lọc đơn theo thanh toán/trạng thái/ngày và kỳ báo cáo. |
| [TESTCASES.md](TESTCASES.md) | Danh sách testcase đã lưu; cần đối chiếu lại với source cuối trước khi dùng làm kết quả nghiệm thu. |

Các nhóm phù hợp trực tiếp: OR01–OR17 (đặt hàng/ưu đãi/hủy), PA01–PA08 (thu tiền), SH01–SH02
(giỏ), TX01–TX03 (rollback/đồng thời), BANK01–BANK04 và PROFIT01–PROFIT02. HTTP 200 của một trang
chỉ chứng minh render thành công, không chứng minh toàn bộ thao tác hoặc công thức tính đúng.

## Vị trí chính để mở code

- [ShopController.cs](../Project_QLThoiTrang/Controllers/ShopController.cs): giỏ và checkout.
- [AccountController.cs](../Project_QLThoiTrang/Controllers/AccountController.cs): đơn của khách và hủy.
- [AdminController.cs](../Project_QLThoiTrang/Controllers/AdminController.cs): Dashboard, đơn, khuyến mãi, báo cáo/CSV.
- [StoreOperations.cs](../Project_QLThoiTrang/Services/StoreOperations.cs): đặt đơn, điều kiện ưu đãi, hủy và thu tiền.
- [Finance.cs](../Project_QLThoiTrang/Services/Finance.cs): doanh thu/giá vốn/lợi nhuận sau hoàn hàng.
- [BusinessChecks](../tests/BusinessChecks/Program.cs): kiểm thử nghiệp vụ tổng hợp.
