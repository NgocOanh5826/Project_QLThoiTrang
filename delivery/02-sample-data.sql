-- VAI TRÒ SCRIPT SQL: Nạp dữ liệu mẫu; các INSERT bên dưới là dữ liệu khởi tạo, không phải thao tác thêm của
-- form website.
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


INSERT INTO KhachHang (HoTen, Email, MatKhau, SoDienThoai, GioiTinh, NgaySinh, DiaChi)
VALUES (N'Nguyễn Văn An', 'an@gmail.com', 'AQAAAAIAAYagAAAAEKPuJVAqcAeyTpY+I3E40hIRuBcGtBPHF6l5m40ZrMUbgvgB6dj+hBFgoLeBKPVdKw==', '0901000001', N'Nam', '2002-05-10', N'Quận 1, TP.HCM'),
       (N'Trần Thị Bình', 'binh@gmail.com', 'AQAAAAIAAYagAAAAECQkQPt+ouo3VN3W+ZwtUFU8Un1J+kMaSo+cAcprMgHUBZfkLBLxMNJ1VhtAY4qYXg==', '0901000002', N'Nữ', '2003-08-15', N'Quận 3, TP.HCM'),
       (N'Lê Hoàng Nam', 'nam@gmail.com', 'AQAAAAIAAYagAAAAELZ23uFhOMsdAYP0AY7E4YUsPdfWJ5r/g9Tk724HO+tftw08D2J3kP7eYEIyeXVi3g==', '0901000003', N'Nam', '2001-11-20', N'Quận Bình Thạnh, TP.HCM'),
       (N'Phạm Ngọc Mai', 'mai@gmail.com', 'AQAAAAIAAYagAAAAEMomMjv8LNx7qgW/dst3NmNigLfD5jUpVU5ElWKopSXxnAd0LHsP/xZVn8yRanoxYg==', '0901000004', N'Nữ', '2004-03-25', N'Thành phố Thủ Đức, TP.HCM'),
       (N'Võ Minh Khang', 'khang@gmail.com', 'AQAAAAIAAYagAAAAEC1dzMkrNaVL3jh0SDNTWtSQmFGlp6kj2M/oII9o9AHOCh1qjqFm/nv+BnfeSr84sw==', '0901000005', N'Nam', '2002-09-12', N'Quận 7, TP.HCM');

GO

/* ============================================================
   NHAN VIEN
   ============================================================ */
INSERT INTO NhanVien (HoTen, Email, MatKhau, SoDienThoai, GioiTinh, NgaySinh, DiaChi, ChucVu)
VALUES (N'Nguyễn Quản Trị', 'admin@shop.com', 'AQAAAAIAAYagAAAAEK/Ry5n8fU+D+TJkDGBbei+Q09ABDN8TcKTB6XiskxaL2td1NDRLWgcrmSVh8fm8zw==', '0911000001', N'Nam', '1995-01-10', N'TP.HCM', N'Admin'),
       (N'Trần Minh Anh', 'sanpham@shop.com', 'AQAAAAIAAYagAAAAEMjseq6WymnvXGRR24jK7qjRSfv+ScwKCMo1Qi3oWjtxm50fTfDueNYMsIlg6nKwdQ==', '0911000002', N'Nữ', '1998-05-20', N'TP.HCM', N'Quản lý sản phẩm'),
       (N'Lê Quốc Huy', 'kho@shop.com', 'AQAAAAIAAYagAAAAEOdAt6MKrzhR+IvZNasQ1lDj29A4Utd1p/A05mU8HrSQqgS5v3zcUXbXrSRWPbhs6w==', '0911000003', N'Nam', '1997-09-12', N'TP.HCM', N'Quản lý kho'),
       (N'Nguyễn Thu Hà', 'donhang@shop.com', 'AQAAAAIAAYagAAAAEMjgN/l+B4aYv3sgchBHxLxJemQ6IxeV8orV9Fur21ZtytsfTGVcQVVZ00kGrjHUAg==', '0911000004', N'Nữ', '1999-02-17', N'TP.HCM', N'Quản lý đơn hàng');

GO

/* ============================================================
   LICH SU DANG NHAP
   ============================================================ */
INSERT INTO LichSuDangNhap (MaKH,
                 MaNV, ThoiGianDangNhap, ThoiGianDangXuat, DiaChiIP, ThietBi, TrangThai)
