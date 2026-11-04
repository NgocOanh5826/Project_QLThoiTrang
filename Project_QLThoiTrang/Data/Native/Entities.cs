// VAI TRÒ FILE: Entity của các bảng SQL gốc. Lớp *Row mô tả hàng dữ liệu; không phải form nhập. Tên
// cột, khóa và
// quan hệ được cấu hình tại Data/Native/Mapping.cs.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

// Generated from supplied QL_CuaHangThoiTrang.sql; no navigation-dependent queries.
namespace Project_QLThoiTrang.Data.Native;

// DỮ LIỆU: ENTITY SQL KhachHang: mỗi đối tượng là một hàng bảng KhachHang; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class KhachHangRow
{
    // THUỘC TÍNH KhachHangRow.MaKH (int): khóa khách hàng SQL; khi là khóa ngoại chỉ khách mà bản
    // ghi này thuộc về.
    public int MaKH { get; set; }
    // THUỘC TÍNH KhachHangRow.HoTen (string): họ tên khách/nhân viên trong bảng gốc.
    public string HoTen { get; set; } = "";
    // THUỘC TÍNH KhachHangRow.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    public string Email { get; set; } = "";
    // THUỘC TÍNH KhachHangRow.MatKhau (string): hash mật khẩu trong bảng SQL gốc sau chuyển đổi,
    // không nhận mật khẩu rõ ở thao tác đăng nhập.
    public string MatKhau { get; set; } = "";
    // THUỘC TÍNH KhachHangRow.SoDienThoai (string?): số điện thoại chuỗi của khách/nhân viên/nhà
    // cung cấp.
    public string? SoDienThoai { get; set; }
    // THUỘC TÍNH KhachHangRow.GioiTinh (string?): giá trị giới tính của hồ sơ SQL gốc.
    public string? GioiTinh { get; set; }
    // THUỘC TÍNH KhachHangRow.NgaySinh (DateTime?): ngày sinh của hồ sơ SQL gốc, có thể null nếu
    // chưa cung cấp.
    public DateTime? NgaySinh { get; set; }
    // THUỘC TÍNH KhachHangRow.DiaChi (string?): địa chỉ liên hệ hoặc vị trí kho trong bảng SQL gốc.
    public string? DiaChi { get; set; }
    // THUỘC TÍNH KhachHangRow.NgayDangKy (DateTime): thời điểm tạo hồ sơ khách trong bảng gốc.
    public DateTime NgayDangKy { get; set; }
    // THUỘC TÍNH KhachHangRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
    // THUỘC TÍNH KhachHangRow.QLegacyId (int?): mã cũ được giữ ở cột mở rộng QOANH để đối chiếu dữ
    // liệu nhập.
    public int? QLegacyId { get; set; }
}

// DỮ LIỆU: ENTITY SQL NhanVien: mỗi đối tượng là một hàng bảng NhanVien; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class NhanVienRow
{
    // THUỘC TÍNH NhanVienRow.MaNV (int): khóa nhân viên dương trong SQL; ứng dụng đổi thành Id âm
    // khi đưa vào Customer.
    public int MaNV { get; set; }
    // THUỘC TÍNH NhanVienRow.HoTen (string): họ tên khách/nhân viên trong bảng gốc.
    public string HoTen { get; set; } = "";
    // THUỘC TÍNH NhanVienRow.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    public string Email { get; set; } = "";
    // THUỘC TÍNH NhanVienRow.MatKhau (string): hash mật khẩu trong bảng SQL gốc sau chuyển đổi,
    // không nhận mật khẩu rõ ở thao tác đăng nhập.
    public string MatKhau { get; set; } = "";
    // THUỘC TÍNH NhanVienRow.SoDienThoai (string?): số điện thoại chuỗi của khách/nhân viên/nhà
    // cung cấp.
    public string? SoDienThoai { get; set; }
    // THUỘC TÍNH NhanVienRow.GioiTinh (string?): giá trị giới tính của hồ sơ SQL gốc.
    public string? GioiTinh { get; set; }
    // THUỘC TÍNH NhanVienRow.NgaySinh (DateTime?): ngày sinh của hồ sơ SQL gốc, có thể null nếu
    // chưa cung cấp.
    public DateTime? NgaySinh { get; set; }
    // THUỘC TÍNH NhanVienRow.DiaChi (string?): địa chỉ liên hệ hoặc vị trí kho trong bảng SQL gốc.
    public string? DiaChi { get; set; }
    // THUỘC TÍNH NhanVienRow.ChucVu (string): chức vụ SQL được StaffRole ánh xạ sang nhóm quyền ứng
    // dụng.
    public string ChucVu { get; set; } = "";
    // THUỘC TÍNH NhanVienRow.NgayVaoLam (DateTime): ngày nhân viên vào làm/được khởi tạo trong dữ
    // liệu gốc.
    public DateTime NgayVaoLam { get; set; }
    // THUỘC TÍNH NhanVienRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với string
    // là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
    // THUỘC TÍNH NhanVienRow.QLegacyId (int?): mã cũ được giữ ở cột mở rộng QOANH để đối chiếu dữ
    // liệu nhập.
    public int? QLegacyId { get; set; }
}

