document.addEventListener('submit', function (event) {
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
});

let cartSaving = false;

document.querySelectorAll('[data-cart-edit]').forEach((form) => {
    const variants = JSON.parse(form.dataset.variants);
    const color = form.querySelector('[data-cart-color]');
    const sizes = form.querySelector('[data-cart-size]');
    const quantity = form.querySelector('[name="quantity"]');

    // save: Kiểm tra và gửi form cập nhật giỏ hàng.
    function save() {
        if (cartSaving) return;
        const variant = variants.find((v) => String(v.id) === sizes.value);
        sizes.setCustomValidity(
            variant?.stock > 0
                ? ''
                : 'Phiên bản này đã hết hàng. Vui lòng chọn màu hoặc kích cỡ khác.',
        );
        if (form.reportValidity()) form.requestSubmit();
    }
    color.addEventListener('change', () => {
        const previous = variants.find((v) => String(v.id) === sizes.value);
        const matches = variants.filter((v) => v.color === color.value);
        const selected =
            matches.find((v) => v.size === previous?.size && v.stock > 0) ||
            matches.find((v) => v.stock > 0) ||
            matches[0];
        sizes.replaceChildren();
        matches.forEach((v) => {
            const option = new Option(v.size + (v.stock <= 0 ? ' (hết hàng)' : ''), String(v.id));
            option.disabled = v.stock <= 0;
            sizes.append(option);
        });
        sizes.value = selected ? String(selected.id) : '';
        quantity.max = String(Math.max(1, Math.min(20, selected?.stock || 0)));
        quantity.value = String(
            Math.max(1, Math.min(Number(quantity.value) || 1, Number(quantity.max))),
        );
        save();
    });
    sizes.addEventListener('change', () => {
        const variant = variants.find((v) => String(v.id) === sizes.value);
        quantity.max = String(Math.max(1, Math.min(20, variant?.stock || 0)));
        quantity.value = String(
            Math.max(1, Math.min(Number(quantity.value) || 1, Number(quantity.max))),
        );
        save();
    });
    quantity.addEventListener('change', save);
    form.addEventListener('submit', (event) => {
        if (cartSaving) {
            event.preventDefault();
            return;
        }
        cartSaving = true;
        const page = form.closest('.cart-page');
        page.classList.add('is-saving');
        page.setAttribute('aria-busy', 'true');
        page.querySelector('[data-cart-save-status]').textContent = 'Đang lưu giỏ hàng…';
    });
});

document.querySelectorAll('[data-review-form]').forEach((form) => {
    const input = form.querySelector('[data-review-upload]');
    const preview = form.querySelector('[data-review-preview]');
    let urls = [];

    // clear: Xóa phần xem trước và thu hồi URL tệp tạm.
    function clear() {
        urls.forEach((url) => URL.revokeObjectURL(url));
        urls = [];
        preview.replaceChildren();
    }
    input.addEventListener('change', () => {
        clear();
        const files = Array.from(input.files);
        const video = (file) => /\.(mp4|webm)$/i.test(file.name);
        let error = '';
        if (
            files.length > 5 ||
            files.filter(video).length > 1 ||
            files.reduce((sum, file) => sum + file.size, 0) > 30 * 1024 * 1024
        )
            error = 'Tối đa 5 tệp, chỉ 1 video và tổng không quá 30 MB.';
        else if (
            files.some(
                (file) =>
                    !/\.(jpe?g|png|gif|webp|mp4|webm)$/i.test(file.name) ||
                    !file.size ||
                    file.size > (video(file) ? 20 : 5) * 1024 * 1024,
            )
        )
            error = 'Chọn ảnh JPG/PNG/GIF/WebP tối đa 5 MB hoặc video MP4/WebM tối đa 20 MB.';
        input.setCustomValidity(error);
        if (error) {
            preview.textContent = error;
            input.reportValidity();
            return;
        }
        files.forEach((file) => {
            const media = document.createElement(video(file) ? 'video' : 'img');
            const url = URL.createObjectURL(file);
            urls.push(url);
            media.src = url;
            if (video(file)) {
                media.controls = true;
                media.preload = 'metadata';
            } else media.alt = 'Xem trước ảnh đánh giá';
            preview.append(media);
        });
    });
    window.addEventListener('pagehide', clear);
});

