# VAI TRÒ SCRIPT: Kiểm tra tải tệp đánh giá multipart và quyền đọc media dựa trên fixture
# ReviewChecks riêng.
# VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi chạy.
# Các bước
# kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của website.

"""Multipart upload/access checks on the isolated host, using ReviewChecks fixture."""

import base64
import http.cookiejar
import json
import re
import urllib.error
import urllib.parse
import urllib.request
import uuid
from html.parser import HTMLParser
from pathlib import Path

BASE = "http://127.0.0.1:5188"
fixture = json.loads(Path("delivery/review-media-fixture.json").read_text())


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


def req(c, path, data=None, headers=None):
    try:
        with c.open(urllib.request.Request(BASE + path, data, headers or {}), timeout=40) as r:
            return r.status, r.geturl(), dict(r.headers), r.read()
    except urllib.error.HTTPError as e:
        return e.code, e.geturl(), dict(e.headers), e.read()


def html(c, path):
    return req(c, path)[3].decode()


def post(c, path, data):
    return req(c, path, urllib.parse.urlencode(data).encode())


def form(c, path, action):
    return next(f for f in Page(html(c, path)).forms if f.get("action") == action)


def login(c, email, password):
    f = form(c, "/Account/Login", "/Account/Login")
    status, url, _, _ = post(c, "/Account/Login", dict(f["inputs"], Email=email, Password=password))
    assert status == 200 and "/Account/Login" not in url


guest = client()
customer = client()
admin = client()
login(customer, fixture["email"], "Review@12345")
login(admin, "admin@qoanh.local", "Admin@12345")
path = "/Shop/Detail/" + str(fixture["productId"])
marker = "HTTP media " + uuid.uuid4().hex
png = base64.b64decode(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a5S8AAAAASUVORK5CYII="
)
mp4 = bytes(
    [
        0,
        0,
        0,
        24,
        102,
        116,
        121,
        112,
        105,
        115,
        111,
        109,
        0,
        0,
        0,
        0,
        105,
        115,
        111,
        109,
        109,
        112,
        52,
        50,
    ]
)


def upload(c, files, token=True):
    f = form(customer, path, "/Shop/Review")
    fields = {"id": str(fixture["productId"]), "stars": "3", "text": marker}
    if token:
        fields["__RequestVerificationToken"] = f["inputs"]["__RequestVerificationToken"]
    boundary = "QA" + uuid.uuid4().hex
    body = bytearray()
    for key, value in fields.items():
        body.extend(
            f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode()
        )
    for name, mime, content in files:
        body.extend(
            f'--{boundary}\r\nContent-Disposition: form-data; name="media"; filename="{name}"\r\nContent-Type: {mime}\r\n\r\n'.encode()
        )
        body.extend(content)
        body.extend(b"\r\n")
    body.extend(f"--{boundary}--\r\n".encode())
    return req(
        c,
        "/Shop/Review",
        bytes(body),
        {"Content-Type": "multipart/form-data; boundary=" + boundary},
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


def markup():
    p = Page(html(customer, path))
    stars = [a for tag, a in p.tags if tag == "input" and a.get("name") == "stars"]
    assert {a["value"] for a in stars} == {"1", "2", "3", "4", "5"} and all(
        a["type"] == "radio" for a in stars
    )
    f = next(f for f in p.forms if f.get("action") == "/Shop/Review")
    assert f["enctype"] == "multipart/form-data"
    assert any(tag == "input" and a.get("type") == "file" and "multiple" in a for tag, a in p.tags)


check("stars and multipart form", markup)
check(
    "upload requires anti-forgery token",
    lambda: ensure(upload(customer, [("a.png", "image/png", png)], False)[0] == 400),
)
check("upload guest redirects to login", lambda: ensure("/Account/Login" in upload(guest, [])[1]))


def invalid():
    assert marker not in html(guest, path)
    response = upload(customer, [("fake.png", "image/png", b"<script>bad</script>")])
    assert response[0] == 200 and marker not in html(guest, path)


check("fake image rejected without review", invalid)


def valid():
    response = upload(
        customer, [("photo.png", "text/html", png), ("clip.mp4", "application/octet-stream", mp4)]
    )
    assert response[0] == 200
    article = next(
        a for a in re.findall(r"<article\b.*?</article>", html(guest, path), re.S) if marker in a
    )
    tags = Page(article).tags
    global image_url, video_url
    image_url = next(a["src"] for tag, a in tags if tag == "img")
    video_url = next(a["src"] for tag, a in tags if tag == "video")
    assert any(a.get("aria-label") == "3 trên 5 sao" for _, a in tags)


check("valid multipart image and video stored", valid)


def media():
    status, _, headers, data = req(guest, image_url)
    assert status == 200 and headers.get("Content-Type") == "image/png" and data == png
    assert headers.get("X-Content-Type-Options") == "nosniff"
    status, _, headers, data = req(guest, video_url, headers={"Range": "bytes=0-7"})
    assert status == 206 and data == mp4[:8] and headers.get("Content-Type") == "video/mp4"


check("image bytes and video byte ranges", media)


def moderation():
    row = next(
        r for r in re.findall(r"<tr\b.*?</tr>", html(admin, "/Admin/Reviews"), re.S) if marker in r
    )
    f = next(f for f in Page(row).forms if f.get("action") == "/Admin/Moderate")
    try:
        post(admin, "/Admin/Moderate", dict(f["inputs"], active="false"))
        assert req(guest, image_url)[0] == 404 and req(customer, video_url)[0] == 404
        assert req(admin, image_url)[0] == 200 and marker not in html(guest, path)
    finally:
        post(admin, "/Admin/Moderate", dict(f["inputs"], active="true"))


check("hidden review media access and restore", moderation)
check(
    "unknown media is 404",
    lambda: ensure(req(guest, "/Shop/ReviewMedia/" + str(uuid.uuid4()))[0] == 404),
)
Path("delivery/review-media-http-results.json").write_text(
    json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
)
raise SystemExit(any(r["status"] == "FAIL" for r in results))
