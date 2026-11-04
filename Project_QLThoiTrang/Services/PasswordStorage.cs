// VAI TRÒ FILE: Nhận diện định dạng hash Identity; việc so khớp mật khẩu nằm trong
// Authenticate/ChangePassword,
// không ở bước nhận diện này.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Buffers.Binary;

namespace Project_QLThoiTrang.Services;

internal static class PasswordStorage
{
    // HÀM IsIdentityHash: Kiểm tra chuỗi Base64 có cấu trúc hash ASP.NET Identity hợp lệ theo phiên
    // bản và độ dài; không xác minh mật khẩu người dùng ở bước này.
    // THAM SỐ value (string?): chuỗi hash Identity cần kiểm tra định dạng.
    // TRẢ VỀ (bool): true nếu byte Base64 đúng cấu trúc Identity được hỗ trợ; không có nghĩa là mật
    // khẩu đã được xác minh.
    public static bool IsIdentityHash(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        // BIẾN bytes (byte[]): byte giải mã từ Base64 của hash Identity để kiểm tra header.
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return false;
        }

        if (bytes.Length == 49 && bytes[0] == 0)
            return true;
        if (bytes.Length < 45 || bytes[0] != 1)
            return false;
        // BIẾN prf (uint): mã thuật toán PRF đọc từ cấu trúc hash Identity, không phải mật khẩu hay
        // khóa bí mật.
        uint prf = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4));
        // BIẾN iterations (uint): số vòng lặp thuật toán băm ghi trong header Identity; kiểm tra
        // cấu trúc yêu cầu giá trị hợp lệ.
        uint iterations = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4));
        // BIẾN saltLength (uint): độ dài salt đọc từ header hash, dùng xác định bố cục byte và giới
        // hạn cấu trúc.
        uint saltLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(9, 4));
        return prf <= 2
            && iterations > 0
            && iterations <= int.MaxValue
            && saltLength >= 16
            && saltLength <= bytes.Length - 29;
    }
}