document.querySelectorAll('[data-product-picker]').forEach((form) => {
    const select = form.querySelector('[data-product-variants]');
    const variants = Array.from(select.options).map((option) => ({
        id: option.value,
        color: option.dataset.color,
        size: option.dataset.size,
        stock: Number(option.dataset.stock),
        price: Number(option.dataset.price),
    }));
    if (!variants.length) return;
    const colors = form.querySelector('[data-product-colors]');
    const sizes = form.querySelector('[data-product-sizes]');
    const quantity = form.querySelector('[name="quantity"]');
    const add = form.querySelector('[data-product-add]');
    const stock = form.querySelector('[data-product-stock]');
    const price = form.closest('.detail-copy').querySelector('[data-product-price]');
    let chosen =
        variants.find((v) => v.id === select.value) ||
        variants.find((v) => v.stock > 0) ||
        variants[0];

    // update: Cập nhật giá, tồn kho và trạng thái nút mua.
    function update() {
        select.value = chosen.id;
        const max = Math.min(20, chosen.stock);
        quantity.max = String(max);
        quantity.disabled = max <= 0;
        quantity.value = String(Math.max(1, Math.min(max || 1, Number(quantity.value) || 1)));
        add.disabled = max <= 0;
        add.textContent = max > 0 ? 'Thêm vào giỏ hàng' : 'Tạm hết hàng';
        stock.textContent = `Còn ${chosen.stock} sản phẩm · ${chosen.color} · ${chosen.size}`;
        price.textContent = new Intl.NumberFormat('vi-VN').format(chosen.price) + ' đ';
    }

    // choice: Tạo lựa chọn màu hoặc kích cỡ sản phẩm.
    function choice(container, name, value, checked, disabled, change) {
        const label = document.createElement('label');
        const input = document.createElement('input');
        input.type = 'radio';
        input.name = name;
        input.value = value;
        input.checked = checked;
        input.disabled = disabled;
        input.addEventListener('change', change);
        const text = document.createElement('span');
        text.textContent = value;
        label.append(input, text);
        container.append(label);
    }

    // renderSizes: Hiển thị size phù hợp với màu đang chọn.
    function renderSizes() {
        sizes.replaceChildren();
        Array.from(new Set(variants.map((v) => v.size))).forEach((size) => {
            const variant = variants.find((v) => v.color === chosen.color && v.size === size);
            choice(
                sizes,
                'productSize',
                size,
                chosen.size === size,
                !variant || variant.stock <= 0,
                () => {
                    chosen = variant;
                    update();
                },
            );
        });
    }
    Array.from(new Set(variants.map((v) => v.color))).forEach((color) => {
        choice(colors, 'productColor', color, chosen.color === color, false, () => {
            const matches = variants.filter((v) => v.color === color);
            chosen =
                matches.find((v) => v.size === chosen.size && v.stock > 0) ||
                matches.find((v) => v.stock > 0) ||
                matches[0];
            renderSizes();
            update();
        });
    });
    renderSizes();
    update();
    form.querySelector('[data-variant-fallback]').hidden = true;
    form.querySelector('[data-color-field]').hidden = false;
    form.querySelector('[data-size-field]').hidden = false;
});

