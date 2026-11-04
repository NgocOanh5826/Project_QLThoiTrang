using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed class ProductImages(IWebHostEnvironment environment)
{
    public const int MaxBytes = 5 * 1024 * 1024;
    const string Prefix = "/images/products/uploads/";

    // IsAllowedPath: Kiểm tra đường dẫn ảnh thư viện hoặc ảnh sản phẩm đã upload.
    public static bool IsAllowedPath([NotNullWhen(true)] string? image) =>
        image != null
        && (
            Enumerable.Range(1, 5).Any(i => image == $"/images/products/aothun{i:00}.jpg")
            || Regex.IsMatch(image, @"^/images/products/uploads/[a-f0-9]{32}\.(jpg|png|gif|webp)\z")
        );

    // Save: Kiểm tra ảnh và lưu bằng tên ngẫu nhiên trong thư mục sản phẩm.
    public async Task<string> Save(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxBytes)
            throw new InvalidOperationException(
                "Ảnh sản phẩm phải có dung lượng từ 1 byte đến 5 MB."
            );
        await using var input = file.OpenReadStream();
        using var content = new MemoryStream();
        var buffer = new byte[65536];
        int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (content.Length + count > MaxBytes)
                throw new InvalidOperationException("Ảnh sản phẩm tối đa 5 MB.");
            content.Write(buffer, 0, count);
        }
        var image = ReviewUploads
            .Validate([new ReviewUpload(file.FileName, content.ToArray())])
            .Single();
        var extension = image.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException("Chỉ nhận ảnh JPG, PNG, GIF hoặc WebP."),
        };
        var name = Guid.NewGuid().ToString("N") + extension;
        var directory = Path.Combine(environment.WebRootPath, "images", "products", "uploads");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, name);
        try
        {
            await File.WriteAllBytesAsync(destination, image.Content, cancellationToken);
        }
        catch
        {
            File.Delete(destination);
            throw;
        }
        return Prefix + name;
    }

    // Remove: Xóa ảnh upload mới khi lưu sản phẩm thất bại.
    public void Remove(string image)
    {
        if (IsAllowedPath(image) && image.StartsWith(Prefix, StringComparison.Ordinal))
            File.Delete(
                Path.Combine(
                    environment.WebRootPath,
                    "images",
                    "products",
                    "uploads",
                    Path.GetFileName(image)
                )
            );
    }
}
