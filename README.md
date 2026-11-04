# QOANH quản lý cửa hàng thời trang

**Quên/đổi mật khẩu:** OTP qua Resend email API, xác minh mã và đăng xuất sau đổi mật khẩu; đã bỏ SMS.
Xem [cấu hình email API và vị trí code](delivery/PASSWORD_RECOVERY.md).

**Đọc và tìm code:** [Hướng dẫn chức năng, điều kiện và vị trí thêm/sửa/xóa](HUONG_DAN_CODE.md). Source đã có comment tiếng Việt tại controller, service, model, form và JavaScript; tài liệu có chỉ mục hàm kèm đường dẫn/số dòng.

**Đánh giá ảnh/video:** thêm chọn 1–5 sao và đính kèm ảnh/video. Database cũ cần sao lưu rồi chạy `delivery/Upgrade-features.sql` (đã bao gồm 08) trước khi chạy bản mới; cài mới `Deploy.sql` tự gọi đủ. Hiện có 38 bảng. Media lưu trong SQL và đi theo backup database. Giới hạn tệp, cài đặt và testcase: [REVIEW_MEDIA.md](delivery/REVIEW_MEDIA.md).

**Lưu ý kết nối khi nâng cấp:** `appsettings.Development.json`, user-secrets và biến môi trường có thể ghi đè connection string trong `appsettings.json`. Trên máy hiện tại, Development dùng `.\MSSQLSERVER01` với database `QL_CuaHangThoiTrang`; mặc định của bản triển khai vẫn là `.\SQLEXPRESS`. Script `delivery/Upgrade-features.sql` sao lưu, xác minh backup và gọi nâng cấp 07 trong transaction, kiểm tra số bản ghi cũ không đổi. Chọn đúng instance đang có hiệu lực khi chạy script. Nếu schema chưa sẵn sàng, bản mới trả trang bảo trì HTTP 503 và tự kiểm tra lại sau 10 giây thay vì truy vấn các cột còn thiếu.

**Bản mở rộng mới:** chat hỗ trợ, khôi phục mật khẩu qua email API/OTP, chuyển khoản bằng QR đã cung cấp, PDF, giá vốn/lợi nhuận/công nợ và adapter GHN. **Database đang dùng phải chạy `delivery/07-customer-finance-features.sql` và `delivery/08-review-media.sql` trước khi mở bản mới (hoặc dùng wrapper `Upgrade-features.sql`).** Xem [hướng dẫn và giới hạn](delivery/FEATURES.md). SMTP/GHN chưa có thông tin kết nối thật và mặc định chưa hoạt động; chuyển khoản chỉ xác nhận sau đối soát thủ công.

Giao diện quản trị đã được cập nhật: menu/tab đang chọn được làm sáng; dùng nút **Thêm** hoặc **Chỉnh sửa** để mở form riêng. Trang danh sách có tìm kiếm trong bảng, lọc trạng thái ở các nhóm hỗ trợ và nút xóa bộ lọc. Chi tiết đường dẫn và kết quả kiểm thử trong `delivery/ADMIN_UI.md`. Sau khi cập nhật mã, khởi động lại web và nhấn Ctrl+F5 để tải CSS/JavaScript mới.

Bản ASP.NET Core MVC .NET 8 dùng EF Core SQL Server 8.0.23, tích hợp trực tiếp **27 bảng của hai script SQL được cung cấp**. Giữ tài nguyên và giao diện QOANH; bổ sung các trang phục vụ biến thể, kho, nhập hàng, nhân viên, đánh giá và trả hàng. Đã đọc báo cáo Word và lập đối chiếu trong `delivery/DOI_CHIEU_YEU_CAU.md`.

## Cài mới trên SQL Server Express

Yêu cầu: Windows, .NET SDK 8 trở lên hỗ trợ target `net8.0`, SQL Server Express instance `SQLEXPRESS`, SSMS hoặc sqlcmd. Tài khoản Windows cài đặt cần quyền tạo database/bảng. Khi chạy IIS, cấp quyền SQL cho đúng application pool identity.

