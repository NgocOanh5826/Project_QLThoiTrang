# Các chức năng mở rộng QOANH

## Nâng cấp database trước khi chạy bản mới

Khuyến nghị dùng `delivery/Upgrade-features.sql` thay cho gọi trực tiếp 07: wrapper sao lưu COPY_ONLY/CHECKSUM, RESTORE VERIFYONLY, lấy khóa nghiệp vụ và kiểm tra số bản ghi mọi bảng cũ không đổi. Nếu sao lưu/kiểm tra thất bại, dừng trước khi thay đổi schema. Quyền SQL cần đủ để backup và ALTER schema.

Connection string thực tế có thể bị `appsettings.Development.json`, user-secrets hoặc biến môi trường ghi đè. Máy hiện tại dùng `.\MSSQLSERVER01` trong Development; dùng đúng instance/database có hiệu lực. Ví dụ trên máy này:

```powershell
sqlcmd -S .\MSSQLSERVER01 -E -C -b -v DatabaseName="QL_CuaHangThoiTrang" -i delivery\Upgrade-features.sql
```

Ứng dụng không tự thay đổi schema khi khởi động. Nếu database chưa nâng cấp, trả trang bảo trì 503; trong Development có hướng dẫn script, Production chỉ có thông báo bảo trì. Sau nâng cấp, trạng thái được kiểm tra lại sau 10 giây.

Sao lưu database, dừng web và chạy từ thư mục solution:

```powershell
sqlcmd -S .\SQLEXPRESS -E -C -b -v DatabaseName="QL_CuaHangThoiTrang" -i delivery\07-customer-finance-features.sql
dotnet restore Project_QLThoiTrang.sln
dotnet build Project_QLThoiTrang.sln -c Release
```

Áp dụng cho database đã có schema gốc và extension 04. Nếu database mới hoàn toàn, `Deploy.sql` nay gọi cả script 07. Không chạy Deploy trên database có dữ liệu. Script 07 thêm 7 bảng và 3 cột nullable, không xóa bảng hoặc dữ liệu cũ; có thể chạy lặp. Bản mới có tổng 27 bảng gốc và 10 bảng QOANH bổ sung.

## Chuyển khoản bằng QR do người dùng cung cấp

Khách chọn COD hoặc Chuyển khoản tại checkout. QR được lấy nguyên điểm ảnh từ vùng mã trong hình người dùng gửi; không đưa phần tên/số tài khoản bên ngoài QR lên website hoặc vào trường cấu hình riêng. Mã nằm trong `wwwroot/images/payments/transfer-qr.png`. Không giải mã QR để lấy tên hay số tài khoản.

Mỗi đơn chuyển khoản chốt đường dẫn QR và một nội dung riêng bắt đầu bằng QO. QR là mã tĩnh, không tự điền số tiền/nội dung; khách nhập đúng thông tin được hiển thị theo đơn. Quét QR không tự xác nhận đã trả tiền. Admin/Sales kiểm tra tiền thực nhận, nhập mã giao dịch duy nhất rồi xác nhận; chống ghi nhận hai lần, sai tiền và chứng từ trùng. Đơn chuyển khoản chưa nhận đủ tiền không được chuyển sang đang giao hoặc tạo vận đơn.

Không có VNPay, MoMo hay webhook ngân hàng trong bản này theo yêu cầu chuyển sang chuyển khoản. Hoàn tiền tiếp tục là ghi nhận chứng từ đã hoàn bên ngoài, không tự chuyển tiền. Tình huống khách đã chuyển nhưng hủy trước khi cửa hàng ghi nhận cần cửa hàng đối soát và xử lý ngoài hệ thống; chưa có luồng hoàn tiền trước giao trong ứng dụng.

## Chat khách hàng và nhân viên

