# VAI TRÒ SCRIPT: Kiểm tra tài khoản nhân viên bị chặn khỏi luồng mua hàng và các đường dẫn liên
# quan.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Run against the isolated verification web host on localhost:5188."""

import http.cookiejar
import json
import re
import urllib.parse
import urllib.request
from html import unescape
from pathlib import Path
from datetime import datetime, timezone

BASE = "http://127.0.0.1:5188"
results = []


def browser():
    return urllib.request.build_opener(
        urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
    )


def request(client, path, data=None):
    with client.open(
        BASE + path, None if data is None else urllib.parse.urlencode(data).encode(), timeout=40
    ) as response:
        return response.geturl(), response.read().decode("utf-8")


def token(page):
    tag = re.search(r'<input[^>]*name="__RequestVerificationToken"[^>]*>', page).group()
    return unescape(re.search(r'value="([^"]+)"', tag).group(1))


def login(client, email, password):
    _, page = request(client, "/Account/Login")
    url, _ = request(
        client,
        "/Account/Login",
        dict(Email=email, Password=password, __RequestVerificationToken=token(page)),
    )
    assert "/Account/Login" not in url


def check(name, condition):
    assert condition, name
    results.append(dict(id=name, status="PASS"))
    print("PASS", name)


admin, customer, guest = browser(), browser(), browser()
login(admin, "admin@qoanh.local", "Admin@12345")
_, page = request(admin, "/Shop/Detail/1")
check("ADMIN01-no-purchase-controls", 'action="/Shop/Add"' not in page and "cart-icon" not in page)
for path in ["/Shop/Cart", "/Shop/Checkout"]:
    check("ADMIN-denied-" + path, "/Account/Denied" in request(admin, path)[0])
for path, values in [
    ("/Shop/Add", dict(id=1, quantity=1, size="M")),
    ("/Shop/UpdateCart", dict(id=1, quantity=2, size="M")),
    ("/Shop/Checkout", dict(Name="Blocked admin")),
]:
    values["__RequestVerificationToken"] = token(page)
    check("ADMIN-post-denied-" + path, "/Account/Denied" in request(admin, path, values)[0])
check("ADMIN02-management-retained", "/Admin" in request(admin, "/Admin")[0])
login(customer, "khach@qoanh.local", "Khach@12345")
_, page = request(customer, "/Shop/Detail/1")
check("CUSTOMER01-purchase-controls", 'action="/Shop/Add"' in page and "cart-icon" in page)
check("CUSTOMER02-cart-available", "/Shop/Cart" in request(customer, "/Shop/Cart")[0])
check("GUEST01-purchase-controls", 'action="/Shop/Add"' in request(guest, "/Shop/Detail/1")[1])
Path("delivery/admin-purchase-results.json").write_text(
    json.dumps(
        dict(
            executedUtc=datetime.now(timezone.utc).isoformat(),
            database="QOANH_Verification_20261103_Review01",
            total=len(results),
            failed=0,
            results=results,
        ),
        indent=2,
    ),
    encoding="utf-8",
)
print(f"{len(results)}/{len(results)} passed")