// DỮ LIỆU: ENTITY SQL LichSuDangNhap: mỗi đối tượng là một hàng bảng LichSuDangNhap; xem MapNative

// để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class LichSuDangNhapRow
{
    // THUỘC TÍNH LichSuDangNhapRow.MaLichSu (long): khóa của một bản ghi nhật ký đăng nhập.
    public long MaLichSu { get; set; }
    // THUỘC TÍNH LichSuDangNhapRow.MaKH (int?): khóa khách hàng SQL; khi là khóa ngoại chỉ khách mà
    // bản ghi này thuộc về.
    public int? MaKH { get; set; }
    // THUỘC TÍNH LichSuDangNhapRow.MaNV (int?): khóa nhân viên dương trong SQL; ứng dụng đổi thành
    // Id âm khi đưa vào Customer.
    public int? MaNV { get; set; }
    // THUỘC TÍNH LichSuDangNhapRow.ThoiGianDangNhap (DateTime): thời điểm đăng nhập ghi vào lịch
    // sử.
    public DateTime ThoiGianDangNhap { get; set; }
    // THUỘC TÍNH LichSuDangNhapRow.ThoiGianDangXuat (DateTime?): thời điểm kết thúc phiên khi
    // logout; có thể null nếu chưa ghi đăng xuất.
    public DateTime? ThoiGianDangXuat { get; set; }
    // THUỘC TÍNH LichSuDangNhapRow.DiaChiIP (string?): IP đăng nhập được cắt tối đa theo cột trước
    // khi lưu.
    public string? DiaChiIP { get; set; }
    // THUỘC TÍNH LichSuDangNhapRow.ThietBi (string?): User-Agent của trình duyệt/thiết bị đăng
    // nhập, được cắt độ dài theo cột.
    public string? ThietBi { get; set; }
    // THUỘC TÍNH LichSuDangNhapRow.TrangThai (string): trạng thái SQL: với bool là hoạt động/ẩn,
    // với string là trạng thái nghiệp vụ của bảng đang khai báo.
    public string TrangThai { get; set; } = "";
}

// DỮ LIỆU: ENTITY SQL DanhMuc: mỗi đối tượng là một hàng bảng DanhMuc; xem MapNative để tra

// khóa/cột/quan hệ. Không
// nhận trực tiếp entity này làm form CRUD.
public class DanhMucRow
{
    // THUỘC TÍNH DanhMucRow.MaDanhMuc (int): khóa danh mục hoặc khóa ngoại danh mục của sản phẩm.
    public int MaDanhMuc { get; set; }
    // THUỘC TÍNH DanhMucRow.TenDanhMuc (string): tên danh mục SQL được đổi thành chuỗi Category ở
    // mô hình hiển thị.
    public string TenDanhMuc { get; set; } = "";
    // THUỘC TÍNH DanhMucRow.MoTa (string?): mô tả văn bản của đối tượng trong bảng gốc.
    public string? MoTa { get; set; }
    // THUỘC TÍNH DanhMucRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với string
    // là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
}

// DỮ LIỆU: ENTITY SQL ThuongHieu: mỗi đối tượng là một hàng bảng ThuongHieu; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class ThuongHieuRow
{
    // THUỘC TÍNH ThuongHieuRow.MaThuongHieu (int): khóa thương hiệu; với sản phẩm có thể null khi
    // chưa chọn.
    public int MaThuongHieu { get; set; }
    // THUỘC TÍNH ThuongHieuRow.TenThuongHieu (string): tên thương hiệu hiển thị.
    public string TenThuongHieu { get; set; } = "";
    // THUỘC TÍNH ThuongHieuRow.MoTa (string?): mô tả văn bản của đối tượng trong bảng gốc.
    public string? MoTa { get; set; }
    // THUỘC TÍNH ThuongHieuRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
}

// DỮ LIỆU: ENTITY SQL SanPham: mỗi đối tượng là một hàng bảng SanPham; xem MapNative để tra

// khóa/cột/quan hệ. Không
// nhận trực tiếp entity này làm form CRUD.
public class SanPhamRow
{
    // THUỘC TÍNH SanPhamRow.MaSP (int): khóa sản phẩm hoặc khóa ngoại trỏ tới SanPham.
    public int MaSP { get; set; }
    // THUỘC TÍNH SanPhamRow.MaDanhMuc (int): khóa danh mục hoặc khóa ngoại danh mục của sản phẩm.
    public int MaDanhMuc { get; set; }
    // THUỘC TÍNH SanPhamRow.MaThuongHieu (int?): khóa thương hiệu; với sản phẩm có thể null khi
    // chưa chọn.
    public int? MaThuongHieu { get; set; }
    // THUỘC TÍNH SanPhamRow.TenSP (string): tên sản phẩm trong bảng SanPham.
    public string TenSP { get; set; } = "";
    // THUỘC TÍNH SanPhamRow.MoTa (string?): mô tả văn bản của đối tượng trong bảng gốc.
    public string? MoTa { get; set; }
    // THUỘC TÍNH SanPhamRow.GiaBan (decimal): giá bán mặc định VND/đơn vị của sản phẩm gốc.
    public decimal GiaBan { get; set; }
    // THUỘC TÍNH SanPhamRow.NgayTao (DateTime): thời điểm tạo bản ghi trong bảng gốc.
    public DateTime NgayTao { get; set; }
    // THUỘC TÍNH SanPhamRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với string
    // là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
    // THUỘC TÍNH SanPhamRow.QImage (string?): đường dẫn ảnh mở rộng phục vụ giao diện QOANH.
    public string? QImage { get; set; }
    // THUỘC TÍNH SanPhamRow.QIsNew (bool): cờ hàng mới mở rộng để lọc bộ sưu tập new.
    public bool QIsNew { get; set; }
    // THUỘC TÍNH SanPhamRow.QOnSale (bool): cờ ưu đãi mở rộng để lọc bộ sưu tập sale.
    public bool QOnSale { get; set; }
    // THUỘC TÍNH SanPhamRow.QLegacyId (int?): mã cũ được giữ ở cột mở rộng QOANH để đối chiếu dữ
    // liệu nhập.
    public int? QLegacyId { get; set; }
}

