// VAI TRÒ FILE: Kiểm tra schema cần thiết và middleware trả 503 khi database chưa sẵn sàng; không
// tự tạo bảng hay
// chạy nâng cấp khi mở website.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;

namespace Project_QLThoiTrang.Services;

// THAM SỐ KHỞI TẠO factory (IDbContextFactory<StoreDbContext>): factory tạo DbContext riêng mỗi lần
// đọc/ghi, tránh giữ một context dùng chung giữa request; do DI hoặc nơi gọi constructor truyền
// vào.
// THAM SỐ KHỞI TẠO logger (ILogger<DatabaseSchemaStatus>): bộ ghi log của lớp, ghi trạng thái/lỗi
// theo các lệnh Log bên dưới; do DI hoặc nơi gọi constructor truyền vào.
public sealed class DatabaseSchemaStatus(
    IDbContextFactory<StoreDbContext> factory,
    ILogger<DatabaseSchemaStatus> logger
)
{
    // BIẾN CỦA LỚP gate (object): đối tượng khóa lock bảo vệ cache kiểm tra schema, tránh nhiều
    // request cùng mở kết nối kiểm tra.
    readonly object gate = new();
    // BIẾN CỦA LỚP nextCheck (DateTime): thời điểm UTC sớm nhất được kiểm tra schema lại; trước mốc
    // này dùng kết quả cache ready.
    DateTime nextCheck = DateTime.MinValue;
    // BIẾN CỦA LỚP ready (bool): kết quả kiểm tra schema gần nhất, được dùng lại trong cửa sổ cache
    // 10 giây.
    bool ready;

    // HÀM IsReady: Kiểm tra kết nối và các cột/bảng cần thiết của SQL; khóa gate ngăn kiểm tra đồng
    // thời, cache kết quả trong 10 giây. Chỉ kiểm tra trạng thái, không tự nâng cấp schema.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (bool): cờ đúng/sai; ý nghĩa được xác định bởi biểu thức return bên dưới.
    public bool IsReady()
    {
        lock (gate)
        {
            if (DateTime.UtcNow < nextCheck)
                return ready;
            try
            {
                // BIẾN db (StoreDbContext): DbContext riêng cho lần đọc/ghi này; using bảo đảm giải
                // phóng kết nối khi kết thúc phạm vi.
                using var db = factory.CreateDbContext();
                // BIẾN connection (DbConnection): kết nối ADO.NET của DbContext dùng kiểm tra
                // schema bằng một câu SQL nhỏ.
                var connection = db.Database.GetDbConnection();
                connection.Open();
                // BIẾN command (DbCommand): lệnh SQL kiểm tra bảng/cột bắt buộc, có timeout riêng
                // để không treo yêu cầu quá lâu.
                using var command = connection.CreateCommand();
                command.CommandTimeout = 5;
                command.CommandText = (
                    "SELECT CASE WHEN COL_LENGTH(N'dbo.DonHang',N'QBankQrImag"
                    + "e') IS NOT NULL AND COL_LENGTH(N'dbo.DonHang',N'QTransfe"
                    + "rReference') IS NOT NULL AND "
                    + "COL_LENGTH(N'dbo.ChiTietDonHang',N'QUnitCost') IS NOT "
                    + "NULL AND OBJECT_ID(N'dbo.QOANH_Configuration',N'U') IS "
                    + "NOT NULL AND NOT EXISTS (SELECT 1 FROM "
                    + "(VALUES(N'QOANH_ChatThreads'),(N'QOANH_ChatMessages'),(N"
                    + "'QOANH_ResetCodes'),(N'QOANH_InventoryValues'),(N'QOANH_"
                    + "SupplierAccounts'),(N'QOANH_SupplierPayments'),(N'QOANH_"
                    + "Shipments'),(N'QOANH_ReviewMedia')) t(Name) WHERE "
                    + "OBJECT_ID(N'dbo.'+t.Name,N'U') IS NULL) THEN 1 ELSE 0 "
                    + "END"
                );
                ready = Convert.ToInt32(command.ExecuteScalar()) == 1;
                if (!ready)
                    logger.LogError(
                        (
                            "QOANH schema is incomplete. Back up and run "
                            + "delivery/Upgrade-features.sql on the configured "
                            + "connection. No automatic schema changes performed."
                        )
                    );
            }
            // BIẾN LỖI ex (Exception): ngoại lệ bắt từ khối try; Message chứa thông báo, còn loại
            // lỗi dùng quyết định cách báo/ghi log ở catch.
            catch (Exception ex)
                when (ex is System.Data.Common.DbException or InvalidOperationException)
            {
                ready = false;
                logger.LogError(
                    (
                        "QOANH database readiness check failed ({ErrorType}). "
                        + "Verify connection and schema upgrade."
                    ),
                    ex.GetType().Name
                );
            }

            nextCheck = DateTime.UtcNow.AddSeconds(10);
            return ready;
        }
    }
}

