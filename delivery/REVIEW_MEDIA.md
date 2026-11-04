# Đánh giá sao và ảnh/video

- Size hết hàng chỉ mờ chữ; bỏ gạch ngang.
- Chọn từ 1 đến 5 sao bằng biểu tượng sao, dùng được bằng chuột hoặc bàn phím; mặc định 5 sao.
- Đính kèm tối đa 5 tệp, gồm tối đa 1 video. Ảnh JPG/JPEG/PNG/GIF/WebP tối đa 5 MB/tệp; MP4/WebM tối đa 20 MB/tệp; tổng tối đa 30 MB. Có xem trước tệp được chọn; chọn lại để thay bộ tệp.
- Máy chủ kiểm tra phần mở rộng và chữ ký tệp, không tin MIME do trình duyệt gửi. Không nhận SVG/HTML/tệp thực thi. Không lưu tên tệp người dùng thành đường dẫn.
- Lưu byte ảnh/video và đánh giá trong cùng transaction SQL. Lỗi ghi tệp rollback cả đánh giá. Sao lưu/restore database bao gồm cả media, không cần chép thư mục upload riêng.
- Chỉ khách đã mua và nhận sản phẩm được đánh giá, một lần mỗi sản phẩm; vẫn kiểm tra số sao và độ dài nội dung.
- Khách chỉ xem được media của đánh giá đang hiện; Admin/Support xem được media khi kiểm duyệt đánh giá đã ẩn. Endpoint hỗ trợ range cho video, đúng Content-Type và nosniff. Video cần codec được trình duyệt hỗ trợ; hệ thống chưa chuyển mã, quét virus hoặc kiểm duyệt nội dung tự động.

## Triển khai

Database hiện có: sao lưu rồi chạy `delivery/Upgrade-features.sql` bằng SQLCMD mode như README. Wrapper đã bao gồm script 08, khóa nghiệp vụ và đối chiếu số bản ghi các bảng cũ. Nếu đã đủ schema trước 08, có thể sao lưu rồi chạy riêng:

```powershell
sqlcmd -S .\SQLEXPRESS -E -C -b -v DatabaseName="QL_CuaHangThoiTrang" -i delivery\08-review-media.sql
```

Máy này Development dùng `.\MSSQLSERVER01`: đã sao lưu/VERIFYONLY và nâng cấp thành 38 bảng, số bản ghi của 37 bảng cũ giữ nguyên (`review-media-local-upgrade.log`). Cài mới: `Deploy.sql` đã gọi thêm 08. Chạy lại 08 không xóa hoặc thêm lại dữ liệu. Khi chạy IIS, cần cho phép request ít nhất 32 MB để nhận bộ tệp tối đa 30 MB; ứng dụng giới hạn request 32 MiB.

## Testcase

`tests/ReviewChecks`: ảnh/video đúng chữ ký, giả phần mở rộng, HTML/SVG/exe, ảnh/video quá dung lượng, quá số tệp, quá 1 video, quá tổng dung lượng; khách chưa nhận hàng; sao/nội dung không hợp lệ; lỗi tệp không tạo review; lỗi SQL khi ghi media rollback review; lưu metadata/byte; chống đánh giá trùng; kiểm duyệt ẩn metadata. Runner chỉ chạy nếu database có tiền tố `QOANH_Verification_`.

Kiểm tra HTTP: multipart upload hợp lệ/không hợp lệ, ảnh/video và Content-Type, byte range, không cho khách đọc media đã ẩn, Admin xem media đã ẩn, sao và form upload hiển thị đúng, token/role. Các tệp video dùng trong kiểm thử chữ ký/range là mẫu container tổng hợp; chưa xác minh phát video, tương tác sao hoặc bố cục trên trình duyệt thật.

Kết quả: 14/14 kiểm tra nghiệp vụ (`review-media-results.json`) và 8/8 HTTP (`review-media-http-results.json`) đạt. Build Release thành công, không có lỗi biên dịch; có cảnh báo NU1900 do không truy cập được dịch vụ kiểm tra lỗ hổng NuGet, nên chưa xác minh được thông tin audit package trong lần này. Kiểm tra JavaScript chọn biến thể và bộ lọc cũ vẫn đạt.
