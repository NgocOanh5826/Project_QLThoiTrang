# Giỏ hàng và khung thanh toán

- Ô số lượng rộng 80px; không còn nút Cập nhật. Đổi số lượng rồi rời ô, chọn size hoặc màu sẽ tự gửi biểu mẫu và tải lại giỏ để lấy giá/tổng tiền từ máy chủ. Trong lúc lưu khóa tương tác chuột trên giỏ để tránh gửi chồng và bấm thanh toán trước khi lưu xong.
- Từng dòng có combobox màu và kích cỡ. Đổi màu giữ size cũ nếu còn hàng, nếu không chọn size còn hàng đầu tiên; size hết hàng bị khóa. Nếu toàn bộ màu hết hàng, thông báo chọn màu/size khác.
- Đổi sang phiên bản đã có trong giỏ sẽ gộp số lượng. Máy chủ xác minh phiên bản thuộc đúng sản phẩm, đang hoạt động và tổng sau gộp không vượt tồn/20 sản phẩm. Giá lấy từ SQL. Dữ liệu sai giữ giỏ trước thay đổi và hiện lỗi.
- Xóa có xác nhận riêng, dùng POST và token chống giả mạo. Ô số lượng min=1 nên không tự xóa khi nhập 0 trên giao diện.
- Khung thanh toán có ảnh, tên, màu, size, số lượng và thành tiền từng dòng; nút Chỉnh sửa về giỏ; tạm tính/phí giao/tổng cộng căn đều khung, tổng màu đỏ. Có ghi chú mã ưu đãi được tính khi xác nhận.
- Không đổi schema SQL hoặc cách trừ tồn khi đặt đơn. Giỏ khách đăng nhập tiếp tục lưu SQL trong transaction hiện có.

## Testcase và kết quả

`scripts/cart_options_checks.mjs`: giữ size theo màu khi còn hàng, chuyển sang size có hàng, khóa lựa chọn hết hàng, kiểm tra số lượng, tự gửi khi đổi, chặn gửi trùng khi đang lưu.

`scripts/cart_options_http_checks.py` có 10 kiểm tra đạt (`cart-options-http-results.json`):

1. Biểu mẫu tự lưu, dữ liệu biến thể và xác nhận xóa.
2. Thay đổi số lượng.
3. Đổi mã biến thể màu/size.
4. Gộp vào dòng đã có.
5. Chặn phiên bản của sản phẩm khác/không tồn tại.
6. Giới hạn số lượng và tổng sau gộp.
7. Bắt buộc token chống giả mạo.
8. Xóa đúng dòng qua POST.
9. Giỏ khách đăng nhập giữ qua phiên mới.
10. Khung thanh toán có ảnh/màu/size/số lượng.

Chỉ kiểm tra trên database `QOANH_Verification_Features_20261103_A`; giỏ thử được dọn sau kiểm tra, không tạo đơn hàng. Build Release 0 lỗi/cảnh báo; kiểm tra chọn biến thể trang chi tiết và bộ lọc vẫn đạt. Chưa kiểm tra trực quan bố cục và thao tác chuột trên trình duyệt thật.