document.addEventListener('click', function (event) {
    const toggle = event.target.closest('[data-password-toggle]');
    if (!toggle) return;
    const input = document.getElementById(toggle.dataset.passwordToggle);
    if (!input) return;
    const visible = input.type === 'password';
    input.type = visible ? 'text' : 'password';
    toggle.setAttribute('aria-pressed', String(visible));
    toggle.setAttribute('aria-label', visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    toggle.querySelector('i')?.classList.toggle('bi-eye', !visible);
    toggle.querySelector('i')?.classList.toggle('bi-eye-slash', visible);
    input.focus();
});

document.addEventListener('submit', function (event) {
    const form = event.target;

    if (!form.matches('.auth-form')) return;
    const submit = form.querySelector('button[type="submit"]');
    if (!submit || submit.disabled) return;
    submit.disabled = true;
    submit.setAttribute('aria-busy', 'true');
    submit.dataset.label = submit.textContent.trim();
    submit.replaceChildren('Đang xử lý…');
});

const addressData = (() => {
    let cache;
    return () => {
        if (!cache) {
            cache = Promise.all([
                fetch('/data/provinces.json').then((res) => res.json()),
                fetch('/data/wards.json').then((res) => res.json()),
            ]).then(([provinces, wards]) => {
                const wardsByProvince = wards.reduce((map, ward) => {
                    if (!map.has(ward.province_id)) map.set(ward.province_id, []);
                    map.get(ward.province_id).push(ward);
                    return map;
                }, new Map());
                return { provinces, wardsByProvince };
            });
        }
        return cache;
    };
})();

// fillOptions: Cập nhật danh sách lựa chọn của combobox.
function fillOptions(select, items, placeholder) {
    select.replaceChildren(new Option(placeholder, ''));
    items.forEach((item) => select.append(new Option(item.name, item.id)));
}

// findSavedAddress: Tách địa chỉ đã lưu thành tỉnh, phường và số nhà.
function findSavedAddress(address, provinces, wardsByProvince) {
    if (!address) return { street: '', provinceId: '', wardId: '' };
    const sortedProvinces = [...provinces].sort((a, b) => b.name.length - a.name.length);
    const province = sortedProvinces.find((item) => address.includes(item.name));
    const provinceWards = province ? wardsByProvince.get(province.id) || [] : [];
    const ward = [...provinceWards]
        .sort((a, b) => b.name.length - a.name.length)
        .find((item) => address.includes(item.name));
    let street = address;
    if (province) street = street.replace(province.name, '');
    if (ward) street = street.replace(ward.name, '');
    street = street.replace(/,+/g, ',').replace(/^[\s,]+|[\s,]+$/g, '');
    return { street, provinceId: province?.id || '', wardId: ward?.id || '' };
}

// initAddressPicker: Khởi tạo bộ chọn tỉnh, phường và địa chỉ nhận hàng.
function initAddressPicker(picker, provinces, wardsByProvince) {
    const fullInput = picker.querySelector('[data-address-full]');
    const provinceSelect = picker.querySelector('[data-address-province]');
    const wardSelect = picker.querySelector('[data-address-ward]');
    const streetInput = picker.querySelector('[data-address-street]');
    if (!fullInput || !provinceSelect || !wardSelect || !streetInput) return;

    const compose = () => {
        const province = provinces.find((item) => item.id === provinceSelect.value);
        const ward = (wardsByProvince.get(provinceSelect.value) || []).find(
            (item) => item.id === wardSelect.value,
        );
        fullInput.value = [streetInput.value.trim(), ward?.name, province?.name]
            .filter(Boolean)
            .join(', ');
    };

    const loadWards = (provinceId, selectedWardId = '') => {
        const wards = wardsByProvince.get(provinceId) || [];
        fillOptions(wardSelect, wards, 'Chọn xã/phường');
        wardSelect.disabled = !provinceId;
        if (selectedWardId && wards.some((item) => item.id === selectedWardId))
            wardSelect.value = selectedWardId;
    };

    fillOptions(provinceSelect, provinces, 'Chọn tỉnh/thành phố');
    const saved = findSavedAddress(fullInput.value, provinces, wardsByProvince);
    streetInput.value = saved.street;
    provinceSelect.value = saved.provinceId;
    loadWards(saved.provinceId, saved.wardId);
    compose();

    provinceSelect.addEventListener('change', () => {
        loadWards(provinceSelect.value);
        compose();
    });
    wardSelect.addEventListener('change', compose);
    streetInput.addEventListener('input', compose);
    picker.closest('form')?.addEventListener('submit', compose);
}

document.addEventListener('DOMContentLoaded', function () {
    const pickers = document.querySelectorAll('[data-address-picker]');
    if (!pickers.length) return;
    addressData()
        .then(({ provinces, wardsByProvince }) =>
            pickers.forEach((picker) => initAddressPicker(picker, provinces, wardsByProvince)),
        )
        .catch(() => pickers.forEach((picker) => picker.classList.add('address-picker-error')));
});

document.querySelectorAll('[data-catalog-filter]').forEach((form) => {
    const catalog = form.closest('[data-catalog]');
    const results = catalog.querySelector('[data-catalog-results]');
    const query = form.querySelector('[name="q"]');
    let timer;
    let controller;
    let revision = 0;

    const update = async (version) => {
        const url = new URL(form.action || window.location.href);
        url.search = new URLSearchParams(new FormData(form)).toString();
        controller = new AbortController();
        results.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch(url, { signal: controller.signal });
            if (!response.ok) throw new Error('Catalog request failed');
            const page = new DOMParser().parseFromString(await response.text(), 'text/html');
            const next = page.querySelector('[data-catalog]');
            if (!next?.querySelector('[data-catalog-results]')) throw new Error('Catalog missing');
            if (version !== revision) return;
            results.innerHTML = next.querySelector('[data-catalog-results]').innerHTML;
            catalog.querySelector('h1').textContent = next.querySelector('h1').textContent;
            catalog.querySelector('.count-pill').textContent =
                next.querySelector('.count-pill').textContent;
            document.title = page.title;
            window.history.replaceState(null, '', url);
        } catch (error) {
            if (error.name !== 'AbortError' && version === revision) window.location.assign(url);
        } finally {
            if (version === revision) results.removeAttribute('aria-busy');
        }
    };

    const schedule = (delay = 0) => {
        window.clearTimeout(timer);
        controller?.abort();
        const version = ++revision;
        results.removeAttribute('aria-busy');
        timer = window.setTimeout(() => update(version), delay);
    };

    form.addEventListener('submit', (event) => {
        event.preventDefault();
        schedule();
    });
    form.querySelectorAll('select').forEach((select) => {
        select.addEventListener('change', () => schedule());
    });
    query.addEventListener('input', (event) => {
        window.clearTimeout(timer);
        controller?.abort();
        ++revision;
        results.removeAttribute('aria-busy');
        if (!event.isComposing) schedule(350);
    });
    query.addEventListener('compositionend', () => schedule(350));
});

