-- VAI TRÒ SCRIPT SQL: Các view/thủ tục SQL hỗ trợ truy vấn và nghiệp vụ; tên đối tượng và tham số được khai báo
-- ngay trong file.
-- LIÊN KẾT CODE: Data/Native/Mapping.cs, Data/FeatureMapping.cs và Data/ReviewMediaMapping.cs mô tả entity tương
-- ứng. Thêm/sửa/xóa từ giao diện được kiểm tra ở controller/service rồi EF gửi SQL; không sửa file này để cập
-- nhật từng đơn hoặc từng sản phẩm.

:ON ERROR EXIT USE [$(DatabaseName)];

GO


SET QUOTED_IDENTIFIER ON;


SET ANSI_NULLS ON;


SET ANSI_WARNINGS ON;


SET ARITHABORT ON;


SET ANSI_PADDING ON;


SET CONCAT_NULL_YIELDS_NULL ON;


SET NUMERIC_ROUNDABORT OFF;


SET XACT_ABORT ON;

BEGIN TRANSACTION;

GO


CREATE VIEW vw_DanhSachSanPham AS
SELECT SP.MaSP, SP.TenSP, DM.TenDanhMuc, TH.TenThuongHieu, SP.GiaBan, SP.MoTa, SP.TrangThai
FROM SanPham SP
INNER JOIN DanhMuc DM ON SP.MaDanhMuc = DM.MaDanhMuc
LEFT JOIN ThuongHieu TH ON SP.MaThuongHieu = TH.MaThuongHieu;

GO

/* ------------------------------------------------------------
   VIEW BIẾN THỂ SẢN PHẨM
   ------------------------------------------------------------ */
CREATE VIEW vw_BienTheSanPham AS
SELECT BT.MaBienThe, SP.MaSP, SP.TenSP, KT.TenKichThuoc, MS.TenMauSac, BT.SKU, COALESCE(BT.GiaBanRieng,
                                                                                   SP.GiaBan) AS GiaBan, BT.TrangThai
FROM BienTheSanPham BT
INNER JOIN SanPham SP ON BT.MaSP = SP.MaSP
INNER JOIN KichThuoc KT ON BT.MaKichThuoc = KT.MaKichThuoc
INNER JOIN MauSac MS ON BT.MaMauSac = MS.MaMauSac;

GO

/* ------------------------------------------------------------
   VIEW TỒN KHO
   ------------------------------------------------------------ */
CREATE VIEW vw_TonKho AS
SELECT K.MaKho, K.TenKho, BT.MaBienThe, SP.TenSP, KT.TenKichThuoc, MS.TenMauSac, TK.SoLuongTon
FROM TonKho TK
INNER JOIN Kho K ON TK.MaKho = K.MaKho
INNER JOIN BienTheSanPham BT ON TK.MaBienThe = BT.MaBienThe
INNER JOIN SanPham SP ON BT.MaSP = SP.MaSP
INNER JOIN KichThuoc KT ON BT.MaKichThuoc = KT.MaKichThuoc
INNER JOIN MauSac MS ON BT.MaMauSac = MS.MaMauSac;

GO

/* ------------------------------------------------------------
   VIEW DANH SÁCH ĐƠN HÀNG
   ------------------------------------------------------------ */
CREATE VIEW vw_DanhSachDonHang AS
SELECT DH.MaDonHang, DH.NgayDat, KH.MaKH, KH.HoTen AS KhachHang, DH.HoTenNguoiNhan, DH.SoDienThoaiNhan, DH.DiaChiGiaoHang, DH.TongTienHang,
       DH.TienGiam, DH.PhiVanChuyen, DH.TongThanhToan, DH.TrangThai
FROM DonHang DH
INNER JOIN KhachHang KH ON DH.MaKH = KH.MaKH;

GO

/* ------------------------------------------------------------
   VIEW CHI TIẾT ĐƠN HÀNG
   ------------------------------------------------------------ */
