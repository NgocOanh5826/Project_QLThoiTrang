// VAI TRÒ FILE: DbContext Entity Framework Core: DbSet là điểm truy cập bảng; OnModelCreating gộp
// mapping gốc/tính
// năng và ràng buộc dữ liệu. ConnectionStrings:Store được đăng ký trong Program.cs.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Data;

// THAM SỐ KHỞI TẠO options (DbContextOptions<StoreDbContext>): các DbContextOptions đã cấu hình
// provider SQL và chuỗi kết nối; do DI hoặc nơi gọi constructor truyền vào.
public partial class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    // THUỘC TÍNH StoreDbContext.Configuration (DbSet<StoreConfiguration>): cửa truy cập EF tới tập
    // entity StoreConfiguration; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<StoreConfiguration> Configuration => Set<StoreConfiguration>();
    // THUỘC TÍNH StoreDbContext.StockMovements (DbSet<StockMovement>): nhật ký biến động tồn hiện
    // có và các dòng callback vừa thêm trước PersistNative.
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    // THUỘC TÍNH StoreDbContext.Payments (DbSet<PaymentRecord>): các bút toán thu/hoàn tiền nghiệp
    // vụ của đơn.
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    // HÀM OnModelCreating: Đăng ký mapping bảng gốc và tính năng; đặt khóa/chỉ mục/ràng buộc nhật
    // ký tồn và tiền, hạn chế xóa quan hệ và chuẩn hóa decimal(18,2). Cấu hình mapping không tự
    // chạy script triển khai.
    // THAM SỐ b (ModelBuilder): ModelBuilder của EF, dùng khai báo bảng, cột, khóa, chỉ mục và quan
    // hệ; không chứa dữ liệu người dùng.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA x (ThanhToanRow): entity của bảng ThanhToan, thông tin phương thức/trạng thái
    // thanh toán gốc; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (IMutableProperty): IMutableProperty; tên này chỉ có nghĩa trong lambda tương
    // ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StoreConfiguration): bản ghi cấu hình cửa hàng và dấu vết dữ liệu đã nhập; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA t (TableBuilder<StockMovement>): TableBuilder<StockMovement>; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA t (TableBuilder<PaymentRecord>): TableBuilder<PaymentRecord>; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    protected override void OnModelCreating(ModelBuilder b)
    {
        MapNative(b);
        MapFeatures(b);
        MapReviewMedia(b);
        b.Entity<ThanhToanRow>()
            .HasIndex(x => x.MaGiaoDich)
            .IsUnique()
            .HasFilter("[MaGiaoDich] IS NOT NULL");
        // BIẾN LẶP entity (IMutableEntityType): IMutableEntityType; mỗi lượt nhận một phần tử của
        // nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var entity in b.Model.GetEntityTypes())
        // BIẾN LẶP p (IMutableProperty): IMutableProperty; mỗi lượt nhận một phần tử của nguồn sau
        // in, chỉ dùng trong vòng foreach này.
        foreach (var p in entity.GetProperties().Where(p => p.GetDefaultValueSql() != null))
            p.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        b.Entity<StoreConfiguration>().ToTable("QOANH_Configuration");
        b.Entity<StoreConfiguration>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<StoreConfiguration>().OwnsOne(x => x.Settings);
        b.Entity<StockMovement>().ToTable("QOANH_StockMovements");
        b.Entity<StockMovement>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<StockMovement>().Property(x => x.Reference).HasMaxLength(100);
        b.Entity<StockMovement>().HasIndex(x => x.Reference).IsUnique();
        b.Entity<StockMovement>()
            .HasOne<SanPhamRow>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_QStockBalance",
                    "[Before]>=0 AND [After]>=0 AND [After]=[Before]+[Delta]"
                )
            );
        b.Entity<PaymentRecord>().ToTable("QOANH_PaymentLedger");
        b.Entity<PaymentRecord>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<PaymentRecord>().Property(x => x.Reference).HasMaxLength(100);
        b.Entity<PaymentRecord>().HasIndex(x => x.Reference).IsUnique();
        b.Entity<PaymentRecord>()
            .ToTable(t =>
                t.HasCheckConstraint("CK_QPayment", "[Amount]>0 AND [Kind] IN ('Receipt','Refund')")
            );
        // BIẾN LẶP entity (IMutableEntityType): IMutableEntityType; mỗi lượt nhận một phần tử của
        // nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var entity in b.Model.GetEntityTypes())
        // BIẾN LẶP p (IMutableProperty): IMutableProperty; mỗi lượt nhận một phần tử của nguồn sau
        // in, chỉ dùng trong vòng foreach này.
        foreach (
            var p in entity
                .GetProperties()
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
        )
        {
            p.SetPrecision(18);
            p.SetScale(2);
        }
    }
}
