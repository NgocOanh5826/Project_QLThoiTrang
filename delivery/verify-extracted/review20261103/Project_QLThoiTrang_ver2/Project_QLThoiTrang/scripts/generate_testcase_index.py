import json
from pathlib import Path
root=Path(__file__).resolve().parent.parent
business=json.loads((root/'delivery/business-check-results.json').read_text())
http=json.loads((root/'delivery/http-check-results.json').read_text(encoding='utf-8'))
lines=['# Danh sách testcase đầy đủ QOANH','',
'## Môi trường và phương pháp','',
'SQL Server Developer instance mặc định, .NET SDK 10.0.202, target .NET 8. Database thử được tạo bằng delivery/Deploy.sql từ cả hai SQL gốc đã sửa an toàn. Chưa chạy trên SQLEXPRESS. Lệnh chạy ở README.', '',
'Mỗi ca nghiệp vụ chạy đúng thứ tự fixture trong tests/BusinessChecks/Program.cs; cần DB mới, đã có dữ liệu mẫu của cả hai SQL. Ca nhập JSON là một phần của runner, không import trước khi chạy. Runner giữ DB sau kiểm tra, không xóa/reset.', '',
'HTTP checks dùng cookie riêng, lấy CSRF token từ form và gọi thật các route MVC. Các ca PAGE kiểm tra HTTP 200/Razor render, không chứng minh JavaScript hoặc bố cục pixel. Ca chức năng kiểm tra phản hồi sau POST/redirect. Dữ liệu chi tiết trong scripts/http_checks.py.', '',
f'## {business["total"]} testcase nghiệp vụ — {business["failed"]} lỗi','',
f'Chạy UTC: {business["executedUtc"]}. Database: `{business["database"]}`.','',
'| Mã | Thao tác và kết quả mong đợi | Kết quả |','|---|---|---|']
for r in business['results']:lines.append(f'| {r["id"]} | {r["expected"]} | {r["status"]} |')
lines+=['',f'## {http["total"]} testcase HTTP — {http["failed"]} lỗi','',f'Chạy UTC: {http["executedUtc"]}. Bản MVC Release, database gốc đã chạy runner phía trên.','',
'| Mã | Thao tác và kết quả mong đợi | Kết quả |','|---|---|---|']
for r in http['results']:
 desc='Đăng nhập đúng vai trò và GET '+r['id'][4:]+': HTTP 200, Razor render không lỗi.' if r['id'].startswith('PAGE') else r['id'].split('-',1)[1].replace('-',' ')
 lines.append(f'| {r["id"]} | {desc} | {r["status"]} |')
