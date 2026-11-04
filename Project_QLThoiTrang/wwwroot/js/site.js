// VAI TRÒ FILE: hành vi form và giao diện dùng chung. Tìm thuộc tính data-* tương ứng trong view để
// biết đoạn
// nào chạy trên trang nào; kiểm tra nghiệp vụ cuối cùng nằm ở controller/service.

document.addEventListener('submit', function (event) {
    // XÁC NHẬN THAO TÁC: form có data-confirm sẽ hỏi lại trước submit; người dùng hủy thì
    // preventDefault chặn
    // gửi.
    // BIẾN JS message: chuỗi data-confirm của form, có nội dung thì hỏi xác nhận trước submit và
    // hủy gửi nếu người dùng chọn hủy.
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
});

// BIẾN JS cartSaving: cờ toàn trang báo một yêu cầu lưu giỏ đang gửi, dùng ngăn hai form giỏ gửi
// chồng nhau.
let cartSaving = false;
// GIỎ HÀNG - SỬA: khi đổi màu/size/lượng, tính lại lựa chọn còn hàng và max = min(20, tồn) rồi gửi
// form
// UpdateCart. Biến cartSaving khóa gửi đồng thời; điều kiện cuối cùng vẫn ở server.
document.querySelectorAll('[data-cart-edit]').forEach((form) => {
    // BIẾN JS variants: các bản ghi biến thể đọc từ dữ liệu Razor, gồm id, màu, size, tồn và/hoặc
    // giá.
    const variants = JSON.parse(form.dataset.variants);
    // BIẾN JS color: phần tử DOM tìm bằng selector [data-cart-color]; ô chọn màu của dòng giỏ, thay
    // đổi sẽ chọn lại các size của màu đó.
    const color = form.querySelector('[data-cart-color]');
    // BIẾN JS sizes: phần tử DOM tìm bằng selector [data-cart-size]; vùng/ô chọn size; nội dung
    // được dựng lại theo màu và các tổ hợp còn tồn.
    const sizes = form.querySelector('[data-cart-size]');
    // BIẾN JS quantity: phần tử DOM tìm bằng selector [name="quantity"]; ô số lượng mua, max đồng
    // bộ bằng min(20, tồn biến thể); server còn kiểm tra lại khi gửi.
    const quantity = form.querySelector('[name="quantity"]');
    // Chỉ gửi giỏ khi chưa có lần lưu đang chạy và biến thể còn tồn; reportValidity hiển thị lỗi
    // trước
    // requestSubmit.
    // HÀM JS save: kiểm tra giỏ còn hợp lệ và gửi form cập nhật khi chưa có lần lưu đang chạy.
    // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng
    // thái hoặc gửi form như thân hàm.
    function save() {
        if (cartSaving) return;
        // BIẾN JS variant: bản ghi biến thể khớp lựa chọn, dùng kiểm tra tồn và mã gửi form.
        const variant = variants.find((v) => String(v.id) === sizes.value);
        sizes.setCustomValidity(
            variant?.stock > 0
                ? ''
                : 'Phiên bản này đã hết hàng. Vui lòng chọn màu hoặc kích cỡ khác.',
        );
        if (form.reportValidity()) form.requestSubmit();
    }
    color.addEventListener('change', () => {
        // BIẾN JS previous: bản ghi biến thể đã chọn trước đổi màu, dùng cố giữ size cũ nếu tổ hợp
        // mới còn hàng.
        const previous = variants.find((v) => String(v.id) === sizes.value);
        // BIẾN JS matches: các biến thể cùng màu đang chọn; trong bộ lọc bảng đây là cờ dòng có
        // khớp điều kiện hay không.
        const matches = variants.filter((v) => v.color === color.value);
        // BIẾN JS selected: biến thể được ưu tiên chọn hoặc cờ một option đang được chọn, tùy biểu
        // thức khởi tạo.
        const selected =
            matches.find((v) => v.size === previous?.size && v.stock > 0) ||
            matches.find((v) => v.stock > 0) ||
            matches[0];
        sizes.replaceChildren();
        matches.forEach((v) => {
            // BIẾN JS option: một lựa chọn HTML mới dựng cho size, có value là mã biến thể và
            // disabled khi hết hàng.
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
        // BIẾN JS variant: bản ghi biến thể khớp lựa chọn, dùng kiểm tra tồn và mã gửi form.
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
        // BIẾN JS page: vùng trang giỏ để báo đang lưu, hoặc DOM của HTML server vừa trả về trong
        // bộ lọc catalogue.
        const page = form.closest('.cart-page');
        page.classList.add('is-saving');
        page.setAttribute('aria-busy', 'true');
        page.querySelector('[data-cart-save-status]').textContent = 'Đang lưu giỏ hàng…';
    });
});

// ĐÁNH GIÁ - XEM TRƯỚC TỆP: kiểm tra nhanh số tệp, dung lượng, đuôi và nhiều nhất một video; tạo

// object URL xem
// trước, thu hồi URL khi thay tệp/rời trang. ReviewUploads kiểm tra byte thật ở server.
document.querySelectorAll('[data-review-form]').forEach((form) => {
    // BIẾN JS input: phần tử DOM tìm bằng selector [data-review-upload]; ô input đang được thao tác
    // như upload/mật khẩu/radio, lấy từ selector hoặc được tạo mới như init bên dưới.
    const input = form.querySelector('[data-review-upload]');
    // BIẾN JS preview: phần tử DOM tìm bằng selector [data-review-preview]; vùng DOM hiện ảnh/video
    // xem trước sau khi chọn tệp đánh giá.
    const preview = form.querySelector('[data-review-preview]');
    // BIẾN JS urls: các object URL đang dùng xem trước tệp; phải revoke khi đổi tệp/rời trang để
    // giải phóng bộ nhớ.
    let urls = [];
    // Thu hồi object URL của tệp xem trước và xóa phần tử media để tránh giữ bộ nhớ sau khi người
    // dùng đổi tệp.
    // HÀM JS clear: thu hồi object URL và xóa nội dung xem trước tệp đã chọn.
    // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng
    // thái hoặc gửi form như thân hàm.
    function clear() {
        urls.forEach((url) => URL.revokeObjectURL(url));
        urls = [];
        preview.replaceChildren();
    }
    input.addEventListener('change', () => {
        clear();
        // BIẾN JS files: mảng File người dùng vừa chọn từ input upload, chưa được server xác nhận
        // hợp lệ.
        const files = Array.from(input.files);
        // BIẾN JS video: hàm nhận một File và trả cờ có đuôi video để chọn giới hạn dung lượng/cách
        // xem trước.
        const video = (file) => /\.(mp4|webm)$/i.test(file.name);
        // BIẾN JS error: chuỗi thông báo lỗi kiểm tra hoặc lỗi nhận từ server; phần tử hiển thị lỗi
        // được nhận biết riêng theo init.
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
            // BIẾN JS media: phần tử img/video dùng xem trước tệp, không phải entity ReviewMedia
            // trong database.
            const media = document.createElement(video(file) ? 'video' : 'img');
            // BIẾN JS url: URL của tệp xem trước hoặc URL GET bộ lọc; xem biểu thức tạo URL để biết
            // nguồn cụ thể.
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

// Keep the submitted variant ID tied to the separate color and size choices.
// CHI TIẾT SẢN PHẨM: dựng nút màu/size từ select biến thể do Razor tạo; luôn đồng bộ mã biến thể sẽ
// gửi, giá và
// tồn tương ứng. Hết hàng thì vô hiệu nút thêm giỏ.
document.querySelectorAll('[data-product-picker]').forEach((form) => {
    // BIẾN JS select: phần tử DOM tìm bằng selector [data-product-variants]; select HTML gốc giữ
    // value thật gửi form, được UI tùy biến đồng bộ thay vì bỏ khỏi form.
    const select = form.querySelector('[data-product-variants]');
    // BIẾN JS variants: các bản ghi biến thể đọc từ dữ liệu Razor, gồm id, màu, size, tồn và/hoặc
    // giá.
    const variants = Array.from(select.options).map((option) => ({
        id: option.value,
        color: option.dataset.color,
        size: option.dataset.size,
        stock: Number(option.dataset.stock),
        price: Number(option.dataset.price),
    }));
    if (!variants.length) return;
    // BIẾN JS colors: phần tử DOM tìm bằng selector [data-product-colors]; vùng chứa các radio màu
    // trên trang chi tiết sản phẩm.
    const colors = form.querySelector('[data-product-colors]');
    // BIẾN JS sizes: phần tử DOM tìm bằng selector [data-product-sizes]; vùng/ô chọn size; nội dung
    // được dựng lại theo màu và các tổ hợp còn tồn.
    const sizes = form.querySelector('[data-product-sizes]');
    // BIẾN JS quantity: phần tử DOM tìm bằng selector [name="quantity"]; ô số lượng mua, max đồng
    // bộ bằng min(20, tồn biến thể); server còn kiểm tra lại khi gửi.
    const quantity = form.querySelector('[name="quantity"]');
    // BIẾN JS add: phần tử DOM tìm bằng selector [data-product-add]; nút thêm vào giỏ, bị vô hiệu
    // khi biến thể chọn không còn hàng.
    const add = form.querySelector('[data-product-add]');
    // BIẾN JS stock: phần tử DOM tìm bằng selector [data-product-stock]; phần tử chữ thông báo
    // tồn/biến thể đang chọn, không phải số tồn SQL có thể sửa ở đây.
    const stock = form.querySelector('[data-product-stock]');
    // BIẾN JS price: phần tử DOM tìm bằng selector [data-product-price]; phần tử hiển thị giá theo
    // biến thể chọn, giá đặt hàng thật vẫn do server kiểm tra.
    const price = form.closest('.detail-copy').querySelector('[data-product-price]');
    // BIẾN JS chosen: bản ghi biến thể hiện chọn của bộ chọn sản phẩm, nguồn để đồng bộ mã/giá/tồn.
    let chosen =
        variants.find((v) => v.id === select.value) ||
        variants.find((v) => v.stock > 0) ||
        variants[0];
    // Đồng bộ mã biến thể, lượng tối đa, nút mua, giá và thông báo tồn theo lựa chọn hiện tại.
    // HÀM JS update: đồng bộ mã biến thể, giá, tồn, max số lượng và trạng thái nút mua.
    // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng
    // thái hoặc gửi form như thân hàm.
    function update() {
        select.value = chosen.id;
        // BIẾN JS max: lượng mua tối đa của biến thể đang chọn sau giới hạn 20 và tồn hiện tại.
        const max = Math.min(20, chosen.stock);
        quantity.max = String(max);
        quantity.disabled = max <= 0;
        quantity.value = String(Math.max(1, Math.min(max || 1, Number(quantity.value) || 1)));
        add.disabled = max <= 0;
        add.textContent = max > 0 ? 'Thêm vào giỏ hàng' : 'Tạm hết hàng';
        stock.textContent = `Còn ${chosen.stock} sản phẩm · ${chosen.color} · ${chosen.size}`;
        price.textContent = new Intl.NumberFormat('vi-VN').format(chosen.price) + ' đ';
    }
    // Tạo một radio cho màu/size, đặt checked/disabled và gắn handler thay đổi được truyền vào.
    // HÀM JS choice: dựng một radio màu/size với checked/disabled và gắn hàm change.
    // THAM SỐ container: phần tử DOM nhận các radio/nhãn được dựng.
    // THAM SỐ name: name của radio để gom các lựa chọn cùng nhóm và gửi/đọc đúng tên.
    // THAM SỐ value: giá trị dữ liệu hoặc nhãn của lựa chọn đang tạo.
    // THAM SỐ checked: cờ radio này có phải lựa chọn hiện tại.
    // THAM SỐ disabled: cờ radio này không được chọn vì tổ hợp không hợp lệ/hết hàng.
    // THAM SỐ change: hàm callback chạy khi người dùng đổi radio.
    // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng
    // thái hoặc gửi form như thân hàm.
    function choice(container, name, value, checked, disabled, change) {
        // BIẾN JS label: phần tử <label> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const label = document.createElement('label');
        // BIẾN JS input: phần tử <input> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const input = document.createElement('input');
        input.type = 'radio';
        input.name = name;
        input.value = value;
        input.checked = checked;
        input.disabled = disabled;
        input.addEventListener('change', change);
        // BIẾN JS text: phần tử <span> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const text = document.createElement('span');
        text.textContent = value;
        label.append(input, text);
        container.append(label);
    }
    // Dựng lại size cho màu đang chọn; tổ hợp không tồn tại hoặc tồn <= 0 bị vô hiệu.
    // HÀM JS renderSizes: dựng lại các radio size theo màu hiện chọn, chặn tổ hợp hết hoặc không
    // tồn tại.
    // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng
    // thái hoặc gửi form như thân hàm.
    function renderSizes() {
        sizes.replaceChildren();
        Array.from(new Set(variants.map((v) => v.size))).forEach((size) => {
            // BIẾN JS variant: bản ghi biến thể khớp lựa chọn, dùng kiểm tra tồn và mã gửi form.
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
            // BIẾN JS matches: các biến thể cùng màu đang chọn; trong bộ lọc bảng đây là cờ dòng có
            // khớp điều kiện hay không.
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
    // HIỆN/ẨN MẬT KHẨU: đổi input.type và biểu tượng; không lưu, băm hoặc gửi mật khẩu ở sự kiện
    // click này.
    // BIẾN JS toggle: nút data-password-toggle mà người dùng vừa bấm để hiện/ẩn ô mật khẩu.
    const toggle = event.target.closest('[data-password-toggle]');
    if (!toggle) return;
    // BIẾN JS input: ô input đang được thao tác như upload/mật khẩu/radio, lấy từ selector hoặc
    // được tạo mới như init bên dưới.
    const input = document.getElementById(toggle.dataset.passwordToggle);
    if (!input) return;
    // BIẾN JS visible: cờ hiện mật khẩu; trong filter() giá trị khởi tạo 0 là bộ đếm số dòng khớp,
    // không phải cờ.
    const visible = input.type === 'password';
    input.type = visible ? 'text' : 'password';
    toggle.setAttribute('aria-pressed', String(visible));
    toggle.setAttribute('aria-label', visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    toggle.querySelector('i')?.classList.toggle('bi-eye', !visible);
    toggle.querySelector('i')?.classList.toggle('bi-eye-slash', visible);
    input.focus();
});

document.addEventListener('submit', function (event) {
    // BIẾN JS form: form HTML hiện đang xử lý, nguồn lấy các trường và action gửi.
    const form = event.target;
    // CHỐNG BẤM LẶP: chỉ áp dụng form tài khoản, vô hiệu nút và báo đang xử lý sau submit.
    if (!form.matches('.auth-form')) return;
    // BIẾN JS submit: phần tử DOM tìm bằng selector button[type="submit"]; nút gửi form đăng
    // nhập/đăng ký, được khóa và đổi nhãn để tránh bấm lặp.
    const submit = form.querySelector('button[type="submit"]');
    if (!submit || submit.disabled) return;
    submit.disabled = true;
    submit.setAttribute('aria-busy', 'true');
    submit.dataset.label = submit.textContent.trim();
    submit.replaceChildren('Đang xử lý…');
});

// DỮ LIỆU ĐỊA CHỈ: tải provinces.json và wards.json một lần, cache Promise và gom phường theo tỉnh

// để nhiều form
// dùng lại.
// BIẾN JS addressData: hàm tải và cache Promise dữ liệu tỉnh/phường, nhiều bộ chọn dùng chung kết
// quả.
const addressData = (() => {
    // BIẾN JS cache: Promise tải dữ liệu địa chỉ đã tạo, giữ lại để không fetch lại cùng JSON nhiều
    // lần.
    let cache;
    return () => {
        if (!cache) {
            cache = Promise.all([
                fetch('/data/provinces.json').then((res) => res.json()),
                fetch('/data/wards.json').then((res) => res.json()),
            ]).then(([provinces, wards]) => {
                // BIẾN JS wardsByProvince: Map mã tỉnh → danh sách phường/xã, giúp đổi tỉnh có ngay
                // các lựa chọn phụ thuộc.
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

// Tạo lại các option cho select, bắt đầu bằng lựa chọn hướng dẫn; dùng new Option để tên địa

// phương được coi là
// text.
// HÀM JS fillOptions: xóa và dựng lại option từ items, thêm lựa chọn placeholder đầu danh sách.
// THAM SỐ select: select HTML gốc cần dựng các option.
// THAM SỐ items: mảng các bản ghi id/name để tạo option.
// THAM SỐ placeholder: chữ hướng dẫn của option đầu có value rỗng.
// TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng thái
// hoặc gửi form như thân hàm.
function fillOptions(select, items, placeholder) {
    select.replaceChildren(new Option(placeholder, ''));
    items.forEach((item) => select.append(new Option(item.name, item.id)));
}

// Tách địa chỉ đã lưu thành phần đường/tỉnh/phường bằng tên địa phương; ưu tiên tên dài để giảm

// nhầm tên trùng
// một phần.
// HÀM JS findSavedAddress: tách chuỗi địa chỉ cũ thành street/provinceId/wardId bằng danh mục địa
// phương.
// THAM SỐ address: chuỗi địa chỉ đã lưu cần tách thành các phần.
// THAM SỐ provinces: danh sách tỉnh/thành từ JSON.
// THAM SỐ wardsByProvince: Map mã tỉnh sang danh sách phường/xã.
// TRẢ VỀ: đối tượng street/provinceId/wardId để điền lại địa chỉ.
function findSavedAddress(address, provinces, wardsByProvince) {
    if (!address) return { street: '', provinceId: '', wardId: '' };
    // BIẾN JS sortedProvinces: bản sao danh sách tỉnh đã xếp tên dài trước để giảm nhầm khi tách
    // địa chỉ chuỗi.
    const sortedProvinces = [...provinces].sort((a, b) => b.name.length - a.name.length);
    // BIẾN JS province: bản ghi tỉnh khớp chuỗi địa chỉ đã lưu, có thể undefined nếu không nhận
    // diện được.
    const province = sortedProvinces.find((item) => address.includes(item.name));
    // BIẾN JS provinceWards: các phường của tỉnh đã nhận diện, dùng tìm tên phường trong địa chỉ
    // cũ.
    const provinceWards = province ? wardsByProvince.get(province.id) || [] : [];
    // BIẾN JS ward: bản ghi phường khớp địa chỉ đã lưu, có thể undefined.
    const ward = [...provinceWards]
        .sort((a, b) => b.name.length - a.name.length)
        .find((item) => address.includes(item.name));
    // BIẾN JS street: phần địa chỉ còn lại sau bỏ tên tỉnh/phường và dọn dấu phẩy.
    let street = address;
    if (province) street = street.replace(province.name, '');
    if (ward) street = street.replace(ward.name, '');
    street = street.replace(/,+/g, ',').replace(/^[\s,]+|[\s,]+$/g, '');
    return { street, provinceId: province?.id || '', wardId: ward?.id || '' };
}

// Nối các ô tỉnh/phường/đường với trường địa chỉ đầy đủ gửi server; chọn tỉnh đổi danh sách

// phường, trường thiếu
// thì bỏ qua bộ chọn này.
// HÀM JS initAddressPicker: gắn dữ liệu tỉnh/phường, khôi phục địa chỉ cũ và ghép fullInput khi
// thay đổi.
// THAM SỐ picker: vùng DOM có các ô tỉnh/phường/đường của một địa chỉ.
// THAM SỐ provinces: danh sách tỉnh/thành từ JSON.
// THAM SỐ wardsByProvince: Map mã tỉnh sang danh sách phường/xã.
// TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng thái
// hoặc gửi form như thân hàm.
function initAddressPicker(picker, provinces, wardsByProvince) {
    // BIẾN JS fullInput: phần tử DOM tìm bằng selector [data-address-full]; trường địa chỉ đầy đủ
    // thật sự gửi server, được ghép lại từ các ô chọn và số nhà/đường.
    const fullInput = picker.querySelector('[data-address-full]');
    // BIẾN JS provinceSelect: phần tử DOM tìm bằng selector [data-address-province]; select
    // tỉnh/thành của bộ chọn địa chỉ.
    const provinceSelect = picker.querySelector('[data-address-province]');
    // BIẾN JS wardSelect: phần tử DOM tìm bằng selector [data-address-ward]; select phường/xã, được
    // thay option khi đổi tỉnh.
    const wardSelect = picker.querySelector('[data-address-ward]');
    // BIẾN JS streetInput: phần tử DOM tìm bằng selector [data-address-street]; ô số nhà/đường để
    // ghép cùng tên tỉnh/phường vào fullInput.
    const streetInput = picker.querySelector('[data-address-street]');
    if (!fullInput || !provinceSelect || !wardSelect || !streetInput) return;

    // BIẾN JS compose: hàm ghép các phần địa chỉ thành chuỗi đầy đủ và cập nhật trạng thái kiểm tra
    // của ô.
    const compose = () => {
        // BIẾN JS province: bản ghi tỉnh khớp chuỗi địa chỉ đã lưu, có thể undefined nếu không nhận
        // diện được.
        const province = provinces.find((item) => item.id === provinceSelect.value);
        // BIẾN JS ward: bản ghi phường khớp địa chỉ đã lưu, có thể undefined.
        const ward = (wardsByProvince.get(provinceSelect.value) || []).find(
            (item) => item.id === wardSelect.value,
        );
        fullInput.value = [streetInput.value.trim(), ward?.name, province?.name]
            .filter(Boolean)
            .join(', ');
    };

    // BIẾN JS loadWards: hàm dựng các option phường theo mã tỉnh, chọn lại phường cũ nếu có.
    const loadWards = (provinceId, selectedWardId = '') => {
        // BIẾN JS wards: danh sách phường/xã của tỉnh đang chọn để đưa vào wardSelect.
        const wards = wardsByProvince.get(provinceId) || [];
        fillOptions(wardSelect, wards, 'Chọn xã/phường');
        wardSelect.disabled = !provinceId;
        if (selectedWardId && wards.some((item) => item.id === selectedWardId))
            wardSelect.value = selectedWardId;
    };

    fillOptions(provinceSelect, provinces, 'Chọn tỉnh/thành phố');
    // BIẾN JS saved: kết quả tách địa chỉ cũ thành street/provinceId/wardId để điền lại form.
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

// KHỞI TẠO SAU KHI HTML SẴN SÀNG: tìm các bộ chọn địa chỉ, tải dữ liệu tỉnh/phường và điền địa chỉ

// đã lưu.
document.addEventListener('DOMContentLoaded', function () {
    // BIẾN JS pickers: các phần tử data-address-picker cần khởi tạo sau DOMContentLoaded.
    const pickers = document.querySelectorAll('[data-address-picker]');
    if (!pickers.length) return;
    addressData()
        .then(({ provinces, wardsByProvince }) =>
            pickers.forEach((picker) => initAddressPicker(picker, provinces, wardsByProvince)),
        )
        .catch(() => pickers.forEach((picker) => picker.classList.add('address-picker-error')));
});

// Update the catalog without replacing the filter inputs or interrupting typing.
// LỌC CỬA HÀNG: thay bộ lọc sẽ gửi form GET, ô tìm kiếm có độ trễ để không tải trang theo từng phím
// quá nhanh.
document.querySelectorAll('[data-catalog-filter]').forEach((form) => {
    // BIẾN JS catalog: vùng catalogue bao tiêu đề, bộ lọc và kết quả cần cập nhật.
    const catalog = form.closest('[data-catalog]');
    // BIẾN JS results: phần tử DOM tìm bằng selector [data-catalog-results]; vùng kết quả sản phẩm
    // sẽ được thay HTML từ phản hồi GET, giữ nguyên input đang gõ.
    const results = catalog.querySelector('[data-catalog-results]');
    // BIẾN JS query: phần tử DOM tìm bằng selector [name="q"]; ô từ khóa tìm kiếm của danh
    // sách/catalogue, không phải kết quả SQL trực tiếp.
    const query = form.querySelector('[name="q"]');
    // BIẾN JS timer: id timeout cho debounce, clearTimeout hủy lần lên lịch cũ khi người dùng tiếp
    // tục gõ.
    let timer;
    // BIẾN JS controller: AbortController của fetch catalogue, dùng hủy yêu cầu cũ khi điều kiện
    // lọc đổi.
    let controller;
    // BIẾN JS revision: số phiên bản bộ lọc hiện tại, tăng mỗi lần đổi điều kiện để loại phản hồi
    // về muộn.
    let revision = 0;

    // BIẾN JS update: phần tử DOM tìm bằng selector [data-catalog]; hàm async gửi GET catalogue và
    // chỉ áp dụng phản hồi nếu version còn bằng revision.
    const update = async (version) => {
        // BIẾN JS url: URL của tệp xem trước hoặc URL GET bộ lọc; xem biểu thức tạo URL để biết
        // nguồn cụ thể.
        const url = new URL(form.action || window.location.href);
        url.search = new URLSearchParams(new FormData(form)).toString();
        controller = new AbortController();
        results.setAttribute('aria-busy', 'true');
        try {
            // BIẾN JS response: Response từ fetch; phải kiểm tra ok trước khi đọc JSON/HTML.
            const response = await fetch(url, { signal: controller.signal });
            if (!response.ok) throw new Error('Catalog request failed');
            // BIẾN JS page: vùng trang giỏ để báo đang lưu, hoặc DOM của HTML server vừa trả về
            // trong bộ lọc catalogue.
            const page = new DOMParser().parseFromString(await response.text(), 'text/html');
            // BIẾN JS next: phần tử DOM tìm bằng selector [data-catalog]; vùng catalogue trong DOM
            // phản hồi mới, nguồn lấy kết quả/tiêu đề/số lượng để cập nhật.
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

    // BIẾN JS schedule: hàm hủy timer/fetch cũ, tăng revision rồi hẹn update sau khoảng delay.
    const schedule = (delay = 0) => {
        window.clearTimeout(timer);
        controller?.abort();
        // BIẾN JS version: bản sao revision của một lần lên lịch, dùng đối chiếu để bỏ phản hồi
        // request cũ.
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
// Filter the rendered administration list without changing its edit forms.
// LỌC BẢNG Ở TRÌNH DUYỆT: đối chiếu từ khóa và thuộc tính data-* trên các dòng đã render, ẩn dòng
// không khớp và
// cập nhật đếm; không truy vấn hoặc sửa SQL.
document.querySelectorAll('[data-admin-list-filter]').forEach((toolbar) => {
    // BIẾN JS table: phần tử DOM tìm bằng selector .data-table; bảng dữ liệu quản trị đang được
    // thanh công cụ lọc điều khiển.
    const table = toolbar.closest('main')?.querySelector('.data-table');
    if (!table?.tBodies.length) return;
    // BIẾN JS rows: mảng các dòng tr đã render trước khi thêm dòng báo rỗng, là tập dữ liệu gốc để
    // lọc.
    const rows = Array.from(table.tBodies[0].rows);
    // BIẾN JS query: phần tử DOM tìm bằng selector [data-admin-list-search]; ô từ khóa tìm kiếm của
    // danh sách/catalogue, không phải kết quả SQL trực tiếp.
    const query = toolbar.querySelector('[data-admin-list-search]');
    // BIẾN JS selectors: các select data-admin-row-filter dùng lọc theo thuộc tính data-filter-*
    // của dòng.
    const selectors = Array.from(toolbar.querySelectorAll('[data-admin-row-filter]'));
    // BIẾN JS rowValue: hàm lấy giá trị một khóa lọc từ thuộc tính data-filter-... trên dòng HTML.
    const rowValue = (row, key) =>
        row.getAttribute('data-filter-' + key) ?? (key === 'state' ? row.dataset.state : '') ?? '';
    selectors.forEach((select) => {
        if (select.dataset.adminFilterAuto !== 'true') return;
        // BIẾN JS key: tên khóa lọc hoặc phím vừa nhấn; biểu thức lấy từ dataset hay event.key phân
        // biệt hai trường hợp.
        const key = select.dataset.adminRowFilter;
        // BIẾN JS choices: Map giá trị lọc → nhãn, loại trùng các option suy ra từ dữ liệu bảng.
        const choices = new Map();
        rows.forEach((row) => {
            // BIẾN JS value: giá trị lọc/option, hoặc vùng chữ trên nút select khi init tạo phần tử
            // DOM.
            const value = rowValue(row, key);
            if (value)
                choices.set(value, row.getAttribute('data-filter-' + key + '-label') || value);
        });
        Array.from(choices)
            .sort((a, b) => a[1].localeCompare(b[1], 'vi', { numeric: true }))
            .forEach(([value, label]) => select.append(new Option(label, value)));
    });
    // BIẾN JS count: phần tử DOM tìm bằng selector [data-admin-list-count]; phần tử hiển thị số
    // dòng đang khớp/tổng dòng của bảng quản trị.
    const count = toolbar.querySelector('[data-admin-list-count]');
    // BIẾN JS normalize: hàm bỏ dấu Unicode, đổi đ/Đ và chuẩn hóa hoa/thường trước tìm kiếm bảng.
    const normalize = (text) =>
        text
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd')
            .replace(/Đ/g, 'D')
            .toLocaleLowerCase('vi');
    // BIẾN JS empty: dòng tr riêng báo không có kết quả, chỉ hiện khi số dòng khớp bằng 0.
    const empty = table.tBodies[0].insertRow();
    empty.className = 'admin-list-empty';
    // BIẾN JS cell: ô td trong dòng báo rỗng, colspan phủ đủ các cột bảng.
    const cell = empty.insertCell();
    cell.colSpan = table.tHead?.rows[0]?.cells.length || 1;
    cell.textContent = 'Không có dữ liệu phù hợp.';
    // Tính các dòng khớp điều kiện tìm kiếm/bộ lọc hiện tại rồi cập nhật hiển thị và số lượng kết
    // quả.
    // HÀM JS filter: lọc các dòng quản trị theo từ khóa và select, cập nhật số khớp và dòng báo
    // rỗng.
    // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng
    // thái hoặc gửi form như thân hàm.
    function filter() {
        // BIẾN JS term: từ khóa đã trim/bỏ dấu/chuẩn hóa để so với chữ của mỗi dòng.
        const term = normalize(query.value.trim());
        // BIẾN JS visible: cờ hiện mật khẩu; trong filter() giá trị khởi tạo 0 là bộ đếm số dòng
        // khớp, không phải cờ.
        let visible = 0;
        rows.forEach((row) => {
            // BIẾN JS matches: các biến thể cùng màu đang chọn; trong bộ lọc bảng đây là cờ dòng có
            // khớp điều kiện hay không.
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
// LỌC QUẢN TRỊ TRÊN SERVER: gửi lại form GET khi chọn điều kiện để controller đọc và lọc danh sách.
document.querySelectorAll('[data-admin-server-filter]').forEach((form) => {
    form.querySelectorAll('select').forEach((select) =>
        select.addEventListener('change', () => form.requestSubmit()),
    );
});
// LỌC NGÀY: gửi form khi đổi kỳ hoặc khoảng ngày; controller vẫn kiểm tra thứ tự ngày và đổi period
// thành mốc
// thời gian.
document.querySelectorAll('[data-admin-date-filter]').forEach((form) => {
    // BIẾN JS period: phần tử DOM tìm bằng selector [name="period"]; select kỳ ngày nhanh; thay đổi
    // sẽ gửi form GET để controller tính khoảng ngày.
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
// Shared select-only comboboxes, including dependent address fields.
// SELECT TÙY BIẾN: tạo giao diện lựa chọn thay cho select thường, đồng bộ value/disabled và phát
// change; hỗ trợ
// bàn phím, đóng/mở. Select có data-native-select hoặc multiple được giữ nguyên.
document
    .querySelectorAll('select:not([multiple]):not([data-native-select])')
    .forEach((select, selectIndex) => {
        if (select.size > 1) return;
        // BIẾN JS baseId: tiền tố id duy nhất của select tùy biến, tránh trùng các liên kết ARIA
        // giữa nhiều ô.
        const baseId = 'styled-select-' + selectIndex;
        // BIẾN JS nativeLabel: label gốc đang bọc select, cần chuyển chữ nhãn sang caption nhưng
        // giữ liên kết nhãn.
        const nativeLabel = select.closest('label');
        if (nativeLabel) {
            // BIẾN JS field: phần tử <div> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi
            // mới append vào DOM.
            const field = document.createElement('div');
            field.className = 'form-select-field';
            // BIẾN JS caption: phần tử <span> mới tạo trong RAM, được gán class/nội dung/sự kiện
            // rồi mới append vào DOM.
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
        // BIẾN JS wrapper: phần tử <div> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const wrapper = document.createElement('div');
        wrapper.className = 'filter-select';
        // BIẾN JS trigger: phần tử <button> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi
        // mới append vào DOM.
        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = 'filter-select-trigger';
        trigger.setAttribute('role', 'combobox');
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');
        // BIẾN JS labelId: id nhãn để công cụ hỗ trợ truy cập đọc đúng tên trường.
        const labelId = select.getAttribute('aria-labelledby');
        // BIẾN JS valueId: id vùng chữ giá trị đang chọn để ghép quan hệ ARIA cho nút.
        const valueId = baseId + '-value';
        if (labelId) trigger.setAttribute('aria-labelledby', labelId + ' ' + valueId);
        else
            trigger.setAttribute(
                'aria-label',
                select.getAttribute('aria-label') || select.name || 'Chọn một mục',
            );
        if (select.required) trigger.setAttribute('aria-required', 'true');
        // BIẾN JS value: phần tử <span> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const value = document.createElement('span');
        value.id = valueId;
        // BIẾN JS chevron: phần tử <i> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const chevron = document.createElement('i');
        chevron.className = 'select-chevron';
        chevron.setAttribute('aria-hidden', 'true');
        trigger.append(value, chevron);
        // BIẾN JS menu: phần tử <ul> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const menu = document.createElement('ul');
        menu.className = 'filter-select-menu';
        menu.id = baseId + '-options';
        menu.setAttribute('role', 'listbox');
        if (labelId) menu.setAttribute('aria-labelledby', labelId);
        menu.hidden = true;
        trigger.setAttribute('aria-controls', menu.id);
        // BIẾN JS options: mảng option HTML gốc để đối chiếu value/text/disabled/selected.
        let options = [];
        // BIẾN JS active: chỉ số mục đang được trỏ bằng bàn phím/chuột, khác selectedIndex đã được
        // xác nhận.
        let active = select.selectedIndex;
        // BIẾN JS search: chuỗi ký tự người dùng gõ nhanh để nhảy tới option bắt đầu bằng chuỗi đó.
        let search = '';
        // BIẾN JS searchTimer: timeout xóa chuỗi tìm option sau khi ngừng gõ, để lần gõ mới bắt đầu
        // lại.
        let searchTimer;

        // BIẾN JS error: phần tử <span> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
        // append vào DOM.
        const error = document.createElement('span');
        error.className = 'select-error';
        error.id = baseId + '-error';
        error.setAttribute('aria-live', 'polite');
        error.hidden = true;
        // BIẾN JS items: các mục DOM hiển thị trong menu tùy biến, ánh xạ theo index tới options
        // gốc.
        let items = [];
        // Dựng danh sách lựa chọn giao diện theo các option hiện có của select gốc.
        // HÀM JS rebuild: đọc option gốc để dựng lại menu select khi DOM/thuộc tính của select thay
        // đổi.
        // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay
        // DOM/trạng thái hoặc gửi form như thân hàm.
        function rebuild() {
            options = Array.from(select.options);
            menu.replaceChildren();
            items = options.map((option, index) => {
                // BIẾN JS item: phần tử <li> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi
                // mới append vào DOM.
                const item = document.createElement('li');
                item.id = menu.id + '-' + index;
                item.className = 'filter-select-option';
                item.setAttribute('role', 'option');
                // BIẾN JS text: phần tử <span> mới tạo trong RAM, được gán class/nội dung/sự kiện
                // rồi mới append vào DOM.
                const text = document.createElement('span');
                text.textContent = option.textContent;
                // BIẾN JS check: phần tử <i> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi
                // mới append vào DOM.
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

        // Đưa nhãn đang chọn và trạng thái disabled của select gốc lên nút giao diện tùy biến.
        // HÀM JS sync: đồng bộ nhãn, disabled, lỗi và dấu chọn của menu từ select gốc.
        // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay
        // DOM/trạng thái hoặc gửi form như thân hàm.
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
                // BIẾN JS selected: biến thể được ưu tiên chọn hoặc cờ một option đang được chọn,
                // tùy biểu thức khởi tạo.
                const selected = index === select.selectedIndex;
                item.setAttribute('aria-selected', String(selected));
                item.querySelector('i').hidden = !selected;
            });
        }
        // Di chuyển lựa chọn đang được trỏ bằng bàn phím và cập nhật trạng thái truy cập.
        // HÀM JS activate: giới hạn và cập nhật chỉ số active để người dùng di chuyển mục bằng bàn
        // phím/chuột.
        // THAM SỐ index: chỉ số option/mục menu cần trỏ hoặc chọn, được kiểm tra trong giới hạn.
        // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay
        // DOM/trạng thái hoặc gửi form như thân hàm.
        function activate(index) {
            active = Math.max(0, Math.min(index, items.length - 1));
            items.forEach((item, i) => item.classList.toggle('is-active', i === active));
            if (items[active]) trigger.setAttribute('aria-activedescendant', items[active].id);
        }
        // Đóng danh sách lựa chọn và cập nhật trạng thái mở cho công cụ hỗ trợ truy cập.
        // HÀM JS close: ẩn menu, cập nhật ARIA và xóa chuỗi gõ nhanh cùng timeout.
        // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay
        // DOM/trạng thái hoặc gửi form như thân hàm.
        function close() {
            menu.hidden = true;
            trigger.setAttribute('aria-expanded', 'false');
            trigger.removeAttribute('aria-activedescendant');
            search = '';
            window.clearTimeout(searchTimer);
        }
        // Mở danh sách lựa chọn khi điều khiển còn hoạt động, chuẩn bị mục được chọn để điều hướng.
        // HÀM JS open: mở menu khi select hoạt động, chọn phía trên/dưới theo chỗ trống viewport.
        // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay
        // DOM/trạng thái hoặc gửi form như thân hàm.
        function open() {
            if (select.disabled || !items.length) return;
            document
                .querySelectorAll('.filter-select-trigger[aria-expanded="true"]')
                .forEach((other) => {
                    if (other !== trigger) other.dispatchEvent(new Event('close-select'));
                });
            menu.hidden = false;
            menu.classList.remove('is-above');
            // BIẾN JS rect: tọa độ/kích thước của trigger trên viewport, dùng tính chỗ mở menu.
            const rect = trigger.getBoundingClientRect();
            // BIẾN JS below: khoảng trống pixel bên dưới trigger sau trừ lề 16px.
            const below = window.innerHeight - rect.bottom - 16;
            // BIẾN JS above: khoảng trống pixel bên trên trigger sau trừ lề 16px.
            const above = rect.top - 16;
            // BIẾN JS showAbove: true khi phía dưới thiếu chỗ và phía trên thoáng hơn, menu mở lên
            // trên.
            const showAbove = below < Math.min(menu.scrollHeight, 260) && above > below;
            menu.classList.toggle('is-above', showAbove);
            menu.style.maxHeight = Math.max(80, Math.min(260, showAbove ? above : below)) + 'px';
            trigger.setAttribute('aria-expanded', 'true');
            activate(select.selectedIndex);
        }
        // Áp dụng lựa chọn về select gốc, phát sự kiện change cho bộ lọc/form và đóng danh sách.
        // HÀM JS choose: xác nhận option, cập nhật select gốc, đóng menu và phát change nếu giá trị
        // đổi.
        // THAM SỐ index: chỉ số option/mục menu cần trỏ hoặc chọn, được kiểm tra trong giới hạn.
        // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay
        // DOM/trạng thái hoặc gửi form như thân hàm.
        function choose(index) {
            if (select.disabled || !options[index] || options[index].disabled) return;
            // BIẾN JS changed: true khi lựa chọn mới khác selectedIndex cũ, chỉ khi đó phát change
            // để tránh gửi form thừa.
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
            // BIẾN JS key: tên khóa lọc hoặc phím vừa nhấn; biểu thức lấy từ dataset hay event.key
            // phân biệt hai trường hợp.
            const key = event.key;
            if (key === 'Tab' || key === 'Escape') {
                close();
                if (key === 'Escape') event.preventDefault();
                return;
            }
            if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(key)) {
                event.preventDefault();
                // BIẾN JS wasClosed: cờ menu đang đóng trước khi xử lý phím, dùng quyết định mở hay
                // di chuyển mục active.
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
                // BIẾN JS index: vị trí option khớp chuỗi gõ, -1 nếu không có; không phải id bản
                // ghi SQL.
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
            // BIẾN JS firstInvalid: ô select/input/textarea lỗi đầu tiên của form, dùng đưa focus
            // đúng chỗ cần sửa.
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
