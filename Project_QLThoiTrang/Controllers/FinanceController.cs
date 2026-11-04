using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

// THAM SỐ KHỞI TẠO store (StoreService): StoreService lấy dữ liệu nghiệp vụ và kiểm tra/lưu SQL;
// được DI cung cấp, không phải dữ liệu form; do DI hoặc nơi gọi constructor truyền vào.
[
    Authorize(Roles = "Admin,Sales"),
    ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)
]
public class FinanceController(StoreService store) : Controller
{
    // THUỘC TÍNH FinanceController.Actor (int): id lấy từ claim của người đang đăng nhập; có thể âm
    // với nhân viên, dương với khách; không lấy từ ô input do người dùng tự chọn.
    int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // HÀM Index: Mở báo cáo doanh thu/giá vốn/lợi nhuận theo ngày; kiểm tra from <= to rồi gọi
    // Profit. Quyền toàn controller là Admin/Sales.
    // THAM SỐ from (DateTime?): ngày bắt đầu kỳ lọc/báo cáo; được lấy phần Date theo quy tắc trong
    // hàm.
    // THAM SỐ to (DateTime?): ngày kết thúc kỳ lọc/báo cáo; phải không trước ngày bắt đầu.
    // THAM SỐ period (string?): kỳ thời gian nhanh, như today/last7/last30/month; controller đổi
    // thành mốc ngày ở switch.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: GET /Finance/Index. Tham số hàm: (DateTime? from, DateTime? to, string? period).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Finance/Index.cshtml.
    public IActionResult Index(DateTime? from, DateTime? to, string? period)
    {
        // BIẾN quick (DateTime?): ngày đầu kỳ suy ra từ lựa chọn nhanh; null nghĩa người dùng không
        // chọn kỳ nhanh hợp lệ.
        var quick = period switch
        {
            "today" => (DateTime?)DateTime.Today,
            "last7" => DateTime.Today.AddDays(-6),
            "last30" => DateTime.Today.AddDays(-29),
            "month" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            _ => null,
        };
        ViewBag.Period = period;
        ViewBag.From = quick ?? from?.Date ?? DateTime.Today.AddDays(-30);
        ViewBag.To = quick.HasValue ? DateTime.Today : to?.Date ?? DateTime.Today;
        // KIỂM TRA FORM: nhánh này thêm lỗi ", "; ModelState sẽ ngăn bước lưu bên dưới.
        if ((DateTime)ViewBag.From > (DateTime)ViewBag.To)
        {
            ModelState.AddModelError("", "Khoảng ngày không hợp lệ.");
            return View(new List<Models.ProfitLine>());
        }

        return View(store.Profit(ViewBag.From, ViewBag.To));
    }

    // HÀM Suppliers: Ghép dữ liệu phiếu nhập, nhà cung cấp, số phải trả và các chứng từ chi để hiển
    // thị công nợ.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // ĐIỂM VÀO: GET /Finance/Suppliers. Tham số hàm: ().
    // GIAO DIỆN: Project_QLThoiTrang/Views/Finance/Suppliers.cshtml.
    public IActionResult Suppliers()
    {
        ViewBag.Receipts = store.NativeRead(db =>
            db.PhieuNhap.OrderByDescending(p => p.NgayNhap).ToList()
        );
        ViewBag.Suppliers = store.Read(d => d.Suppliers);
        ViewBag.Accounts = store.NativeRead(db => db.SupplierAccounts.ToList());
        return View(store.NativeRead(db => db.SupplierPayments.ToList()));
    }

