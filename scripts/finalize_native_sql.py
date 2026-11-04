# VAI TRÒ SCRIPT: Chuẩn bị script SQL triển khai theo schema gốc và các phần bổ sung; nội dung SQL
# trong chuỗi
# được giữ nguyên.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
model = (root / "delivery/native-model.generated.sql").read_text(encoding="utf-8-sig")
header = ":ON ERROR EXIT\nUSE [$(DatabaseName)];\nGO\nSET QUOTED_IDENTIFIER ON;\nSET ANSI_NULLS ON;\nSET ANSI_WARNINGS ON;\nSET ARITHABORT ON;\nSET ANSI_PADDING ON;\nSET CONCAT_NULL_YIELDS_NULL ON;\nSET NUMERIC_ROUNDABORT OFF;\nSET XACT_ABORT ON;\nBEGIN TRANSACTION;\nGO\n"
extra = []
for table in re.finditer(r"CREATE TABLE \[([^]]+)\] \((.*?)\);\s*GO", model, re.S):
    name, body = table.groups()
    if name.startswith("QOANH_"):
        extra.append(
            f"IF OBJECT_ID(N'dbo.{name}',N'U') IS NULL\nBEGIN\n"
            + table[0].rsplit("GO", 1)[0]
            + "\nEND;\nGO"
        )
    else:
        for line in body.splitlines():
            m = re.match(r"\s*\[(Q\w+)\]\s+(.+?)(?:,)?$", line)
            if m:
                definition = m[2].rstrip(",")
                if "NOT NULL" in definition:
                    definition += " DEFAULT 0 WITH VALUES"
                extra.append(
                    f"IF COL_LENGTH(N'dbo.{name}',N'{m[1]}') IS NULL ALTER TABLE [{name}] ADD [{m[1]}] {definition};\nGO"
                )
for index in re.finditer(r"CREATE (?:UNIQUE )?INDEX .*?;\s*GO", model, re.S):
    if "QOANH_" in index[0] or re.search(r"\[Q\w+\]", index[0]):
        name = re.search(r"INDEX \[([^]]+)\]", index[0])[1]
        table = re.search(r"ON \[([^]]+)\]", index[0])[1]
        extra.append(
            f"IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'{name}' AND object_id=OBJECT_ID(N'dbo.{table}'))\n"
            + index[0]
        )
extra.append(
    "IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_ThanhToan_MaGiaoDich' AND object_id=OBJECT_ID(N'dbo.ThanhToan')) CREATE UNIQUE INDEX UX_ThanhToan_MaGiaoDich ON ThanhToan(MaGiaoDich) WHERE MaGiaoDich IS NOT NULL;\nGO"
)
extra.append(
    "INSERT INTO QOANH_StockMovements(Id,ProductId,[Before],Delta,[After],Reason,Reference,Supplier,UnitCost,ActorId,Created) SELECT (SELECT COALESCE(MAX(Id),0) FROM QOANH_StockMovements)+ROW_NUMBER() OVER(ORDER BY p.MaSP),p.MaSP,0,COALESCE(s.Qty,0),COALESCE(s.Qty,0),N'Số dư kho từ hai script SQL gốc',CONCAT('SQL-OPEN-',p.MaSP),N'',0,NULL,SYSUTCDATETIME() FROM SanPham p LEFT JOIN (SELECT b.MaSP,SUM(t.SoLuongTon) Qty FROM TonKho t JOIN BienTheSanPham b ON b.MaBienThe=t.MaBienThe GROUP BY b.MaSP) s ON s.MaSP=p.MaSP WHERE NOT EXISTS(SELECT 1 FROM QOANH_StockMovements m WHERE m.ProductId=p.MaSP);\nGO"
)
(root / "delivery/04-qoanh-extensions.sql").write_text(
    header + "\n".join(extra) + "\nCOMMIT;\nGO\n", encoding="utf-8-sig"
)
# Preserve supplied views/procedures but make revenue deduct historical refunds.
p = root / "delivery/05-views-procedures.sql"
text = p.read_text(encoding="utf-8-sig")
replacement = """CREATE PROCEDURE sp_ThongKeDoanhThu @TuNgay DATE,@DenNgay DATE
AS
BEGIN
 SELECT CAST(d.NgayDat AS DATE) Ngay,COUNT(*) SoDonHang,
 SUM(d.TongThanhToan-COALESCE(r.Refund,0)) DoanhThu
 FROM DonHang d LEFT JOIN (SELECT MaDonHang,SUM(TongTienHoan) Refund FROM PhieuTraHang WHERE TrangThai IN(N'Đã hoàn tiền',N'Hoàn tất') GROUP BY MaDonHang) r ON r.MaDonHang=d.MaDonHang
 WHERE d.TrangThai IN(N'Đã giao',N'Hoàn thành') AND CAST(d.NgayDat AS DATE) BETWEEN @TuNgay AND @DenNgay
 GROUP BY CAST(d.NgayDat AS DATE) ORDER BY Ngay;
END;
GO"""
text = re.sub(r"CREATE PROCEDURE sp_ThongKeDoanhThu.*?\bGO\b", replacement, text, flags=re.S | re.I)
p.write_text(text, encoding="utf-8-sig")
(root / "delivery/Deploy.sql").write_text(
    r""":ON ERROR EXIT
-- SQLCMD mode, run from solution root; use -v DatabaseName="QL_CuaHangThoiTrang".
:r .\delivery\00-database.sql
:r .\delivery\01-schema.sql
:r .\delivery\02-sample-data.sql
:r .\delivery\03-Insert_QLThoiTrang.sql
:r .\delivery\04-qoanh-extensions.sql
:r .\delivery\05-views-procedures.sql
""",
    encoding="utf-8-sig",
)
print("Native SQL deployment finalized.")