document.querySelectorAll('[data-admin-list-filter]').forEach((toolbar) => {
    const table = toolbar.closest('main')?.querySelector('.data-table');
    if (!table?.tBodies.length) return;
    const rows = Array.from(table.tBodies[0].rows);
    const query = toolbar.querySelector('[data-admin-list-search]');
    const selectors = Array.from(toolbar.querySelectorAll('[data-admin-row-filter]'));
    const rowValue = (row, key) =>
        row.getAttribute('data-filter-' + key) ?? (key === 'state' ? row.dataset.state : '') ?? '';
    selectors.forEach((select) => {
        if (select.dataset.adminFilterAuto !== 'true') return;
        const key = select.dataset.adminRowFilter;
        const choices = new Map();
        rows.forEach((row) => {
            const value = rowValue(row, key);
            if (value)
                choices.set(value, row.getAttribute('data-filter-' + key + '-label') || value);
        });
        Array.from(choices)
            .sort((a, b) => a[1].localeCompare(b[1], 'vi', { numeric: true }))
            .forEach(([value, label]) => select.append(new Option(label, value)));
    });
    const count = toolbar.querySelector('[data-admin-list-count]');
    const normalize = (text) =>
        text
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd')
            .replace(/Đ/g, 'D')
            .toLocaleLowerCase('vi');
    const empty = table.tBodies[0].insertRow();
    empty.className = 'admin-list-empty';
    const cell = empty.insertCell();
    cell.colSpan = table.tHead?.rows[0]?.cells.length || 1;
    cell.textContent = 'Không có dữ liệu phù hợp.';

    // filter: Lọc danh sách quản trị và cập nhật số kết quả.
    function filter() {
        const term = normalize(query.value.trim());
        let visible = 0;
        rows.forEach((row) => {
            const matches =
                normalize(row.textContent).includes(term) &&
                selectors.every(
                    (select) =>
                        !select.value ||
                        rowValue(row, select.dataset.adminRowFilter) === select.value,
                );
            row.hidden = !matches;
            if (matches) visible++;
        });
        empty.hidden = visible > 0;
        count.textContent = visible + ' / ' + rows.length + ' bản ghi';
    }
    query.addEventListener('input', filter);
    selectors.forEach((select) => select.addEventListener('change', filter));
    toolbar.querySelector('[data-admin-list-reset]').addEventListener('click', () => {
        query.value = '';
        selectors.forEach((select) => {
            select.value = '';
            select.dispatchEvent(new Event('change', { bubbles: true }));
        });
        filter();
        query.focus();
    });
    filter();
});

document.querySelectorAll('[data-admin-server-filter]').forEach((form) => {
    form.querySelectorAll('select').forEach((select) =>
        select.addEventListener('change', () => form.requestSubmit()),
    );
});