    // HÀM Receipt: Mở một phiếu nhập, công nợ đã đối soát, các lần chi và tổng chi tiết tính lại;
    // phiếu không tồn tại trả 404.
    // THAM SỐ id (int): khóa SQL của phiếu nhập kho cần xem/in, không phải mã đơn bán hàng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA a (SupplierAccount): số phải trả đã đối soát của một phiếu nhập; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (SupplierPayment): chứng từ chi thực tế cho công nợ nhà cung cấp; tên này chỉ
    // có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ChiTietPhieuNhapRow): entity của bảng ChiTietPhieuNhap, số lượng và giá nhập
    // từng biến thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // ĐIỂM VÀO: GET /Finance/Receipt. Tham số hàm: (int id).
    // GIAO DIỆN: Project_QLThoiTrang/Views/Finance/Receipt.cshtml.
    public IActionResult Receipt(int id)
    {
        // BIẾN p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; kết quả tra cứu có
        // thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var p = store.NativeRead(db => db.PhieuNhap.SingleOrDefault(p => p.MaPhieuNhap == id));
        // KHÔNG CÓ DỮ LIỆU HOẶC KHÔNG ĐƯỢC XEM: trả HTTP 404 và dừng, không tiếp tục dùng đối tượng
        // null.
        if (p == null)
            return NotFound();
        ViewBag.Account = store.NativeRead(db =>
            db.SupplierAccounts.SingleOrDefault(a => a.ReceiptId == id)
        );
        ViewBag.Payments = store.NativeRead(db =>
            db.SupplierPayments.Where(p => p.ReceiptId == id).ToList()
        );
        ViewBag.DetailTotal = store.NativeRead(db =>
            db.ChiTietPhieuNhap.Where(l => l.MaPhieuNhap == id)
                .Sum(l => (decimal?)l.SoLuong * l.GiaNhap)
            ?? 0
        );
        return View(p);
    }

    // HÀM Reconcile: Nhận số phải trả/căn cứ đối soát rồi gọi ReconcileSupplier; không được đổi số
    // phải trả sau khi đã phát sinh chi tiền.
    // THAM SỐ receiptId (int): khóa SQL của phiếu nhập cần xem/đối soát/chi tiền, không phải mã đơn
    // bán hàng.
    // THAM SỐ amount (decimal): tổng số phải trả VND được đối soát của phiếu nhập, không phải một
    // lần chi thực tế.
    // THAM SỐ note (string): ghi chú hoặc căn cứ điều chỉnh; được kiểm tra độ dài trước khi ghi
    // nhận người/thời điểm cập nhật.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Finance/Reconcile. Tham số hàm: (int receiptId, decimal amount, string note).
    [HttpPost]
    public IActionResult Reconcile(int receiptId, decimal amount, string note)
    {
        try
        {
            // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để
            // không lưu dữ liệu
            // sai.
            if (!ModelState.IsValid)
                throw new InvalidOperationException("Dữ liệu không hợp lệ.");
            store.ReconcileSupplier(receiptId, amount, note, Actor);
            TempData["Success"] = "Đã đối soát số phải trả.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Receipt), new { id = receiptId });
    }

    // HÀM Pay: Nhận chứng từ chi và số tiền; PaySupplier kiểm tra phiếu đã đối soát, chứng từ duy
    // nhất và số tiền không vượt phần còn nợ.
    // THAM SỐ receiptId (int): khóa SQL của phiếu nhập cần xem/đối soát/chi tiền, không phải mã đơn
    // bán hàng.
    // THAM SỐ amount (decimal): số tiền VND thực chi lần này, > 0 và không vượt công nợ còn lại của
    // phiếu.
    // THAM SỐ reference (string): mã chứng từ/biên nhận do người dùng cung cấp; các luồng tiền/nhập
    // kho kiểm tra chưa dùng để chống ghi trùng.
    // TRẢ VỀ (IActionResult): phản hồi HTTP của MVC: View/JSON/File/chuyển hướng hoặc mã lỗi theo
    // các nhánh return; caller là pipeline HTTP của ASP.NET Core.
    // ĐIỂM VÀO: POST /Finance/Pay. Tham số hàm: (int receiptId, decimal amount, string reference).
    [HttpPost]
    public IActionResult Pay(int receiptId, decimal amount, string reference)
    {
        try
        {
            // DỮ LIỆU FORM: DataAnnotations hoặc lỗi binding chưa hợp lệ; nhánh này dừng xử lý để
            // không lưu dữ liệu
            // sai.
            if (!ModelState.IsValid)
                throw new InvalidOperationException("Dữ liệu không hợp lệ.");
            store.PaySupplier(receiptId, amount, reference, Actor);
            TempData["Success"] = "Đã ghi nhận chứng từ chi.";
        }
        // BIẾN LỖI ex (InvalidOperationException): ngoại lệ bắt từ khối try; Message chứa thông
        // báo, còn loại lỗi dùng quyết định cách báo/ghi log ở catch.
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Receipt), new { id = receiptId });
    }
}
