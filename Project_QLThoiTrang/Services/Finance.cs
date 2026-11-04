// VAI TRÒ FILE: Giá vốn bình quân, doanh thu sau trả hàng và đối soát/chi công nợ nhà cung cấp. Các
// số tiền nghiệp
// vụ được kiểm tra trước khi ghi SQL.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // HÀM UpdateCosting: Chụp giá vốn cho dòng đơn mới, rồi tính giá vốn bình quân di động khi nhập
    // kho/nhận hàng hủy/nhận hàng trả. Nếu tồn cũ hoặc hàng nhập thiếu giá vốn thì giữ kết quả null
    // thay vì tự suy đoán lợi nhuận.
    // THAM SỐ db (StoreDbContext): DbContext đang dùng trong giao dịch; đọc DbSet để lấy dữ liệu,
    // sửa entity rồi SaveChanges để đẩy SQL.
    // THAM SỐ data (StoreData): StoreData sau khi callback nghiệp vụ chạy, dùng đối chiếu và cập
    // nhật giá vốn.
    // THAM SỐ before (Dictionary<int, int>): bảng tra VariantId → tổng tồn trước giao dịch để tính
    // giá vốn bình quân.
    // THAM SỐ oldStatuses (Dictionary<string, string>): bảng tra mã đơn → trạng thái trước giao
    // dịch, giúp phát hiện đơn vừa chuyển sang hủy.
    // THAM SỐ approved (HashSet<int>): tập mã phiếu đã Approved trước giao dịch; so sánh để chỉ
    // tính phần hàng trả vừa được duyệt.
    // THAM SỐ movementCount (int): số nhật ký tồn đã có trước giao dịch; Skip(movementCount) chỉ
    // lấy biến động mới phát sinh.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA v (InventoryValue): bản ghi giá vốn bình quân của một biến thể; giá null nghĩa là
    // chưa xác định; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA a ((int Variant, int Quantity, decimal? Cost)): (int Variant, int Quantity,
    // decimal? Cost); tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    void UpdateCosting(
        StoreDbContext db,
        StoreData data,
        Dictionary<int, int> before,
        Dictionary<string, string> oldStatuses,
        HashSet<int> approved,
        int movementCount
    )
    {
        // BIẾN values (Dictionary<int, InventoryValue>): bảng tra VariantId → InventoryValue hiện
        // có, dùng chụp và cập nhật giá vốn.
        var values = db.InventoryValues.ToDictionary(v => v.VariantId);
        // BIẾN LẶP order (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
        // khoản tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var order in data.Orders)
        // BIẾN LẶP line (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua;
        // mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var line in order.Lines.Where(l => l.Id == 0))
            line.UnitCost = values.GetValueOrDefault(line.VariantId)?.AverageCost;
        // BIẾN additions (List<(int Variant, int Quantity, decimal? Cost)>): các lượng nhập/nhận
        // lại và giá vốn tương ứng phát sinh trong giao dịch, gom theo biến thể để tính bình quân.
        var additions = data
            .StockMovements.Skip(movementCount)
            .Where(m => m.Reason == "Nhập kho" && m.VariantId.HasValue)
            .Select(m =>
                (Variant: m.VariantId!.Value, Quantity: m.Delta, Cost: (decimal?)m.UnitCost)
            )
            .ToList();
        // BIẾN LẶP order (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
        // khoản tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (
            var order in data.Orders.Where(o =>
                o.Status == "Đã hủy"
                && oldStatuses.GetValueOrDefault(o.Id) != o.Status
                && oldStatuses.ContainsKey(o.Id)
            )
        )
        // BIẾN LẶP l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; mỗi
        // lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var l in order.Lines)
            additions.Add((l.VariantId, l.Quantity, l.UnitCost));
        // BIẾN LẶP r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
        // hoàn; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (
            var r in data.Returns.Where(r => r.Status == "Approved" && !approved.Contains(r.Id))
        )
        // BIẾN LẶP returned (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn
        // và tình trạng hàng; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
        // foreach này.
        foreach (var returned in r.Lines)
        {
            // BIẾN l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua.
            var l = data
                .Orders.Single(o => o.Id == r.OrderId)
                .Lines.Single(l => l.Id == returned.OrderLineId);
            additions.Add((l.VariantId, returned.Quantity, l.UnitCost));
        }

        // BIẾN LẶP g (IGrouping<int, (int Variant, int Quantity, decimal? Cost)>): một nhóm phần tử
        // có cùng Key do GroupBy tạo ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi lượt nhận một
        // phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var g in additions.GroupBy(a => a.Variant))
        {
            // BIẾN oldQuantity (int): tồn biến thể trước giao dịch, dùng làm trọng số cho phần giá
            // vốn cũ.
            int oldQuantity = before.GetValueOrDefault(g.Key);
            // BIẾN value (InventoryValue): bản ghi giá vốn bình quân của một biến thể; giá null
            // nghĩa là chưa xác định.
            var value = values.GetValueOrDefault(g.Key);
            if (value == null)
            {
                value = new() { VariantId = g.Key };
                db.InventoryValues.Add(value);
                values.Add(g.Key, value);
            }

            value.AverageCost =
                (oldQuantity > 0 && !value.AverageCost.HasValue) || g.Any(a => !a.Cost.HasValue)
                    ? null
                    : decimal.Round(
                        (
                            oldQuantity * (value.AverageCost ?? 0)
                            + g.Sum(a => a.Quantity * a.Cost!.Value)
                        ) / (oldQuantity + g.Sum(a => a.Quantity)),
                        2
                    );
            value.UpdatedUtc = DateTime.UtcNow;
            value.Note = "Bình quân di động sau nhập/nhận lại";
        }
    }

    // HÀM SetInventoryValue: Inventory/Admin khai báo giá vốn cho biến thể; cost 0–100 triệu, lý do
    // 5–300 ký tự và tổng tồn phải bằng expected. Giá mới phục vụ giao dịch phát sinh sau, không
    // ghi đè giá vốn đã chụp trên đơn cũ.
    // THAM SỐ variantId (int): mã biến thể sản phẩm, dùng phân biệt đúng tổ hợp size/màu thay vì
    // chỉ mã sản phẩm.
    // THAM SỐ expected (int): số tồn người dùng đã thấy lúc mở form; so với tồn hiện tại để phát
    // hiện thay đổi và tránh ghi đè.
    // THAM SỐ cost (decimal): giá vốn VND cho một đơn vị hàng; phải không âm và nằm trong giới hạn
    // của hàm.
    // THAM SỐ note (string): ghi chú hoặc căn cứ điều chỉnh; được kiểm tra độ dài trước khi ghi
    // nhận người/thời điểm cập nhật.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA v (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
    // và SKU; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA t (TonKhoRow): entity của bảng TonKho, tồn của từng biến thể tại từng kho; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (InventoryValue): bản ghi giá vốn bình quân của một biến thể; giá null nghĩa là
    // chưa xác định; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void SetInventoryValue(
        int variantId,
        int expected,
        decimal cost,
        string note,
        int actor
    ) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Inventory");
            // ĐIỀU KIỆN: Giá vốn không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(cost >= 0 && cost <= 100000000, "Giá vốn không hợp lệ.");
            note = Text(note, 5, 300, "Lý do khai báo giá vốn");
            // ĐIỀU KIỆN: Không tìm thấy biến thể. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(
                db.BienTheSanPham.Any(v => v.MaBienThe == variantId),
                "Không tìm thấy biến thể."
            );
            // ĐIỀU KIỆN: Tồn đã thay đổi, tải lại trang. Không đạt thì Require ném lỗi và dừng
            // luồng xử lý.
            Require(
                db.TonKho.Where(t => t.MaBienThe == variantId)
                    .Sum(t => (int?)t.SoLuongTon)
                    .GetValueOrDefault() == expected,
                "Tồn đã thay đổi, tải lại trang."
            );
            // BIẾN value (InventoryValue): bản ghi giá vốn bình quân của một biến thể; giá null
            // nghĩa là chưa xác định; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm
            // tra hoặc ?? ngay sau.
            var value = db.InventoryValues.SingleOrDefault(v => v.VariantId == variantId);
            if (value == null)
            {
                value = new() { VariantId = variantId };
                db.InventoryValues.Add(value);
            }

            value.AverageCost = cost;
            value.UpdatedUtc = DateTime.UtcNow;
            value.ActorId = actor;
            value.Note = note;
            return 0;
        });

    // HÀM Profit: Lập dòng doanh thu/giá vốn theo ngày tạo đơn, chỉ xét đơn đã giao/hoàn thành/đã
    // hoàn hàng. Trừ lượng trả và tiền hoàn của phiếu Approved; giá vốn null được giữ để báo cáo
    // nhận biết dữ liệu còn thiếu.
    // THAM SỐ from (DateTime): ngày bắt đầu kỳ lọc/báo cáo; được lấy phần Date theo quy tắc trong
    // hàm.
    // THAM SỐ to (DateTime): ngày kết thúc kỳ lọc/báo cáo; phải không trước ngày bắt đầu.
    // TRẢ VỀ (List<ProfitLine>): danh sách/truy vấn các phần tử: dòng doanh thu/giá vốn sau trả
    // hàng phục vụ báo cáo lợi nhuận; có thể rỗng khi không có dữ liệu khớp.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn và tình
    // trạng hàng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Read; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public List<ProfitLine> Profit(DateTime from, DateTime to) =>
        Read(d =>
        {
            // ĐIỀU KIỆN: Khoảng ngày không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(from.Date <= to.Date, "Khoảng ngày không hợp lệ.");
            // BIẾN rows (List<ProfitLine>): danh sách/truy vấn các phần tử: dòng doanh thu/giá vốn
            // sau trả hàng phục vụ báo cáo lợi nhuận; được khởi tạo mới trong bộ nhớ tại đây, không
            // đồng nghĩa SQL đã commit.
            var rows = new List<ProfitLine>();
            // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
            // khoản tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
            // này.
            foreach (
                var o in d.Orders.Where(o =>
                    o.Created.Date >= from.Date
                    && o.Created.Date <= to.Date
                    && o.Status is "Đã giao" or "Hoàn thành" or "Đã hoàn hàng"
                )
            )
            {
                // BIẾN LẶP l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm
                // mua; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
                foreach (var l in o.Lines)
                {
                    // BIẾN returned (int): tổng số lượng của dòng đơn đã được trả và duyệt
                    // Approved, dùng trừ khỏi lượng bán ròng.
                    int returned = d
                        .Returns.Where(r => r.OrderId == o.Id && r.Status == "Approved")
                        .SelectMany(r => r.Lines)
                        .Where(r => r.OrderLineId == l.Id)
                        .Sum(r => r.Quantity);
                    // BIẾN refunds (decimal): tổng tiền hoàn đã duyệt của dòng đơn, dùng trừ khỏi
                    // doanh thu sau ưu đãi.
                    decimal refunds = d
                        .Returns.Where(r => r.OrderId == o.Id && r.Status == "Approved")
                        .SelectMany(r => r.Lines)
                        .Where(r => r.OrderLineId == l.Id)
                        .Sum(r => r.Refund);
                    // BIẾN quantity (int): số nguyên 32 bit.
                    int quantity = Math.Max(0, l.Quantity - returned);
                    // BIẾN revenue (decimal): doanh thu hàng ròng của dòng sau phân bổ giảm giá và
                    // trừ hoàn; đơn hoàn toàn bộ cho doanh thu 0.
                    decimal revenue =
                        o.Status == "Đã hoàn hàng"
                            ? 0
                            : Math.Max(
                                0,
                                (
                                    o.Subtotal == 0
                                        ? 0
                                        : decimal.Round(
                                            l.Price
                                                * l.Quantity
                                                * (o.Subtotal - o.Discount)
                                                / o.Subtotal,
                                            2
                                        )
                                ) - refunds
                            );
                    rows.Add(
                        new(
                            o.Id,
                            o.Created,
                            l.Name,
                            quantity,
                            revenue,
                            quantity == 0 ? 0 : l.UnitCost * quantity
                        )
                    );
                }
            }

            return rows;
        });

    // HÀM ReconcileSupplier: Sales/Admin đối soát số phải trả của phiếu nhập trước khi chi tiền;
    // yêu cầu phiếu tồn tại, số tiền hợp lệ, có căn cứ đối soát và chưa có SupplierPayment. Thêm
    // hoặc sửa SupplierAccount.
    // THAM SỐ receiptId (int): khóa SQL của phiếu nhập cần xem/đối soát/chi tiền, không phải mã đơn
    // bán hàng.
    // THAM SỐ amount (decimal): tổng số phải trả VND được đối soát của phiếu nhập, không phải một
    // lần chi thực tế.
    // THAM SỐ note (string): ghi chú hoặc căn cứ điều chỉnh; được kiểm tra độ dài trước khi ghi
    // nhận người/thời điểm cập nhật.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA a (SupplierAccount): số phải trả đã đối soát của một phiếu nhập; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (SupplierPayment): chứng từ chi thực tế cho công nợ nhà cung cấp; tên này chỉ
    // có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void ReconcileSupplier(int receiptId, decimal amount, string note, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Sales");
            // ĐIỀU KIỆN: Phiếu nhập không tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(db.PhieuNhap.Any(p => p.MaPhieuNhap == receiptId), "Phiếu nhập không tồn tại.");
            // ĐIỀU KIỆN: Công nợ không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(amount >= 0 && amount <= 10000000000000m, "Công nợ không hợp lệ.");
            note = Text(note, 5, 300, "Căn cứ đối soát");
            // BIẾN account (SupplierAccount): số phải trả đã đối soát của một phiếu nhập; kết quả
            // tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var account = db.SupplierAccounts.SingleOrDefault(a => a.ReceiptId == receiptId);
            // ĐIỀU KIỆN: Phiếu đã chi tiền; không thay số phải trả. Không đạt thì Require ném lỗi
            // và dừng luồng xử lý.
            Require(
                !db.SupplierPayments.Any(p => p.ReceiptId == receiptId),
                "Phiếu đã chi tiền; không thay số phải trả."
            );
            if (account == null)
            {
                account = new() { ReceiptId = receiptId };
                db.SupplierAccounts.Add(account);
            }

            account.Amount = amount;
            account.Note = note;
            account.ActorId = actor;
            account.UpdatedUtc = DateTime.UtcNow;
            return 0;
        });

    // HÀM PaySupplier: Sales/Admin thêm chứng từ chi cho phiếu đã đối soát; mã chứng từ chưa dùng,
    // amount > 0 và không vượt công nợ còn lại sau các lần chi trước. Lưu người chi và thời điểm
    // UTC.
    // THAM SỐ receiptId (int): khóa SQL của phiếu nhập cần xem/đối soát/chi tiền, không phải mã đơn
    // bán hàng.
    // THAM SỐ amount (decimal): số tiền VND thực chi lần này, > 0 và không vượt công nợ còn lại của
    // phiếu.
    // THAM SỐ reference (string): mã chứng từ/biên nhận do người dùng cung cấp; các luồng tiền/nhập
    // kho kiểm tra chưa dùng để chống ghi trùng.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA a (SupplierAccount): số phải trả đã đối soát của một phiếu nhập; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (SupplierPayment): chứng từ chi thực tế cho công nợ nhà cung cấp; tên này chỉ
    // có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void PaySupplier(int receiptId, decimal amount, string reference, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Sales");
            // BIẾN account (SupplierAccount): số phải trả đã đối soát của một phiếu nhập; kết quả
            // tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var account = db.SupplierAccounts.SingleOrDefault(a => a.ReceiptId == receiptId);
            // ĐIỀU KIỆN: Cần đối soát phiếu nhập trước khi chi. Không đạt thì Require ném lỗi và
            // dừng luồng xử lý.
            Require(account != null, "Cần đối soát phiếu nhập trước khi chi.");
            reference = Text(reference, 1, 100, "Chứng từ chi");
            // ĐIỀU KIỆN: Chứng từ chi đã sử dụng. Không đạt thì Require ném lỗi và dừng luồng xử
            // lý.
            Require(
                !db.SupplierPayments.Any(p => p.Reference == reference),
                "Chứng từ chi đã sử dụng."
            );
            // BIẾN paid (decimal): tổng các lần đã chi cho phiếu nhập, dùng tính số công nợ còn
            // được phép trả.
            decimal paid =
                db.SupplierPayments.Where(p => p.ReceiptId == receiptId)
                    .Sum(p => (decimal?)p.Amount)
                ?? 0;
            // ĐIỀU KIỆN: Số tiền vượt phần công nợ còn lại. Không đạt thì Require ném lỗi và dừng
            // luồng xử lý.
            Require(
                amount > 0 && amount <= account!.Amount - paid,
                "Số tiền vượt phần công nợ còn lại."
            );
            db.SupplierPayments.Add(
                new()
                {
                    ReceiptId = receiptId,
                    Amount = amount,
                    Reference = reference,
                    ActorId = actor,
                    CreatedUtc = DateTime.UtcNow,
                }
            );
            return 0;
        });
}
