using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public static class ReviewUploads
{
    public const int ImageLimit = 5 * 1024 * 1024,
        VideoLimit = 20 * 1024 * 1024,
        TotalLimit = 30 * 1024 * 1024;

    // Validate: Kiểm tra số lượng, dung lượng và định dạng tệp đánh giá.
    public static List<ReviewMedia> Validate(IReadOnlyList<ReviewUpload>? uploads)
    {
        uploads ??= [];
        if (uploads.Count > 5 || uploads.Sum(x => (long)x.Content.Length) > TotalLimit)
            throw new InvalidOperationException("Tối đa 5 tệp, tổng dung lượng không quá 30 MB.");
        var result = new List<ReviewMedia>();
        foreach (var upload in uploads)
        {
            var bytes = upload.Content;
            var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();

            bool Starts(params byte[] prefix) => bytes.AsSpan().StartsWith(prefix);
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
