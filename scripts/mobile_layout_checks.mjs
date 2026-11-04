// VAI TRÒ SCRIPT: Kiểm tra bố cục các trang ở kích thước màn hình di động, lưu kết quả/screenshot.
// VỊ TRÍ CHẠY: thư mục gốc dự án; đọc hằng BASE/đường dẫn/biến môi trường trong file trước khi
// chạy. Các bước
// kiểm tra có thể tạo dữ liệu thử, gửi HTTP và ghi báo cáo; đây không phải mã nhận form của
// website.

// Render temporary SSR fixtures in an isolated headless Chrome profile.
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
const pause = (ms) => new Promise((r) => setTimeout(r, ms));
let pages;
for (let i = 0; i < 100; i++) {
    try {
        pages = await (await fetch('http://127.0.0.1:9226/json/list')).json();
        break;
    } catch {
        await pause(100);
    }
}
if (!pages) throw Error('Headless Chrome not available');
const ws = new WebSocket(pages.find((p) => p.type === 'page').webSocketDebuggerUrl);
await new Promise((resolve, reject) => {
    ws.addEventListener('open', resolve, { once: true });
    ws.addEventListener('error', reject, { once: true });
});
let seq = 0;
const pending = new Map();
ws.addEventListener('message', (event) => {
    const msg = JSON.parse(event.data);
    if (msg.id && pending.has(msg.id)) {
        const p = pending.get(msg.id);
        pending.delete(msg.id);
        clearTimeout(p.timer);
        msg.error ? p.reject(Error(JSON.stringify(msg.error))) : p.resolve(msg.result);
    }
});
function cdp(method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = ++seq;
        const timer = setTimeout(() => {
            pending.delete(id);
            reject(Error('Timeout ' + method));
        }, 20000);
        pending.set(id, { resolve, reject, timer });
        ws.send(JSON.stringify({ id, method, params }));
    });
}
async function evaluate(expression) {
    const r = await cdp('Runtime.evaluate', { expression, returnByValue: true });
    if (r.exceptionDetails) throw Error(JSON.stringify(r.exceptionDetails));
    return r.result.value;
}
await cdp('Page.enable');
const results = [];
try {
    for (const name of ['cart', 'checkout', 'detail', 'account']) {
        for (const width of [320, 390, 768, 1440]) {
            await cdp('Emulation.setDeviceMetricsOverride', {
                width,
                height: 844,
                deviceScaleFactor: 1,
                mobile: width <= 600,
            });
            await cdp('Page.navigate', {
                url: pathToFileURL(
                    path.resolve('delivery/verify-extracted/mobile-' + name + '.html'),
                ).href,
            });
            for (let i = 0; i < 100; i++) {
                if ((await evaluate('document.readyState')) === 'complete') break;
                await pause(100);
            }
            await pause(500);
            const metrics = await evaluate(`(()=>{
    const rect=e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height,right:r.right,bottom:r.bottom}};
    const header=document.querySelector('.main-header');
    return {viewport:innerWidth,scrollWidth:document.documentElement.scrollWidth,header:rect(header),logo:rect(header.querySelector('.logo')),icons:[...header.querySelectorAll('.header-actions .header-icon')].map(rect),fields:[...document.querySelectorAll('.cart-edit-controls .filter-select-trigger,.cart-quantity input')].map(rect)};
   })()`);
            const errors = [];
            if (metrics.viewport !== width || metrics.scrollWidth > width + 1)
                errors.push('Horizontal overflow');
            if (width <= 600) {
                if (metrics.header.height > 200) errors.push('Header too tall');
                if (metrics.icons.some((r) => Math.abs(r.y - metrics.icons[0].y) > 1))
                    errors.push('Icons wrap');
                if (metrics.logo.right > metrics.icons[0].x) errors.push('Logo overlaps actions');
                if (metrics.fields.some((r) => r.right > width - 8 || r.x < 8))
                    errors.push('Cart controls overflow');
            }
            results.push({
                page: name,
                width,
                status: errors.length ? 'FAIL' : 'PASS',
                errors,
                metrics,
            });
            console.log(
                (errors.length ? 'FAIL' : 'PASS') +
                    ' ' +
                    name +
                    ' ' +
                    width +
                    'px (header ' +
                    Math.round(metrics.header.height) +
                    'px, document ' +
                    metrics.scrollWidth +
                    'px)',
            );
            if (width === 390 || (width === 320 && name === 'cart')) {
                const dimensions = await cdp('Page.getLayoutMetrics');
                const screenshot = await cdp('Page.captureScreenshot', {
                    format: 'png',
                    captureBeyondViewport: true,
                    clip: {
                        x: 0,
                        y: 0,
                        width,
                        height: Math.ceil(dimensions.cssContentSize.height),
                        scale: 1,
                    },
                });
                fs.writeFileSync(
                    'delivery/verify-extracted/mobile-' + name + '-' + width + '.png',
                    Buffer.from(screenshot.data, 'base64'),
                );
            }
        }
    }
} finally {
    fs.writeFileSync('delivery/mobile-layout-results.json', JSON.stringify(results, null, 2));
    try {
        await cdp('Browser.close');
    } catch {}
    ws.close();
}
if (results.some((r) => r.status !== 'PASS')) process.exitCode = 1;