// DỮ LIỆU: ENTITY SQL KichThuoc: mỗi đối tượng là một hàng bảng KichThuoc; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class KichThuocRow
{
    // THUỘC TÍNH KichThuocRow.MaKichThuoc (int): khóa size hoặc khóa ngoại của biến thể đến
    // KichThuoc.
    public int MaKichThuoc { get; set; }
    // THUỘC TÍNH KichThuocRow.TenKichThuoc (string): tên size như S/M/L trong bảng gốc.
    public string TenKichThuoc { get; set; } = "";
}

// DỮ LIỆU: ENTITY SQL MauSac: mỗi đối tượng là một hàng bảng MauSac; xem MapNative để tra

// khóa/cột/quan hệ. Không
// nhận trực tiếp entity này làm form CRUD.
public class MauSacRow
{
    // THUỘC TÍNH MauSacRow.MaMauSac (int): khóa màu hoặc khóa ngoại của biến thể đến MauSac.
    public int MaMauSac { get; set; }
    // THUỘC TÍNH MauSacRow.TenMauSac (string): tên màu hiển thị.
    public string TenMauSac { get; set; } = "";
    // THUỘC TÍNH MauSacRow.MaMau (string?): mã màu cấu hình cho danh mục màu, khác khóa MaMauSac.
    public string? MaMau { get; set; }
}

// DỮ LIỆU: ENTITY SQL BienTheSanPham: mỗi đối tượng là một hàng bảng BienTheSanPham; xem MapNative

// để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class BienTheSanPhamRow
{
    // THUỘC TÍNH BienTheSanPhamRow.MaBienThe (int): khóa biến thể hoặc khóa ngoại đến
    // BienTheSanPham.
    public int MaBienThe { get; set; }
    // THUỘC TÍNH BienTheSanPhamRow.MaSP (int): khóa sản phẩm hoặc khóa ngoại trỏ tới SanPham.
    public int MaSP { get; set; }
    // THUỘC TÍNH BienTheSanPhamRow.MaKichThuoc (int): khóa size hoặc khóa ngoại của biến thể đến
    // KichThuoc.
    public int MaKichThuoc { get; set; }
    // THUỘC TÍNH BienTheSanPhamRow.MaMauSac (int): khóa màu hoặc khóa ngoại của biến thể đến
    // MauSac.
    public int MaMauSac { get; set; }
    // THUỘC TÍNH BienTheSanPhamRow.SKU (string): mã nhận diện biến thể trong bảng SQL, được kiểm
    // tra duy nhất khi thêm/sửa.
    public string SKU { get; set; } = "";
    // THUỘC TÍNH BienTheSanPhamRow.GiaBanRieng (decimal?): giá VND/đơn vị riêng của biến thể; null
    // thì dùng GiaBan sản phẩm.
    public decimal? GiaBanRieng { get; set; }
    // THUỘC TÍNH BienTheSanPhamRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
}

// DỮ LIỆU: ENTITY SQL HinhAnhSanPham: mỗi đối tượng là một hàng bảng HinhAnhSanPham; xem MapNative

// để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class HinhAnhSanPhamRow
{
    // THUỘC TÍNH HinhAnhSanPhamRow.MaHinh (int): khóa của ảnh sản phẩm trong bảng HinhAnhSanPham.
    public int MaHinh { get; set; }
    // THUỘC TÍNH HinhAnhSanPhamRow.MaSP (int): khóa sản phẩm hoặc khóa ngoại trỏ tới SanPham.
    public int MaSP { get; set; }
    // THUỘC TÍNH HinhAnhSanPhamRow.DuongDan (string): đường dẫn ảnh sản phẩm trong dữ liệu SQL gốc.
    public string DuongDan { get; set; } = "";
    // THUỘC TÍNH HinhAnhSanPhamRow.AnhChinh (bool): cờ ảnh đại diện/chính của sản phẩm.
    public bool AnhChinh { get; set; }
}

// DỮ LIỆU: ENTITY SQL Kho: mỗi đối tượng là một hàng bảng Kho; xem MapNative để tra khóa/cột/quan

