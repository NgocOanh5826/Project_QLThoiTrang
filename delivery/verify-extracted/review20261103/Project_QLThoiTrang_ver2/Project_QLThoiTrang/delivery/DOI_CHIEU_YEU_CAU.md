# Đối chiếu báo cáo đồ án và database

Căn cứ: Nhom11_TaiKhaiOanh_QuanLyCuaHangThoiTrang.docx, đặc biệt mục 1.4, 2.1, 2.6–2.10, 3.2–3.3 và phụ lục kiểm thử; hai SQL gốc. Báo cáo phân biệt phạm vi JSON hiện có với các hướng mở rộng, nên không coi các sơ đồ/kế hoạch cũ là bằng chứng chức năng đã hoàn thành.

| Yêu cầu / phần báo cáo | Bản cập nhật | Căn cứ kiểm tra |
|---|---|---|
| UC-001 đặt hàng COD | Đặt theo biến thể/kho, kiểm tra server, snapshot, ưu đãi, transaction và sổ thu COD; chưa gateway | OR/PA/TX, HTTP08 |
| UC-002 khách hàng | Bổ sung sửa/khóa từ Admin/Support; giữ lịch sử và quyền chủ đơn | MG/AU, HTTP quyền và trang EditCustomer |
| UC-003 hàng hóa/size | Biến thể thực, SKU/màu/size, giá riêng, tồn theo kho, thương hiệu | MG/IN/OR, trang Variants/References/VariantStock |
| UC-004 đơn hàng | Bổ sung chuẩn bị/hoàn thành và lịch sử gốc; hủy hoàn kho/lượt ưu đãi; trả từng phần | OR/RT, HTTP08 |
| UC-005 tài khoản/quyền | Customer + Admin và 4 nhóm nhân viên; khóa/đổi quyền vô hiệu cookie; đổi mật khẩu, log và rate limit | AU/MG, HTTP10–22 |
| 1.4, 3.2: thương hiệu/NCC/phiếu nhập còn thiếu | Thêm màn hình quản lý, phiếu nhiều dòng và sổ biến động | MG/IN, HTTP trang mở rộng/nhập kho |
| 1.4, 3.2: đánh giá còn thiếu | Khách đã mua/giao mới được đánh giá; kiểm duyệt | RV, trang Reviews/Shop Detail |
| 3.3: lưu giỏ theo tài khoản | Dùng bảng giỏ/yêu thích gốc, hợp nhất giỏ vãng lai | SH, HTTP21 |
| 3.3: SQL Server và nhất quán | Ánh xạ 27 bảng gốc, extension, Serializable/applock và khóa database | SQL/DB/TX, Deploy.sql |
| 3.3: ưu đãi có điều kiện | Phần trăm/số tiền, tối thiểu/trần/lượt/ngày/sản phẩm | OR08–10 và trang Promotion |
| 3.3: thanh toán/vận chuyển ngoài | Chưa gateway/vận chuyển; COD ledger và hoàn tiền ghi nhận thủ công | Hạn chế trong báo cáo bàn giao |
| Giữ giao diện QOANH | CSS/JS/ảnh/trang chủ/catalog/login giữ nguyên; bổ sung field/trang cần thiết | preservation-check.json, original-zip-comparison.json |

## Phụ lục testcase báo cáo gốc

| Ca gốc | Cách đối chiếu bản mới |
|---|---|
| KT01 email trùng | AU05 |
| KT02 mật khẩu thiếu chữ/số | DataAnnotations RegisterInput; AU06 kiểm tra mật khẩu yếu; trường hợp cụ thể thiếu chữ/số còn trong kiểm thử chấp nhận |
| KT03 Customer vào Admin | HTTP06 |
| KT04 size/số lượng/tồn | OR02–04, TX02; chọn size từ biến thể thực thay danh sách cố định |
| KT05 đặt đơn hợp lệ | OR04, SH02, HTTP08 |
| KT06 699.000/700.000 | OR17 dùng giá bán có hiệu lực của biến thể |
| KT07 đơn khách khác | HTTP07 và kiểm tra chủ thể OR12/RT01 |
| KT08 hủy một lần | OR13 |
| KT09 nhảy trạng thái | OR14 |
| KT10 xóa danh mục đang có sản phẩm | Guard AdminController hiện có + FK; thao tác form cụ thể còn trong nghiệm thu thủ công |
| KT11 doanh thu chỉ đơn đã giao | Mở rộng gồm Hoàn thành theo SQL; trừ giảm/hoàn; view Reports đã sửa, công thức số liệu mẫu cần đối soát theo báo cáo |
| KT12 mã chưa làm giảm tiền (phạm vi JSON cũ) | **Đã thay yêu cầu** theo nhiệm vụ bổ sung: mã hợp lệ phải giảm tiền; OR08–10. Không coi kỳ vọng cũ “chưa áp mã” là lỗi của bản mới |

## Đối chiếu 27 bảng

Ánh xạ cột/khóa được ghi chi tiết ở `original-schema-map.json`; entity/mapping ở `Data/Native/Entities.cs` và `Mapping.cs`. Các nhóm đều có nơi sử dụng:

- KhachHang, NhanVien, LichSuDangNhap: tài khoản, quyền và nhật ký.
- DanhMuc, ThuongHieu, SanPham, KichThuoc, MauSac, BienTheSanPham, HinhAnhSanPham: danh mục/catalog/biến thể/ảnh.
- Kho, TonKho, NhaCungCap, PhieuNhap, ChiTietPhieuNhap: tồn và nhận hàng.
- GioHang, ChiTietGioHang, YeuThich: lưu mua sắm theo tài khoản.
- KhuyenMai, ChiTietKhuyenMai: điều kiện và phạm vi ưu đãi.
- DonHang, ChiTietDonHang, ThanhToan, LichSuTrangThaiDonHang: đơn, tiền, lịch sử.
- PhieuTraHang, ChiTietTraHang: trả từng phần và hoàn tiền.
- DanhGia: đánh giá và kiểm duyệt.

Không dùng các bảng tiếng Anh của bản tạm trước thay cho cấu trúc database gốc.
