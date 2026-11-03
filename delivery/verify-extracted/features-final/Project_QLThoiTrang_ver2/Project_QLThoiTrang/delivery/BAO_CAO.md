# Báo cáo bàn giao QOANH

Bản ASP.NET Core MVC .NET 8, EF Core SQL Server, cấu hình mặc định .\SQLEXPRESS và database QL_CuaHangThoiTrang. Tích hợp 27 bảng SQL gốc, 3 bảng mở rộng ban đầu và 7 bảng chức năng mới. Giữ dữ liệu SQL/JSON và giao diện QOANH, bổ sung các màn hình theo yêu cầu. Chi tiết cài đặt mới ở README.md; nâng cấp chức năng mới ở FEATURES.md.

## Chức năng đã có

- Đăng ký/đăng nhập, cookie, mật khẩu Identity hash, đổi mật khẩu, phân quyền Admin/Catalog/Inventory/Sales/Support/Customer. Admin và nhân viên không được mua hàng.
- Sản phẩm, biến thể, danh mục, thương hiệu, size, màu, kho, nhà cung cấp, khuyến mãi có điều kiện, khách hàng/nhân viên, đánh giá và kiểm duyệt.
- Giỏ/yêu thích của khách đăng nhập lưu SQL; đặt hàng kiểm tra giá, tồn theo biến thể/kho và token chống lặp; hủy hoàn tồn/lượt ưu đãi; trạng thái và lịch sử đơn.
- Nhập kho một/nhiều dòng, mỗi loại ở tab riêng; ghi phiếu và chi tiết nguyên tử, kiểm kê có tồn kỳ vọng. Trả hàng từng phần/toàn bộ, chống trả/hoàn vượt mức và duyệt trùng.
- COD và chuyển khoản bằng ảnh QR đã được người dùng cung cấp. Chỉ lấy vùng QR, không thêm tên/số tài khoản thành trường riêng. Chốt nội dung chuyển khoản theo đơn; Admin/Sales đối soát rồi ghi nhận đúng tiền với chứng từ duy nhất. Không tự xác nhận tiền khi quét QR.
- Chat khách hàng với Admin/Support, lưu SQL, cách ly hội thoại, trạng thái đọc, token chống gửi lặp và giới hạn tốc độ. Tự tải tin mới mỗi 3 giây.
- Quên mật khẩu khách hàng qua SMTP/OTP: mã HMAC, 10 phút, 5 lần thử, dùng một lần, chặn gửi dồn; đổi mật khẩu vô hiệu cookie cũ. Nhân viên tiếp tục do Admin đặt lại.
- Adapter GHN staging/production: tạo vận đơn và đồng bộ trạng thái, client_order_code cố định, chống gửi trùng, kiểm tra phần tiền cần thu; timeout giữ trạng thái chưa rõ để đối soát. Không tự thu tiền hoặc tự chuyển trạng thái nghiệp vụ theo thông báo giao hàng.
- PDF đơn hàng, phiếu nhập, phiếu trả: kiểm tra chủ sở hữu/quyền, Unicode, xuống dòng, phân trang. Là chứng từ nội bộ, không phải hóa đơn điện tử.
- Giá vốn bình quân di động theo biến thể, snapshot từng dòng đơn mới, đảo giá vốn khi trả hàng; báo cáo lợi nhuận gộp và cảnh báo thiếu giá vốn.
- Công nợ theo phiếu/nhà cung cấp, đối soát phiếu cũ, ghi chi từng phần, chặn vượt dư nợ/chứng từ trùng. Không tự chuyển tiền cho NCC.
- Menu quản trị 5 nhóm mở/thu gọn, sáng đúng mục; danh sách tách khỏi form thêm/sửa; lọc bảng; bảng phiếu nhập/trả và trang xử lý riêng. Đã sửa combobox địa chỉ hiển thị trùng và mã SP hiện nguyên biểu thức Razor.

## Transaction và bảo mật

Ghi qua StoreService dùng Serializable và khóa SQL QOANH.Store.Write, phối hợp nhiều connection. Các lần SaveChanges liên quan vẫn nằm trong cùng transaction; lỗi trước commit phải rollback. FK/check/unique bảo vệ dữ liệu cốt lõi, token và chứng từ. Các dịch vụ ngoài được gọi sau bước ghi nhận yêu cầu, không giữ transaction SQL trong lúc chờ mạng; GHN dùng mã yêu cầu cố định và lưu trạng thái Unknown khi chưa xác định kết quả.

Mật khẩu chỉ lưu hash. OTP chỉ lưu HMAC, không ghi mã/địa chỉ nhận vào log. SMTP password, OTP HashKey và GHN token cấu hình bằng user-secrets/biến môi trường, không nằm trong source. Chat hiển thị nội dung bằng textContent. Controller và tầng nghiệp vụ đều kiểm tra quyền cho thao tác tài chính/chuyên biệt. PDF không cho khách truy cập đơn/phiếu trả của người khác.

## Kết quả kiểm chứng

| Bộ kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Nghiệp vụ nền trên schema mới | 80/80 | business-check-results.json, features-business-regression.log |
| HTTP hồi quy | 64/64 | http-check-results.json, features-http-regression.log |
| Nghiệp vụ mở rộng | 31/31 | features-check-results.json, features-check.log |
| HTTP mở rộng | 23/23 | features-http-results.json, features-http.log |
| Build Release | 0 lỗi/cảnh báo | features-build.log |
| SQL 07 chạy lặp | Đạt trên database thử | features-upgrade-repeat.log |