document.querySelectorAll('[data-admin-date-filter]').forEach((form) => {
    const period = form.querySelector('[name="period"]');
    period.addEventListener('change', (event) => {
        if (!event.detail?.dateChanged) form.requestSubmit();
    });
    form.querySelectorAll('input[type="date"]').forEach((input) =>
        input.addEventListener('change', () => {
            period.value = '';
            period.dispatchEvent(
                new CustomEvent('change', { bubbles: true, detail: { dateChanged: true } }),
            );
        }),
    );
});

document
    .querySelectorAll('select:not([multiple]):not([data-native-select])')
    .forEach((select, selectIndex) => {
        if (select.size > 1) return;
        const baseId = 'styled-select-' + selectIndex;
        const nativeLabel = select.closest('label');
        if (nativeLabel) {
            const field = document.createElement('div');
            field.className = 'form-select-field';
            const caption = document.createElement('span');
            caption.id = baseId + '-label';
            caption.textContent = Array.from(nativeLabel.childNodes)
                .filter((node) => node !== select)
                .map((node) => node.textContent)
                .join('')
                .trim();
            nativeLabel.replaceWith(field);
            field.append(caption, select);
            select.setAttribute('aria-labelledby', caption.id);
        }
        const wrapper = document.createElement('div');
        wrapper.className = 'filter-select';
        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = 'filter-select-trigger';
        trigger.setAttribute('role', 'combobox');
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');
        const labelId = select.getAttribute('aria-labelledby');
        const valueId = baseId + '-value';
        if (labelId) trigger.setAttribute('aria-labelledby', labelId + ' ' + valueId);
        else
            trigger.setAttribute(
                'aria-label',
                select.getAttribute('aria-label') || select.name || 'Chọn một mục',
            );
        if (select.required) trigger.setAttribute('aria-required', 'true');
        const value = document.createElement('span');
        value.id = valueId;
        const chevron = document.createElement('i');
        chevron.className = 'select-chevron';
        chevron.setAttribute('aria-hidden', 'true');
        trigger.append(value, chevron);
        const menu = document.createElement('ul');
        menu.className = 'filter-select-menu';
        menu.id = baseId + '-options';
        menu.setAttribute('role', 'listbox');
        if (labelId) menu.setAttribute('aria-labelledby', labelId);
        menu.hidden = true;
        trigger.setAttribute('aria-controls', menu.id);
        let options = [];
        let active = select.selectedIndex;
        let search = '';
        let searchTimer;

        const error = document.createElement('span');
        error.className = 'select-error';
        error.id = baseId + '-error';
        error.setAttribute('aria-live', 'polite');
        error.hidden = true;
        let items = [];

        // rebuild: Dựng lại menu từ các lựa chọn của select gốc.
        function rebuild() {
            options = Array.from(select.options);
            menu.replaceChildren();
            items = options.map((option, index) => {
                const item = document.createElement('li');
                item.id = menu.id + '-' + index;
                item.className = 'filter-select-option';
                item.setAttribute('role', 'option');
                const text = document.createElement('span');
                text.textContent = option.textContent;
                const check = document.createElement('i');
                check.className = 'select-check';
                check.setAttribute('aria-hidden', 'true');
                item.append(text, check);
                item.setAttribute('aria-disabled', String(option.disabled));
                item.addEventListener('pointerdown', (event) => event.preventDefault());
                item.addEventListener('click', () => choose(index));
                item.addEventListener('pointermove', () => activate(index));
                menu.append(item);
                return item;
            });
            sync();
        }

        // sync: Đồng bộ trạng thái menu với select gốc.
        function sync() {
            value.textContent = options[select.selectedIndex]?.textContent || 'Chọn một mục';
            trigger.disabled = select.disabled;
            if (select.disabled) close();
            if (select.validity.valid) {
                error.hidden = true;
                trigger.removeAttribute('aria-invalid');
                trigger.removeAttribute('aria-describedby');
            }
            items.forEach((item, index) => {
                const selected = index === select.selectedIndex;
                item.setAttribute('aria-selected', String(selected));
                item.querySelector('i').hidden = !selected;
            });
        }

        // activate: Cập nhật lựa chọn đang di chuyển bằng bàn phím.
        function activate(index) {
            active = Math.max(0, Math.min(index, items.length - 1));
            items.forEach((item, i) => item.classList.toggle('is-active', i === active));
            if (items[active]) trigger.setAttribute('aria-activedescendant', items[active].id);
        }

        // close: Đóng menu lựa chọn.
        function close() {
            menu.hidden = true;
            trigger.setAttribute('aria-expanded', 'false');
            trigger.removeAttribute('aria-activedescendant');
            search = '';
            window.clearTimeout(searchTimer);
        }

        // open: Mở menu lựa chọn theo không gian hiển thị.
        function open() {
            if (select.disabled || !items.length) return;
            document
                .querySelectorAll('.filter-select-trigger[aria-expanded="true"]')
                .forEach((other) => {
                    if (other !== trigger) other.dispatchEvent(new Event('close-select'));
                });
            menu.hidden = false;
            menu.classList.remove('is-above');
            const rect = trigger.getBoundingClientRect();
            const below = window.innerHeight - rect.bottom - 16;
            const above = rect.top - 16;
            const showAbove = below < Math.min(menu.scrollHeight, 260) && above > below;
            menu.classList.toggle('is-above', showAbove);
            menu.style.maxHeight = Math.max(80, Math.min(260, showAbove ? above : below)) + 'px';
            trigger.setAttribute('aria-expanded', 'true');
            activate(select.selectedIndex);
        }

        // choose: Chọn giá trị và thông báo thay đổi combobox.
        function choose(index) {
            if (select.disabled || !options[index] || options[index].disabled) return;
            const changed = select.selectedIndex !== index;
            select.selectedIndex = index;
            sync();
            close();
            trigger.focus({ preventScroll: true });
            if (changed) select.dispatchEvent(new Event('change', { bubbles: true }));
        }

        trigger.addEventListener('click', () => (menu.hidden ? open() : close()));
        trigger.addEventListener('close-select', close);
        trigger.addEventListener('keydown', (event) => {
            const key = event.key;
            if (key === 'Tab' || key === 'Escape') {
                close();
                if (key === 'Escape') event.preventDefault();
                return;
            }
            if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(key)) {
                event.preventDefault();
                const wasClosed = menu.hidden;
                if (wasClosed) open();
                if (key === 'Home') activate(0);
                else if (key === 'End') activate(items.length - 1);
                else if (!wasClosed) activate(active + (key === 'ArrowDown' ? 1 : -1));
                items[active]?.scrollIntoView({ block: 'nearest' });
            } else if (key === 'Enter' || key === ' ') {
                event.preventDefault();
                if (menu.hidden) open();
                else choose(active);
            } else if (key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
                event.preventDefault();
                if (menu.hidden) open();
                search += key.toLocaleLowerCase('vi');
                window.clearTimeout(searchTimer);
                searchTimer = window.setTimeout(() => {
                    search = '';
                }, 600);
                const index = options.findIndex((option) =>
                    option.textContent.toLocaleLowerCase('vi').startsWith(search),
                );
                if (index >= 0) {
                    activate(index);
                    items[active]?.scrollIntoView({ block: 'nearest' });
                }
            }
        });
        wrapper.addEventListener('focusout', (event) => {
            if (!wrapper.contains(event.relatedTarget)) close();
        });
        document.addEventListener('pointerdown', (event) => {
            if (!wrapper.contains(event.target)) close();
        });
        window.addEventListener('resize', close);
        select.addEventListener('change', sync);
        select.after(wrapper);
        wrapper.append(trigger, menu, error);
        select.classList.add('styled-select-native');
        select.tabIndex = -1;
        select.setAttribute('aria-hidden', 'true');
        select.addEventListener('invalid', (event) => {
            event.preventDefault();
            error.textContent = select.validationMessage;
            error.hidden = false;
            trigger.setAttribute('aria-invalid', 'true');
            trigger.setAttribute('aria-describedby', error.id);
            const firstInvalid = select.form?.querySelector(
                'select:invalid, input:invalid, textarea:invalid',
            );
            if (!firstInvalid || firstInvalid === select) trigger.focus();
        });
        select.form?.addEventListener('reset', () =>
            window.setTimeout(() => {
                close();
                sync();
            }, 0),
        );
        new MutationObserver(rebuild).observe(select, {
            childList: true,
            subtree: true,
            characterData: true,
            attributes: true,
            attributeFilter: ['disabled', 'selected', 'label', 'value', 'required'],
        });
        rebuild();
    });
