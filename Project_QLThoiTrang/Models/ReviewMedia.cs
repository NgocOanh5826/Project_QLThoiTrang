// VAI TRÒ FILE: Entity chứa byte ảnh/video và các record trao đổi tệp; ReviewUploads kiểm tra trước
// lưu,
// Shop/ReviewMedia kiểm tra quyền trước trả tệp.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

namespace Project_QLThoiTrang.Models;

// DỮ LIỆU: Tệp ảnh/video đánh giá lưu trong SQL bằng Guid, ReviewId, ContentType và Content; giới

// hạn được
// ReviewUploads kiểm tra.
public sealed class ReviewMedia
{
    // THUỘC TÍNH ReviewMedia.Id (Guid): mã định danh của ReviewMedia; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public Guid Id { get; set; }
    // THUỘC TÍNH ReviewMedia.ReviewId (int): khóa MaDanhGia mà một tệp media thuộc về.
    public int ReviewId { get; set; }
    // THUỘC TÍNH ReviewMedia.ContentType (string): MIME đã được xác minh từ chữ ký byte, dùng trả
    // header khi phục vụ ảnh/video.
    public string ContentType { get; set; } = "";
    // THUỘC TÍNH ReviewMedia.Content (byte[]): mảng byte thật của tệp media lưu trong SQL, không
    // phải đường dẫn tệp ngoài.
    public byte[] Content { get; set; } = [];
}

// DỮ LIỆU: Thông tin nhẹ dùng hiển thị tệp: mã Guid và cờ video; URL đọc byte do Shop/ReviewMedia

// xử lý.
public sealed record ReviewMediaInfo(Guid Id, bool IsVideo);

// DỮ LIỆU: Tệp đầu vào gồm tên và byte trước kiểm tra; không tin đuôi tệp cho tới khi

// ReviewUploads đối chiếu chữ
// ký byte.
public sealed record ReviewUpload(string FileName, byte[] Content);