VALUES (1, NULL, '2026-09-18 08:00', '2026-09-18 08:30', '192.168.1.10', N'Chrome - Windows', N'Thành công'),
       (2, NULL, '2026-09-18 09:15', '2026-09-18 09:45', '192.168.1.11', N'Chrome - Android', N'Thành công'),
       (NULL, 1, '2026-09-18 07:30', '2026-09-18 17:00', '192.168.1.20', N'Edge - Windows', N'Thành công'),
       (NULL, 3, '2026-09-19 08:00', '2026-09-19 16:30', '192.168.1.21', N'Chrome - Windows', N'Thành công');

GO

/* ============================================================
   DANH MUC
   ============================================================ */
INSERT INTO DanhMuc (TenDanhMuc, MoTa)
VALUES (N'Áo nam', N'Các sản phẩm áo dành cho nam'),
       (N'Áo nữ', N'Các sản phẩm áo dành cho nữ'),
       (N'Quần nam', N'Quần thời trang nam'),
       (N'Quần nữ', N'Quần thời trang nữ'),
       (N'Váy', N'Váy và đầm nữ'),
       (N'Phụ kiện', N'Các phụ kiện thời trang');

GO

/* ============================================================
   THUONG HIEU
   ============================================================ */
INSERT INTO ThuongHieu (TenThuongHieu, MoTa)
VALUES (N'Nike', N'Thương hiệu Nike'),
       (N'Adidas', N'Thương hiệu Adidas'),
       (N'Levi''s', N'Thương hiệu thời trang Levi''s'),
       (N'Zara', N'Thương hiệu Zara'),
       (N'Local Brand', N'Thương hiệu thời trang trong nước');

GO

/* ============================================================
   KICH THUOC
   ============================================================ */
INSERT INTO KichThuoc (TenKichThuoc)
VALUES (N'XS'),
       (N'S'),
       (N'M'),
       (N'L'),
       (N'XL'),
       (N'XXL');

GO

/* ============================================================
   MAU SAC
   ============================================================ */
INSERT INTO MauSac (TenMauSac, MaMau)
VALUES (N'Đen', '#000000'),
       (N'Trắng', '#FFFFFF'),
       (N'Đỏ', '#FF0000'),
       (N'Xanh dương', '#0000FF'),
       (N'Xám', '#808080'),
       (N'Be', '#F5F5DC');

GO

/* ============================================================
   SAN PHAM
   ============================================================ */
INSERT INTO SanPham (MaDanhMuc, MaThuongHieu, TenSP, MoTa, GiaBan)
VALUES (1, 1, N'Áo thun Nike Basic', N'Áo thun nam chất liệu cotton', 350000),
       (1, 2, N'Áo Polo Adidas', N'Áo polo nam phong cách thể thao', 550000),
       (3, 3, N'Quần Jean Levi''s', N'Quần jean nam form regular', 850000),
       (2, 4, N'Áo sơ mi nữ Zara', N'Áo sơ mi nữ công sở', 490000),
       (5, 4, N'Váy nữ Zara', N'Váy nữ phong cách hiện đại', 750000),
       (4, 5, N'Quần ống rộng nữ', N'Quần nữ form rộng', 420000),
       (6, 1, N'Nón Nike', N'Nón thời trang Nike', 300000),
       (6, 5, N'Túi đeo chéo', N'Túi thời trang unisex', 390000);

GO

/* ============================================================
   BIEN THE SAN PHAM
   ============================================================ */
INSERT INTO BienTheSanPham (MaSP, MaKichThuoc, MaMauSac, SKU, GiaBanRieng)
VALUES (1, 3, 1, 'NIKE-TS-DEN-M', 350000),
       (1, 4, 1, 'NIKE-TS-DEN-L', 350000),
       (1, 3, 2, 'NIKE-TS-TRANG-M', 350000),
       (2, 3, 1, 'ADI-POLO-DEN-M', 550000),
       (2, 4, 2, 'ADI-POLO-TRANG-L', 550000),
       (3, 3, 4, 'LEVIS-JEAN-XANH-M', 850000),
       (3, 4, 4, 'LEVIS-JEAN-XANH-L', 850000),
       (4, 2, 2, 'ZARA-SOMI-TRANG-S', 490000),
       (4, 3, 2, 'ZARA-SOMI-TRANG-M', 490000),
       (5, 2, 1, 'ZARA-VAY-DEN-S', 750000),
       (5, 3, 1, 'ZARA-VAY-DEN-M', 750000),
       (6, 2, 6, 'LOCAL-QR-BE-S', 420000),
       (6, 3, 6, 'LOCAL-QR-BE-M', 420000),
       (7, 3, 1, 'NIKE-NON-DEN', 300000),
       (8, 3, 1, 'LOCAL-TUI-DEN', 390000);

