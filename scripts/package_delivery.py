# VAI TRÒ SCRIPT: Đóng gói source và kết quả kiểm tra vào ZIP, đối chiếu tệp được bảo toàn, tạo
# manifest/hash;
# không chạy tự động mỗi lần build.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Package source and review artifacts, excluding IDE/build/session data."""

from pathlib import Path
import hashlib, json, subprocess, zipfile

root = Path(__file__).resolve().parent.parent
delivery = root / "delivery"
prefix = "Project_QLThoiTrang_ver2/Project_QLThoiTrang/"


def sha(data):
    return hashlib.sha256(data).hexdigest()


protected = [p for p in (root / "Project_QLThoiTrang/wwwroot").rglob("*") if p.is_file()]
protected += [
    root / "Project_QLThoiTrang" / s
    for s in [
        "App_Data/store.json",
        "Views/Home/Index.cshtml",
        "Views/Shop/Index.cshtml",
        "Views/Account/Login.cshtml",
    ]
]
ui = []
for p in protected:
    rel = p.relative_to(root).as_posix()
    old = subprocess.run(["git", "show", "HEAD:" + rel], cwd=root, capture_output=True)
    if old.returncode != 0 and rel not in {
        "Project_QLThoiTrang/wwwroot/images/payments/transfer-qr.png",
        "Project_QLThoiTrang/wwwroot/js/chat.js",
    }:
        raise RuntimeError("Cannot compare original file " + rel)
    content = p.read_bytes()
    is_text = (
        p.suffix.lower()
        in {".css", ".js", ".html", ".cshtml", ".json", ".svg", ".txt", ".map", ".md"}
        or p.name == "LICENSE"
    )
    normalize = lambda data: data.replace(b"\r\n", b"\n") if is_text else data
    unchanged = normalize(old.stdout) == normalize(content)
    intentional = rel in {
        "Project_QLThoiTrang/wwwroot/css/screens.css",
        "Project_QLThoiTrang/wwwroot/js/site.js",
        "Project_QLThoiTrang/wwwroot/js/chat.js",
        "Project_QLThoiTrang/wwwroot/images/payments/transfer-qr.png",
        "Project_QLThoiTrang/Views/Account/Login.cshtml",
    }
    if not unchanged and not intentional:
        raise RuntimeError("Protected UI/data file changed: " + rel)
    ui.append(
        dict(
            path=rel,
            sha256=sha(content),
            unchangedFromGitHEAD=unchanged,
            intentionalChange=(
                "User requested administration, recovery, chat and QR payment UI changes"
                if intentional and not unchanged
                else None
            ),
            comparison="CRLF normalized" if is_text else "exact bytes",
        )
    )
(delivery / "preservation-check.json").write_text(
    json.dumps(ui, ensure_ascii=False, indent=2), encoding="utf-8"
)

files = []
for filename in ["README.md", "Project_QLThoiTrang.sln", ".gitignore", ".gitattributes"]:
    files.append(root / filename)
for folder in ["Project_QLThoiTrang", "tests", "scripts", "delivery"]:
    for p in (root / folder).rglob("*"):
        if not p.is_file():
            continue
        rel = p.relative_to(root)
        if any(
            part
            in {"bin", "obj", ".vs", ".git", "__pycache__", "publish-check", "verify-extracted"}
            for part in rel.parts
        ):
            continue
        if p.suffix in {".zip", ".user", ".pyc"} or p.name in {
            "source-manifest.json",
            "package-sha256.txt",
            "package-verification.json",
            "report-source.txt",
            "original-overwrite-verification.json",
        }:
            continue
        files.append(p)
files = sorted(set(files))
manifest = [
    dict(path=p.relative_to(root).as_posix(), bytes=p.stat().st_size, sha256=sha(p.read_bytes()))
    for p in files
]
(delivery / "source-manifest.json").write_text(
    json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
)
files.append(delivery / "source-manifest.json")
archive = delivery / "Project_QLThoiTrang_ver2_Updated.zip"
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=7) as z:
    for p in files:
        z.write(p, prefix + p.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert len(z.namelist()) == len(files)
    for p in files:
        assert z.read(prefix + p.relative_to(root).as_posix()) == p.read_bytes()
checksum = sha(archive.read_bytes())
(delivery / "package-sha256.txt").write_text(
    checksum + "  " + archive.name + "\n", encoding="ascii"
)
with zipfile.ZipFile(delivery / "SQL_Deployment.zip", "w", zipfile.ZIP_DEFLATED) as z:
    for name in [
        "Deploy.sql",
        "00-database.sql",
        "01-schema.sql",
        "02-sample-data.sql",
        "03-Insert_QLThoiTrang.sql",
        "04-qoanh-extensions.sql",
        "05-views-procedures.sql",
        "07-customer-finance-features.sql",
        "08-review-media.sql",
        "Upgrade-features.sql",
    ]:
        p = delivery / name
        z.write(p, "delivery/" + p.name)
    z.write(root / "README.md", "README.md")
summary = dict(
    archive=archive.name,
    files=len(files),
    bytes=archive.stat().st_size,
    sha256=checksum,
    zipCrc="PASS",
    sourceByteComparison="PASS",
    protectedFilesUnchanged=sum(x["unchangedFromGitHEAD"] for x in ui),
    intentionalUiChanges=sum(not x["unchangedFromGitHEAD"] for x in ui),
)
(delivery / "package-verification.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
print(json.dumps(summary, indent=2))
