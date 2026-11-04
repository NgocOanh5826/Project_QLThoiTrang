# VAI TRÒ SCRIPT: Đối chiếu/tích hợp schema SQL gốc và các phần mở rộng theo dữ liệu đầu vào của dự
# án.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Read supplied SQL without executing its DROP DATABASE block. Generate typed EF mapping
and safe deployment copies. Only Python stdlib; original files remain untouched.
"""

from pathlib import Path
import re, json, hashlib, secrets, struct, base64

ROOT = Path(__file__).resolve().parent.parent
sql = Path(r"D:/HK5/ASP.NET_2/QL_CuaHangThoiTrang.sql").read_text(encoding="utf-8-sig")
extra = Path(r"D:/HK5/ASP.NET_2/Insert_QLThoiTrang.sql").read_text(encoding="utf-8-sig")
matches = list(re.finditer(r"CREATE TABLE (\w+)\s*\((.*?)\);\s*GO", sql, re.S | re.I))
types = []
config = []
info = []
extensions = {
    "SanPham": [
        ("QImage", "string?"),
        ("QIsNew", "bool"),
        ("QOnSale", "bool"),
        ("QLegacyId", "int?"),
    ],
    "KhachHang": [("QLegacyId", "int?")],
    "NhanVien": [("QLegacyId", "int?")],
    "DonHang": [("QPublicId", "string?"), ("QCheckoutToken", "string?"), ("QLegacyId", "string?")],
    "ChiTietDonHang": [("QImage", "string?"), ("QWarehouseId", "int?")],
    "PhieuNhap": [("QReference", "string?")],
    "PhieuTraHang": [("QReference", "string?")],
}


def quoted(s):
    return json.dumps(s, ensure_ascii=True)


for m in matches:
    name, body = m.groups()
    cols = []
    starts = (
        list(
            re.finditer(
                r"^\s*(\w+)\s+(INT|BIGINT|BIT|DATE(?:TIME2)?|DECIMAL\(\d+,\d+\)|N?VARCHAR\((?:MAX|\d+)\))\b?",
                body,
                re.M | re.I,
            )
        )
        if False
        else list(
            re.finditer(
                r"^\s*(\w+)\s+(INT\b|BIGINT\b|BIT\b|DATETIME2\b|DATE\b|DECIMAL\(\d+,\d+\)|N?VARCHAR\((?:MAX|\d+)\))",
                body,
                re.M | re.I,
            )
        )
    )
    for i, c in enumerate(starts):
        col, kind = c.groups()
        tail = (
            body[c.end() : starts[i + 1].start() if i + 1 < len(starts) else len(body)]
            .split("CONSTRAINT")[0]
            .strip()
            .rstrip(",")
        )
        nullable = not ("NOT NULL" in tail.upper() or "PRIMARY KEY" in tail.upper())
        cs = (
            "string"
            if "VARCHAR" in kind.upper()
            else (
                "decimal"
                if "DECIMAL" in kind.upper()
                else {
                    "INT": "int",
                    "BIGINT": "long",
                    "BIT": "bool",
                    "DATE": "DateTime",
                    "DATETIME2": "DateTime",
                }[kind.upper()]
            )
        )
        cols.append(
            dict(
                name=col,
                sql=kind,
                cs=cs + ("?" if nullable else ""),
                nullable=nullable,
                identity="IDENTITY" in tail.upper(),
                primary="PRIMARY KEY" in tail.upper(),
                tail=tail,
            )
        )
    composite = re.search(r"PRIMARY KEY\s*\(([^)]+)\)", body, re.I)
    keys = (
        [x.strip() for x in composite[1].split(",")]
        if composite
        else [c["name"] for c in cols if c["primary"]]
    )
    for c in cols:
        if c["name"] in keys:
            c["nullable"] = False
            c["cs"] = c["cs"].rstrip("?")
    props = [
        f' public {c["cs"]} {c["name"]} {{ get; set; }}' + (' = "";' if c["cs"] == "string" else "")
        for c in cols
    ]
    props += [f" public {typ} {col} {{ get; set; }}" for col, typ in extensions.get(name, [])]
    types.append("public class " + name + "Row\n{\n" + "\n".join(props) + "\n}")
    entity = f"b.Entity<{name}Row>()"
    key = (
        "x=>x." + keys[0] if len(keys) == 1 else "x=>new{" + ",".join("x." + k for k in keys) + "}"
    )
    config += [f'  {entity}.ToTable("{name}");', f"  {entity}.HasKey({key});"]
    for c in cols:
        line = f'  {entity}.Property(x=>x.{c["name"]})'
        if c["identity"]:
            line += ".UseIdentityColumn()"
        if "VARCHAR" in c["sql"].upper():
            length = re.search(r"\((\d+)\)", c["sql"])
            line += f".HasMaxLength({length[1]})" if length else ""
            if c["sql"].upper().startswith("VARCHAR"):
                line += ".IsUnicode(false)"
        if c["sql"] == "DATE":
            line += '.HasColumnType("date")'
        if not c["nullable"]:
            line += ".IsRequired()"
        default = re.search(
            r"DEFAULT\s+(SYSDATETIME\(\)|GETDATE\(\)|N?'[^']*'|\d+)", c["tail"], re.I
        )
        if default:
            line += ".HasDefaultValueSql(" + quoted(default[1]) + ")"
        config.append(line + ";")
        if "UNIQUE" in c["tail"].upper():
            filt = f'.HasFilter("[{c["name"]}] IS NOT NULL")' if c["nullable"] else ""
            config.append(f'  {entity}.HasIndex(x=>x.{c["name"]}).IsUnique(){filt};')
    for unique in re.finditer(r"CONSTRAINT\s+(\w+)\s+UNIQUE\s*\(([^)]+)\)", body, re.I):
        config.append(
            f"  {entity}.HasIndex(x=>new{{"
            + ",".join("x." + x.strip() for x in unique[2].split(","))
            + "}).IsUnique();"
        )
    for fk in re.finditer(
        r"FOREIGN KEY\s*\(([^)]+)\)\s*REFERENCES\s+(\w+)\s*\(([^)]+)\)(\s*ON DELETE CASCADE)?",
        body,
        re.I,
    ):
        fields = fk[1].split(",")
        key = (
            "x=>x." + fields[0].strip()
            if len(fields) == 1
            else "x=>new{" + ",".join("x." + x.strip() for x in fields) + "}"
        )
        config.append(
            f"  {entity}.HasOne<{fk[2]}Row>().WithMany().HasForeignKey({key}).OnDelete(DeleteBehavior."
            + ("Cascade" if fk[4] else "Restrict")
            + ");"
        )
    for ck in re.finditer(r"CONSTRAINT\s+(\w+)\s+CHECK\s*\(", body, re.I):
        start = ck.end()
        depth = 1
        idx = start
        in_string = False
        while depth:
            char = body[idx]
            if char == "'":
                in_string = not in_string
            if not in_string:
                depth += (char == "(") - (char == ")")
            idx += 1
        expr = body[start : idx - 1].strip()
        config.append(f'  {entity}.ToTable(t=>t.HasCheckConstraint("{ck[1]}",{quoted(expr)}));')
    info.append(dict(table=name, columns=cols, keys=keys))
    for col, typ in extensions.get(name, []):
        if typ.startswith("string"):
            config.append(
                f'  {entity}.Property(x=>x.{col}).HasMaxLength({64 if col=="QCheckoutToken" else 500});'
            )
config += [
    '  b.Entity<DonHangRow>().HasIndex(x=>x.QPublicId).IsUnique().HasFilter("[QPublicId] IS NOT NULL");',
    '  b.Entity<DonHangRow>().HasIndex(x=>x.QCheckoutToken).IsUnique().HasFilter("[QCheckoutToken] IS NOT NULL");',
    '  b.Entity<PhieuNhapRow>().HasIndex(x=>x.QReference).IsUnique().HasFilter("[QReference] IS NOT NULL");',
]
out = ROOT / "Project_QLThoiTrang/Data/Native"
out.mkdir(exist_ok=True)
(out / "Entities.cs").write_text(
    "// Generated from supplied QL_CuaHangThoiTrang.sql; no navigation-dependent queries.\nnamespace Project_QLThoiTrang.Data.Native;\n"
    + "\n".join(types)
    + "\n",
    encoding="utf-8",
)
(out / "Mapping.cs").write_text(
    "using Microsoft.EntityFrameworkCore;\nusing Project_QLThoiTrang.Data.Native;\nnamespace Project_QLThoiTrang.Data;\npublic partial class StoreDbContext\n{\n"
    + "\n".join(f" public DbSet<{m[1]}Row> {m[1]} => Set<{m[1]}Row>();" for m in matches)
    + "\n void MapNative(ModelBuilder b)\n {\n"
    + "\n".join(config)
    + "\n }\n}\n",
    encoding="utf-8",
)
(ROOT / "delivery/original-schema-map.json").write_text(
    json.dumps(info, ensure_ascii=False, indent=2), encoding="utf-8"
)


def secure(text):
    def hashpw(m):
        salt = secrets.token_bytes(16)
        key = hashlib.pbkdf2_hmac("sha512", m[1].encode(), salt, 100000, 32)
        return (
            "'"
            + base64.b64encode(b"\x01" + struct.pack(">III", 2, 100000, 16) + salt + key).decode()
            + "'"
        )

    return re.sub(r"'(123456|TEST_ONLY_123456)'", hashpw, text)


header = ":ON ERROR EXIT\nUSE [$(DatabaseName)];\nGO\nSET QUOTED_IDENTIFIER ON;\nSET ANSI_NULLS ON;\nSET ANSI_WARNINGS ON;\nSET ARITHABORT ON;\nSET ANSI_PADDING ON;\nSET CONCAT_NULL_YIELDS_NULL ON;\nSET NUMERIC_ROUNDABORT OFF;\nSET XACT_ABORT ON;\n"
schema = sql[sql.index("CREATE TABLE KhachHang") : sql.index("INSERT INTO KhachHang")]
schema = schema.replace("SoDienThoai VARCHAR(15) UNIQUE", "SoDienThoai VARCHAR(15)")
schema += "\nCREATE UNIQUE INDEX UX_KhachHang_Phone ON KhachHang(SoDienThoai) WHERE SoDienThoai IS NOT NULL;\nCREATE UNIQUE INDEX UX_NhanVien_Phone ON NhanVien(SoDienThoai) WHERE SoDienThoai IS NOT NULL;\nGO\n"
views = sql[sql.index("CREATE VIEW vw_DanhSachSanPham") : sql.index("SELECT * FROM KhachHang;")]
sample = secure(
    sql[sql.index("INSERT INTO KhachHang") : sql.index("CREATE VIEW vw_DanhSachSanPham")]
)
extra = secure(re.sub(r"^USE QL_CuaHangThoiTrang;\s*GO", "", extra, flags=re.I))
for filename, content in [
    ("01-schema.sql", schema),
    ("02-sample-data.sql", sample),
    ("03-Insert_QLThoiTrang.sql", extra),
    ("05-views-procedures.sql", views),
]:
    guard = (
        "IF EXISTS(SELECT 1 FROM sys.tables WHERE is_ms_shipped=0) THROW 51000,'Schema requires empty database; refusing overwrite.',1;\n"
        if filename.startswith("01")
        else ""
    )
    (ROOT / "delivery" / filename).write_text(
        header + guard + "BEGIN TRANSACTION;\nGO\n" + content + "\nCOMMIT;\nGO\n",
        encoding="utf-8-sig",
    )
print(
    f"Generated {len(matches)} native entity mappings and safe copies of both supplied SQL scripts."
)