GO

/* ============================================================
   HINH ANH SAN PHAM
   ============================================================ */
INSERT INTO HinhAnhSanPham (MaSP, DuongDan, AnhChinh)
VALUES (1, '/images/products/ao-nike-1.jpg', 1),
       (1, '/images/products/ao-nike-2.jpg', 0),
       (2, '/images/products/polo-adidas.jpg', 1),
       (3, '/images/products/jean-levis.jpg', 1),
       (4, '/images/products/somi-zara.jpg', 1),
       (5, '/images/products/vay-zara.jpg', 1),
       (6, '/images/products/quan-ong-rong.jpg', 1),
       (7, '/images/products/non-nike.jpg', 1),
       (8, '/images/products/tui-deo-cheo.jpg', 1);

GO

/* ============================================================
   KHO
   ============================================================ */
INSERT INTO Kho (TenKho, DiaChi)
VALUES (N'Kho chính', N'Quận 1, TP.HCM'),
       (N'Kho phụ', N'Thành phố Thủ Đức, TP.HCM');

GO

/* ============================================================
   TON KHO
   ============================================================ */
INSERT INTO TonKho (MaKho, MaBienThe, SoLuongTon)
VALUES (1,1,30),
       (1,2,25),
       (1,3,20),
       (1,4,15),
       (1,5,20),
       (1,6,18),
       (1,7,16),
       (1,8,12),
       (1,9,15),
       (1,10,10),
       (1,11,12),
       (1,12,15),
       (1,13,18),
       (1,14,20),
       (1,15,15),
       (2,1,10),
       (2,2,10),
       (2,3,8),
       (2,4,10),
       (2,5,10);

GO

/* ============================================================
   NHA CUNG CAP
   ============================================================ */
INSERT INTO NhaCungCap (TenNCC, SoDienThoai, Email, DiaChi)
VALUES (N'Công ty Thời Trang Việt', '0281111111', 'vietfashion@gmail.com', N'TP.HCM'),
       (N'Công ty Phân Phối ABC', '0282222222', 'abc@gmail.com', N'Bình Dương'),
       (N'Nhà cung cấp Minh Phát', '0283333333', 'minhphat@gmail.com', N'Đồng Nai');

GO

/* ============================================================
   PHIEU NHAP
   ============================================================ */
INSERT INTO PhieuNhap (MaNCC, MaKho, MaNV, NgayNhap, TongTien, GhiChu)
VALUES (1,1,3, '2026-09-01 08:00', 15000000, N'Nhập hàng đầu tháng'),
       (2,1,3, '2026-09-10 09:00', 12000000, N'Nhập bổ sung'),
       (3,2,3, '2026-09-15 10:00', 7000000, N'Nhập kho phụ');

GO

/* ============================================================
   CHI TIET PHIEU NHAP
   ============================================================ */
INSERT INTO ChiTietPhieuNhap (MaPhieuNhap, MaBienThe, SoLuong, GiaNhap)
VALUES (1,1,30,220000),
       (1,2,25,220000),
       (1,3,20,220000),
       (2,4,15,350000),
       (2,5,20,350000),
       (2,6,10,600000),
       (3,12,10,280000),
       (3,13,10,280000);

GO

/* ============================================================
   GIO HANG
   ============================================================ */
INSERT INTO GioHang (MaKH)
VALUES (1),
       (2),
       (3),
       (4),
       (5);

GO

/* ============================================================
   CHI TIET GIO HANG
   ============================================================ */
INSERT INTO ChiTietGioHang (MaGioHang, MaBienThe, SoLuong)
VALUES (1,4,1),
       (1,6,1),
       (2,8,2),
       (3,10,1),
       (4,12,1),
       (4,15,1),
       (5,1,2);

