# VAI TRÒ SCRIPT: Kiểm tra HTTP sửa/gộp/xóa dòng giỏ, bao gồm dữ liệu biến thể và giới hạn tồn.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Read/write cart checks on the isolated local fixture; never place an order."""

import http.cookiejar, json, re, urllib.error, urllib.parse, urllib.request, os
from html.parser import HTMLParser
from html import unescape
from pathlib import Path

BASE = "http://127.0.0.1:5188"


class Page(HTMLParser):
    def __init__(self, html):
        super().__init__()
        self.forms = []
        self.tags = []
        self.current = None
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
    try:
        with c.open(
            BASE + path, None if data is None else urllib.parse.urlencode(data).encode(), timeout=40
        ) as r:
            return r.status, r.geturl(), r.read().decode()
    except urllib.error.HTTPError as e:
        return e.code, e.geturl(), e.read().decode()


def page(c, path):
    return Page(request(c, path)[2])


def login(c, email, password):
    f = next(f for f in page(c, "/Account/Login").forms if f.get("action") == "/Account/Login")
    status, url, _ = request(c, "/Account/Login", dict(f["inputs"], Email=email, Password=password))
    assert status == 200 and "/Account/Login" not in url


def edits(c):
    return [f for f in page(c, "/Shop/Cart").forms if "data-cart-edit" in f]


def add(c, product, variant, quantity=1):
    f = next(
        f for f in page(c, "/Shop/Detail/" + str(product)).forms if f.get("action") == "/Shop/Add"
    )
    status, _, _ = request(
        c, "/Shop/Add", dict(f["inputs"], id=product, variantId=variant, quantity=quantity)
    )
    assert status == 200


def update(c, f, **values):
    return request(c, "/Shop/UpdateCart", dict(f["inputs"], **values))


guest = client()
products = []
for product in range(1, 9):
    p = page(guest, "/Shop/Detail/" + str(product))
    variants = [a for t, a in p.tags if t == "option" and int(a.get("data-stock", "0")) >= 3]
    if variants:
        products.append((product, variants))
product, options = next((p, v) for p, v in products if len(v) >= 2)
first, second = map(lambda o: int(o["value"]), options[:2])
other = next(int(v[0]["value"]) for p, v in products if p != product)
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


add(guest, product, first)


def markup():
    p = page(guest, "/Shop/Cart")
    f = next(f for f in p.forms if "data-cart-edit" in f)
    assert json.loads(f["data-variants"]) and f["inputs"]["quantity"] == "1"
    remove = next(f for f in p.forms if f.get("class") == "cart-remove")
    assert remove.get("data-confirm") and remove["inputs"]["__RequestVerificationToken"]
    assert all(
        a.get("min") == "1"
        for t, a in p.tags
        if t == "input" and a.get("name") == "quantity" and a.get("type") == "number"
    )


check("cart auto-edit controls and delete confirmation", markup)


def quantity():
    update(guest, edits(guest)[0], quantity=2, newVariantId=first)
    assert edits(guest)[0]["inputs"]["quantity"] == "2"


check("quantity update", quantity)


def change():
    update(guest, edits(guest)[0], quantity=2, newVariantId=second)
    f = edits(guest)[0]
    assert int(f["inputs"]["variantId"]) == second and f["inputs"]["quantity"] == "2"


check("change size/color variant", change)


def merge():
    add(guest, product, first)
    rows = edits(guest)
    assert len(rows) == 2
    source = next(f for f in rows if int(f["inputs"]["variantId"]) == first)
    update(guest, source, quantity=1, newVariantId=second)
    rows = edits(guest)
    assert (
        len(rows) == 1
        and rows[0]["inputs"]["quantity"] == "3"
        and int(rows[0]["inputs"]["variantId"]) == second
    )


check("merge into existing variant", merge)


def reject_variant():
    for target in [other, 99999999]:
        update(guest, edits(guest)[0], quantity=1, newVariantId=target)
        f = edits(guest)[0]
        assert f["inputs"]["quantity"] == "3" and int(f["inputs"]["variantId"]) == second


check("cross-product and missing variant rejected", reject_variant)


def limit():
    update(guest, edits(guest)[0], quantity=21, newVariantId=second)
    assert edits(guest)[0]["inputs"]["quantity"] == "3"
    # A merging request must include the quantity already in the target line.
    add(guest, product, first)
    source = next(f for f in edits(guest) if int(f["inputs"]["variantId"]) == first)
    update(guest, source, quantity=20, newVariantId=second)
    assert len(edits(guest)) == 2


check("quantity and merged total limit", limit)
check(
    "cart update requires anti-forgery token",
    lambda: ensure(
        request(guest, "/Shop/UpdateCart", dict(id=product, variantId=second, quantity=1))[0] == 400
    ),
)


def delete():
    for f in [f for f in page(guest, "/Shop/Cart").forms if f.get("class") == "cart-remove"]:
        update(guest, f, remove="true")
    assert not edits(guest)


check("delete POST removes selected lines", delete)
fixture = json.loads(Path("delivery/review-media-fixture.json").read_text())
customer = client()
login(customer, fixture["email"], "Review@12345")
assert not edits(customer), "Expected empty isolated HTTP customer cart"
add(customer, product, first)
add(customer, product, second)


def persistent():
    another = client()
    login(another, fixture["email"], "Review@12345")
    assert len(edits(another)) == 2


check("customer cart persists across sessions", persistent)


def checkout():
    status, url, html = request(customer, "/Shop/Checkout")
    assert status == 200 and "/Shop/Checkout" in url
    p = Page(html)
    assert (
        len([a for tag, a in p.tags if tag == "div" and a.get("class") == "checkout-summary-item"])
        == 2
    )
    assert "checkout-order-summary" in html and "/Shop/Cart" in html
    assert all(
        o["data-color"] in unescape(html) and o["data-size"] in unescape(html) for o in options[:2]
    )
    if os.environ.get("QOANH_CAPTURE_MOBILE") == "1":
        folder = Path("delivery/verify-extracted")
        folder.mkdir(exist_ok=True)
        for name, body in [
            ("mobile-checkout", html),
            ("mobile-cart", request(customer, "/Shop/Cart")[2]),
            ("mobile-detail", request(customer, "/Shop/Detail/" + str(product))[2]),
            ("mobile-account", request(customer, "/Account")[2]),
        ]:
            body = body.replace("<head>", '<head><base href="http://127.0.0.1:5188/">', 1)
            body = re.sub(
                r'(<input[^>]*name="__RequestVerificationToken"[^>]*value=")[^"]*', r"\1", body
            )
            (folder / (name + ".html")).write_text(body, encoding="utf-8")


check("checkout summary thumbnails color size quantity", checkout)
for f in [f for f in page(customer, "/Shop/Cart").forms if f.get("class") == "cart-remove"]:
    update(customer, f, remove="true")
assert not edits(customer)
Path("delivery/cart-options-http-results.json").write_text(
    json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
)
raise SystemExit(any(r["status"] == "FAIL" for r in results))