// hệ. Không nhận
// trực tiếp entity này làm form CRUD.
public class KhoRow
{
    // THUỘC TÍNH KhoRow.MaKho (int): khóa kho hoặc khóa ngoại xác định kho liên quan đến tồn/phiếu
    // nhập.
    public int MaKho { get; set; }
    // THUỘC TÍNH KhoRow.TenKho (string): tên kho hiển thị cho người dùng.
    public string TenKho { get; set; } = "";
    // THUỘC TÍNH KhoRow.DiaChi (string?): địa chỉ liên hệ hoặc vị trí kho trong bảng SQL gốc.
    public string? DiaChi { get; set; }
    // THUỘC TÍNH KhoRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với string là
    // trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
}

// DỮ LIỆU: ENTITY SQL TonKho: mỗi đối tượng là một hàng bảng TonKho; xem MapNative để tra

// khóa/cột/quan hệ. Không
// nhận trực tiếp entity này làm form CRUD.
public class TonKhoRow
{
    // THUỘC TÍNH TonKhoRow.MaKho (int): khóa kho hoặc khóa ngoại xác định kho liên quan đến
    // tồn/phiếu nhập.
    public int MaKho { get; set; }
    // THUỘC TÍNH TonKhoRow.MaBienThe (int): khóa biến thể hoặc khóa ngoại đến BienTheSanPham.
    public int MaBienThe { get; set; }
    // THUỘC TÍNH TonKhoRow.SoLuongTon (int): số lượng còn tại một kho cho một biến thể, không phải
    // tồn tổng mọi sản phẩm.
    public int SoLuongTon { get; set; }
}

// DỮ LIỆU: ENTITY SQL NhaCungCap: mỗi đối tượng là một hàng bảng NhaCungCap; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class NhaCungCapRow
{
    // THUỘC TÍNH NhaCungCapRow.MaNCC (int): khóa nhà cung cấp hoặc khóa ngoại nhà cung cấp của
    // phiếu nhập.
    public int MaNCC { get; set; }
    // THUỘC TÍNH NhaCungCapRow.TenNCC (string): tên nhà cung cấp trong bảng gốc.
    public string TenNCC { get; set; } = "";
    // THUỘC TÍNH NhaCungCapRow.SoDienThoai (string?): số điện thoại chuỗi của khách/nhân viên/nhà
    // cung cấp.
    public string? SoDienThoai { get; set; }
    // THUỘC TÍNH NhaCungCapRow.Email (string?): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    public string? Email { get; set; }
    // THUỘC TÍNH NhaCungCapRow.DiaChi (string?): địa chỉ liên hệ hoặc vị trí kho trong bảng SQL
    // gốc.
    public string? DiaChi { get; set; }
    // THUỘC TÍNH NhaCungCapRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
}

// DỮ LIỆU: ENTITY SQL PhieuNhap: mỗi đối tượng là một hàng bảng PhieuNhap; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class PhieuNhapRow
{
    // THUỘC TÍNH PhieuNhapRow.MaPhieuNhap (int): khóa đầu phiếu nhập hoặc khóa ngoại chi tiết đến
    // đầu phiếu.
    public int MaPhieuNhap { get; set; }
    // THUỘC TÍNH PhieuNhapRow.MaNCC (int): khóa nhà cung cấp hoặc khóa ngoại nhà cung cấp của phiếu
    // nhập.
    public int MaNCC { get; set; }
    // THUỘC TÍNH PhieuNhapRow.MaKho (int): khóa kho hoặc khóa ngoại xác định kho liên quan đến
    // tồn/phiếu nhập.
    public int MaKho { get; set; }
    // THUỘC TÍNH PhieuNhapRow.MaNV (int): khóa nhân viên dương trong SQL; ứng dụng đổi thành Id âm
    // khi đưa vào Customer.
    public int MaNV { get; set; }
    // THUỘC TÍNH PhieuNhapRow.NgayNhap (DateTime): thời điểm ghi nhận phiếu nhập.
    public DateTime NgayNhap { get; set; }
    // THUỘC TÍNH PhieuNhapRow.TongTien (decimal): tổng tiền VND ghi trên phiếu nhập; view có thể
    // tính lại tổng chi tiết để đối soát.
    public decimal TongTien { get; set; }
    // THUỘC TÍNH PhieuNhapRow.GhiChu (string?): nội dung ghi chú của chứng từ/bản ghi gốc.
    public string? GhiChu { get; set; }
    // THUỘC TÍNH PhieuNhapRow.QReference (string?): mã tham chiếu mở rộng liên kết chứng từ gốc với
    // luồng nhập/thu/hoàn QOANH.
    public string? QReference { get; set; }
}

// DỮ LIỆU: ENTITY SQL ChiTietPhieuNhap: mỗi đối tượng là một hàng bảng ChiTietPhieuNhap; xem

