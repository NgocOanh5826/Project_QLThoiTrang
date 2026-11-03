# Bộ lọc các trang quản lý

Giữ giao diện QOANH. Thanh tìm kiếm có thêm combobox riêng theo dữ liệu từng trang, tự xuống dòng khi không đủ chiều rộng. Có thể kết hợp nhiều điều kiện cùng lúc; “Xóa bộ lọc” trả về toàn bộ danh sách và xóa từ khóa.

| Trang | Combobox |
|---|---|
| Sản phẩm | Danh mục, thương hiệu, đang bán/đã ẩn |
| Biến thể | Sản phẩm, kích thước, màu sắc, trạng thái, mức tồn |
| Danh mục | Đang được/chưa được sử dụng |
| Thương hiệu, kho, nhà cung cấp | Trạng thái, đang được/chưa được sử dụng |
| Kích thước, màu sắc | Đang được/chưa được sử dụng |
| Khuyến mãi | Hiệu lực/sắp diễn ra/hết hạn/đã tắt, loại giảm, tháng kết thúc |
| Tồn kho | Danh mục, hết/sắp hết/còn hàng |
| Kho / biến thể | Kho, sản phẩm, kích thước, màu, mức tồn |
| Phiếu nhập | Kho, nhà cung cấp, nhân viên, tháng nhập |
| Đơn hàng | Trạng thái, phương thức thanh toán, hôm nay/7 ngày/30 ngày |
| Trả hàng | Trạng thái duyệt, tháng yêu cầu |
| Khách hàng | Hoạt động/đã khóa, đã có/chưa có đơn |
| Đánh giá | Hiển thị/đã ẩn, số sao, sản phẩm |
| Nhân viên | Trạng thái, vai trò |
| Lịch sử đăng nhập | Khách/nhân viên, kết quả, tháng |
| Công nợ nhà cung cấp | Nhà cung cấp, tình trạng công nợ, tháng nhập |
| Giá vốn | Danh mục, có/chưa có giá vốn, tình trạng tồn |
| Tin nhắn | Có tin chưa đọc/đã đọc hết, tháng cập nhật |
| Báo cáo doanh thu và lợi nhuận | Hôm nay, 7 ngày, 30 ngày, tháng này hoặc tự chọn từ/đến ngày |

Sản phẩm/khách hàng/đơn hàng lọc phía máy chủ qua GET; đổi combobox tự gửi bộ điều kiện hiện tại. Các danh sách khác lọc phía trình duyệt trên dữ liệu đã tải, tùy chọn động lấy từ bảng; lịch sử đăng nhập giữ giới hạn tải 300 dòng và hộp thư giữ giới hạn 200 hội thoại hiện có. Báo cáo dùng ngày của máy chủ; 7 ngày gồm hôm nay và 6 ngày trước. Chỉnh ngày thủ công bỏ lựa chọn nhanh, bấm “Áp dụng” để lọc.

## Kiểm thử

- Build Release: 0 lỗi, 0 cảnh báo (`page-filters-build.log`).
- `node scripts/admin_filter_checks.mjs`: tùy chọn động, nhãn, chống trùng, tìm tiếng Việt không dấu, lọc kết hợp AND, không có kết quả, đếm và xóa tất cả bộ lọc.
- `python scripts/page_filter_checks.py`: combobox/thuộc tính dòng của từng trang, tìm và reset trên ba danh sách máy chủ, lọc danh mục + trạng thái sản phẩm, tìm có/không dấu, khách đã có/chưa có đơn, phương thức thanh toán/trạng thái/khoảng ngày đơn hàng, khoảng ngày báo cáo và lựa chọn được giữ lại. Kết quả: `page-filter-results.json`.
- `scripts/admin_layout_checks.py`: 39/39 kiểm tra trang danh sách/thêm/sửa/chi tiết và phân quyền đạt (`page-filters-layout-check.log`).

Kiểm tra HTTP dùng database kiểm thử riêng `QOANH_Verification_Features_20261103_A`, không dùng database bán hàng thực tế. Chưa kiểm tra trực quan hoặc tương tác chuột/bàn phím trên trình duyệt thật; cần kiểm tra hiển thị ở các kích thước màn hình khi chạy trên máy người dùng. Không thay đổi schema SQL hoặc nghiệp vụ ghi tồn/thanh toán trong lần bổ sung bộ lọc này.
