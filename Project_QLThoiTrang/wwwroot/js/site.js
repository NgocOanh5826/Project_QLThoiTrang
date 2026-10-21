document.addEventListener('submit', function (event) {
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
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
                fetch('/data/wards.json').then((res) => res.json())
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

function fillOptions(select, items, placeholder) {
    select.replaceChildren(new Option(placeholder, ''));
    items.forEach((item) => select.append(new Option(item.name, item.id)));
}

function findSavedAddress(address, provinces, wardsByProvince) {
    if (!address) return { street: '', provinceId: '', wardId: '' };
    const sortedProvinces = [...provinces].sort((a, b) => b.name.length - a.name.length);
    const province = sortedProvinces.find((item) => address.includes(item.name));
    const provinceWards = province ? wardsByProvince.get(province.id) || [] : [];
    const ward = [...provinceWards].sort((a, b) => b.name.length - a.name.length).find((item) => address.includes(item.name));
    let street = address;
    if (province) street = street.replace(province.name, '');
    if (ward) street = street.replace(ward.name, '');
    street = street.replace(/,+/g, ',').replace(/^[\s,]+|[\s,]+$/g, '');
    return { street, provinceId: province?.id || '', wardId: ward?.id || '' };
}

function initAddressPicker(picker, provinces, wardsByProvince) {
    const fullInput = picker.querySelector('[data-address-full]');
    const provinceSelect = picker.querySelector('[data-address-province]');
    const wardSelect = picker.querySelector('[data-address-ward]');
    const streetInput = picker.querySelector('[data-address-street]');
    if (!fullInput || !provinceSelect || !wardSelect || !streetInput) return;

    const compose = () => {
        const province = provinces.find((item) => item.id === provinceSelect.value);
        const ward = (wardsByProvince.get(provinceSelect.value) || []).find((item) => item.id === wardSelect.value);
        fullInput.value = [streetInput.value.trim(), ward?.name, province?.name].filter(Boolean).join(', ');
    };

    const loadWards = (provinceId, selectedWardId = '') => {
        const wards = wardsByProvince.get(provinceId) || [];
        fillOptions(wardSelect, wards, 'Chọn xã/phường');
        wardSelect.disabled = !provinceId;
        if (selectedWardId && wards.some((item) => item.id === selectedWardId)) wardSelect.value = selectedWardId;
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
        .then(({ provinces, wardsByProvince }) => pickers.forEach((picker) => initAddressPicker(picker, provinces, wardsByProvince)))
        .catch(() => pickers.forEach((picker) => picker.classList.add('address-picker-error')));
});
