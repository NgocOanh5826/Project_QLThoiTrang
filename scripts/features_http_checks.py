# VAI TRÒ SCRIPT: Kiểm tra các endpoint chức năng mở rộng trên host/fixture thử nghiệm.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Run against localhost:5188 backed by the isolated feature-test database."""

import http.cookiejar, json, re, sys, traceback, urllib.request, urllib.parse, urllib.error, uuid
from pathlib import Path
from datetime import datetime, timezone
from html import unescape

BASE = "http://127.0.0.1:5188"
results = []
sys.stdout.reconfigure(encoding="utf-8")


def browser():
    return urllib.request.build_opener(
        urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
    )


def request(c, path, data=None):
    try:
        with c.open(
            BASE + path, None if data is None else urllib.parse.urlencode(data).encode(), timeout=40
        ) as r:
            return r.status, r.geturl(), r.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.geturl(), e.read().decode("utf-8")


def field(page, name):
    for tag in re.findall(r"<input\b[^>]*>", page):
        attrs = dict(re.findall(r'([\w-]+)="([^"]*)"', tag))
        if attrs.get("name") == name:
            return unescape(attrs.get("value", ""))
    raise AssertionError("Missing field " + name)


def post(c, path, page, data):
    return request(
        c, path, dict(data, __RequestVerificationToken=field(page, "__RequestVerificationToken"))
    )


def ensure(value):
    assert value


def check(name, fn):
    try:
        fn()
        results.append(dict(id=name, status="PASS", error=""))
        print("PASS", name)
    except Exception:
        results.append(dict(id=name, status="FAIL", error=traceback.format_exc()))
        print("FAIL", name, traceback.format_exc())


def login(c, email, password):
    _, _, page = request(c, "/Account/Login")
    status, url, _ = post(c, "/Account/Login", page, dict(Email=email, Password=password))
    assert status == 200 and "/Account/Login" not in url


admin, customer, other, guest = browser(), browser(), browser(), browser()
login(admin, "admin@qoanh.local", "Admin@12345")
login(customer, "khach@qoanh.local", "Khach@12345")
login(other, "an@gmail.com", "123456")
for path in [
    "/Finance",
    "/Finance/Suppliers",
    "/Finance/Receipt/1",
    "/Valuation",
    "/Valuation/Edit/1",
    "/Chat",
    "/Chat?customerId=1",
    "/Shipping?id=QH-DEMO01",
]:
    check("PAGE" + path, lambda p=path: ensure(request(admin, p)[0] == 200))
check("PAGE-customer-chat", lambda: ensure(request(customer, "/Chat")[0] == 200))
check(
    "AUTH-customer-finance-denied",
    lambda: ensure("/Account/Denied" in request(customer, "/Finance")[1]),
)
check(
    "AUTH-customer-shipping-denied",
    lambda: ensure("/Account/Denied" in request(customer, "/Shipping?id=QH-DEMO01")[1]),
)
check(
    "AUTH-customer-valuation-denied",
    lambda: ensure("/Account/Denied" in request(customer, "/Valuation")[1]),
)
check("AUTH-guest-chat-login", lambda: ensure("/Account/Login" in request(guest, "/Chat")[1]))
for path in ["/Recovery/Forgot", "/Recovery/Reset"]:
    check("PAGE" + path, lambda p=path: ensure(request(guest, p)[0] == 200))


def recovery_generic():
    _, _, page = request(guest, "/Recovery/Forgot")
    status, url, body = post(guest, "/Recovery/Forgot", page, dict(Email="unknown@example.test"))
    ensure(status == 200 and "/Recovery/Reset" in url)


check("OTP-generic-response-when-unconfigured", recovery_generic)


def chat():
    _, _, page = request(customer, "/Chat")
    client_token = uuid.uuid4().hex
    text = "HTTP chat " + client_token
    status, _, body = post(
        customer, "/Chat/Send", page, dict(customerId=999999, text=text, clientToken=client_token)
    )
    ensure(status == 200)
    message_id = json.loads(body)["id"]
    status, _, body = post(
        customer, "/Chat/Send", page, dict(customerId=999999, text=text, clientToken=client_token)
    )
    ensure(json.loads(body)["id"] == message_id)
    status, _, body = request(customer, "/Chat/Messages?customerId=999999")
    ensure(any(m["text"] == text for m in json.loads(body)))
    status, _, body = request(other, "/Chat/Messages?customerId=999999")
    ensure(not any(m["text"] == text for m in json.loads(body)))
    _, _, inbox = request(admin, "/Chat")
    ensure("/Chat?customerId=" in inbox)


check("CHAT-customer-conversation-isolation-and-idempotency", chat)
order_id = ""


def bank_order():
    global order_id
    _, _, page = request(customer, "/Shop/Detail/1")
    status, _, _ = post(customer, "/Shop/Add", page, dict(id=1, size="M", quantity=1))
    ensure(status == 200)
    _, _, page = request(customer, "/Shop/Checkout")
    ensure("Chuyển khoản" in unescape(page))
    status, url, page = post(
        customer,
        "/Shop/Checkout",
        page,
        dict(
            Name="Bank HTTP test",
            Phone="0901234567",
            Address="Verification address",
            Payment="Chuyển khoản",
            CheckoutToken=field(page, "CheckoutToken"),
        ),
    )
    ensure(status == 200 and "/Shop/Success/" in url)
    order_id = url.rsplit("/", 1)[1]
    ensure("/images/payments/transfer-qr.png" in page and 'class="transfer-reference"' in page)
    ensure('name="AccountNumber"' not in page and 'name="AccountName"' not in page)
    _, _, page = request(admin, "/Admin/Order/" + order_id)
    amount = field(page, "amount")
    status, _, body = post(
        admin,
        "/Admin/Payment",
        page,
        dict(id=order_id, amount=amount, reference="BANKHTTP-" + uuid.uuid4().hex),
    )
    ensure(status == 200 and "Đã thanh toán" in unescape(body))


check("BANK-QR-only-checkout-and-manual-reconciliation", bank_order)


def pdf(c, path, name):
    with c.open(BASE + path, timeout=40) as r:
        content = r.read()
        assert r.headers.get_content_type() == "application/pdf" and content.startswith(b"%PDF-")
    out = Path("delivery/verify-extracted/pdfs")
    out.mkdir(parents=True, exist_ok=True)
    (out / name).write_bytes(content)


check("PDF-order-owner", lambda: pdf(customer, "/Documents/Order/" + order_id, "order.pdf"))
check("PDF-receipt-inventory", lambda: pdf(admin, "/Documents/Receipt/1", "receipt.pdf"))
check("PDF-return-admin", lambda: pdf(admin, "/Documents/Return/1", "return.pdf"))
check(
    "PDF-other-customer-order-hidden",
    lambda: ensure(request(other, "/Documents/Order/" + order_id)[0] == 404),
)
check(
    "PDF-customer-receipt-denied",
    lambda: ensure("/Account/Denied" in request(customer, "/Documents/Receipt/1")[1]),
)
failed = sum(r["status"] == "FAIL" for r in results)
Path("delivery/features-http-results.json").write_text(
    json.dumps(
        dict(
            executedUtc=datetime.now(timezone.utc).isoformat(),
            total=len(results),
            failed=failed,
            results=results,
        ),
        ensure_ascii=False,
        indent=2,
    ),
    encoding="utf-8",
)
print(f"{len(results)-failed}/{len(results)} passed")
sys.exit(bool(failed))
