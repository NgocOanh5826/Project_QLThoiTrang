// setupRecoveryForm: Đồng bộ email và xử lý gửi OTP ngay trên trang.
function setupRecoveryForm(form) {
    const contact = form.querySelector('[data-recovery-contact]');
    const submit = Array.from(form.elements).find((element) =>
        element.matches('[data-recovery-submit]'),
    );
    const unavailable = form.querySelector('[data-recovery-unavailable]');
    const inline = form.hasAttribute('data-inline-otp');
    const reset = inline
        ? form.closest('.recovery-card').querySelector('[data-inline-reset]')
        : null;
    const code = reset?.querySelector('[data-otp-code]');
    const status = form.querySelector('[data-recovery-status]');
    let sending = false;
    let remaining = Math.max(0, Number(form.dataset.recoveryWait) || 0);
    let deadline = Date.now() + remaining * 1000;
    let requested = remaining > 0;
    let timer;
    let previousContact = contact.value.trim();

    // updateSendButton: Cập nhật nút gửi mã và thời gian chờ gửi lại.
    function updateSendButton() {
        if (!submit) return;
        const ready = form.dataset.emailReady === 'true';
        remaining = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
        submit.disabled = !ready || sending || remaining > 0;
        submit.textContent = sending
            ? 'Đang gửi…'
            : remaining > 0
              ? `Gửi lại (${remaining}s)`
              : requested
                ? 'Gửi lại mã'
                : 'Gửi mã OTP';
        if (unavailable) unavailable.hidden = ready;
        if (remaining === 0) window.clearInterval(timer);
    }

    // syncContact: Đồng bộ email nhận mã sang form đặt lại mật khẩu.
    function syncContact() {
        if (!reset) return;
        reset.querySelector('[data-recovery-reset-contact]').value = contact.value.trim();
        const current = contact.value.trim();
        if (current !== previousContact) {
            if (code) code.value = '';
            if (status) status.hidden = true;
            deadline = 0;
            requested = false;
        }
        previousContact = current;
        updateSendButton();
    }

    contact.addEventListener('input', syncContact);
    syncContact();
    updateSendButton();
    if (remaining > 0) timer = window.setInterval(updateSendButton, 1000);
    if (!inline) return;

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (sending || submit.disabled || !form.reportValidity()) return;
        const body = new FormData(form);
        sending = true;
        contact.readOnly = true;
        form.setAttribute('aria-busy', 'true');
        updateSendButton();
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body,
                headers: { Accept: 'application/json' },
                credentials: 'same-origin',
            });
            const payload = await response.json();
            status.textContent = payload.message || 'Chưa thể gửi mã. Vui lòng thử lại.';
            status.hidden = false;
            if (response.ok) {
                requested = true;
                deadline = Date.now() + Math.max(60, Number(payload.resendAfter) || 60) * 1000;
                window.clearInterval(timer);
                timer = window.setInterval(updateSendButton, 1000);
                code?.focus();
            }
        } catch {
            status.textContent = 'Chưa thể gửi mã. Kiểm tra kết nối và thử lại.';
            status.hidden = false;
        } finally {
            sending = false;
            contact.readOnly = false;
            form.removeAttribute('aria-busy');
            updateSendButton();
        }
    });
}

// setupPasswordValidation: Gắn kiểm tra mật khẩu mới và xác nhận.
function setupPasswordValidation(form) {
    const password = form.querySelector('[name="NewPassword"], [name="Password"]');
    const confirm = form.querySelector('[name="ConfirmPassword"]');
    if (!password || !confirm) return;
    // validate: Kiểm tra độ mạnh và độ khớp của mật khẩu.
    function validate() {
        const value = password.value;
        password.setCustomValidity(
            value && (!/\p{L}/u.test(value) || !/[0-9]/.test(value))
                ? 'Mật khẩu cần có ít nhất một chữ cái và một chữ số.'
                : '',
        );
        confirm.setCustomValidity(
            confirm.value && confirm.value !== value ? 'Mật khẩu xác nhận chưa khớp.' : '',
        );
    }
    password.addEventListener('input', validate);
    confirm.addEventListener('input', validate);
}

document.querySelectorAll('[data-recovery-form]').forEach(setupRecoveryForm);
document.querySelectorAll('[data-password-validation]').forEach(setupPasswordValidation);

document.querySelectorAll('[data-otp-code]').forEach((input) => {
    input.addEventListener('input', () => {
        input.value = input.value.replace(/[^0-9]/g, '').slice(0, 6);
    });
    input.addEventListener('paste', (event) => {
        if (!event.clipboardData) return;
        event.preventDefault();
        input.value = event.clipboardData
            .getData('text')
            .replace(/[^0-9]/g, '')
            .slice(0, 6);
        input.dispatchEvent(new Event('input', { bubbles: true }));
    });
});
