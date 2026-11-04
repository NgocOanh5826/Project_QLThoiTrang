# VAI TRÒ SCRIPT: Kiểm tra nội dung trang chi tiết sản phẩm và các dữ liệu biến thể được render.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Verify detail markup and favorite round-trip on isolated test data."""

import http.cookiejar
import json
import re
import urllib.parse
import urllib.request
from html.parser import HTMLParser
from pathlib import Path

BASE = "http://127.0.0.1:5188"


class Page(HTMLParser):
    def __init__(self, html):
        super().__init__()
        self.forms = []
        self.current = None
        self.tags = []
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        self.tags.append((tag, a))
        if tag == "form":
            self.current = {**a, "inputs": {}}
            self.forms.append(self.current)
        if tag == "input" and self.current is not None:
            self.current["inputs"][a.get("name")] = a.get("value", "")

    def handle_endtag(self, tag):
        if tag == "form":
            self.current = None


def client():
    return urllib.request.build_opener(
        urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
    )


def request(c, path, data=None):
    with c.open(
        BASE + path, None if data is None else urllib.parse.urlencode(data).encode(), timeout=30
    ) as r:
        return r.geturl(), r.read().decode()


def login(c, email, password):
    _, html = request(c, "/Account/Login")
    f = next(f for f in Page(html).forms if f.get("action") == "/Account/Login")
    url, _ = request(c, "/Account/Login", dict(f["inputs"], Email=email, Password=password))
    assert "/Account/Login" not in url


results = []


def check(name, fn):
    try:
        fn()
        results.append({"id": name, "status": "PASS"})
        print("PASS", name)
    except Exception as e:
        results.append({"id": name, "status": "FAIL", "error": repr(e)})
        print("FAIL", name, repr(e))


guest = client()
customer = client()
admin = client()
login(customer, "khach@qoanh.local", "Khach@12345")
login(admin, "admin@qoanh.local", "Admin@12345")


def options():
    _, html = request(guest, "/Shop/Detail/1")
    page = Page(html)
    for key in [
        "data-product-price",
        "data-product-picker",
        "data-product-colors",
        "data-product-sizes",
    ]:
        assert any(key in a for _, a in page.tags)
    form = next(f for f in page.forms if f.get("action") == "/Shop/Add")
    assert form["inputs"]["__RequestVerificationToken"]
    assert any(
        tag == "option"
        and all(k in a for k in ["data-color", "data-size", "data-stock", "data-price"])
        for tag, a in page.tags
    )


check("separate options, price and post token", options)


def header():
    _, html = request(customer, "/Shop/Detail/1")
    header = re.search(r"<header\b[^>]*>(.*?)</header>", html, re.S).group(1)
    assert (
        header.index("/Shop/Wishlist")
        < header.index("/Shop/Cart")
        < header.index("/Account/Logout")
    )
    form = next(f for f in Page(header).forms if f.get("action") == "/Account/Logout")
    assert form.get("data-confirm")
    assert 'aria-label="Đăng xuất"' in header and "<span>Đăng xuất</span>" not in header


check("logout icon last with confirmation", header)


def favorite(c):
    def read():
        _, html = request(c, "/Shop/Detail/1")
        page = Page(html)
        button = next(
            a
            for tag, a in page.tags
            if tag == "button" and "product-favorite" in a.get("class", "")
        )
        form = next(f for f in page.forms if f.get("action") == "/Shop/Favorite")
        state = button["aria-pressed"] == "true"
        assert ("is-favorite" in button["class"]) == state
        return state, form

    initial, form = read()
    try:
        request(c, "/Shop/Favorite", form["inputs"])
        assert read()[0] != initial
        assert read()[0] != initial
    finally:
        state, form = read()
        if state != initial:
            request(c, "/Shop/Favorite", form["inputs"])
    assert read()[0] == initial


check("guest favorite selected/unselected after refresh", lambda: favorite(guest))
check("customer favorite selected/unselected after refresh", lambda: favorite(customer))


def staff():
    _, html = request(admin, "/Shop/Detail/1")
    assert not any("data-product-picker" in a for _, a in Page(html).tags)


check("admin still cannot purchase", staff)
Path("delivery/product-detail-results.json").write_text(
    json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
)
raise SystemExit(any(r["status"] == "FAIL" for r in results))
