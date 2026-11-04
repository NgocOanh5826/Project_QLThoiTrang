// VAI TRÒ FILE: Mapping bảng tệp đánh giá QOANH_ReviewMedia với khóa Guid và quan hệ tới DanhGia.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Data;

public partial class StoreDbContext
{
    // THUỘC TÍNH StoreDbContext.ReviewMedia (DbSet<ReviewMedia>): cửa truy cập EF tới tập entity
    // ReviewMedia; ToTable trong mapping chỉ rõ tên bảng SQL.
    public DbSet<ReviewMedia> ReviewMedia => Set<ReviewMedia>();

    // HÀM MapReviewMedia: Ánh xạ ReviewMedia sang QOANH_ReviewMedia, khóa Guid tự cấp, ContentType
    // ASCII tối đa 30 ký tự và khóa ngoại đến DanhGia với Restrict.
    // THAM SỐ b (ModelBuilder): ModelBuilder của EF, dùng khai báo bảng, cột, khóa, chỉ mục và quan
    // hệ; không chứa dữ liệu người dùng.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA x (ReviewMedia): tệp đánh giá đã xác định loại nội dung, chứa mã Guid, ReviewId
    // và byte tệp; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
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
