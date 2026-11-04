# VAI TRÒ SCRIPT: Kiểm tra các bộ lọc danh sách theo tham số và nội dung trả về.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Read-only checks against the isolated verification host (port 5188)."""

import http.cookiejar
import json
import re
import urllib.parse
import urllib.request
from datetime import date, datetime, timedelta
from html import unescape
from html.parser import HTMLParser
from pathlib import Path


class Page(HTMLParser):
    def __init__(self, html):
        super().__init__()
        self.selects = {}
        self.current = None
        self.rows = []
        self.inputs = {}
        self.in_body = False
        self.row = None
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if tag == "select":
            self.current = a.get("name") or a.get("data-admin-row-filter")
            self.selects[self.current] = []
        if tag == "option" and self.current:
            self.selects[self.current].append(a)
        if tag == "input":
            self.inputs[a.get("name")] = a.get("value", "")
        if tag == "tbody":
            self.in_body = True
        if tag == "tr" and self.in_body:
            self.row = {"attrs": a, "text": ""}
            self.rows.append(self.row)

    def handle_endtag(self, tag):
        if tag == "select":
            self.current = None
        if tag == "tbody":
            self.in_body = False
        if tag == "tr":
            self.row = None

    def handle_data(self, text):
        if self.row is not None:
            self.row["text"] += text + " "


client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def get(path):
    with client.open("http://127.0.0.1:5188" + path, timeout=40) as response:
        return response.read().decode()


html = get("/Account/Login")
token = unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', html).group(1))
with client.open(
    "http://127.0.0.1:5188/Account/Login",
    urllib.parse.urlencode(
        dict(Email="admin@qoanh.local", Password="Admin@12345", __RequestVerificationToken=token)
    ).encode(),
    timeout=40,
) as response:
    assert "/Account/Login" not in response.geturl()
results = []


def check(name, action):
    try:
        action()
        results.append({"id": name, "status": "PASS"})
        print("PASS", name)
    except Exception as error:
        results.append({"id": name, "status": "FAIL", "error": str(error)})
        print("FAIL", name, repr(error))


def selectors(path, keys):
    page = Page(get(path))
    assert set(keys) <= page.selects.keys(), page.selects.keys()
    for row in page.rows:
        for key in keys:
            assert (
                "data-filter-" + key in row["attrs"]
                or key == "state"
                and "data-state" in row["attrs"]
            ), (key, row)


for path, keys in [
    ("/Admin/Promotions", ["state", "kind", "month"]),
    ("/Admin/Variants", ["product", "size", "color", "state", "stock"]),
    ("/Admin/Staff", ["state", "role"]),
    ("/Admin/Categories", ["usage"]),
    ("/Admin/Inventory", ["category", "stock"]),
    ("/Admin/VariantStock", ["warehouse", "product", "size", "color", "stock"]),
    ("/Admin/Receipts", ["warehouse", "supplier", "staff", "month"]),
    ("/Admin/Returns", ["state", "month"]),
    ("/Admin/Reviews", ["state", "stars", "product"]),
    ("/Admin/LoginHistory", ["actor", "result", "month"]),
    ("/Finance/Suppliers", ["supplier", "debt", "month"]),
    ("/Valuation", ["category", "cost", "stock"]),
    ("/Chat", ["unread", "month"]),
    ("/Admin/References?kind=brands", ["usage", "state"]),
    ("/Admin/References?kind=sizes", ["usage"]),
]:
    check("comboboxes " + path, lambda p=path, k=keys: selectors(p, k))


def server_lists():
    for path, keys in [
        ("/Admin/Products", ["category", "brandId", "state"]),
        ("/Admin/Customers", ["state", "hasOrders"]),
        ("/Admin/Orders", ["status", "payment", "period"]),
    ]:
        baseline = Page(get(path))
        assert set(keys) <= baseline.selects.keys()
        empty = Page(get(path + "?q=zzzz-no-match-filter-098765"))
        assert len(empty.rows) == 0
        assert len(Page(get(path)).rows) == len(baseline.rows)


check("server search and reset on 3 lists", server_lists)


def products():
    page = Page(get("/Admin/Products"))
    assert page.rows
    for state, label in [("active", "Đang bán"), ("inactive", "Đã ẩn")]:
        filtered = Page(get("/Admin/Products?state=" + state))
        assert all(label in r["text"] for r in filtered.rows)
    category = next(o["value"] for o in page.selects["category"] if o.get("value"))
    filtered = Page(
        get("/Admin/Products?" + urllib.parse.urlencode({"category": category, "state": "active"}))
    )
    assert all(category in r["text"] and "Đang bán" in r["text"] for r in filtered.rows)
    assert (
        len(Page(get("/Admin/Products?q=" + urllib.parse.quote("Áo"))).rows)
        == len(Page(get("/Admin/Products?q=ao")).rows)
        > 0
    )


check("products combined category/state and Vietnamese search", products)


def customers():
    yes = Page(get("/Admin/Customers?hasOrders=yes"))
    no = Page(get("/Admin/Customers?hasOrders=no"))
    all_rows = Page(get("/Admin/Customers"))
    assert len(yes.rows) + len(no.rows) == len(all_rows.rows)
    assert all(
        "Đang hoạt động" in r["text"] for r in Page(get("/Admin/Customers?state=active")).rows
    )


check("customer order history partition and state", customers)


def orders():
    for payment in ["COD", "Chuyển khoản", "Ví điện tử"]:
        page = Page(get("/Admin/Orders?" + urllib.parse.urlencode({"payment": payment})))
        assert all(payment in r["text"] for r in page.rows)


check("orders payment filters", orders)


def order_status_dates():
    baseline = Page(get("/Admin/Orders"))
    for option in baseline.selects["status"]:
        status = option.get("value", "")
        if status:
            filtered = Page(get("/Admin/Orders?" + urllib.parse.urlencode({"status": status})))
            assert all(status in r["text"] for r in filtered.rows)
    for period, days in [("today", 0), ("last7", 6), ("last30", 29)]:
        filtered = Page(get("/Admin/Orders?period=" + period))
        for row in filtered.rows:
            day = datetime.strptime(
                re.search(r"\d{2}/\d{2}/\d{4}", row["text"]).group(), "%d/%m/%Y"
            ).date()
            assert date.today() - timedelta(days=days) <= day <= date.today()


check("orders status and date boundaries", order_status_dates)


def periods():
    today = date.today()
    for path in ["/Admin/Reports", "/Finance"]:
        for period, start in [
            ("today", today),
            ("last7", today - timedelta(days=6)),
            ("last30", today - timedelta(days=29)),
            ("month", today.replace(day=1)),
        ]:
            page = Page(get(path + "?period=" + period))
            assert (
                page.inputs["from"] == start.isoformat() and page.inputs["to"] == today.isoformat()
            ), page.inputs
            assert any(o.get("value") == period and "selected" in o for o in page.selects["period"])


check("reports quick date ranges and selected combobox", periods)
Path("delivery/page-filter-results.json").write_text(
    json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
)
print(f'{sum(r["status"]=="PASS" for r in results)}/{len(results)} passed')
raise SystemExit(any(r["status"] == "FAIL" for r in results))