lines+=['','## Kiểm tra build và SQL','',
'| Mã | Tiêu chí | Kết quả |','|---|---|---|',
'| BUILD01 | Solution build Release, 0 lỗi/cảnh báo | PASS |',
'| BUILD02 | Publish Release | PASS |',
'| SQLDEP01 | Deploy hai SQL gốc đã sửa an toàn, 27 bảng + 3 bảng mở rộng | PASS |',
'| SQLDEP02 | Chạy lặp 04-qoanh-extensions.sql không tạo lại/bổ sung số dư đã có | PASS |',
'| SOURCE01 | 126 tệp nguồn ZIP gốc khớp mã nền trước sửa | PASS |',
'| SOURCE02 | CSS/JS/ảnh và các trang bảo toàn khớp nguồn, chuẩn hóa CRLF khi so sánh text | Xem preservation-check.json |',
'| PACKAGE01 | CRC ZIP và đối chiếu byte từng file | Xem package-verification.json |',
'| PACKAGE02 | Build mã giải nén từ ZIP | Xem archive-build.log |',
'','## Kiểm thử chấp nhận chưa thực hiện','',
'Các ca dưới đây **CHƯA CHẠY**, không tính trong số PASS. Cần áp dụng phạm vi được hỗ trợ trong BAO_CAO.md.', '',
'| Mã | Bước thực hiện / tiêu chí | Trạng thái |','|---|---|---|']
manual=[
('M01','Máy sạch .\\SQLEXPRESS: Deploy/import/build/start; đăng nhập, đặt/thu/giao/trả; dữ liệu giữ sau restart.'),
('M02','Nghiệm thu trực tiếp với nhóm theo báo cáo Word và chính sách cửa hàng; xác nhận phân quyền/các nghiệp vụ mở rộng.'),
('M03','Browser desktop/mobile: kiểm tra QOANH, các form mới, ảnh, font, cuộn, menu và responsive; so sánh ảnh chụp.'),
('M04','Browser JavaScript: chọn tỉnh/xã/đường trong checkout, thêm/bớt nhiều dòng nhập, validation và submit đúng dữ liệu.'),
('M05','Dừng SQL/ngắt kết nối giữa những SaveChanges: không commit một phần; khởi động lại và đối chiếu kho/đơn/tiền.'),
('M06','Hai web host độc lập cùng DB, session phân tán: đặt/thu/hoàn đồng thời; không oversell/thu/hoàn trùng.'),
('M07','Gây deadlock/lock timeout: rollback và kiểm tra trang lỗi; không tự lặp giao dịch tiền bên ngoài.'),
('M08','Backup/restore trên Express rồi so sánh toàn bộ bảng và đăng nhập.'),
('M09','IIS identity riêng và HTTPS: quyền SQL, cookie, CSRF, Data Protection key persistence.'),
('M10','Nâng cấp bản sao DB gốc đã có dữ liệu thực bằng extension và --hash-passwords; kiểm tra hash cũ giữ nguyên, mật khẩu rõ đã được băm.'),
('M11','CRUD qua browser: xóa danh mục có SP bị chặn; thương hiệu/size/màu/NCC/kho, SKU và dữ liệu trùng/sai; lưu rồi refresh.'),
('M12','Mật khẩu chỉ chữ hoặc chỉ số, xác nhận khác: không đăng ký; đăng nhập quá hạn, đổi/reset mật khẩu làm cookie cũ mất hiệu lực.'),
('M13','Giỏ 0/21 sản phẩm, nhiều dòng cùng biến thể, >100 dòng, giá/ẩn SP thay đổi khi checkout: không tạo đơn sai.'),
('M14','Mã ưu đãi hết lượt/bắt đầu tương lai/ngày cuối/min/trần/sản phẩm hỗn hợp và hủy hoàn lượt: kiểm tra các biên chưa nằm trong runner.'),
('M15','Tamper role/CustomerId/VariantId/giá/tổng tiền qua request; HTML/script trong tên/ghi chú/lý do: không nâng quyền và Razor encode.'),
('M16','Báo cáo ngày đảo ngược, giảm giá, trả từng phần/nhiều đợt và dữ liệu mẫu lệch; đối chiếu CSV mở trong Excel.'),
('M17','JSON hỏng/FK/email trùng/hash không hợp lệ; import rollback, không xóa/sửa nguồn; dữ liệu thực tế vượt kích thước cột phải được báo rõ.'),
('M18','Hoàn từ nhiều nhân viên đồng thời, kho gốc bị ngừng hoạt động, hàng hỏng không nhập lại: xác nhận chính sách ngoài phạm vi.'),
('M19','Dữ liệu lớn/tải đồng thời: số truy vấn, thời gian khóa, throughput và dung lượng Express; chưa có cam kết hiệu năng.'),
('M20','Đối soát tiền thu/hoàn với chứng từ bên ngoài; hệ thống chỉ ghi nhận COD, không thực chuyển tiền.'),
('M21','Chạy lại toàn Deploy trên bản sao DB có dữ liệu: schema phải từ chối; không xóa/ghi đè.'),
]
for key,desc in manual:lines.append(f'| {key} | {desc} | CHƯA CHẠY |')
(root/'delivery/TESTCASES.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
