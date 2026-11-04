// VAI TRÒ FILE: Cấu hình lựa chọn và helper tìm kiếm của các danh sách quản trị; được
// controller/view dùng chung.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

namespace Project_QLThoiTrang.Models;

// DỮ LIỆU: Một lựa chọn giá trị/nhãn hiển thị cho bộ lọc danh sách quản trị.
public sealed record AdminFilterOption(string Value, string Text);

// DỮ LIỆU: Cấu hình và helper lọc danh sách; không thực hiện thêm/sửa/xóa dữ liệu.
public sealed record AdminListFilter(
    string Key,
    string Label,
    IReadOnlyList<AdminFilterOption>? Options = null
)
{
    // HÀM Choice: Tạo cấu hình một ô lọc và các cặp giá trị/nhãn hiển thị dùng chung cho thanh công
    // cụ danh sách quản trị.
    // THAM SỐ key (string): tên khóa Session hoặc tên trường bộ lọc; cùng key phải được dùng thống
    // nhất khi đọc/ghi.
    // THAM SỐ label (string): nhãn tiếng Việt hiển thị cho một bộ lọc.
    // THAM SỐ choices ((string Value, string Text)[]): các cặp Value/Text: Value gửi/lọc dữ liệu,
    // Text là nhãn người dùng nhìn thấy.
    // TRẢ VỀ (AdminListFilter): AdminListFilter; ý nghĩa được xác định bởi biểu thức return bên
    // dưới.
    // BIẾN LAMBDA c ((string Value, string Text)): (string Value, string Text); tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    public static AdminListFilter Choice(
        string key,
        string label,
        params (string Value, string Text)[] choices
    ) => new(key, label, choices.Select(c => new AdminFilterOption(c.Value, c.Text)).ToArray());

    // HÀM Matches: So khớp từ khóa với chuỗi tìm kiếm theo biểu thức bên dưới; từ khóa rỗng cho
    // phép giữ lại bản ghi.
    // THAM SỐ text (string): chuỗi đầu vào cần chuẩn hóa, so khớp hoặc chia dòng theo chức năng của
    // hàm.
    // THAM SỐ query (string?): từ khóa cần so khớp; rỗng thì helper cho phép giữ bản ghi.
    // TRẢ VỀ (bool): cờ đúng/sai; ý nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA c (char): char; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    public static bool Matches(string text, string? query)
    {
        // HÀM Fold: Hàm nội bộ của Matches: tách dấu Unicode, bỏ dấu kết hợp, đổi đ/Đ thành d/D và
        // chuyển chữ hoa để tìm kiếm không phân biệt dấu/hoa thường.
        // THAM SỐ value (string): chuỗi cần bỏ dấu tiếng Việt và chuẩn hóa chữ hoa để so khớp tìm
        // kiếm.
        // TRẢ VỀ (string): chuỗi ký tự; ý nghĩa được xác định bởi biểu thức return bên dưới.
        static string Fold(string value) =>
            new string(
                value
                    .Normalize(System.Text.NormalizationForm.FormD)
                    .Where(c =>
                        System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark
                    )
                    .ToArray()
            )
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .ToUpperInvariant();
        return string.IsNullOrWhiteSpace(query)
            || Fold(text).Contains(Fold(query.Trim()), StringComparison.Ordinal);
    }
}
