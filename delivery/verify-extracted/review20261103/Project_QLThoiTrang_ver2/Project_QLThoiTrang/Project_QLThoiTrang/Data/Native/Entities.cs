// Generated from supplied QL_CuaHangThoiTrang.sql; no navigation-dependent queries.
namespace Project_QLThoiTrang.Data.Native;
public class KhachHangRow
{
 public int MaKH { get; set; }
 public string HoTen { get; set; } = "";
 public string Email { get; set; } = "";
 public string MatKhau { get; set; } = "";
 public string? SoDienThoai { get; set; }
 public string? GioiTinh { get; set; }
 public DateTime? NgaySinh { get; set; }
 public string? DiaChi { get; set; }
 public DateTime NgayDangKy { get; set; }
 public bool TrangThai { get; set; }
 public int? QLegacyId { get; set; }
}
public class NhanVienRow
{
 public int MaNV { get; set; }
 public string HoTen { get; set; } = "";
 public string Email { get; set; } = "";
 public string MatKhau { get; set; } = "";
 public string? SoDienThoai { get; set; }
 public string? GioiTinh { get; set; }
 public DateTime? NgaySinh { get; set; }
 public string? DiaChi { get; set; }
 public string ChucVu { get; set; } = "";
 public DateTime NgayVaoLam { get; set; }
 public bool TrangThai { get; set; }
 public int? QLegacyId { get; set; }
}
public class LichSuDangNhapRow
{
 public long MaLichSu { get; set; }
 public int? MaKH { get; set; }
 public int? MaNV { get; set; }
 public DateTime ThoiGianDangNhap { get; set; }
 public DateTime? ThoiGianDangXuat { get; set; }
 public string? DiaChiIP { get; set; }
 public string? ThietBi { get; set; }
 public string TrangThai { get; set; } = "";
}
public class DanhMucRow
{
 public int MaDanhMuc { get; set; }
 public string TenDanhMuc { get; set; } = "";
 public string? MoTa { get; set; }
 public bool TrangThai { get; set; }
}
public class ThuongHieuRow
{
 public int MaThuongHieu { get; set; }
 public string TenThuongHieu { get; set; } = "";
 public string? MoTa { get; set; }
 public bool TrangThai { get; set; }
}
public class SanPhamRow
{
 public int MaSP { get; set; }
 public int MaDanhMuc { get; set; }
 public int? MaThuongHieu { get; set; }
 public string TenSP { get; set; } = "";
 public string? MoTa { get; set; }
 public decimal GiaBan { get; set; }
 public DateTime NgayTao { get; set; }
 public bool TrangThai { get; set; }
 public string? QImage { get; set; }
 public bool QIsNew { get; set; }
 public bool QOnSale { get; set; }
 public int? QLegacyId { get; set; }
}
public class KichThuocRow
{
 public int MaKichThuoc { get; set; }
 public string TenKichThuoc { get; set; } = "";
}
public class MauSacRow
{
 public int MaMauSac { get; set; }
 public string TenMauSac { get; set; } = "";
 public string? MaMau { get; set; }
}
public class BienTheSanPhamRow
{
 public int MaBienThe { get; set; }
 public int MaSP { get; set; }
 public int MaKichThuoc { get; set; }
 public int MaMauSac { get; set; }
 public string SKU { get; set; } = "";
 public decimal? GiaBanRieng { get; set; }
 public bool TrangThai { get; set; }
}
public class HinhAnhSanPhamRow
{
 public int MaHinh { get; set; }
 public int MaSP { get; set; }
 public string DuongDan { get; set; } = "";
 public bool AnhChinh { get; set; }
}
public class KhoRow
{
 public int MaKho { get; set; }
 public string TenKho { get; set; } = "";
 public string? DiaChi { get; set; }
 public bool TrangThai { get; set; }
}
public class TonKhoRow
{
 public int MaKho { get; set; }
 public int MaBienThe { get; set; }
 public int SoLuongTon { get; set; }
}
public class NhaCungCapRow
{
 public int MaNCC { get; set; }
 public string TenNCC { get; set; } = "";
 public string? SoDienThoai { get; set; }
 public string? Email { get; set; }
 public string? DiaChi { get; set; }
 public bool TrangThai { get; set; }
}
public class PhieuNhapRow
{
 public int MaPhieuNhap { get; set; }
 public int MaNCC { get; set; }
 public int MaKho { get; set; }
 public int MaNV { get; set; }
 public DateTime NgayNhap { get; set; }
 public decimal TongTien { get; set; }
 public string? GhiChu { get; set; }
 public string? QReference { get; set; }
}
public class ChiTietPhieuNhapRow
{
 public int MaPhieuNhap { get; set; }
 public int MaBienThe { get; set; }
 public int SoLuong { get; set; }
 public decimal GiaNhap { get; set; }
}
public class GioHangRow
{
 public int MaGioHang { get; set; }
 public int MaKH { get; set; }
 public DateTime NgayTao { get; set; }
}
public class ChiTietGioHangRow
{
 public int MaGioHang { get; set; }
 public int MaBienThe { get; set; }
 public int SoLuong { get; set; }
}
public class YeuThichRow
{
 public int MaKH { get; set; }
 public int MaSP { get; set; }
 public DateTime NgayThem { get; set; }
}
public class KhuyenMaiRow
{
 public int MaKM { get; set; }
 public string? MaCode { get; set; }
 public string TenKhuyenMai { get; set; } = "";
 public string LoaiGiam { get; set; } = "";
 public decimal GiaTriGiam { get; set; }
 public decimal DonHangToiThieu { get; set; }
 public decimal? GiamToiDa { get; set; }
 public int? SoLuong { get; set; }
 public DateTime NgayBatDau { get; set; }
 public DateTime NgayKetThuc { get; set; }
 public bool TrangThai { get; set; }
}
public class ChiTietKhuyenMaiRow
{
 public int MaKM { get; set; }
 public int MaSP { get; set; }
}
public class DonHangRow
{
 public int MaDonHang { get; set; }
 public int MaKH { get; set; }
 public int? MaKM { get; set; }
 public DateTime NgayDat { get; set; }
 public string HoTenNguoiNhan { get; set; } = "";
 public string SoDienThoaiNhan { get; set; } = "";
 public string DiaChiGiaoHang { get; set; } = "";
 public decimal TongTienHang { get; set; }
 public decimal TienGiam { get; set; }
 public decimal PhiVanChuyen { get; set; }
 public decimal TongThanhToan { get; set; }
 public string TrangThai { get; set; } = "";
 public string? GhiChu { get; set; }
 public string? QPublicId { get; set; }
 public string? QCheckoutToken { get; set; }
 public string? QLegacyId { get; set; }
}
public class ChiTietDonHangRow
{
 public int MaCTDH { get; set; }
 public int MaDonHang { get; set; }
 public int MaBienThe { get; set; }
 public string TenSanPham { get; set; } = "";
 public string? KichThuoc { get; set; }
 public string? MauSac { get; set; }
 public int SoLuong { get; set; }
 public decimal DonGia { get; set; }
 public string? QImage { get; set; }
 public int? QWarehouseId { get; set; }
}
public class ThanhToanRow
{
 public int MaThanhToan { get; set; }
 public int MaDonHang { get; set; }
 public string PhuongThuc { get; set; } = "";
 public decimal SoTien { get; set; }
 public string TrangThai { get; set; } = "";
 public string? MaGiaoDich { get; set; }
 public DateTime? NgayThanhToan { get; set; }
}
public class LichSuTrangThaiDonHangRow
{
 public long MaLichSuTrangThai { get; set; }
 public int MaDonHang { get; set; }
 public string TrangThai { get; set; } = "";
 public DateTime ThoiGian { get; set; }
 public string? GhiChu { get; set; }
}
public class PhieuTraHangRow
{
 public int MaPhieuTra { get; set; }
 public int MaDonHang { get; set; }
 public int MaKH { get; set; }
 public int? MaNV { get; set; }
 public DateTime NgayYeuCau { get; set; }
 public string LyDo { get; set; } = "";
 public string TrangThai { get; set; } = "";
 public decimal TongTienHoan { get; set; }
 public DateTime? NgayXuLy { get; set; }
 public string? GhiChu { get; set; }
 public string? QReference { get; set; }
}
public class ChiTietTraHangRow
{
 public int MaCTTra { get; set; }
 public int MaPhieuTra { get; set; }
 public int MaCTDH { get; set; }
 public int SoLuongTra { get; set; }
 public string? LyDoChiTiet { get; set; }
 public string? TinhTrangSanPham { get; set; }
 public string? HinhAnh { get; set; }
 public decimal SoTienHoan { get; set; }
}
public class DanhGiaRow
{
 public int MaDanhGia { get; set; }
 public int MaKH { get; set; }
 public int MaSP { get; set; }
 public int SoSao { get; set; }
 public string? NoiDung { get; set; }
 public DateTime NgayDanhGia { get; set; }
 public bool TrangThai { get; set; }
}
