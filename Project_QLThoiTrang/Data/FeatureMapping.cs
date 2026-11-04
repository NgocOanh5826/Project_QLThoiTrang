// VAI TRÒ FILE: Mapping EF cho chat, OTP, giá vốn, công nợ và vận đơn; tên bảng đều được khai báo
// trong ToTable bên
// dưới.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Data;

public partial class StoreDbContext
{
    // THUỘC TÍNH StoreDbContext.ChatThreads (DbSet<ChatThread>): cửa truy cập EF tới tập entity
    // ChatThread; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    // THUỘC TÍNH StoreDbContext.ChatMessages (DbSet<ChatMessage>): cửa truy cập EF tới tập entity
    // ChatMessage; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    // THUỘC TÍNH StoreDbContext.ResetCodes (DbSet<ResetCode>): cửa truy cập EF tới tập entity
    // ResetCode; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<ResetCode> ResetCodes => Set<ResetCode>();
    // THUỘC TÍNH StoreDbContext.InventoryValues (DbSet<InventoryValue>): cửa truy cập EF tới tập
    // entity InventoryValue; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<InventoryValue> InventoryValues => Set<InventoryValue>();
    // THUỘC TÍNH StoreDbContext.SupplierAccounts (DbSet<SupplierAccount>): cửa truy cập EF tới tập
    // entity SupplierAccount; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<SupplierAccount> SupplierAccounts => Set<SupplierAccount>();
    // THUỘC TÍNH StoreDbContext.SupplierPayments (DbSet<SupplierPayment>): cửa truy cập EF tới tập
    // entity SupplierPayment; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    // THUỘC TÍNH StoreDbContext.Shipments (DbSet<Shipment>): các vận đơn được Load vào ảnh
    // StoreData, dùng kiểm tra trạng thái GHN liên quan tới đơn.
    public DbSet<Shipment> Shipments => Set<Shipment>();

    // HÀM MapFeatures: Ánh xạ chat, OTP, giá vốn, công nợ và GHN sang bảng QOANH_*; cấu hình khóa
    // ngoại Restrict và chỉ mục duy nhất để chống trùng token/chứng từ/mã vận đơn.
    // THAM SỐ b (ModelBuilder): ModelBuilder của EF, dùng khai báo bảng, cột, khóa, chỉ mục và quan
    // hệ; không chứa dữ liệu người dùng.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA x (ChatThread): hội thoại của một khách, chứa mốc đã đọc của khách và nhân viên;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ChatMessage): tin nhắn gồm người gửi, nội dung, thời gian và token chống gửi
    // trùng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ResetCode): bản ghi OTP chỉ giữ hash, stamp mật khẩu, hạn dùng, số lần thử và
    // trạng thái sử dụng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (InventoryValue): bản ghi giá vốn bình quân của một biến thể; giá null nghĩa là
    // chưa xác định; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (SupplierAccount): số phải trả đã đối soát của một phiếu nhập; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (SupplierPayment): chứng từ chi thực tế cho công nợ nhà cung cấp; tên này chỉ
    // có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và
    // mã vận đơn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ChiTietDonHangRow): entity của bảng ChiTietDonHang, các dòng hàng đã mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static void MapFeatures(ModelBuilder b)
    {
        b.Entity<ChatThread>().ToTable("QOANH_ChatThreads").HasKey(x => x.CustomerId);
        b.Entity<ChatThread>().Property(x => x.CustomerId).ValueGeneratedNever();
        b.Entity<ChatThread>()
            .HasOne<KhachHangRow>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<ChatMessage>().ToTable("QOANH_ChatMessages");
        b.Entity<ChatMessage>().Property(x => x.Text).HasMaxLength(2000);
        b.Entity<ChatMessage>().HasIndex(x => new { x.CustomerId, x.ClientToken }).IsUnique();
        b.Entity<ChatMessage>()
            .HasOne<ChatThread>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<ResetCode>().ToTable("QOANH_ResetCodes");
        b.Entity<ResetCode>().Property(x => x.Hash).HasMaxLength(64);
        b.Entity<ResetCode>().Property(x => x.Stamp).HasMaxLength(64);
        b.Entity<ResetCode>().HasIndex(x => new { x.CustomerId, x.CreatedUtc });
        b.Entity<ResetCode>()
            .HasOne<KhachHangRow>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<InventoryValue>().ToTable("QOANH_InventoryValues").HasKey(x => x.VariantId);
        b.Entity<InventoryValue>().Property(x => x.VariantId).ValueGeneratedNever();
        b.Entity<InventoryValue>().Property(x => x.Note).HasMaxLength(300);
        b.Entity<InventoryValue>()
            .HasOne<BienTheSanPhamRow>()
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<SupplierAccount>().ToTable("QOANH_SupplierAccounts").HasKey(x => x.ReceiptId);
        b.Entity<SupplierAccount>().Property(x => x.ReceiptId).ValueGeneratedNever();
        b.Entity<SupplierAccount>().Property(x => x.Note).HasMaxLength(300);
        b.Entity<SupplierAccount>()
            .HasOne<PhieuNhapRow>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<SupplierPayment>().ToTable("QOANH_SupplierPayments");
        b.Entity<SupplierPayment>().Property(x => x.Reference).HasMaxLength(100);
        b.Entity<SupplierPayment>().HasIndex(x => x.Reference).IsUnique();
        b.Entity<SupplierPayment>()
            .HasOne<SupplierAccount>()
            .WithMany()
            .HasForeignKey(x => x.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Shipment>().ToTable("QOANH_Shipments").HasKey(x => x.OrderId);
        b.Entity<Shipment>().Property(x => x.OrderId).ValueGeneratedNever();
        b.Entity<Shipment>().Property(x => x.ClientCode).HasMaxLength(50);
        b.Entity<Shipment>().Property(x => x.TrackingCode).HasMaxLength(50);
        b.Entity<Shipment>().Property(x => x.Status).HasMaxLength(60);
        b.Entity<Shipment>().Property(x => x.Error).HasMaxLength(300);
        b.Entity<Shipment>()
            .HasOne<DonHangRow>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Shipment>().Property(x => x.RequestHash).HasMaxLength(64);
        b.Entity<Shipment>().HasIndex(x => x.ClientCode).IsUnique();
        b.Entity<Shipment>()
            .HasIndex(x => x.TrackingCode)
            .IsUnique()
            .HasFilter("[TrackingCode] IS NOT NULL");
        b.Entity<DonHangRow>()
            .HasIndex(x => x.QTransferReference)
            .IsUnique()
            .HasFilter("[QTransferReference] IS NOT NULL");
        b.Entity<ChiTietDonHangRow>().Property(x => x.QUnitCost).HasPrecision(18, 2);
        b.Entity<DonHangRow>().Property(x => x.QBankQrImage).HasMaxLength(200);
        b.Entity<DonHangRow>().Property(x => x.QTransferReference).HasMaxLength(30);
    }
}