Database thử: QOANH_Verification_Features_20261103_A trên instance SQL Server Developer. Các runner ghi thêm dữ liệu thử; database được giữ, không xóa. Email và GHN được giả lập trong ca tự động, không gửi email hoặc tạo vận đơn thật. Các nhóm log từ những lần bàn giao trước là lịch sử, không cộng dồn như một lượt chạy duy nhất. PDF mẫu được render và kiểm tra hình ảnh; kết quả cấu trúc ở pdf-check-results.json.

## Chưa hoàn thành hoặc chưa nghiệm thu

1. Chưa có thông tin SMTP/GHN thật; SMTP chưa gửi thật, GHN mặc định tắt. Cần cấu hình và kiểm thử với tài khoản dịch vụ. Không có VNPay/MoMo/webhook ngân hàng; đã chuyển sang QR thủ công theo yêu cầu.
2. Chưa nghiệm thu trên .\SQLEXPRESS, IIS/HTTPS, hai web host, tải lớn, sự cố mất SQL/deadlock giữa giao dịch và quy trình backup/restore thực tế. Cookie/Data Protection/session nhiều host cần cấu hình vận hành.
3. Chưa kiểm tra trực tiếp browser/responsive sau tất cả thay đổi. Computer Use trước đó dừng vì không xác định được URL Chrome đủ chắc chắn; không tiếp tục thao tác sau khi công cụ dừng. Chọn địa chỉ, thêm/bớt dòng nhập, chat polling và menu cần nghiệm thu trình duyệt thật; HTTP/Node không thay thế kiểm tra này.
4. Chat chưa có push khi đóng trang, đính kèm, chatbot hoặc SignalR; hộp thư lấy 200 hội thoại gần nhất. OTP chưa áp dụng cho tài khoản nhân viên; chưa có quản lý thời hạn lưu mã/log.
5. GHN chưa có báo giá cước, chọn địa chỉ từ API trên giao diện, webhook, hủy vận đơn từ ứng dụng, đối soát COD tự động hoặc nhiều kiện/từ 20 kg trở lên. Khi Unknown cần đồng bộ/đối soát, không đổi mã đơn để thử lại.
6. Chưa có hủy kèm hoàn tiền trước giao, kho hỏng, đổi size/hàng, thời hạn trả hoặc phí trả theo chính sách. Chuyển khoản đã gửi nhưng chưa ghi nhận cần xử lý đối soát bên ngoài. Hoàn tiền là ghi nhận chứng từ, không thực chuyển tiền.
7. Giá vốn đơn cũ chưa biết giữ null, không gán bằng 0. Chưa có điều chỉnh ngược lịch sử/sổ tái định giá đầy đủ, kế toán thuế, phân bổ chi phí vận hành hoặc công nợ trả NCC/bù trừ/hạn thanh toán. Báo cáo là lãi gộp theo ngày đặt, không phải lãi ròng hay báo cáo thu/chi kế toán.
8. Chưa tối ưu phân trang/truy vấn dữ liệu lớn; nhiều thao tác vẫn đọc toàn bộ bảng và dùng khóa ghi chung. Session khách vãng lai trong bộ nhớ. Chưa có EF migrations nâng cấp mọi schema tùy ý, chỉ có SQL nâng cấp đúng schema đã cung cấp.
9. Lịch sử biến động kho vẫn được lưu trong SQL nhưng bảng _StockHistory hiện chưa có trang gọi sau khi tách giao diện; chưa phục hồi trang tra cứu riêng trong đợt chức năng mới này.
10. Báo cáo Word gốc chưa cập nhật từ JSON sang EF Core, các vai trò và giao diện mới. Tài liệu Markdown bàn giao đã cập nhật; không coi Word cũ là mô tả bản triển khai cuối.

## Bất nhất dữ liệu mẫu và hạn chế dữ liệu

- Phiếu nhập mẫu 1/2/3 có tổng header 15.000.000 / 12.000.000 / 7.000.000, trong khi tổng chi tiết 16.500.000 / 18.250.000 / 5.600.000. Không tự sửa lịch sử; công nợ cũ yêu cầu đối soát. Phiếu mới tính từ chi tiết.
- Đơn mẫu có hoàn một áo 350.000 mặc dù giá phân bổ sau giảm là 315.000; giữ số nguồn, giới hạn các lần hoàn tiếp theo theo số thực còn lại. Một số đánh giá mẫu không khớp điều kiện đã giao; đánh giá mới có kiểm tra.
- JSON chỉ có tồn tổng: khi nhập giữ tổng và phân bổ vào size M, cần kiểm kê thực tế. Đơn lịch sử không có kho xuất sẽ dùng kho mặc định hoạt động khi nhận lại; đơn mới lưu kho xuất.
- Ảnh SQL không đầy đủ, storefront dùng các ảnh QOANH có sẵn; chưa có upload/multi-image/ảnh thật cho từng sản phẩm. Các danh mục có trạng thái dùng ngừng hoạt động, không xóa lịch sử liên kết.
- Một số lỗi hạ tầng còn hiển thị trang lỗi chung. Phiếu nhiều dòng sai dữ liệu giữ đúng tab nhưng chưa giữ đầy đủ mọi giá trị đã nhập. Không chứng nhận hết lỗi chỉ từ các ca đã đạt.
