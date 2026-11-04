# Giao diện điện thoại

- Header dưới 600px: logo và dãy icon trên cùng một hàng; danh mục cuộn ngang nếu cần; tìm kiếm ở hàng riêng cùng icon chat. Giữ icon đăng xuất cuối dãy và xác nhận trước khi thoát.
- Dùng lề 16px (12px ở màn rất hẹp), giảm khoảng trống, cỡ tiêu đề và padding các khung.
- Giỏ hàng: ảnh căn lên đầu; màu chiếm một hàng, size/số lượng cùng hàng; combobox co theo khung, không ép chiều rộng cố định; giá dòng đặt dưới phần thông tin.
- Chi tiết, tài khoản và thanh toán xếp một cột; ảnh, trường nhập, tóm tắt đơn và media đánh giá vừa chiều ngang. Input trên điện thoại dùng 16px để hạn chế trình duyệt phóng to khi nhập.
- Chỉ áp dụng bố cục này cho storefront; CSS quản trị và bố cục desktop được giữ.

## Kiểm tra

Build Release 0 lỗi/cảnh báo. 10 kiểm tra HTTP giỏ/thanh toán và 5 kiểm tra chi tiết/header/yêu thích đạt; các kiểm tra JavaScript giỏ, biến thể và bộ lọc vẫn đạt.

Đã dùng Chrome headless với profile tạm riêng và device metrics 320/390/768/1440px để render bản HTML tạm của bốn trang: giỏ hàng, thanh toán, chi tiết, tài khoản. **16/16 đạt**: không tràn ngang, các icon không xuống dòng/chồng logo, ô giỏ không vượt khung. Header ở 320/390px cao khoảng 162px; desktop 84px. Đã xem ảnh render giỏ 320/390px, thanh toán và chi tiết 390px. Kết quả đo: `mobile-layout-results.json`.

HTML/screenshot tạm ở `delivery/verify-extracted` không đưa vào ZIP; token chống giả mạo đã xóa khỏi snapshot. Đây là kiểm tra bố cục HTML bằng engine Chrome, không thay thế kiểm tra tương tác đầy đủ trên điện thoại thật, iOS/Safari, phóng to chữ hoặc bàn phím ảo. Luồng nghiệp vụ được kiểm tra qua HTTP trên database thử nghiệm riêng. Không có thay đổi schema SQL trong lần chỉnh giao diện mobile này.
