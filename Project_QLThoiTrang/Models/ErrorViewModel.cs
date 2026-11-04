// VAI TRÒ FILE: Dữ liệu trang lỗi: mã request giúp liên hệ nội dung hiển thị với nhật ký xử lý của
// server.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

namespace Project_QLThoiTrang.Models
{
    // DỮ LIỆU: Mã truy vết request và điều kiện có hiển thị mã hay không trên trang lỗi.
    public class ErrorViewModel
    {
        // THUỘC TÍNH ErrorViewModel.RequestId (string?): mã request dùng đối chiếu trang lỗi với
        // log máy chủ.
        public string? RequestId { get; set; }
        // THUỘC TÍNH ErrorViewModel.ShowRequestId (bool): true khi RequestId có nội dung và view
        // được phép hiện mã truy vết.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
