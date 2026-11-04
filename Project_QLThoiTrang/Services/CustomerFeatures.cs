using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    public BankTransferOptions BankOptions =>
        configuration?.GetSection("BankTransfer").Get<BankTransferOptions>() ?? new();

    // ChatAccess: Kiểm tra quyền truy cập hội thoại.
    static void ChatAccess(Data.StoreDbContext db, int actor, int customer)
    {
        Require(
            db.KhachHang.Any(c => c.MaKH == customer && c.TrangThai),
            "Khách hàng không hoạt động."
        );
        if (actor == customer && actor > 0)
            return;
        NativePermission(db, actor, "Support");
    }

    // Messages: Lấy các tin nhắn theo mốc thời gian.
    public List<ChatMessage> Messages(int customer, int actor, long before = 0, long after = 0) =>
        NativeRead(db =>
        {
            ChatAccess(db, actor, customer);
            var query = db.ChatMessages.Where(m =>
                m.CustomerId == customer && (before == 0 || m.Id < before) && m.Id > after
            );
            return (after > 0 ? query.OrderBy(m => m.Id) : query.OrderByDescending(m => m.Id))
                .Take(100)
                .ToList()
                .OrderBy(m => m.Id)
                .ToList();
        });

    // SendMessage: Lưu tin nhắn và ngăn gửi trùng.
    public ChatMessage SendMessage(int customer, int actor, string text, Guid clientToken) =>
        NativeWrite(db =>
        {
            ChatAccess(db, actor, customer);
            text = Text(text, 1, 2000, "Tin nhắn");

            Require(clientToken != Guid.Empty, "Mã tin nhắn không hợp lệ.");
            var old = db.ChatMessages.SingleOrDefault(m =>
                m.CustomerId == customer && m.ClientToken == clientToken
            );

            if (old != null)
            {
                Require(old.SenderId == actor && old.Text == text, "Mã tin nhắn đã sử dụng.");
                return old;
            }

            var since = DateTime.UtcNow.AddMinutes(-1);

            Require(
                db.ChatMessages.Count(m => m.SenderId == actor && m.CreatedUtc > since) < 20,
                "Bạn gửi quá nhanh. Vui lòng chờ một phút."
            );
            var thread = db.ChatThreads.SingleOrDefault(t => t.CustomerId == customer);
            if (thread == null)
            {
                thread = new() { CustomerId = customer };
                db.ChatThreads.Add(thread);
            }

            thread.UpdatedUtc = DateTime.UtcNow;
            var message = new ChatMessage
            {
                CustomerId = customer,
                SenderId = actor,
                Text = text,
                ClientToken = clientToken,
                CreatedUtc = DateTime.UtcNow,
            };
            db.ChatMessages.Add(message);
            return message;
        });

    // MarkChatRead: Đánh dấu tin nhắn hội thoại đã đọc.
    public void MarkChatRead(int customer, int actor, long last) =>
        NativeWrite(db =>
        {
            ChatAccess(db, actor, customer);
            var t = db.ChatThreads.SingleOrDefault(t => t.CustomerId == customer);
            if (t == null)
                return 0;
            long max =
                db.ChatMessages.Where(m => m.CustomerId == customer).Select(m => (long?)m.Id).Max()
                ?? 0;
            last = Math.Min(Math.Max(0, last), max);
            if (actor > 0)
                t.CustomerReadId = Math.Max(t.CustomerReadId, last);
            else
                t.StaffReadId = Math.Max(t.StaffReadId, last);
            return 0;
        });

    // ChatInbox: Lấy danh sách hội thoại hỗ trợ.
    public List<(int Id, string Name, DateTime Updated, int Unread)> ChatInbox(int actor) =>
        NativeRead(db =>
        {
            NativePermission(db, actor, "Support");
            return (
                from t in db.ChatThreads
                join c in db.KhachHang on t.CustomerId equals c.MaKH
                orderby t.UpdatedUtc descending
                select new
                {
                    c.MaKH,
                    c.HoTen,
                    t.UpdatedUtc,
                    Unread = db.ChatMessages.Count(m =>
                        m.CustomerId == t.CustomerId && m.SenderId > 0 && m.Id > t.StaffReadId
                    ),
                }
            )
                .Take(200)
                .ToList()
                .Select(t => (t.MaKH, t.HoTen, t.UpdatedUtc, t.Unread))
                .ToList();
        });
}