Khách đăng nhập mở Chat hỗ trợ; Admin/Support vào nhóm Khách hàng → Tin nhắn hỗ trợ. Lưu hội thoại/tin nhắn/đã đọc trong SQL; kiểm tra chủ hội thoại tại server, giới hạn độ dài và tốc độ gửi, token chống gửi lặp. Giao diện dùng textContent khi hiển thị nội dung và tự tải tin mới mỗi 3 giây; có xem tin cũ. Không dùng SignalR trong bản này, không có thông báo push khi đóng trang, tệp đính kèm hoặc chatbot.

## Quên mật khẩu bằng email OTP

`/Recovery/Forgot` gửi mã cho tài khoản Customer đang hoạt động; `/Recovery/Reset` xác thực mã và đặt mật khẩu mới. Mã ngẫu nhiên 6 số, hết hạn sau 10 phút, tối đa 5 lần thử, dùng một lần; chặn gửi dồn trong 60 giây và tối đa 5 yêu cầu/giờ/tài khoản. Database chỉ lưu HMAC của mã, dấu mật khẩu và thời gian; không ghi OTP vào log. Đổi mật khẩu làm cookie cũ mất hiệu lực. Câu trả lời công khai không xác nhận email có tồn tại hay không. Tài khoản nhân viên vẫn do Admin đặt lại mật khẩu.

SMTP chưa có thông tin kết nối thật nên mặc định chưa bật gửi. Cấu hình bằng user-secrets trong Development hoặc biến môi trường ở máy triển khai, không ghi mật khẩu vào source:

```text
Smtp__Host
Smtp__Port=587
Smtp__User
Smtp__Password
Smtp__From
Smtp__EnableSsl=true
PasswordReset__HashKey  (chuỗi bí mật ngẫu nhiên ít nhất 32 ký tự, giữ ổn định và bảo vệ như mật khẩu)
```

Ứng dụng yêu cầu SMTP có TLS. Cần kiểm tra email gửi thành công, thư rác, độ trễ và khôi phục trên máy đích. Các ca tự động dùng IMailSender giả, không gửi email thật.

## GHN

Admin/Sales mở đơn → Vận đơn GHN. Mặc định `Ghn:Enabled=false`. Cần cấu hình `Ghn__Enabled=true`, `Ghn__Token`, `Ghn__ShopId`. BaseUrl mặc định môi trường staging là `https://dev-online-gateway.ghn.vn/shiip/public-api/`; production dùng `https://online-gateway.ghn.vn/shiip/public-api/`. Chỉ cho phép hai địa chỉ này.

Hỗ trợ tạo và truy vấn trạng thái theo client_order_code cố định, lưu mã vận đơn, hạn chế tạo trùng; trường hợp timeout giữ trạng thái Unknown để đối soát thay vì tự coi là thất bại. Một lần từ chối sau timeout không được xóa trạng thái chưa rõ của lần gửi trước. Đơn có vận đơn đang hoạt động/không rõ kết quả bị chặn hủy trực tiếp. GHN báo giao thành công không tự đánh dấu đã thu tiền hay tự đổi trạng thái nghiệp vụ của đơn.

Biểu mẫu dùng tên tỉnh/phường theo danh mục GHN mới, trọng lượng dưới 20 kg, một kiện; người dùng xác nhận địa chỉ và kích thước. Shop trả phí GHN, phí đã báo khách trên đơn không tự đổi. COD gửi GHN bằng phần tiền còn thiếu, có kiểm tra lại tại thời điểm nhận yêu cầu tạo vận đơn. Chưa có tra cứu danh mục địa chỉ GHN trên giao diện, báo giá cước, webhook, hủy vận đơn từ ứng dụng hoặc xử lý nhiều kiện; việc hủy/đối soát ngoại lệ thực hiện tại GHN rồi đồng bộ. Chưa kiểm thử tài khoản GHN thật; ca tự động dùng HttpMessageHandler giả.