// MapNative để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class ChiTietPhieuNhapRow
{
    // THUỘC TÍNH ChiTietPhieuNhapRow.MaPhieuNhap (int): khóa đầu phiếu nhập hoặc khóa ngoại chi
    // tiết đến đầu phiếu.
    public int MaPhieuNhap { get; set; }
    // THUỘC TÍNH ChiTietPhieuNhapRow.MaBienThe (int): khóa biến thể hoặc khóa ngoại đến
    // BienTheSanPham.
    public int MaBienThe { get; set; }
    // THUỘC TÍNH ChiTietPhieuNhapRow.SoLuong (int): số đơn vị hàng của dòng nhập/giỏ/đơn trong bảng
    // đang khai báo.
    public int SoLuong { get; set; }
    // THUỘC TÍNH ChiTietPhieuNhapRow.GiaNhap (decimal): giá nhập VND cho một đơn vị biến thể trong
    // chi tiết phiếu nhập.
    public decimal GiaNhap { get; set; }
}

// DỮ LIỆU: ENTITY SQL GioHang: mỗi đối tượng là một hàng bảng GioHang; xem MapNative để tra

// khóa/cột/quan hệ. Không
// nhận trực tiếp entity này làm form CRUD.
public class GioHangRow
{
    // THUỘC TÍNH GioHangRow.MaGioHang (int): khóa đầu giỏ của khách hoặc khóa ngoại của dòng giỏ.
    public int MaGioHang { get; set; }
    // THUỘC TÍNH GioHangRow.MaKH (int): khóa khách hàng SQL; khi là khóa ngoại chỉ khách mà bản ghi
    // này thuộc về.
    public int MaKH { get; set; }
    // THUỘC TÍNH GioHangRow.NgayTao (DateTime): thời điểm tạo bản ghi trong bảng gốc.
    public DateTime NgayTao { get; set; }
}

// DỮ LIỆU: ENTITY SQL ChiTietGioHang: mỗi đối tượng là một hàng bảng ChiTietGioHang; xem MapNative

// để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class ChiTietGioHangRow
{
    // THUỘC TÍNH ChiTietGioHangRow.MaGioHang (int): khóa đầu giỏ của khách hoặc khóa ngoại của dòng
    // giỏ.
    public int MaGioHang { get; set; }
    // THUỘC TÍNH ChiTietGioHangRow.MaBienThe (int): khóa biến thể hoặc khóa ngoại đến
    // BienTheSanPham.
    public int MaBienThe { get; set; }
    // THUỘC TÍNH ChiTietGioHangRow.SoLuong (int): số đơn vị hàng của dòng nhập/giỏ/đơn trong bảng
    // đang khai báo.
    public int SoLuong { get; set; }
}

// DỮ LIỆU: ENTITY SQL YeuThich: mỗi đối tượng là một hàng bảng YeuThich; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class YeuThichRow
{
    // THUỘC TÍNH YeuThichRow.MaKH (int): khóa khách hàng SQL; khi là khóa ngoại chỉ khách mà bản
    // ghi này thuộc về.
    public int MaKH { get; set; }
    // THUỘC TÍNH YeuThichRow.MaSP (int): khóa sản phẩm hoặc khóa ngoại trỏ tới SanPham.
    public int MaSP { get; set; }
    // THUỘC TÍNH YeuThichRow.NgayThem (DateTime): thời điểm thêm sản phẩm vào danh sách yêu thích.
    public DateTime NgayThem { get; set; }
}

