using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Data;

public partial class StoreDbContext
{
    public DbSet<ReviewMedia> ReviewMedia => Set<ReviewMedia>();

    // MapReviewMedia: Ánh xạ bảng ảnh và video đánh giá.
    static void MapReviewMedia(ModelBuilder b)
    {
        b.Entity<ReviewMedia>().ToTable("QOANH_ReviewMedia");
        b.Entity<ReviewMedia>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<ReviewMedia>().Property(x => x.ContentType).HasMaxLength(30).IsUnicode(false);
        b.Entity<ReviewMedia>().HasIndex(x => x.ReviewId);
        b.Entity<ReviewMedia>()
            .HasOne<DanhGiaRow>()
            .WithMany()
            .HasForeignKey(x => x.ReviewId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
