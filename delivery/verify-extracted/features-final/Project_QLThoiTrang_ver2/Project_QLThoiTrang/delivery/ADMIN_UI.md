# Cập nhật giao diện quản trị

Theo yêu cầu: mục đang chọn được làm sáng; danh sách chỉ hiển thị bộ lọc, bảng và nút mở trang thêm; form thêm/sửa nằm ở trang riêng. Giữ màu sắc, font và layout QOANH.

- Sidebar đánh dấu mục hiện tại bằng nền sáng và `aria-current="page"`, kể cả khi vào trực tiếp trang sửa/chi tiết. Thương hiệu, kích thước, màu dùng chung nhóm; kho và nhà cung cấp có mục riêng. Các tab danh mục đánh dấu đúng tab hiện tại.
- Tách form thương hiệu/kích thước/màu/kho/nhà cung cấp, biến thể, nhân viên và thêm danh mục sản phẩm. Có nút lưu, quay lại, hiển thị lỗi ngay trên form; lưu thành công quay lại danh sách.
- Trang tồn kho dùng bảng, kiểm kê theo biến thể/kho ở trang riêng. Phiếu nhập có bảng danh sách và trang chi tiết. Yêu cầu trả hàng có bảng danh sách và trang xử lý riêng.
- Tìm kiếm trực tiếp trong dữ liệu bảng, hỗ trợ từ khóa tiếng Việt không dấu; lọc trạng thái cho danh mục mở rộng, biến thể và nhân viên. Có xóa bộ lọc, số bản ghi và trạng thái không có kết quả. Bộ lọc áp dụng trên dữ liệu đã tải, ví dụ lịch sử đăng nhập vẫn giới hạn 300 bản ghi như trước.
- Giữ đường dẫn sửa và POST cũ tương thích; route mới được kiểm tra quyền tương ứng. Quy định Admin không được mua hàng được giữ nguyên.
- Ẩn vùng validation khi chưa có lỗi để loại bỏ dải nền đỏ trống.

## Kiểm tra

- Build Release: 0 lỗi, 0 cảnh báo (`admin-layout-build.log`).
- Bộ HTTP hồi quy: 64/64 đạt (`admin-layout-http.log`, `http-check-results.json`).
- Bộ kiểm tra mới: 39/39 đạt (`admin-layout-results.json`, `admin-layout-checks.log`). Có kiểm tra danh sách/form, menu hiện tại, lưu và validation, đường dẫn cũ, phân quyền Catalog/Inventory/Sales.
- JavaScript: kiểm tra cú pháp đạt; bộ kiểm tra handler lọc bằng Node đạt cho tìm kiếm không dấu, kết hợp trạng thái, số bản ghi, không có kết quả và reset (`admin-filter-checks.log`). Đây là kiểm tra logic với đối tượng mô phỏng DOM, không thay thế kiểm thử browser.
- Database thử: `QOANH_Verification_20261103_Review01` trên SQL Server Developer. Không kiểm tra bằng dữ liệu thật hoặc SQL Express.

Chưa kiểm chứng trực quan/responsive trên trình duyệt thật: Computer Use dừng vì không xác định được URL Chrome đủ chắc chắn; không tiếp tục thao tác trình duyệt sau khi công cụ dừng. Không có ảnh nghiệm thu hoặc kết luận so sánh pixel. Nghiệp vụ SQL không thay đổi; không chạy lại bộ 80 ca nghiệp vụ trong lần chỉnh giao diện này.

## Đường dẫn

| Danh sách | Thêm hoặc sửa |
|---|---|
| `/Admin/References?kind=brands` (hoặc sizes/colors/warehouses/suppliers) | `/Admin/Reference?kind=brands`, thêm `id` khi sửa |
| `/Admin/Variants` | `/Admin/Variant`, `/Admin/Variant/{id}` |
| `/Admin/Staff` | `/Admin/StaffMember`, `/Admin/StaffMember/{id}` |
| `/Admin/Categories` | `/Admin/Category` |
| `/Admin/VariantStock` | `/Admin/StockCount?variantId=…&warehouseId=…` |
| `/Admin/Receipts` | `/Admin/Receipt/{id}` xem chi tiết; `/Admin/Receive` nhập mới |
| `/Admin/Returns` | `/Admin/ReturnRequest/{id}` |
