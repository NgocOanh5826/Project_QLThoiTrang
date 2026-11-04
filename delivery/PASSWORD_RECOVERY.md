# Quên mật khẩu bằng email API và đổi mật khẩu

Ứng dụng chỉ gửi OTP qua **Resend email API**. Đã bỏ SMS, Twilio và SMTP Gmail.
Không cần mật khẩu ứng dụng Google. Email nhận mã phải trùng hồ sơ khách hàng đang hoạt động.

## Thiết lập

1. Đăng ký [Resend](https://resend.com/signup), có thể dùng `oanhnguyen.410207@gmail.com`.
2. Tạo [API key](https://resend.com/api-keys) có quyền gửi email.
3. Chạy từ thư mục gốc dự án:

   ```powershell
   python scripts/configure_email_api.py
   ```

4. Nhập API key khi được hỏi và chọn địa chỉ gửi như hướng dẫn bên dưới.
5. Khởi động lại website trong môi trường **Development**, thử form quên mật khẩu.

Script nhập API key ẩn, truyền qua stdin và lưu bằng .NET user-secrets. Khóa HMAC được tạo
ngẫu nhiên nếu thiếu; khóa hiện có được giữ để OTP chưa hết hạn vẫn dùng được. Không đưa
API key vào chat/Git. Script không gửi thư và không xóa các bí mật cấu hình khác.

### Thử nghiệm khi chưa có tên miền

Dùng `EmailApi:From = onboarding@resend.dev`. Địa chỉ thử nghiệm chỉ gửi đến email dùng đăng ký
Resend. Nếu đăng ký bằng `oanhnguyen.410207@gmail.com`, tài khoản khách hàng của website phải dùng
đúng email đó để thử OTP. Không thay người nhận của khách thành email chủ Resend.

### Gửi cho khách hàng bất kỳ

Thêm và xác minh tên miền tại [Resend Domains](https://resend.com/domains), cấu hình DNS theo
hướng dẫn rồi đặt `EmailApi:From` thành email thuộc tên miền đó, ví dụ `otp@tenmiencuaban.vn`.
Không dùng Gmail làm From của Resend vì bạn không sở hữu tên miền gmail.com để xác minh.
`oanhnguyen.410207@gmail.com` được giữ làm ReplyTo, không phải tài khoản SMTP.

Nguồn: [giới hạn resend.dev](https://resend.com/docs/knowledge-base/403-error-resend-dev-domain),
[xác minh tên miền](https://resend.com/docs/dashboard/domains/introduction),
[API gửi email](https://resend.com/docs/api-reference/emails/send-email).

## Triển khai

User-secrets dùng cho Development. Với Production/IIS, đặt biến môi trường:

| Biến | Giá trị |
| --- | --- |
| `EmailApi__ApiKey` | API key Resend |
| `EmailApi__From` | Email thuộc tên miền đã xác minh |
| `EmailApi__ReplyTo` | Email nhận phản hồi, có thể để trống |
| `PasswordReset__HashKey` | Khóa ngẫu nhiên ít nhất 32 ký tự |

Khởi động lại sau thay đổi. Các máy chủ cần cùng khóa HMAC; đổi khóa làm OTP đã cấp mất hiệu lực.
Các khóa `Smtp:*` và `Sms:*` cũ không còn được sử dụng.

## Luồng và vị trí code

| File | Chức năng |
| --- | --- |
| `Services/ResendMailSender.cs` | POST cố định tới Resend, Bearer API key, JSON email; kiểm tra phản hồi |
| `Services/PasswordRecovery.cs` | Tìm khách bằng email, cấp OTP, ký HMAC, giới hạn gửi và xác minh |
| `Models/PasswordRecoveryModels.cs` | Kiểm tra email, OTP, mật khẩu, cấu hình API; từ chối SMS cũ |
| `Controllers/RecoveryController.cs` | JSON hoặc PRG, lỗi tại cùng trang, đăng xuất sau đặt lại |
| `Views/Recovery` | Email, gửi/gửi lại mã, ô OTP số và mật khẩu mới |
| `wwwroot/js/password-recovery.js` | Gửi tại chỗ, chờ 60 giây, lọc OTP 6 số, xác nhận mật khẩu |
| `Services/NativeManagement.cs` | Đổi mật khẩu đã đăng nhập, yêu cầu mật khẩu hiện tại |
| `tests/PasswordChecks` | Database riêng và HTTP API giả lập, không gửi thư thật |

OTP áp dụng cho khách hàng. Đổi mật khẩu đã đăng nhập hỗ trợ cả khách và nhân viên/quản trị.
OTP có 6 chữ số, hạn 10 phút, tối đa 5 lần thử; cấp tối đa một lần/phút và năm lần/giờ/tài khoản.
Database chỉ giữ HMAC. Mã mới vô hiệu mã cũ; mã chưa được API tiếp nhận không được dùng.
API thành công có mã thư chỉ có nghĩa đã tiếp nhận, chưa bảo đảm thư đã vào hộp thư đến.

Thông báo chung không tiết lộ email có tài khoản hay không. Nếu không nhận thư, kiểm tra thư rác,
người nhận thử nghiệm, tên miền và nhật ký Resend. Ứng dụng không log OTP/API key; HTTP client timeout
15 giây, không theo redirect, không tự thử lại. Mật khẩu mới 8–100 ký tự, có chữ và số, xác nhận khớp.
Sau đổi mật khẩu, phiên hiện tại được xóa; cookie khác bị từ chối khi kiểm tra security stamp.

## Kiểm thử

```powershell
dotnet run --project tests/PasswordChecks
```

Runner tạo database `QOANH_Verification_Password_<ngau-nhien>` trên instance `.\MSSQLSERVER01`, chỉ xóa
fixture của nó và không sửa database cửa hàng. Đổi instance bằng `QOANH_TEST_SERVER` nếu cần.
Mail và HTTP đều giả lập; không xác nhận gửi thư với tài khoản Resend thật.
