from pathlib import Path
import json
p=Path('README.md');s=p.read_text(encoding='utf-8')
s=s.replace('# QOANH quản lý cửa hàng thời trang','# QOANH quản lý cửa hàng thời trang\n\n**Bản mở rộng mới:** chat hỗ trợ, khôi phục mật khẩu qua SMTP/OTP, chuyển khoản bằng QR đã cung cấp, PDF, giá vốn/lợi nhuận/công nợ và adapter GHN. **Database đang dùng phải chạy `delivery/07-customer-finance-features.sql` trước khi mở bản mới.** Xem [hướng dẫn và giới hạn](delivery/FEATURES.md). SMTP/GHN chưa có thông tin kết nối thật và mặc định chưa hoạt động; chuyển khoản chỉ xác nhận sau đối soát thủ công.',1)
s=s.replace('| `05-views-procedures.sql` | Các view/procedure gốc; báo cáo doanh thu trừ hoàn tiền |','| `05-views-procedures.sql` | Các view/procedure gốc; báo cáo doanh thu trừ hoàn tiền |\n| `07-customer-finance-features.sql` | Chat, OTP, giá vốn, công nợ, vận đơn và snapshot chuyển khoản |')
s=s.replace('1. Đối chiếu schema trước; chạy `04-qoanh-extensions.sql` với biến DatabaseName.','1. Đối chiếu schema trước; chạy `04-qoanh-extensions.sql`, sau đó `07-customer-finance-features.sql` với biến DatabaseName.')
s=s.replace('3 bảng cấu hình, lịch sử kho, ledger tiền','3 bảng cấu hình, lịch sử kho, ledger tiền (các tính năng mới ở script 07)')
s=s.replace('- Thanh toán là **ghi nhận COD thực tế**, chưa có cổng ngân hàng/ví.','- Thanh toán hỗ trợ **COD và chuyển khoản QR đối soát thủ công**. Chưa có cổng ngân hàng/ví hoặc webhook tự xác nhận.')
s=s.replace('## Kiểm chứng lại ngày 03/11/2026','## Kiểm chứng lại ngày 03/11/2026 (trước phần mở rộng mới)')
p.write_text(s,encoding='utf-8')
p=Path('tests/BusinessChecks/Program.cs');s=p.read_text(encoding='utf-8');s=s.replace('27 bảng gốc + 3 bảng mở rộng','27 bảng gốc + 10 bảng mở rộng');p.write_text(s,encoding='utf-8')
p=Path('scripts/generate_testcase_index.py');s=p.read_text(encoding='utf-8');s=s.replace('27 bảng + 3 bảng mở rộng','27 bảng + 10 bảng mở rộng')
s+='''
for filename,title in [('features-check-results.json','Nghiệp vụ mở rộng'),('features-http-results.json','HTTP mở rộng')]:
 p=root/'delivery'/filename
 if not p.exists():continue
 data=json.loads(p.read_text(encoding='utf-8'))
 with (root/'delivery/TESTCASES.md').open('a',encoding='utf-8') as target:
  target.write('\\n## '+title+'\\n\\n'+str(data['total'])+' ca, '+str(data['failed'])+' lỗi. Chi tiết đầu vào/kết quả mong đợi trong runner và FEATURES.md.\\n\\n| Mã | Kết quả |\\n|---|---|\\n')
  for r in data['results']:target.write('| '+r['id']+' | '+r['status']+' |\\n')
'''
p.write_text(s,encoding='utf-8')
