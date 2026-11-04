// VAI TRÒ FILE: Kiểm tra ảnh/video đánh giá dựa trên dung lượng, đuôi tệp và byte nhận dạng; trả
// entity đã kiểm tra
// để lưu trong transaction của đánh giá.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public static class ReviewUploads
{
    // BIẾN CỦA LỚP ImageLimit (int): giới hạn mỗi ảnh = 5 × 1024 × 1024 byte (5 MiB).
    // BIẾN CỦA LỚP VideoLimit (int): giới hạn mỗi video = 20 × 1024 × 1024 byte (20 MiB).
    // BIẾN CỦA LỚP TotalLimit (int): giới hạn tổng tệp một đánh giá = 30 × 1024 × 1024 byte (30
    // MiB).
    public const int ImageLimit = 5 * 1024 * 1024,
        VideoLimit = 20 * 1024 * 1024,
        TotalLimit = 30 * 1024 * 1024;

    // HÀM Validate: Kiểm tra tối đa 5 tệp/tổng 30 MB; nhận ảnh JPG/PNG/GIF/WebP tối đa 5 MB và
    // video MP4/WebM tối đa 20 MB, nhiều nhất 1 video. Đối chiếu đuôi tệp với chữ ký byte rồi tạo
    // ReviewMedia để lưu SQL.
    // THAM SỐ uploads (IReadOnlyList<ReviewUpload>?): danh sách tên tệp và byte của tệp đánh giá
    // trước khi xác minh chữ ký và giới hạn dung lượng.
    // TRẢ VỀ (List<ReviewMedia>): List<ReviewMedia> của các tệp hợp lệ; đầu vào null cho danh sách
    // rỗng; sai định dạng/dung lượng ném lỗi.
    // BIẾN LAMBDA x (ReviewUpload): tệp người dùng gửi gồm tên tệp và byte, còn phải kiểm tra định
    // dạng/dung lượng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ReviewMedia): tệp đánh giá đã xác định loại nội dung, chứa mã Guid, ReviewId
    // và byte tệp; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    public static List<ReviewMedia> Validate(IReadOnlyList<ReviewUpload>? uploads)
    {
        uploads ??= [];
        if (uploads.Count > 5 || uploads.Sum(x => (long)x.Content.Length) > TotalLimit)
            throw new InvalidOperationException("Tối đa 5 tệp, tổng dung lượng không quá 30 MB.");
        // BIẾN result (List<ReviewMedia>): kết quả được tích lũy rồi trả sau xử lý: danh sách/truy
        // vấn các phần tử: tệp đánh giá đã xác định loại nội dung, chứa mã Guid, ReviewId và byte
        // tệp.
        var result = new List<ReviewMedia>();
        // BIẾN LẶP upload (ReviewUpload): tệp người dùng gửi gồm tên tệp và byte, còn phải kiểm tra
        // định dạng/dung lượng; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
        // foreach này.
        foreach (var upload in uploads)
        {
            // BIẾN bytes (byte[]): byte thật của tệp upload để đối chiếu chữ ký và dung lượng.
            var bytes = upload.Content;
            // BIẾN extension (string): đuôi tệp upload đã chuẩn hóa chữ thường; phải khớp chữ ký
            // byte mới chấp nhận loại nội dung.
            var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
            // HÀM Starts: Hàm nội bộ kiểm tra mảng byte tệp có bắt đầu đúng chữ ký prefix hay
            // không; không dựa riêng vào tên tệp.
            // THAM SỐ prefix (byte[]): các byte mở đầu mà định dạng tệp bắt buộc phải có; dùng nhận
            // diện chữ ký tệp.
            // TRẢ VỀ (bool): cờ đúng/sai; ý nghĩa được xác định bởi biểu thức return bên dưới.
            bool Starts(params byte[] prefix) => bytes.AsSpan().StartsWith(prefix);
            // BIẾN type (string?): MIME nhận diện từ đuôi và chữ ký byte, ví dụ image/png hoặc
            // video/mp4; null là không hợp lệ.
            string? type = extension switch
            {
                ".jpg" or ".jpeg" when Starts(0xff, 0xd8, 0xff) => "image/jpeg",
                ".png" when Starts(137, 80, 78, 71, 13, 10, 26, 10) => "image/png",
                ".gif" when Starts(71, 73, 70, 56, 55, 97) || Starts(71, 73, 70, 56, 57, 97) =>
                    "image/gif",
                ".webp"
                    when bytes.Length >= 12
                        && Starts(82, 73, 70, 70)
                        && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8) => "image/webp",
                ".mp4" when bytes.Length >= 16 && bytes.AsSpan(4, 4).SequenceEqual("ftyp"u8) =>
                    "video/mp4",
                ".webm"
                    when Starts(0x1a, 0x45, 0xdf, 0xa3)
                        && bytes.AsSpan(0, Math.Min(bytes.Length, 4096)).IndexOf("webm"u8) >= 0 =>
                    "video/webm",
                _ => null,
            };
            if (type == null)
                throw new InvalidOperationException(
                    (
                        "Tệp không hợp lệ. Chỉ nhận ảnh JPG, PNG, GIF, WebP và "
                        + "video MP4, WebM đúng định dạng."
                    )
                );
            if (bytes.Length > (type.StartsWith("video/") ? VideoLimit : ImageLimit))
                throw new InvalidOperationException("Mỗi ảnh tối đa 5 MB, mỗi video tối đa 20 MB.");
            result.Add(
                new()
                {
                    Id = Guid.NewGuid(),
                    ContentType = type,
                    Content = bytes,
                }
            );
        }

        if (result.Count(x => x.ContentType.StartsWith("video/")) > 1)
            throw new InvalidOperationException("Mỗi đánh giá chỉ đính kèm tối đa 1 video.");
        return result;
    }
}
