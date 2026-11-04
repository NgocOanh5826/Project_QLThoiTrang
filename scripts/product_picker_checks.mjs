// VAI TRÒ SCRIPT: Dùng trình duyệt kiểm tra bộ chọn màu/size, giá, tồn và nút thêm giỏ trên trang
// chi tiết.
// VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi
// chạy. Các bước
// kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của
// website.

import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync('Project_QLThoiTrang/wwwroot/js/site.js', 'utf8');
const start = source.indexOf("document.querySelectorAll('[data-product-picker]')");
const end = source.indexOf("document.addEventListener('click'", start);
function element() {
    return {
        children: [],
        events: {},
        style: {},
        setAttribute(key, value) {
            this[key] = value;
        },
        prepend(item) {
            this.children.unshift(item);
        },
        append(...items) {
            this.children.push(...items);
        },
        replaceChildren() {
            this.children = [];
        },
        addEventListener(key, fn) {
            this.events[key] = fn;
        },
    };
}
const variants = [
    ['1', 'Đỏ', 'S', 0, 100000],
    ['2', 'Đỏ', 'M', 3, 150000],
    ['3', 'Xanh', 'S', 7, 200000],
    ['4', 'Xanh', 'M', 0, 220000],
    ['5', 'Vàng', 'XL', 0, 250000],
];
const select = {
    value: '2',
    options: variants.map(([id, color, size, stock, price]) => ({
        value: id,
        dataset: {
            color,
            colorHex: color === 'Đỏ' ? '#FF0000' : color === 'Xanh' ? '#0000FF' : '#FFFF00',
            size,
            stock: String(stock),
            price: String(price),
        },
    })),
};
const fields = Object.fromEntries(
    [
        '[data-product-colors]',
        '[data-product-sizes]',
        '[name="quantity"]',
        '[data-product-add]',
        '[data-product-stock]',
        '[data-product-price]',
        '[data-variant-fallback]',
        '[data-color-field]',
        '[data-size-field]',
    ].map((k) => [k, element()]),
);
fields['[data-product-variants]'] = select;
fields['[name="quantity"]'].value = '10';
const form = {
    querySelector: (key) => fields[key],
    closest: () => ({ querySelector: (key) => fields[key] }),
};
vm.runInNewContext(source.slice(start, end), {
    document: { querySelectorAll: () => [form], createElement: element },
    Intl,
});
const qty = fields['[name="quantity"]'],
    add = fields['[data-product-add]'];
function choice(key, value) {
    return fields[key].children
        .map((label) => label.children[0])
        .find((input) => input.value === value);
}
function change(key, value) {
    const input = choice(key, value);
    assert(input && !input.disabled);
    input.events.change();
}
assert.equal(select.value, '2');
assert.equal(qty.max, '3');
assert.equal(qty.value, '3');
assert.equal(fields['[data-variant-fallback]'].hidden, true);
assert.equal(fields['[data-color-field]'].hidden, false);
assert.equal(fields['[data-product-colors]'].children[0].children[1].textContent, 'Đỏ');
assert.equal(fields['[data-product-colors]'].children[0].children[1].children.length, 0);
assert.equal(fields['[data-product-sizes]'].children[0].children[1].children.length, 0);
change('[data-product-colors]', 'Xanh');
assert.equal(select.value, '3');
assert.equal(qty.max, '7');
assert.equal(choice('[data-product-sizes]', 'M').disabled, true);
assert.equal(choice('[data-product-sizes]', 'XL').disabled, true);
assert.equal(fields['[data-product-price]'].textContent, '200.000 đ');
change('[data-product-colors]', 'Đỏ');
assert.equal(select.value, '2');
change('[data-product-colors]', 'Vàng');
assert.equal(select.value, '5');
assert(add.disabled && qty.disabled);
change('[data-product-colors]', 'Xanh');
assert(!add.disabled && !qty.disabled);
assert.equal(select.value, '3');
console.log(
    'PASS: separate color/size, variant ID, stock limits, prices, unavailable combinations, sold-out color and recovery',
);