// DỮ LIỆU: ENTITY SQL KhuyenMai: mỗi đối tượng là một hàng bảng KhuyenMai; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class KhuyenMaiRow
{
    // THUỘC TÍNH KhuyenMaiRow.MaKM (int): khóa ưu đãi hoặc khóa ngoại trong bảng liên kết áp dụng
    // sản phẩm.
    public int MaKM { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.MaCode (string?): mã ưu đãi khách nhập để service tra chương trình.
    public string? MaCode { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.TenKhuyenMai (string): tên hiển thị của chương trình khuyến mãi.
    public string TenKhuyenMai { get; set; } = "";
    // THUỘC TÍNH KhuyenMaiRow.LoaiGiam (string): kiểu giảm PHANTRAM hoặc SOTIEN của chương trình.
    public string LoaiGiam { get; set; } = "";
    // THUỘC TÍNH KhuyenMaiRow.GiaTriGiam (decimal): giá trị giảm theo LoaiGiam, không mặc định luôn
    // là phần trăm.
    public decimal GiaTriGiam { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.DonHangToiThieu (decimal): mức tiền hàng tối thiểu VND để áp dụng ưu
    // đãi.
    public decimal DonHangToiThieu { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.GiamToiDa (decimal?): trần số tiền VND được giảm nếu chương trình có
    // giới hạn.
    public decimal? GiamToiDa { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.SoLuong (int?): số đơn vị hàng của dòng nhập/giỏ/đơn trong bảng đang
    // khai báo.
    public int? SoLuong { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.NgayBatDau (DateTime): mốc bắt đầu hiệu lực ưu đãi.
    public DateTime NgayBatDau { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.NgayKetThuc (DateTime): mốc kết thúc hiệu lực ưu đãi.
    public DateTime NgayKetThuc { get; set; }
    // THUỘC TÍNH KhuyenMaiRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
}

// DỮ LIỆU: ENTITY SQL ChiTietKhuyenMai: mỗi đối tượng là một hàng bảng ChiTietKhuyenMai; xem

// MapNative để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class ChiTietKhuyenMaiRow
{
    // THUỘC TÍNH ChiTietKhuyenMaiRow.MaKM (int): khóa ưu đãi hoặc khóa ngoại trong bảng liên kết áp
    // dụng sản phẩm.
    public int MaKM { get; set; }
    // THUỘC TÍNH ChiTietKhuyenMaiRow.MaSP (int): khóa sản phẩm hoặc khóa ngoại trỏ tới SanPham.
    public int MaSP { get; set; }
}

// DỮ LIỆU: ENTITY SQL DonHang: mỗi đối tượng là một hàng bảng DonHang; xem MapNative để tra

// khóa/cột/quan hệ. Không
// nhận trực tiếp entity này làm form CRUD.
public class DonHangRow
{
    // THUỘC TÍNH DonHangRow.QBankQrImage (string?): ảnh QR được chụp cho đơn chuyển khoản.
    public string? QBankQrImage { get; set; }
    // THUỘC TÍNH DonHangRow.QTransferReference (string?): nội dung chuyển khoản chụp cho đơn để đối
    // soát.
    public string? QTransferReference { get; set; }
    // THUỘC TÍNH DonHangRow.MaDonHang (int): khóa số nguyên SQL của đơn hoặc khóa ngoại tới đầu
    // đơn; khác mã QPublicId.
    public int MaDonHang { get; set; }
    // THUỘC TÍNH DonHangRow.MaKH (int): khóa khách hàng SQL; khi là khóa ngoại chỉ khách mà bản ghi
    // này thuộc về.
    public int MaKH { get; set; }
    // THUỘC TÍNH DonHangRow.MaKM (int?): khóa ưu đãi hoặc khóa ngoại trong bảng liên kết áp dụng
    // sản phẩm.
    public int? MaKM { get; set; }
    // THUỘC TÍNH DonHangRow.NgayDat (DateTime): thời điểm đặt đơn trong dữ liệu SQL.
    public DateTime NgayDat { get; set; }
    // THUỘC TÍNH DonHangRow.HoTenNguoiNhan (string): tên người nhận hàng chụp trên đơn.
    public string HoTenNguoiNhan { get; set; } = "";
    // THUỘC TÍNH DonHangRow.SoDienThoaiNhan (string): số điện thoại nhận hàng chụp trên đơn.
    public string SoDienThoaiNhan { get; set; } = "";
    // THUỘC TÍNH DonHangRow.DiaChiGiaoHang (string): địa chỉ giao hàng đã xác nhận cho đơn.
    public string DiaChiGiaoHang { get; set; } = "";
    // THUỘC TÍNH DonHangRow.TongTienHang (decimal): tổng giá trị dòng hàng trước phí và ưu đãi.
    public decimal TongTienHang { get; set; }
    // THUỘC TÍNH DonHangRow.TienGiam (decimal): tổng tiền VND ưu đãi đã trừ trên đơn.
    public decimal TienGiam { get; set; }
    // THUỘC TÍNH DonHangRow.PhiVanChuyen (decimal): phí vận chuyển VND đã chụp cho đơn.
    public decimal PhiVanChuyen { get; set; }
    // THUỘC TÍNH DonHangRow.TongThanhToan (decimal): tổng VND khách phải trả sau phí và giảm giá.
    public decimal TongThanhToan { get; set; }
    // THUỘC TÍNH DonHangRow.TrangThai (string): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public string TrangThai { get; set; } = "";
    // THUỘC TÍNH DonHangRow.GhiChu (string?): nội dung ghi chú của chứng từ/bản ghi gốc.
    public string? GhiChu { get; set; }
    // THUỘC TÍNH DonHangRow.QPublicId (string?): mã đơn công khai QH-...; dữ liệu cũ null được
    // adapter hiển thị QH-SQL-{MaDonHang}.
    public string? QPublicId { get; set; }
    // THUỘC TÍNH DonHangRow.QCheckoutToken (string?): token checkout chống tạo lại cùng một đơn khi
    // gửi form lần hai.
    public string? QCheckoutToken { get; set; }
    // THUỘC TÍNH DonHangRow.QLegacyId (string?): mã cũ được giữ ở cột mở rộng QOANH để đối chiếu dữ
    // liệu nhập.
    public string? QLegacyId { get; set; }
}

// DỮ LIỆU: ENTITY SQL ChiTietDonHang: mỗi đối tượng là một hàng bảng ChiTietDonHang; xem MapNative

// để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class ChiTietDonHangRow
{
    // THUỘC TÍNH ChiTietDonHangRow.QUnitCost (decimal?): giá vốn VND/đơn vị đã chụp trên dòng đơn
    // lúc xuất; null là chưa xác định.
    public decimal? QUnitCost { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.MaCTDH (int): khóa dòng chi tiết đơn hoặc khóa ngoại của dòng
    // trả đến dòng đã mua.
    public int MaCTDH { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.MaDonHang (int): khóa số nguyên SQL của đơn hoặc khóa ngoại tới
    // đầu đơn; khác mã QPublicId.
    public int MaDonHang { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.MaBienThe (int): khóa biến thể hoặc khóa ngoại đến
    // BienTheSanPham.
    public int MaBienThe { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.TenSanPham (string): tên sản phẩm chụp tại thời điểm đặt đơn,
    // không thay theo tên sản phẩm sửa sau này.
    public string TenSanPham { get; set; } = "";
    // THUỘC TÍNH ChiTietDonHangRow.KichThuoc (string?): tên size chụp trên dòng đơn.
    public string? KichThuoc { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.MauSac (string?): tên màu chụp trên dòng đơn.
    public string? MauSac { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.SoLuong (int): số đơn vị hàng của dòng nhập/giỏ/đơn trong bảng
    // đang khai báo.
    public int SoLuong { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.DonGia (decimal): giá bán VND/đơn vị chụp trên dòng đơn.
    public decimal DonGia { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.QImage (string?): đường dẫn ảnh mở rộng phục vụ giao diện QOANH.
    public string? QImage { get; set; }
    // THUỘC TÍNH ChiTietDonHangRow.QWarehouseId (int?): mã kho đã xuất cho dòng đơn, dùng hoàn tồn
    // về đúng kho khi hủy/trả.
    public int? QWarehouseId { get; set; }
}

// DỮ LIỆU: ENTITY SQL ThanhToan: mỗi đối tượng là một hàng bảng ThanhToan; xem MapNative để tra

// khóa/cột/quan hệ.
// Không nhận trực tiếp entity này làm form CRUD.
public class ThanhToanRow
{
    // THUỘC TÍNH ThanhToanRow.MaThanhToan (int): khóa bản ghi thanh toán SQL gốc.
    public int MaThanhToan { get; set; }
    // THUỘC TÍNH ThanhToanRow.MaDonHang (int): khóa số nguyên SQL của đơn hoặc khóa ngoại tới đầu
    // đơn; khác mã QPublicId.
    public int MaDonHang { get; set; }
    // THUỘC TÍNH ThanhToanRow.PhuongThuc (string): phương thức thanh toán của đơn như COD/chuyển
    // khoản.
    public string PhuongThuc { get; set; } = "";
    // THUỘC TÍNH ThanhToanRow.SoTien (decimal): số tiền VND ghi nhận trên bản ghi thanh toán gốc.
    public decimal SoTien { get; set; }
    // THUỘC TÍNH ThanhToanRow.TrangThai (string): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public string TrangThai { get; set; } = "";
    // THUỘC TÍNH ThanhToanRow.MaGiaoDich (string?): mã biên nhận/giao dịch thanh toán; chỉ mục duy
    // nhất chống dùng lại mã khi có giá trị.
    public string? MaGiaoDich { get; set; }
    // THUỘC TÍNH ThanhToanRow.NgayThanhToan (DateTime?): thời điểm ghi nhận thanh toán.
    public DateTime? NgayThanhToan { get; set; }
}

// DỮ LIỆU: ENTITY SQL LichSuTrangThaiDonHang: mỗi đối tượng là một hàng bảng

// LichSuTrangThaiDonHang; xem MapNative
// để tra khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class LichSuTrangThaiDonHangRow
{
    // THUỘC TÍNH LichSuTrangThaiDonHangRow.MaLichSuTrangThai (long): khóa một mốc lịch sử thay đổi
    // trạng thái đơn.
    public long MaLichSuTrangThai { get; set; }
    // THUỘC TÍNH LichSuTrangThaiDonHangRow.MaDonHang (int): khóa số nguyên SQL của đơn hoặc khóa
    // ngoại tới đầu đơn; khác mã QPublicId.
    public int MaDonHang { get; set; }
    // THUỘC TÍNH LichSuTrangThaiDonHangRow.TrangThai (string): trạng thái SQL: với bool là hoạt
    // động/ẩn, với string là trạng thái nghiệp vụ của bảng đang khai báo.
    public string TrangThai { get; set; } = "";
    // THUỘC TÍNH LichSuTrangThaiDonHangRow.ThoiGian (DateTime): thời điểm chuyển trạng thái đơn của
    // bản ghi lịch sử.
    public DateTime ThoiGian { get; set; }
    // THUỘC TÍNH LichSuTrangThaiDonHangRow.GhiChu (string?): nội dung ghi chú của chứng từ/bản ghi
    // gốc.
    public string? GhiChu { get; set; }
}

// DỮ LIỆU: ENTITY SQL PhieuTraHang: mỗi đối tượng là một hàng bảng PhieuTraHang; xem MapNative để

// tra khóa/cột/quan
// hệ. Không nhận trực tiếp entity này làm form CRUD.
public class PhieuTraHangRow
{
    // THUỘC TÍNH PhieuTraHangRow.MaPhieuTra (int): khóa phiếu trả hoặc khóa ngoại chi tiết trả tới
    // đầu phiếu.
    public int MaPhieuTra { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.MaDonHang (int): khóa số nguyên SQL của đơn hoặc khóa ngoại tới
    // đầu đơn; khác mã QPublicId.
    public int MaDonHang { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.MaKH (int): khóa khách hàng SQL; khi là khóa ngoại chỉ khách mà
    // bản ghi này thuộc về.
    public int MaKH { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.MaNV (int?): khóa nhân viên dương trong SQL; ứng dụng đổi thành Id
    // âm khi đưa vào Customer.
    public int? MaNV { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.NgayYeuCau (DateTime): thời điểm khách tạo yêu cầu trả hàng.
    public DateTime NgayYeuCau { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.LyDo (string): lý do trả hàng khách cung cấp.
    public string LyDo { get; set; } = "";
    // THUỘC TÍNH PhieuTraHangRow.TrangThai (string): trạng thái SQL: với bool là hoạt động/ẩn, với
    // string là trạng thái nghiệp vụ của bảng đang khai báo.
    public string TrangThai { get; set; } = "";
    // THUỘC TÍNH PhieuTraHangRow.TongTienHoan (decimal): tổng tiền VND của phiếu yêu cầu/đã duyệt
    // hoàn.
    public decimal TongTienHoan { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.NgayXuLy (DateTime?): thời điểm duyệt/từ chối phiếu trả; chưa xử
    // lý có thể null.
    public DateTime? NgayXuLy { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.GhiChu (string?): nội dung ghi chú của chứng từ/bản ghi gốc.
    public string? GhiChu { get; set; }
    // THUỘC TÍNH PhieuTraHangRow.QReference (string?): mã tham chiếu mở rộng liên kết chứng từ gốc
    // với luồng nhập/thu/hoàn QOANH.
    public string? QReference { get; set; }
}

// DỮ LIỆU: ENTITY SQL ChiTietTraHang: mỗi đối tượng là một hàng bảng ChiTietTraHang; xem MapNative

// để tra
// khóa/cột/quan hệ. Không nhận trực tiếp entity này làm form CRUD.
public class ChiTietTraHangRow
{
    // THUỘC TÍNH ChiTietTraHangRow.MaCTTra (int): khóa một dòng chi tiết trả hàng.
    public int MaCTTra { get; set; }
    // THUỘC TÍNH ChiTietTraHangRow.MaPhieuTra (int): khóa phiếu trả hoặc khóa ngoại chi tiết trả
    // tới đầu phiếu.
    public int MaPhieuTra { get; set; }
    // THUỘC TÍNH ChiTietTraHangRow.MaCTDH (int): khóa dòng chi tiết đơn hoặc khóa ngoại của dòng
    // trả đến dòng đã mua.
    public int MaCTDH { get; set; }
    // THUỘC TÍNH ChiTietTraHangRow.SoLuongTra (int): số lượng yêu cầu/đã duyệt trả của dòng mua
    // được tham chiếu.
    public int SoLuongTra { get; set; }
    // THUỘC TÍNH ChiTietTraHangRow.LyDoChiTiet (string?): ghi chú lý do của dòng trả trong schema
    // gốc.
    public string? LyDoChiTiet { get; set; }
    // THUỘC TÍNH ChiTietTraHangRow.TinhTrangSanPham (string?): tình trạng hàng được nhận lại khi
    // kiểm tra trả hàng.
    public string? TinhTrangSanPham { get; set; }
    // THUỘC TÍNH ChiTietTraHangRow.HinhAnh (string?): ảnh minh họa trong dữ liệu trả hàng gốc, khác
    // bảng media của đánh giá.
    public string? HinhAnh { get; set; }
    // THUỘC TÍNH ChiTietTraHangRow.SoTienHoan (decimal): tiền VND hoàn cho dòng trả sau quy tắc
    // phân bổ ưu đãi.
    public decimal SoTienHoan { get; set; }
}

// DỮ LIỆU: ENTITY SQL DanhGia: mỗi đối tượng là một hàng bảng DanhGia; xem MapNative để tra

// khóa/cột/quan hệ. Không
// nhận trực tiếp entity này làm form CRUD.
public class DanhGiaRow
{
    // THUỘC TÍNH DanhGiaRow.MaDanhGia (int): khóa đánh giá, được media dùng làm ReviewId.
    public int MaDanhGia { get; set; }
    // THUỘC TÍNH DanhGiaRow.MaKH (int): khóa khách hàng SQL; khi là khóa ngoại chỉ khách mà bản ghi
    // này thuộc về.
    public int MaKH { get; set; }
    // THUỘC TÍNH DanhGiaRow.MaSP (int): khóa sản phẩm hoặc khóa ngoại trỏ tới SanPham.
    public int MaSP { get; set; }
    // THUỘC TÍNH DanhGiaRow.SoSao (int): số sao đánh giá trong bảng gốc, luồng thêm chỉ chấp nhận
    // 1–5.
    public int SoSao { get; set; }
    // THUỘC TÍNH DanhGiaRow.NoiDung (string?): nội dung chữ của đánh giá trong bảng gốc.
    public string? NoiDung { get; set; }
    // THUỘC TÍNH DanhGiaRow.NgayDanhGia (DateTime): thời điểm khách gửi đánh giá.
    public DateTime NgayDanhGia { get; set; }
    // THUỘC TÍNH DanhGiaRow.TrangThai (bool): trạng thái SQL: với bool là hoạt động/ẩn, với string
    // là trạng thái nghiệp vụ của bảng đang khai báo.
    public bool TrangThai { get; set; }
}
