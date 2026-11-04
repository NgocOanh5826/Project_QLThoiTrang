# Ảnh sản phẩm và lọc danh mục

Form `/Admin/Product` hỗ trợ upload ảnh JPG, PNG, GIF, WebP tối đa 5 MB và xem trước ảnh.
Không chọn ảnh mới thì giữ ảnh thư viện/ảnh hiện tại. Khi thay ảnh, ảnh cũ được giữ để đơn hàng
đã đặt vẫn hiển thị đúng ảnh tại thời điểm mua.

Ảnh được kiểm tra phần mở rộng và chữ ký tệp, đổi tên ngẫu nhiên rồi lưu dưới
`Project_QLThoiTrang/wwwroot/images/products/uploads`. Tài khoản chạy ứng dụng cần quyền ghi
thư mục này; khi chuyển máy hoặc sao lưu cần giữ cả thư mục ảnh cùng database. Không cần đổi schema.
File mới được dọn khi việc lưu sản phẩm thất bại.

Danh mục được đối chiếu không phân biệt hoa/thường và khoảng trắng hai đầu. Thêm lại một danh mục
đang bị ẩn sẽ kích hoạt danh mục đó. Danh mục không hợp lệ được báo lỗi ngay trên form sản phẩm.

Trang bán hàng hiển thị riêng bộ lọc **Danh mục** và **Bộ sưu tập**. Ví dụ “Áo nam” có sản phẩm
nhưng chưa có sản phẩm đánh dấu Hàng mới thì kết hợp hai bộ lọc vẫn cho 0 kết quả. Chọn bộ sưu tập
“Tất cả sản phẩm” hoặc bấm “Bỏ lọc bộ sưu tập” để xem các sản phẩm của danh mục đó. Không tự đánh
dấu sản phẩm cũ thành hàng mới và không thay đổi dữ liệu sản phẩm chỉ để làm xuất hiện kết quả.

| File | Chức năng |
| --- | --- |
| `Services/ProductImages.cs` | Kiểm tra đường dẫn, lưu và dọn file upload mới |
| `Controllers/AdminController.cs` | Nhận multipart upload và lưu sản phẩm |
| `Services/NativeStoreAdapter.cs` | Giữ URL ảnh upload khi đọc SQL, kích hoạt lại danh mục |
| `Views/Admin/Product.cshtml` | Form chọn ảnh và xem trước |
| `wwwroot/js/product-form.js` | Xem trước, kiểm tra dung lượng/loại ảnh phía trình duyệt |
| `Views/Shop/Index.cshtml` | Hiện rõ bộ lọc bộ sưu tập và cách bỏ lọc |
| `tests/ProductChecks` | Kiểm tra database, file ảnh và tùy chọn kiểm tra trình duyệt |

```powershell
dotnet run --project tests/ProductChecks
```

Runner dùng database riêng có tên `QOANH_Verification_Product_...` và thư mục fixture trong
`BuildReview/product_upload`; tự xóa fixture sau kiểm tra, không sửa database cửa hàng. Mặc định
SQL Server là `.\MSSQLSERVER01`, thay bằng biến `QOANH_TEST_SERVER` nếu cần. Kiểm tra trình duyệt
`--ui` là công cụ phát triển trên máy hiện tại, cần Chrome và Playwright đã có trong npm cache.
