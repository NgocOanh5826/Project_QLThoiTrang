namespace Project_QLThoiTrang.Models;

public sealed class ReviewMedia
{
    public Guid Id { get; set; }
    public int ReviewId { get; set; }
    public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];
}

public sealed record ReviewMediaInfo(Guid Id, bool IsVideo);

public sealed record ReviewUpload(string FileName, byte[] Content);
