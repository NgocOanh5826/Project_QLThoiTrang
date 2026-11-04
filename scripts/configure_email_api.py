"""Thiết lập Resend API cho OTP; nhập API key ẩn và lưu vào .NET user-secrets."""

import getpass
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys


def main():
    """Lưu API key/địa chỉ gửi và tạo khóa OTP nếu thiếu; không tự gửi email."""
    if not sys.stdin.isatty():
        raise SystemExit("Hay chay script trong Terminal de nhap API key an.")
    project = Path(__file__).resolve().parents[1] / "Project_QLThoiTrang"
    secret_file = (
        Path(os.environ["APPDATA"])
        / "Microsoft/UserSecrets/b900d589-ded9-4055-94f6-b5bca7e712f8/secrets.json"
    )
    existing = (
        json.loads(secret_file.read_text(encoding="utf-8-sig"))
        if secret_file.exists()
        else {}
    )
    print("Resend API: khong can mat khau Gmail.")
    print("Dung onboarding@resend.dev chi gui thu toi email dang ky Resend.")
    print("Gui cho khach hang khac can dia chi thuoc ten mien da xac minh.")
    api_key = getpass.getpass("API key Resend (an, bat dau re_): ").strip()
    if not api_key.startswith("re_") or any(c.isspace() for c in api_key):
        raise SystemExit("API key khong hop le. Chua luu cau hinh.")
    sender = input("Email gui [onboarding@resend.dev]: ").strip() or "onboarding@resend.dev"
    if "@" not in sender or any(c.isspace() for c in sender):
        raise SystemExit("Nhap dia chi email gui, khong kem ten hien thi.")
    if sender.lower().endswith("@gmail.com"):
        raise SystemExit("Khong dung Gmail lam From cua Resend. Dung email ten mien da xac minh.")
    reset_key = existing.get("PasswordReset:HashKey", "")
    if not isinstance(reset_key, str) or len(reset_key) < 32:
        reset_key = secrets.token_urlsafe(48)

    # Truyền bí mật qua stdin; không đưa API key vào tham số dòng lệnh hoặc mã nguồn.
    result = subprocess.run(
        ["dotnet", "user-secrets", "set", "--project", str(project)],
        input=json.dumps(
            {
                "EmailApi:ApiKey": api_key,
                "EmailApi:From": sender,
                "EmailApi:ReplyTo": "oanhnguyen.410207@gmail.com",
                "PasswordReset:HashKey": reset_key,
            }
        ),
        text=True,
        capture_output=True,
        check=False,
    )
    if result.returncode:
        raise SystemExit("Khong luu duoc user-secrets. Kiem tra .NET SDK va quyen thu muc.")
    print("Da luu. Khoi dong lai website trong moi truong Development.")
    print("Chua gui thu. Thu form OTP voi email khach hang da dang ky va duoc Resend cho phep.")


if __name__ == "__main__":
    main()