1. Giải nén ZIP, mở PowerShell tại thư mục chứa `Project_QLThoiTrang.sln`, README và thư mục `delivery`. Sao lưu database/JSON đang dùng. Script cài mới sẽ **từ chối database đã có bảng**, không xóa database.
2. Khôi phục package và biên dịch:

   ```powershell
   dotnet restore .\Project_QLThoiTrang.sln
   dotnet build .\Project_QLThoiTrang.sln -c Release --no-restore
   ```

3. Triển khai schema và dữ liệu từ **cả hai SQL gốc**, bằng SQLCMD mode:

   ```powershell
   sqlcmd -S .\SQLEXPRESS -E -C -b -v DatabaseName="QL_CuaHangThoiTrang" -i delivery\Deploy.sql
   ```

   Nếu sqlcmd chưa có trong PATH, dùng đường dẫn đầy đủ tới SQLCMD.EXE. Với SSMS, bật **Query → SQLCMD Mode**, đặt biến `DatabaseName` và sửa đường dẫn `:r` theo thư mục giải nén. Khuyến nghị dùng lệnh sqlcmd ở trên để tránh sai thư mục làm việc.
4. Nhập thêm dữ liệu JSON cũ để giữ tài khoản, sản phẩm và đơn của website:

   ```powershell
   cd Project_QLThoiTrang
   dotnet run -c Release --no-launch-profile -- --import-json App_Data/store.json
   dotnet run -c Release --no-launch-profile --urls http://localhost:5188
   ```

5. Mở `http://localhost:5188`. Có thể đăng nhập bằng tài khoản JSON cũ hoặc tài khoản SQL mẫu.

Connection string mặc định trong `Project_QLThoiTrang/appsettings.json`:

```text
Server=.\SQLEXPRESS;Database=QL_CuaHangThoiTrang;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=False
```

Có thể ghi đè trong terminal chạy web/import:

```powershell
$env:ConnectionStrings__Store='Server=.\SQLEXPRESS;Database=QL_CuaHangThoiTrang;Trusted_Connection=True;TrustServerCertificate=True'
```

Chứng chỉ tự ký cục bộ dùng `TrustServerCertificate=True`; khi vận hành HTTPS/SQL trên máy khác cần cấu hình chứng chỉ phù hợp. Không ghi mật khẩu SQL vào source. Khởi động web bình thường không tự tạo, xóa hoặc seed dữ liệu.

## Các script bàn giao

`Deploy.sql` gọi lần lượt:

| Tệp | Nguồn và mục đích |
|---|---|
| `00-database.sql` | Tạo database nếu chưa có; không DROP |
| `01-schema.sql` | 27 bảng, khóa/check/index từ QL_CuaHangThoiTrang.sql; bỏ đoạn xóa database; phone nullable dùng unique filtered index |
| `02-sample-data.sql` | Dữ liệu mẫu từ script đầu; thay mật khẩu rõ bằng Identity V3 hash |
| `03-Insert_QLThoiTrang.sql` | Dữ liệu bổ sung từ Insert_QLThoiTrang.sql; mật khẩu đã băm |
| `04-qoanh-extensions.sql` | Các cột Q dùng cho mã đơn cũ/token/snapshot; 3 bảng cấu hình, lịch sử kho, ledger tiền (các tính năng mới ở script 07); số dư kho mở đầu; unique chứng từ |
| `05-views-procedures.sql` | Các view/procedure gốc; báo cáo doanh thu trừ hoàn tiền |
| `07-customer-finance-features.sql` | Chat, OTP, giá vốn, công nợ, vận đơn và snapshot chuyển khoản |

Không chạy lại file SQL gốc chưa chỉnh sửa: script đầu có DROP DATABASE và cả hai có mật khẩu rõ. Các bản triển khai đã điều chỉnh được cung cấp đầy đủ trong `delivery`.

