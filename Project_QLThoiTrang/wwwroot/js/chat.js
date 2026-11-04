// VAI TRÒ FILE: chỉ khởi tạo trên phần tử data-chat của Views/Chat/Index.cshtml; gọi Chat/Messages,
// Chat/Send và
// Chat/Read. Quyền hội thoại, giới hạn tốc độ và chống trùng được kiểm tra ở CustomerFeatures.cs.

(() => {
    // BIẾN JS root: phần tử DOM tìm bằng selector [data-chat]; vùng data-chat; không tìm thấy thì
    // file dừng khởi tạo, không chạy trên trang khác.
    const root = document.querySelector('[data-chat]');
    if (!root) return;
    // BIẾN JS form: phần tử DOM tìm bằng selector [data-chat-form]; form gửi tin, cung cấp
    // text/customerId và token CSRF.
    // BIẾN JS list: phần tử DOM tìm bằng selector [data-chat-messages]; vùng chứa các bong bóng tin
    // nhắn để thêm mới hoặc chèn lịch sử.
    // BIẾN JS status: phần tử DOM tìm bằng selector [data-chat-status]; phần tử chữ báo kết nối/gửi
    // lỗi cho người dùng, không phải mã trạng thái SQL.
    // BIẾN JS older: phần tử DOM tìm bằng selector [data-chat-older]; nút tải tin cũ trước mã
    // first, bị khóa khi đang tải lịch sử.
    const form = root.querySelector('[data-chat-form]'),
        list = root.querySelector('[data-chat-messages]'),
        status = root.querySelector('[data-chat-status]'),
        older = root.querySelector('[data-chat-older]');
    // BIẾN JS customerId: mã khách của hội thoại từ data-customer-id, server vẫn kiểm tra quyền
    // trước đọc/gửi.
    // BIẾN JS csrf: phần tử DOM tìm bằng selector [name="__RequestVerificationToken"]; giá trị
    // __RequestVerificationToken của form để gửi kèm các POST chat.
    const customerId = root.dataset.customerId,
        csrf = form.querySelector('[name="__RequestVerificationToken"]').value;
    // BIẾN JS first: mã tin nhỏ nhất đã render, làm mốc before khi tải thêm lịch sử.
    // BIẾN JS last: mã tin lớn nhất đã render, làm mốc after để lấy tin mới và đánh dấu đã đọc.
    // BIẾN JS loading: cờ một lần poll tin mới đang chạy, ngăn poll trùng đồng thời.
    // BIẾN JS pending: tin đang chờ gửi gồm text/clientToken; thử lại cùng text giữ token cũ để
    // tránh tạo trùng.
    let first = 0,
        last = 0,
        loading = false,
        pending = null;
    // Gửi POST tới API chat cùng customerId và token chống CSRF; chuyển HTTP lỗi/redirect đăng nhập
    // thành thông
    // báo cho giao diện.
    // HÀM JS post: gửi POST API chat cùng mã khách/token CSRF, chuyển lỗi HTTP thành Error.
    // THAM SỐ path: đường dẫn endpoint chat cần POST.
    // THAM SỐ data: các trường dữ liệu POST, được ghép thêm customerId và token CSRF.
    // TRẢ VỀ: Promise vì có fetch/await; post trả Response khi thành công, load tự lên lịch lần gọi
    // tiếp.
    async function post(path, data) {
        // BIẾN JS response: Response từ fetch API chat, kiểm tra ok và redirect trước đọc kết quả.
        const response = await fetch(path, {
            method: 'POST',
            body: new URLSearchParams({ ...data, customerId, __RequestVerificationToken: csrf }),
        });
        if (response.redirected || !response.ok) {
            // BIẾN JS error: thông báo lỗi server trả hoặc lỗi bị bắt khi fetch/gửi, dùng hiện vào
            // status.
            let error;
            try {
                error = (await response.json()).error;
            } catch {}
            throw new Error(error || 'Không gửi được. Kiểm tra phiên đăng nhập.');
        }
        return response;
    }
    // Thêm tin chưa xuất hiện theo data-message-id, dùng textContent để nội dung được coi là chữ;
    // giữ vị trí khi
    // chèn lịch sử và cuộn xuống nếu đang ở cuối.
    // HÀM JS render: dựng tin chưa có, dùng textContent và giữ cuộn khi thêm mới/chèn lịch sử.
    // THAM SỐ messages: mảng tin JSON cần render.
    // THAM SỐ prepend = false: true chèn lịch sử lên đầu, false thêm tin mới cuối danh sách; mặc
    // định false.
    // TRẢ VỀ: các hàm cập nhật giao diện không trả dữ liệu nghiệp vụ; tác động là thay DOM/trạng
    // thái hoặc gửi form như thân hàm.
    function render(messages, prepend = false) {
        // BIẾN JS atBottom: true khi vị trí cuộn cách cuối danh sách dưới 80px, khi thêm tin mới
        // mới tự cuộn xuống.
        const atBottom = list.scrollHeight - list.scrollTop - list.clientHeight < 80;
        // BIẾN JS fragment: DocumentFragment gom các bong bóng để chèn DOM một lần.
        const fragment = document.createDocumentFragment();
        // BIẾN JS message: một tin JSON từ server có id/text/mine/staff/created, không phải nội
        // dung HTML được tin cậy.
        for (const message of messages) {
            if (list.querySelector('[data-message-id="' + message.id + '"]')) continue;
            // BIẾN JS bubble: phần tử <div> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi
            // mới append vào DOM.
            const bubble = document.createElement('div');
            bubble.className = 'chat-bubble ' + (message.mine ? 'mine' : '');
            bubble.dataset.messageId = message.id;
            // BIẾN JS label: phần tử <small> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi
            // mới append vào DOM.
            const label = document.createElement('small');
            label.textContent =
                (message.staff ? 'Nhân viên' : 'Khách hàng') +
                ' · ' +
                new Date(message.created).toLocaleString('vi-VN');
            // BIẾN JS text: phần tử <p> mới tạo trong RAM, được gán class/nội dung/sự kiện rồi mới
            // append vào DOM.
            const text = document.createElement('p');
            text.textContent = message.text;
            bubble.append(label, text);
            fragment.append(bubble);
            first = first ? Math.min(first, message.id) : message.id;
            last = Math.max(last, message.id);
        }
        // BIẾN JS height: scrollHeight trước khi prepend tin cũ, dùng bù scrollTop để giữ vị trí
        // người đang đọc.
        const height = list.scrollHeight;
        if (prepend) {
            list.prepend(fragment);
            list.scrollTop += list.scrollHeight - height;
        } else {
            list.append(fragment);
            if (atBottom) list.scrollTop = list.scrollHeight;
        }
    }
    // Lấy tin có id > last, đánh dấu đã đọc khi tab hiện; lên lịch lấy lại sau 3 giây hoặc 15 giây
    // khi tab ẩn.
    // loading ngăn hai lần tải đồng thời.
    // HÀM JS load: poll tin mới, render, báo đã đọc khi tab hiện và hẹn lần poll tiếp theo.
    // TRẢ VỀ: Promise vì có fetch/await; post trả Response khi thành công, load tự lên lịch lần gọi
    // tiếp.
    async function load() {
        if (loading) return;
        loading = true;
        try {
            // BIẾN JS response: Response từ fetch API chat, kiểm tra ok và redirect trước đọc kết
            // quả.
            const response = await fetch(
                '/Chat/Messages?customerId=' + customerId + '&after=' + last,
            );
            if (!response.ok) throw new Error();
            // BIẾN JS messages: mảng tin JSON nhận được từ API, theo mốc before/after của lần gọi.
            const messages = await response.json();
            render(messages);
            if (last && document.visibilityState === 'visible') await post('/Chat/Read', { last });
            status.textContent = 'Tự cập nhật mỗi 3 giây.';
        } catch {
            status.textContent = 'Chưa kết nối được. Hệ thống sẽ thử tải lại.';
        } finally {
            loading = false;
            window.setTimeout(load, document.hidden ? 15000 : 3000);
        }
    }
    // TẢI LỊCH SỬ: dùng before = first, chèn tin cũ lên đầu và ẩn nút khi lô trả về ít hơn 100 tin.
    older.addEventListener('click', async () => {
        older.disabled = true;
        try {
            // BIẾN JS response: Response từ fetch API chat, kiểm tra ok và redirect trước đọc kết
            // quả.
            const response = await fetch(
                '/Chat/Messages?customerId=' + customerId + '&before=' + first,
            );
            if (!response.ok) throw new Error();
            // BIẾN JS messages: mảng tin JSON nhận được từ API, theo mốc before/after của lần gọi.
            const messages = await response.json();
            render(messages, true);
            older.hidden = messages.length < 100;
        } catch {
            status.textContent = 'Không tải được tin trước.';
        } finally {
            older.disabled = false;
        }
    });
    // GỬI TIN: trim nội dung, tạo clientToken cho một tin đang chờ; khi thử lại cùng nội dung giữ
    // token cũ để
    // server trả kết quả cũ thay vì thêm trùng.
    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        // BIẾN JS input: ô nhập text của form chat, được xóa chỉ sau khi server chấp nhận gửi.
        // BIẾN JS text: chuỗi tin đã trim khi gửi hoặc phần tử p khi render, textContent bảo đảm
        // nội dung là chữ.
        const input = form.elements.text,
            text = input.value.trim();
        if (!text) return;
        if (!pending || pending.text !== text) pending = { text, clientToken: crypto.randomUUID() };
        // BIẾN JS button: phần tử DOM tìm bằng selector button; nút gửi tin, disabled trong thời
        // gian POST để tránh bấm liên tục.
        const button = form.querySelector('button');
        button.disabled = true;
        try {
            await post('/Chat/Send', pending);
            input.value = '';
            pending = null;
            status.textContent = 'Đã gửi.';
        } catch (error) {
            status.textContent = error.message;
        } finally {
            button.disabled = false;
        }
    });
    load();
})();