Tài liệu đối chiếu: [Tạo đơn GHN](https://developer.ghn.vn/vi/docs/order/create), [Tra cứu theo mã shop](https://developer.ghn.vn/vi/docs/order/info-by-client-code).

## PDF

Tải từ chi tiết đơn hàng, phiếu nhập hoặc phiếu trả. PDF có Unicode tiếng Việt, tự xuống dòng, phân trang và số trang. Khách chỉ tải đơn/phiếu trả của mình; Admin/Sales tải đơn/phiếu trả; Admin/Inventory tải phiếu nhập. Đây là chứng từ nội bộ, không phải hóa đơn điện tử hoặc hóa đơn thuế.

PDFsharp 6.2.2 dùng Arial/Arial Bold qua custom font resolver; mặc định lấy từ thư mục Fonts của Windows. Nếu cần vị trí khác, cấu hình `Pdf:FontDirectory` có arial.ttf và arialbd.ttf hợp pháp. Không đóng gói font của Windows vào ZIP. Đã render mẫu PDF thành ảnh để kiểm tra; ảnh và PDF mẫu nằm trong thư mục kiểm thử tạm, không đưa vào ZIP mã nguồn.

## Giá vốn, lợi nhuận và công nợ

- Kho hàng → Giá vốn tồn kho: khai báo giá vốn tồn đầu theo từng biến thể, kiểm tra tồn kỳ vọng và ghi căn cứ/người nhập. Bình quân di động theo biến thể trên toàn bộ kho; nhập thêm tính theo số lượng và đơn giá nhập. Hàng nhận lại dùng giá vốn chốt trên dòng đơn để cập nhật bình quân.
- Dòng đơn mới chốt giá vốn khi đặt; thay giá vốn hiện tại không sửa đơn đã chốt. Đơn cũ thiếu dữ liệu giữ giá vốn null; nhập hàng trên số tồn đầu chưa biết giá vốn không tự suy ra giá vốn cho toàn bộ tồn. Chưa có điều chỉnh ngược lịch sử hoặc sổ tái định giá đầy đủ; bảng giá vốn giữ căn cứ cập nhật gần nhất.
- Bán hàng → Lợi nhuận: tiền hàng sau giảm/hoàn trừ giá vốn số lượng còn bán. Thiếu giá vốn thì tổng lợi nhuận hiện chưa xác định, không giả định bằng 0. Kỳ theo ngày đặt, chưa phải báo cáo kế toán theo ngày thu/chi; chưa trừ phí vận chuyển, thuế, lương hoặc thuê mặt bằng.
- Bán hàng → Công nợ NCC: phiếu mới ghi số phải trả từ chi tiết; phiếu lịch sử cần đối soát vì dữ liệu mẫu có chênh lệch header/chi tiết. Có chi từng phần, chống vượt dư nợ, chứng từ duy nhất và transaction/khóa SQL. Chỉ ghi nhận tiền thực chi bên ngoài; chưa thực hiện chuyển tiền, hoàn/đổi trả NCC, bù trừ công nợ hoặc quản lý hạn thanh toán.

## Kiểm thử lại

Tạo database thử mới có tiền tố QOANH_Verification_ bằng Deploy.sql; đặt biến QOANH_TEST_CONNECTION trỏ đúng database thử. Chạy lần lượt:

```powershell
dotnet run --project tests\BusinessChecks -c Release
dotnet run --project tests\FeatureChecks -c Release
```

FeatureChecks dùng tài khoản demo/fixture của BusinessChecks và email/GHN giả, ghi thêm dữ liệu thử, không xóa database. Sau đó chạy web localhost:5188 với ConnectionStrings__Store trỏ database thử; chạy `python scripts/features_http_checks.py` và bộ hồi quy `python scripts/http_checks.py`. Bộ hồi quy kích hoạt giới hạn đăng nhập ở cuối; restart host trước lượt HTTP tiếp theo. Không chạy các runner trên dữ liệu thực.

Kết quả chính thức trong các JSON/log có tiền tố features-, business-check-results.json và http-check-results.json. Các log cũ là lịch sử các lần bàn giao trước, không cộng dồn như một lượt chạy duy nhất. Chưa nghiệm thu SMTP/GHN thật, SQL Express, IIS hoặc thao tác JavaScript trên trình duyệt thật.