Hai script gốc có tổng **20 khách, 20 nhân viên**, 8 sản phẩm, 4 đơn và các dữ liệu liên quan. Bản cập nhật giữ dữ liệu này, không trừ lại tồn theo các phiếu/đơn lịch sử. Một số số liệu mẫu gốc chưa khớp nghiệp vụ, được ghi trong báo cáo; không tự sửa lịch sử tiền của nguồn.

## Database đã có dữ liệu

Nếu database đang có đúng **27 bảng gốc**, không chạy `Deploy.sql`. Sao lưu và thực hiện ngoài thời gian chạy web:

1. Đối chiếu schema trước; chạy `04-qoanh-extensions.sql`, sau đó `07-customer-finance-features.sql` với biến DatabaseName. Script này có kiểm tra cột/bảng/index đã tồn tại và không tạo lại số dư kho đã có lịch sử.
2. Database chạy bằng SQL gốc có thể còn mật khẩu rõ. Sau khi thêm extension, từ thư mục project chạy:

   ```powershell
   dotnet run -c Release --no-launch-profile -- --hash-passwords
   ```

   Lệnh chỉ băm các giá trị chưa có dạng Identity hash, giữ nguyên mật khẩu người dùng và không in giá trị mật khẩu. Cần kiểm tra bản sao lưu và sao lưu SQL trước khi làm trên dữ liệu thật. Đường nâng cấp từ database gốc có dữ liệu thực tế **chưa được kiểm thử trên máy người dùng**; bản triển khai mới từ hai SQL và chạy lặp extension đã được kiểm thử.
3. Sau đó có thể nhập JSON một lần bằng `--import-json`. Nếu email/mã ưu đãi trùng nhưng dữ liệu không tương thích, toàn bộ lần nhập bị từ chối để tránh ghi đè.

Database của bản thử trước có các bảng `Products/Customers/Orders` tiếng Anh không phải schema 27 bảng gốc. Không chạy chồng script hoặc xóa DB đó; dùng database mới và giữ backup. Không có migration tự động từ schema thử tiếng Anh sang schema gốc trong bản này.

## Bảo toàn JSON và mã cũ

- JSON gốc trong `App_Data/store.json` không bị ghi đè. Sau nhập, SQL là nguồn dữ liệu chính; thay JSON không cập nhật dữ liệu web.
- Bản ghi JSON được thêm vào dữ liệu SQL mẫu, không thay thế bản ghi có cùng mã số. Mã sản phẩm/tài khoản JSON cũ được lưu ở `QLegacyId`; mã đơn QH được giữ ở `DonHang.QPublicId`. Khóa ngoại được ánh xạ sang mã SQL mới trong một transaction. Có SHA256 đánh dấu lần nhập; nhập lặp bị chặn.
- Tài khoản nhân viên lấy từ NhanVien, khách từ KhachHang. Claim nội bộ dùng ID âm cho nhân viên để tránh trùng với khách; khóa MaNV trong SQL vẫn là số dương.
- JSON chỉ có tồn tổng, không biết tồn từng size: khi nhập, tạo S/M/L/XL và phân bổ số dư vào size **M**. Tổng tồn giữ nguyên; phải kiểm kê và phân bổ lại theo hàng thực tế trước vận hành. Không chia đều một cách suy đoán.
- Đơn JSON đã giao không được suy luận là đã thu tiền. Nhân viên cần đối soát bằng chứng từ thực tế trước khi hoàn tiền.
- Ảnh gốc SQL có đường dẫn chưa tồn tại trong ZIP. Database giữ đường dẫn gốc; storefront dùng ảnh QOANH có sẵn để tránh ảnh lỗi.

## Tài khoản và phân quyền

Tài khoản chỉ dành cho dữ liệu mẫu:

| Email | Mật khẩu mẫu | Vai trò |
|---|---|---|
| admin@shop.com | 123456 | Admin |
| sanpham@shop.com | 123456 | Catalog |
| kho@shop.com | 123456 | Inventory |
| donhang@shop.com | 123456 | Sales |
| an@gmail.com | 123456 | Customer |
| admin@qoanh.local | Admin@12345 | Admin, sau nhập JSON |
| khach@qoanh.local | Khach@12345 | Customer, sau nhập JSON |

