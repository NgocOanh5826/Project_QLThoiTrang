using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;

namespace Project_QLThoiTrang.Services;

public sealed class DatabaseSchemaStatus(
    IDbContextFactory<StoreDbContext> factory,
    ILogger<DatabaseSchemaStatus> logger
)
{
    readonly object gate = new();
    DateTime nextCheck = DateTime.MinValue;
    bool ready;

    // IsReady: Kiểm tra kết nối và schema database cần thiết.
    public bool IsReady()
    {
        lock (gate)
        {
            if (DateTime.UtcNow < nextCheck)
                return ready;
            try
            {
                using var db = factory.CreateDbContext();
                var connection = db.Database.GetDbConnection();
                connection.Open();
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

public sealed class DatabaseSchemaGuard(RequestDelegate next)
{
    // InvokeAsync: Chặn yêu cầu động khi database chưa sẵn sàng.
    public async Task InvokeAsync(
        HttpContext context,
        DatabaseSchemaStatus status,
        IWebHostEnvironment environment
    )
    {
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
