using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Data;

public partial class StoreDbContext
{
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ResetCode> ResetCodes => Set<ResetCode>();
    public DbSet<InventoryValue> InventoryValues => Set<InventoryValue>();
    public DbSet<SupplierAccount> SupplierAccounts => Set<SupplierAccount>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    // MapFeatures: Ánh xạ các bảng chức năng mở rộng.
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