Các mật khẩu mẫu gốc được giữ để đăng nhập dữ liệu mẫu nhưng **chỉ hash được lưu vào SQL**. Đổi mật khẩu tại `/Account/Password` trước khi dùng thật. Admin tạo/khóa/đặt lại mật khẩu và gán quyền nhân viên tại `/Admin/Staff`. Không tự khóa/hạ quyền Admin đang dùng; bảo vệ Admin cuối cùng.

- Catalog: sản phẩm, biến thể, danh mục/thương hiệu/size/màu, khuyến mãi.
- Inventory: kho, nhà cung cấp, nhập hàng, kiểm kê, lịch sử phiếu.
- Sales: đơn hàng, COD, trả hàng/hoàn tiền, báo cáo.
- Support: khách hàng và kiểm duyệt đánh giá.
- Admin: toàn bộ chức năng, nhân viên, cấu hình và lịch sử đăng nhập.

Kiểm tra quyền cả controller và các nghiệp vụ chuyên biệt. Cookie được kiểm tra lại theo trạng thái/quyền/hash; khóa tài khoản, đổi quyền hoặc đổi mật khẩu làm phiên cũ bị từ chối. Đăng nhập giới hạn 20 POST/phút/IP; vượt trả 429. Lịch sử ghi thành công/thất bại với tài khoản xác định được, IP/thiết bị và đăng xuất; email không tồn tại không được tạo bản ghi giả vì FK gốc yêu cầu MaKH hoặc MaNV.

## Nghiệp vụ

- Chọn biến thể có size/màu/SKU/giá riêng. Đặt kiểm tra tồn theo biến thể ở các kho hoạt động, phân bổ theo mã kho tăng dần, lưu kho xuất trên dòng đơn. Giá và báo giá checkout được xác minh lại tại server. Mỗi biến thể tối đa 20 sản phẩm/đơn.
- Đặt đơn trừ kho ngay. Xác nhận/chuẩn bị/giao không trừ lần nữa. Hủy đơn chưa thu tiền trả đúng kho đã xuất, chống cộng lặp. Khách chỉ hủy đơn chờ xác nhận của mình.
- Trạng thái hỗ trợ Chờ xác nhận, Đã xác nhận, Đang chuẩn bị, Đang giao, Đã giao, Hoàn thành, Đã hủy; lịch sử nằm ở LichSuTrangThaiDonHang. Có thể đi tắt từ xác nhận sang đang giao để giữ luồng cũ.
- Thanh toán hỗ trợ **COD và chuyển khoản QR đối soát thủ công**. Chưa có cổng ngân hàng/ví hoặc webhook tự xác nhận. Nhân viên nhập chứng từ và đúng phần còn phải thu. Chặn chứng từ trùng cả ledger mới và ThanhToan gốc; phải thu đủ trước khi đánh dấu đã giao. Giao dịch thu/hoàn và trạng thái cập nhật nguyên tử trong SQL. Hệ thống không tự chuyển tiền.
- Nhập một/nhiều dòng trong cùng phiếu, cùng kho/nhà cung cấp. Tổng phiếu tính từ chi tiết, ghi người nhận và giá nhập. Dòng sai làm rollback toàn phiếu. Kiểm kê theo biến thể/kho cần lý do và tồn kỳ vọng; CRUD sản phẩm không chỉnh tồn.
- Trả từng phần hoặc toàn bộ phần còn lại. Chặn trả vượt số đã mua và chặn yêu cầu mới khi đang chờ. Duyệt nghĩa là đã nhận hàng có thể nhập lại và đã hoàn tiền bên ngoài, ghi chứng từ. Trả một phần phân bổ giảm giá; trả nốt hoàn phần tiền còn lại gồm phí giao hàng. Từ chối không đổi kho/tiền. Hàng lỗi không thể nhập lại chưa có luồng kho hỏng riêng.
- Ưu đãi hỗ trợ phần trăm/số tiền, đơn tối thiểu, trần giảm, ngày hiệu lực, số lượt và danh sách sản phẩm. Hủy đơn hoàn lại lượt đã giữ. Miễn phí giao từ 700.000 đ trước giảm; dưới ngưỡng 30.000 đ. Đơn lưu snapshot tiền.
- Khách đã mua/nhận hàng được đánh giá một lần/sản phẩm; Support/Admin ẩn/hiện. Giỏ/yêu thích của khách đăng nhập lưu trong các bảng gốc; giỏ khách vãng lai được hợp nhất khi đăng nhập. Giỏ được xóa cùng transaction tạo đơn; khách chưa đăng nhập vẫn dùng session.
- Mọi ghi dùng transaction Serializable và khóa SQL `sp_getapplock` chung giữa các connection. Có FK/check/unique index. Timeout/SQL lỗi chưa commit phải rollback; không tự lặp thao tác thu tiền bên ngoài.