GO

/* ============================================================
   YEU THICH
   ============================================================ */
INSERT INTO YeuThich (MaKH, MaSP)
VALUES (1,1),
       (1,3),
       (2,4),
       (2,5),
       (3,2),
       (4,6),
       (5,1),
       (5,8);

GO

/* ============================================================
   KHUYEN MAI
   ============================================================ */
INSERT INTO KhuyenMai (MaCode,
                 TenKhuyenMai, LoaiGiam, GiaTriGiam, DonHangToiThieu, GiamToiDa, SoLuong, NgayBatDau, NgayKetThuc)
VALUES ('SALE10', N'Giảm 10%', 'PHANTRAM', 10, 300000, 100000, 100, '2026-09-01', '2026-12-31'),
       ('GIAM50K', N'Giảm 50.000 đồng', 'SOTIEN', 50000, 500000, 50000, 50, '2026-09-01', '2026-12-31'),
       ('FASHION20', N'Khuyến mãi thời trang 20%', 'PHANTRAM', 20, 1000000, 200000, 30, '2026-09-01', '2026-10-31');

GO

/* ============================================================
   CHI TIET KHUYEN MAI
   ============================================================ */
INSERT INTO ChiTietKhuyenMai (MaKM, MaSP)
VALUES (1,1),
       (1,2),
       (1,4),
       (2,3),
       (2,5),
       (3,6),
       (3,8);

GO

/* ============================================================
   DON HANG
   ============================================================ */
INSERT INTO DonHang (MaKH,
                 MaKM, NgayDat, HoTenNguoiNhan, SoDienThoaiNhan, DiaChiGiaoHang, TongTienHang, TienGiam, PhiVanChuyen,
                 TongThanhToan, TrangThai, GhiChu)
VALUES /* Đơn 1 - hoàn thành */ (1, 1, '2026-09-10 08:00', N'Nguyễn Văn An', '0901000001', N'Quận 1, TP.HCM', 700000, 70000, 30000, 660000, N'Hoàn thành', N'Giao giờ hành chính'),
                                /* Đơn 2 - đang giao */ (2, NULL, '2026-09-18 09:00', N'Trần Thị Bình', '0901000002', N'Quận 3, TP.HCM', 980000, 0, 30000, 1010000, N'Đang giao', NULL),
                                                        /* Đơn 3 - đã xác nhận */ (3, 2, '2026-09-19 10:00', N'Lê Hoàng Nam', '0901000003', N'Bình Thạnh, TP.HCM', 850000, 50000, 30000, 830000, N'Đã xác nhận', NULL),
                                                                                  /* Đơn 4 - đã hủy */ (4, NULL, '2026-09-15 11:00', N'Phạm Ngọc Mai', '0901000004', N'Thành phố Thủ Đức, TP.HCM', 750000, 0, 30000, 780000, N'Đã hủy', N'Khách hàng yêu cầu hủy');

GO

/* ============================================================
   CHI TIET DON HANG
   ============================================================ */
INSERT INTO ChiTietDonHang (MaDonHang, MaBienThe, TenSanPham, KichThuoc, MauSac, SoLuong, DonGia)
VALUES /* Đơn 1 */ (1,1, N'Áo thun Nike Basic', N'M', N'Đen', 2, 350000),
                   /* Đơn 2 */ (2,8, N'Áo sơ mi nữ Zara', N'S', N'Trắng', 2, 490000),
                               /* Đơn 3 */ (3,6, N'Quần Jean Levi''s', N'M', N'Xanh dương', 1, 850000),
                                           /* Đơn 4 */ (4,10, N'Váy nữ Zara', N'S', N'Đen', 1, 750000);

GO

/* ============================================================
   THANH TOAN
   ============================================================ */
INSERT INTO ThanhToan (MaDonHang, PhuongThuc, SoTien, TrangThai, MaGiaoDich, NgayThanhToan)
VALUES (1, N'Chuyển khoản', 660000, N'Đã thanh toán', 'GD000001', '2026-09-10 08:10'),
       (2, N'COD', 1010000, N'Chưa thanh toán', NULL, NULL),
       (3, N'Ví điện tử', 830000, N'Đã thanh toán', 'GD000003', '2026-09-19 10:05'),
       (4, N'COD', 780000, N'Chưa thanh toán', NULL, NULL);