// THAM SỐ KHỞI TẠO next (RequestDelegate): delegate gọi middleware tiếp theo trong pipeline khi
// điều kiện cho phép; do DI hoặc nơi gọi constructor truyền vào.
public sealed class DatabaseSchemaGuard(RequestDelegate next)
{
    // HÀM InvokeAsync: Middleware cho tài nguyên tĩnh đi tiếp; yêu cầu động chỉ chạy tiếp khi
    // database sẵn sàng. Nếu thiếu kết nối/schema thì trả HTTP 503 với Retry-After 10 giây và hướng
    // dẫn riêng cho Development.
    // THAM SỐ context (HttpContext): ngữ cảnh yêu cầu hoặc bộ lọc MVC đang chạy, chứa route, tham
    // số, User và/hoặc phản hồi theo kiểu cụ thể.
    // THAM SỐ status (DatabaseSchemaStatus): trạng thái đích muốn cập nhật; phải nằm trong tập
    // chuyển trạng thái được phép của luồng tương ứng.
    // THAM SỐ environment (IWebHostEnvironment): thông tin môi trường chạy; IsDevelopment quyết
    // định có hiện hướng dẫn lỗi kỹ thuật hay không.
    // TRẢ VỀ (Task): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm sau các bước
    // await.
    // BIẾN LAMBDA prefix (string): chuỗi ký tự; tên này chỉ có nghĩa trong lambda tương ứng, không
    // phải biến toàn cục.
    public async Task InvokeAsync(
        HttpContext context,
        DatabaseSchemaStatus status,
        IWebHostEnvironment environment
    )
    {
        // BIẾN path (PathString): đường dẫn URL đang được yêu cầu, dùng cho phép tài nguyên tĩnh đi
        // tiếp qua guard schema.
        var path = context.Request.Path;
        if (
            new[] { "/css", "/js", "/images", "/lib", "/data", "/favicon.ico" }.Any(prefix =>
                path.StartsWithSegments(prefix)
            ) || status.IsReady()
        )
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.RetryAfter = "10";
        // BIẾN details (string): đoạn HTML thông báo database chưa sẵn sàng; Development có hướng
        // dẫn chi tiết, môi trường khác dùng thông báo chung.
        string details = environment.IsDevelopment()
            ? (
                "<p>Database chưa sẵn sàng hoặc chưa có schema mới. Kiểm "
                + "tra ConnectionStrings:Store đang có hiệu lực, sao lưu "
                + "rồi chạy <code>delivery/Upgrade-features.sql</code> "
                + "trên đúng instance/database. Hướng dẫn trong "
                + "<code>delivery/FEATURES.md</code>.</p>"
            )
            : "<p>Vui lòng thử lại sau hoặc liên hệ cửa hàng.</p>";
        await context.Response.WriteAsync(
            (
                "<!doctype html><html lang=\"vi\"><head><meta "
                + "charset=\"utf-8\"><meta name=\"viewport\" "
                + "content=\"width=device-width,initial-scale=1\"><title>QOAN"
                + "H đang cập nhật</title></head><body "
                + "style=\"font-family:Arial,sans-serif;max-width:720px;marg"
                + "in:64px auto;padding:24px;line-height:1.7\"><h1>QOANH "
                + "đang cập nhật dữ liệu</h1>"
            )
                + details
                + "<p>Trang sẽ kiểm tra lại khi bạn tải lại sau 10 giây.</p></body></html>"
        );
    }
}