## Kiểm thử và bàn giao

Xem `delivery/TESTCASES.md`, các JSON/log kết quả, `delivery/BAO_CAO.md` và `delivery/DOI_CHIEU_YEU_CAU.md`. Máy kiểm thử có SQL Server Developer instance mặc định `.`, **chưa có `.\SQLEXPRESS`**; cần chạy lại trên máy Express đích.

Từ thư mục solution, tạo database thử **mới mỗi lần** bằng chính bộ SQL:

```powershell
sqlcmd -S .\SQLEXPRESS -E -C -b -v DatabaseName="QOANH_Verification_MyRun01" -i delivery\Deploy.sql
$env:QOANH_TEST_CONNECTION='Server=.\SQLEXPRESS;Database=QOANH_Verification_MyRun01;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run --project tests\BusinessChecks -c Release
```

Runner từ chối tên không có tiền tố QOANH_Verification_ và DB đã nhập JSON; không reset/xóa dữ liệu. Sau test, chạy web bằng database thử đó trong terminal thứ hai (đặt lại biến nếu terminal mới):

```powershell
$env:ConnectionStrings__Store='Server=.\SQLEXPRESS;Database=QOANH_Verification_MyRun01;Trusted_Connection=True;TrustServerCertificate=True'
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project Project_QLThoiTrang -c Release --no-launch-profile --urls http://127.0.0.1:5188
```

Terminal đầu chạy `python scripts/http_checks.py` (Python 3, thư viện chuẩn). HTTP test ghi dữ liệu thử, khóa nhân viên thử và cuối cùng kích hoạt rate limit; chờ ít nhất một phút hoặc restart web trước khi chạy lại HTTP. Dừng web bằng Ctrl+C.

`scripts/integrate_original_schema.py` dùng để tái tạo mapping/bản SQL an toàn từ đường dẫn đầu vào gốc, không cần chạy khi cài đặt. `native-model.generated.sql` là đối chiếu mô hình, **không chạy thêm vào database đã Deploy**. Không dùng script tạo dữ liệu mẫu của bản bàn giao cũ.

Tham khảo kỹ thuật: [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions), [PasswordHasher](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1).

## Ki?m ch?ng l?i ng?y 03/11/2026

ZIP ??u v?o hi?n ?? ch?a b?n EF Core. L?n r? so?t n?y gi? ph?n ?? t?ch h?p, s?a l?nh qu?n tr? ?? kh?ng g?i EnsureCreated t?o schema thi?u r?ng bu?c SQL g?c, ??ng g?i SQL ch? g?m Deploy v? 00?05, r?i ch?y l?i ki?m th? tr?n database ri?ng `QOANH_Verification_20261103_Review01`. Log c? ti?n t? `review-`; k?t qu? ch?nh th?c trong `business-check-results.json`, `http-check-results.json` v? `TESTCASES.md`. Kh?ng thay d? li?u ?ng d?ng trong database th?t khi ki?m th?.