GO

/* ============================================================
   LICH SU TRANG THAI DON HANG
   ============================================================ */ /* Đơn 1 */
INSERT INTO LichSuTrangThaiDonHang (MaDonHang, TrangThai, ThoiGian, GhiChu)
VALUES (1,N'Chờ xác nhận', '2026-09-10 08:00', N'Đơn hàng vừa được tạo'),
       (1,N'Đã xác nhận', '2026-09-10 09:00', N'Nhân viên xác nhận đơn'),
       (1,N'Đang chuẩn bị', '2026-09-10 10:00', N'Đang đóng gói sản phẩm'),
       (1,N'Đang giao', '2026-09-11 08:00', N'Đơn hàng đang được giao'),
       (1,N'Đã giao', '2026-09-11 14:30', N'Khách hàng đã nhận hàng'),
       (1,N'Hoàn thành', '2026-09-11 15:00', N'Đơn hàng hoàn tất');

/* Đơn 2 */
INSERT INTO LichSuTrangThaiDonHang (MaDonHang, TrangThai, ThoiGian, GhiChu)
VALUES (2,N'Chờ xác nhận', '2026-09-18 09:00', N'Đơn hàng vừa được tạo'),
       (2,N'Đã xác nhận', '2026-09-18 09:30', N'Đã xác nhận'),
       (2,N'Đang chuẩn bị', '2026-09-18 11:00', N'Đang đóng gói'),
       (2,N'Đang giao', '2026-09-19 08:00', N'Đơn vị vận chuyển đã nhận hàng');

/* Đơn 3 */
INSERT INTO LichSuTrangThaiDonHang (MaDonHang, TrangThai, ThoiGian, GhiChu)
VALUES (3,N'Chờ xác nhận', '2026-09-19 10:00', N'Đơn hàng vừa được tạo'),
       (3,N'Đã xác nhận', '2026-09-19 10:30', N'Đơn hàng đã được xác nhận');

/* Đơn 4 */
INSERT INTO LichSuTrangThaiDonHang (MaDonHang, TrangThai, ThoiGian, GhiChu)
VALUES (4,N'Chờ xác nhận', '2026-09-15 11:00', N'Đơn hàng vừa được tạo'),
       (4,N'Đã hủy', '2026-09-15 11:30', N'Khách hàng yêu cầu hủy');

GO

/* ============================================================
   PHIEU TRA HANG

   Khách hàng 1 trả 1/2 áo Nike trong đơn 1
   ============================================================ */
INSERT INTO PhieuTraHang (MaDonHang, MaKH, MaNV, NgayYeuCau, LyDo, TrangThai, TongTienHoan, NgayXuLy, GhiChu)
VALUES (1, 1, 4, '2026-09-13 09:00', N'Sản phẩm không vừa kích thước', N'Đã hoàn tiền', 350000, '2026-09-14 14:00', N'Đã kiểm tra và chấp nhận trả hàng');

GO

/* ============================================================
   CHI TIET TRA HANG
   ============================================================ */
INSERT INTO ChiTietTraHang (MaPhieuTra,
                 MaCTDH, SoLuongTra, LyDoChiTiet, TinhTrangSanPham, HinhAnh, SoTienHoan)
VALUES (1, 1, 1, N'Áo không vừa size', N'Còn nguyên tem, chưa sử dụng', '/images/returns/tra-hang-001.jpg', 350000);

GO

/* ============================================================
   DANH GIA
   ============================================================ */
INSERT INTO DanhGia (MaKH, MaSP, SoSao, NoiDung, NgayDanhGia)
VALUES (1, 1, 5, N'Áo đẹp, chất liệu tốt.', '2026-09-12'),
       (2, 4, 4, N'Sản phẩm đẹp, giao hàng nhanh.', '2026-09-20'),
       (3, 3, 5, N'Quần đẹp và đúng mô tả.', '2026-09-20');

GO

/* ============================================================
   CÁC VIEW PHỤC VỤ WEBSITE
   ============================================================ */ /* ------------------------------------------------------------
   VIEW SẢN PHẨM + DANH MỤC + THƯƠNG HIỆU
   ------------------------------------------------------------ */
COMMIT;

GO
