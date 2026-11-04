# VAI TRÒ SCRIPT: Kiểm tra nút đăng xuất, phương thức POST và trạng thái phiên sau thao tác.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Check logout markup and session behavior on the isolated local test host."""

import http.cookiejar
import json
import urllib.error
import urllib.parse
import urllib.request
from html.parser import HTMLParser
from pathlib import Path

BASE = "http://127.0.0.1:5188"


class Forms(HTMLParser):
    def __init__(self, html):
        super().__init__()
        self.forms = []
        self.current = None
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "form":
            self.current = {**attrs, "inputs": {}}
            self.forms.append(self.current)
        if tag == "input" and self.current is not None:
            self.current["inputs"][attrs.get("name")] = attrs.get("value", "")

    def handle_endtag(self, tag):
        if tag == "form":
            self.current = None


def client():
    return urllib.request.build_opener(
        urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
    )


def request(c, path, data=None):
    try:
        with c.open(
            BASE + path, None if data is None else urllib.parse.urlencode(data).encode(), timeout=30
        ) as r:
            return r.status, r.geturl(), r.read().decode()
    except urllib.error.HTTPError as e:
        return e.code, e.geturl(), e.read().decode()


def logout_forms(html):
    return [f for f in Forms(html).forms if f.get("action") == "/Account/Logout"]


def token(html):
    return next(
        f["inputs"]["__RequestVerificationToken"]
        for f in Forms(html).forms
        if "__RequestVerificationToken" in f["inputs"]
    )


results = []


def check(name, fn):
    try:
        fn()
        results.append({"id": name, "status": "PASS"})
        print("PASS", name)
    except Exception as e:
        results.append({"id": name, "status": "FAIL", "error": repr(e)})
        print("FAIL", name, repr(e))


def ensure(value):
    assert value


guest = client()
check("guest has no logout button", lambda: ensure(not logout_forms(request(guest, "/")[2])))
for role, email, password, path in [
    ("customer", "khach@qoanh.local", "Khach@12345", "/Account"),
    ("admin", "admin@qoanh.local", "Admin@12345", "/Admin"),
]:
    c = client()
    html = request(c, "/Account/Login")[2]
    code, url, _ = request(
        c,
        "/Account/Login",
        {"Email": email, "Password": password, "__RequestVerificationToken": token(html)},
    )
    assert code == 200 and "/Account/Login" not in url
    for page in ["/", path]:

        def markup(page=page, c=c):
            forms = logout_forms(request(c, page)[2])
            assert forms
            assert all(
                f.get("method", "").lower() == "post"
                and f.get("data-confirm") == "Bạn có chắc muốn đăng xuất không?"
                and f["inputs"].get("__RequestVerificationToken")
                for f in forms
            )

        check(role + " confirmation and anti-forgery " + page, markup)
    check(
        role + " GET does not sign out",
        lambda: ensure(
            request(c, "/Account/Logout")[0] == 405
            and "/Account/Login" not in request(c, "/Account")[1]
        ),
    )
    check(
        role + " POST without token rejected",
        lambda: ensure(
            request(c, "/Account/Logout", {})[0] == 400
            and "/Account/Login" not in request(c, "/Account")[1]
        ),
    )
    html = request(c, path)[2]

    def sign_out():
        status, url, body = request(
            c, "/Account/Logout", {"__RequestVerificationToken": token(html)}
        )
        assert status == 200 and not logout_forms(body)
        assert "/Account/Login" in request(c, "/Account")[1]

    check(role + " confirmed POST signs out", sign_out)
Path("delivery/logout-results.json").write_text(
    json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
)
print(f'{sum(r["status"]=="PASS" for r in results)}/{len(results)} passed')
raise SystemExit(any(r["status"] == "FAIL" for r in results))