CREATE VIEW vw_ChiTietDonHang AS
SELECT CT.MaCTDH, CT.MaDonHang, CT.TenSanPham, CT.KichThuoc, CT.MauSac, CT.SoLuong, CT.DonGia, CT.SoLuong * CT.DonGia AS ThanhTien
FROM ChiTietDonHang CT;

GO

/* ------------------------------------------------------------
   VIEW ĐÁNH GIÁ
   ------------------------------------------------------------ */
CREATE VIEW vw_DanhGiaSanPham AS
SELECT DG.MaDanhGia, SP.MaSP, SP.TenSP, KH.HoTen AS KhachHang, DG.SoSao, DG.NoiDung, DG.NgayDanhGia
FROM DanhGia DG
INNER JOIN SanPham SP ON DG.MaSP = SP.MaSP
INNER JOIN KhachHang KH ON DG.MaKH = KH.MaKH
WHERE DG.TrangThai = 1;

GO

/* ============================================================
   STORED PROCEDURE: THEO DÕI ĐƠN HÀNG
   ============================================================ */
CREATE PROCEDURE sp_TheoDoiDonHang @MaDonHang INT AS BEGIN
SELECT DH.MaDonHang, DH.NgayDat, DH.HoTenNguoiNhan, DH.DiaChiGiaoHang, DH.TrangThai AS TrangThaiHienTai, LS.TrangThai, LS.ThoiGian,
       LS.GhiChu
FROM DonHang DH
INNER JOIN LichSuTrangThaiDonHang LS ON DH.MaDonHang = LS.MaDonHang
WHERE DH.MaDonHang = @MaDonHang
ORDER BY LS.ThoiGian ASC; END;

GO

/* ============================================================
   STORED PROCEDURE: LỊCH SỬ ĐƠN HÀNG KHÁCH HÀNG
   ============================================================ */
CREATE PROCEDURE sp_LichSuDonHangKhachHang @MaKH INT AS BEGIN
SELECT MaDonHang, NgayDat, TongThanhToan, TrangThai
FROM DonHang
WHERE MaKH = @MaKH
ORDER BY NgayDat DESC; END;

GO

/* ============================================================
   STORED PROCEDURE: TÌM KIẾM SẢN PHẨM
   ============================================================ */
CREATE PROCEDURE sp_TimKiemSanPham @TuKhoa NVARCHAR(200) AS BEGIN
SELECT SP.MaSP, SP.TenSP, DM.TenDanhMuc, TH.TenThuongHieu, SP.GiaBan, SP.MoTa
FROM SanPham SP
INNER JOIN DanhMuc DM ON SP.MaDanhMuc = DM.MaDanhMuc
LEFT JOIN ThuongHieu TH ON SP.MaThuongHieu = TH.MaThuongHieu
WHERE SP.TrangThai = 1
    AND (SP.TenSP LIKE N'%' + @TuKhoa + N'%'
         OR DM.TenDanhMuc LIKE N'%' + @TuKhoa + N'%'
         OR TH.TenThuongHieu LIKE N'%' + @TuKhoa + N'%'); END;

GO

/* ============================================================
   STORED PROCEDURE: DOANH THU
   ============================================================ */
CREATE PROCEDURE sp_ThongKeDoanhThu @TuNgay DATE,@DenNgay DATE AS BEGIN
SELECT CAST(d.NgayDat AS DATE) Ngay,COUNT(*) SoDonHang, SUM(d.TongThanhToan-COALESCE(r.Refund,
                                                                                0)) DoanhThu
FROM DonHang d
LEFT JOIN
    (SELECT MaDonHang,SUM(TongTienHoan) Refund
     FROM PhieuTraHang
     WHERE TrangThai IN(N'Đã hoàn tiền',N'Hoàn tất')
     GROUP BY MaDonHang) r ON r.MaDonHang=d.MaDonHang
WHERE d.TrangThai IN(N'Đã giao',N'Hoàn thành')
    AND CAST(d.NgayDat AS DATE) BETWEEN @TuNgay AND @DenNgay
GROUP BY CAST(d.NgayDat AS DATE)
ORDER BY Ngay; END;

GO

/* ============================================================
   KIỂM TRA DATABASE
   ============================================================ */
COMMIT;

GO
