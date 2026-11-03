using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Models;
using Project_QLThoiTrang.Data.Native;
namespace Project_QLThoiTrang.Data;
public partial class StoreDbContext(DbContextOptions<StoreDbContext> options):DbContext(options)
{
 public DbSet<StoreConfiguration> Configuration=>Set<StoreConfiguration>();
 public DbSet<StockMovement> StockMovements=>Set<StockMovement>();
 public DbSet<PaymentRecord> Payments=>Set<PaymentRecord>();
 protected override void OnModelCreating(ModelBuilder b)
 {
  MapNative(b);
  MapFeatures(b);
  b.Entity<ThanhToanRow>().HasIndex(x=>x.MaGiaoDich).IsUnique().HasFilter("[MaGiaoDich] IS NOT NULL");
  foreach(var entity in b.Model.GetEntityTypes())foreach(var p in entity.GetProperties().Where(p=>p.GetDefaultValueSql()!=null))p.ValueGenerated=Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
  b.Entity<StoreConfiguration>().ToTable("QOANH_Configuration");
  b.Entity<StoreConfiguration>().Property(x=>x.Id).ValueGeneratedNever();
  b.Entity<StoreConfiguration>().OwnsOne(x=>x.Settings);
  b.Entity<StockMovement>().ToTable("QOANH_StockMovements");
  b.Entity<StockMovement>().Property(x=>x.Id).ValueGeneratedNever();
  b.Entity<StockMovement>().Property(x=>x.Reference).HasMaxLength(100);
  b.Entity<StockMovement>().HasIndex(x=>x.Reference).IsUnique();
  b.Entity<StockMovement>().HasOne<SanPhamRow>().WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<StockMovement>().ToTable(t=>t.HasCheckConstraint("CK_QStockBalance","[Before]>=0 AND [After]>=0 AND [After]=[Before]+[Delta]"));
  b.Entity<PaymentRecord>().ToTable("QOANH_PaymentLedger");
  b.Entity<PaymentRecord>().Property(x=>x.Id).ValueGeneratedNever();
  b.Entity<PaymentRecord>().Property(x=>x.Reference).HasMaxLength(100);
  b.Entity<PaymentRecord>().HasIndex(x=>x.Reference).IsUnique();
  b.Entity<PaymentRecord>().ToTable(t=>t.HasCheckConstraint("CK_QPayment","[Amount]>0 AND [Kind] IN ('Receipt','Refund')"));
  foreach(var entity in b.Model.GetEntityTypes()) foreach(var p in entity.GetProperties().Where(p=>p.ClrType==typeof(decimal)||p.ClrType==typeof(decimal?))){p.SetPrecision(18);p.SetScale(2);}
 }
}
