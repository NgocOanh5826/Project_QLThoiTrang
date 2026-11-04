// VAI TRÒ SCRIPT: Dùng trình duyệt kiểm tra chọn màu/size/số lượng và cập nhật giỏ.
// VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi
// chạy. Các bước
// kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của
// website.

import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync('Project_QLThoiTrang/wwwroot/js/site.js', 'utf8');
const script = source.slice(
    source.indexOf('let cartSaving = false;'),
    source.indexOf("document.querySelectorAll('[data-review-form]')"),
);
function control(value = '') {
    return {
        value,
        events: {},
        options: [],
        addEventListener(k, f) {
            this.events[k] = f;
        },
        replaceChildren() {
            this.options = [];
        },
        append(option) {
            this.options.push(option);
        },
        setCustomValidity(error) {
            this.error = error;
        },
    };
}
const color = control('Đỏ'),
    sizes = control('2'),
    quantity = control('3');
quantity.max = '3';
const status = {};
let submits = 0;
const page = {
    classList: { add() {} },
    setAttribute() {},
    querySelector() {
        return status;
    },
};
const form = {
    dataset: {
        variants: JSON.stringify([
            { id: 1, color: 'Đỏ', size: 'S', stock: 0 },
            { id: 2, color: 'Đỏ', size: 'M', stock: 3 },
            { id: 3, color: 'Xanh', size: 'S', stock: 7 },
            { id: 4, color: 'Xanh', size: 'M', stock: 0 },
        ]),
    },
    events: {},
    querySelector(key) {
        return {
            '[data-cart-color]': color,
            '[data-cart-size]': sizes,
            '[name="quantity"]': quantity,
        }[key];
    },
    addEventListener(k, f) {
        this.events[k] = f;
    },
    closest() {
        return page;
    },
    reportValidity() {
        return (
            !sizes.error &&
            Number(quantity.value) >= 1 &&
            Number(quantity.value) <= Number(quantity.max)
        );
    },
    requestSubmit() {
        submits++;
    },
};
vm.runInNewContext(script, {
    document: {
        querySelectorAll() {
            return [form];
        },
    },
    Option: function (text, value) {
        return { text, value };
    },
});
color.value = 'Xanh';
color.events.change();
assert.equal(sizes.value, '3');
assert.equal(quantity.max, '7');
assert.equal(submits, 1);
assert(sizes.options.find((o) => o.value === '4').disabled);
quantity.value = '0';
quantity.events.change();
assert.equal(submits, 1);
quantity.value = '2';
quantity.events.change();
assert.equal(submits, 2);
form.events.submit({
    preventDefault() {
        throw Error('First submit blocked');
    },
});
assert.equal(status.textContent, 'Đang lưu giỏ hàng…');
let blocked = false;
form.events.submit({
    preventDefault() {
        blocked = true;
    },
});
assert(blocked);
quantity.events.change();
assert.equal(submits, 2);
console.log(
    'PASS: cart color/size, available fallback, quantity validation, auto-save and duplicate navigation guard',
);
