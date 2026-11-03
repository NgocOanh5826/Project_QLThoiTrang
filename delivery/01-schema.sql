:ON ERROR EXIT
USE [$(DatabaseName)];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
IF EXISTS(SELECT 1 FROM sys.tables WHERE is_ms_shipped=0) THROW 51000,'Schema requires empty database; refusing overwrite.',1;
BEGIN TRANSACTION;
GO
CREATE TABLE KhachHang (
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    SoDienThoai VARCHAR(15),
    GioiTinh NVARCHAR(10),
    NgaySinh DATE,
    DiaChi NVARCHAR(255),
    NgayDangKy DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_KhachHang_GioiTinh
    CHECK (
        GioiTinh IS NULL
        OR GioiTinh IN (N'Nam', N'Nữ', N'Khác')
    )
);
GO


/* ============================================================
   2. NHAN VIEN
   ============================================================ */

CREATE TABLE NhanVien (
    MaNV INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    SoDienThoai VARCHAR(15),
    GioiTinh NVARCHAR(10),
    NgaySinh DATE,
    DiaChi NVARCHAR(255),

    ChucVu NVARCHAR(50) NOT NULL,

    NgayVaoLam DATE NOT NULL DEFAULT GETDATE(),
    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_NhanVien_GioiTinh
    CHECK (
        GioiTinh IS NULL
        OR GioiTinh IN (N'Nam', N'Nữ', N'Khác')
    )
);
GO


/* ============================================================
   3. LICH SU DANG NHAP
   ============================================================ */

CREATE TABLE LichSuDangNhap (
    MaLichSu BIGINT IDENTITY(1,1) PRIMARY KEY,

    MaKH INT NULL,
    MaNV INT NULL,

    ThoiGianDangNhap DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    ThoiGianDangXuat DATETIME2 NULL,

    DiaChiIP VARCHAR(45),
    ThietBi NVARCHAR(255),

    TrangThai NVARCHAR(20) NOT NULL,

    CONSTRAINT FK_LSDN_KhachHang
    FOREIGN KEY (MaKH)
    REFERENCES KhachHang(MaKH),

    CONSTRAINT FK_LSDN_NhanVien
    FOREIGN KEY (MaNV)
    REFERENCES NhanVien(MaNV),

    CONSTRAINT CK_LSDN_NguoiDung
    CHECK (
        (MaKH IS NOT NULL AND MaNV IS NULL)
        OR
        (MaKH IS NULL AND MaNV IS NOT NULL)
    ),

    CONSTRAINT CK_LSDN_TrangThai
    CHECK (
        TrangThai IN (N'Thành công', N'Thất bại')
    )
);
GO


/* ============================================================
   4. DANH MUC
   ============================================================ */

CREATE TABLE DanhMuc (
    MaDanhMuc INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL UNIQUE,
    MoTa NVARCHAR(500),
    TrangThai BIT NOT NULL DEFAULT 1
);
GO


/* ============================================================
   5. THUONG HIEU
   ============================================================ */

CREATE TABLE ThuongHieu (
    MaThuongHieu INT IDENTITY(1,1) PRIMARY KEY,
    TenThuongHieu NVARCHAR(100) NOT NULL UNIQUE,
    MoTa NVARCHAR(500),
    TrangThai BIT NOT NULL DEFAULT 1
);
GO


/* ============================================================
   6. SAN PHAM
   ============================================================ */

CREATE TABLE SanPham (
    MaSP INT IDENTITY(1,1) PRIMARY KEY,

    MaDanhMuc INT NOT NULL,
    MaThuongHieu INT NULL,

    TenSP NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),

    GiaBan DECIMAL(18,2) NOT NULL,

    NgayTao DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_SP_DanhMuc
    FOREIGN KEY (MaDanhMuc)
    REFERENCES DanhMuc(MaDanhMuc),

    CONSTRAINT FK_SP_ThuongHieu
    FOREIGN KEY (MaThuongHieu)
    REFERENCES ThuongHieu(MaThuongHieu),

    CONSTRAINT CK_SP_GiaBan
    CHECK (GiaBan >= 0)
);
GO


/* ============================================================
   7. KICH THUOC
   ============================================================ */

CREATE TABLE KichThuoc (
    MaKichThuoc INT IDENTITY(1,1) PRIMARY KEY,
    TenKichThuoc NVARCHAR(30) NOT NULL UNIQUE
);
GO


/* ============================================================
   8. MAU SAC
   ============================================================ */

CREATE TABLE MauSac (
    MaMauSac INT IDENTITY(1,1) PRIMARY KEY,
    TenMauSac NVARCHAR(50) NOT NULL UNIQUE,
    MaMau VARCHAR(20)
);
GO


/* ============================================================
   9. BIEN THE SAN PHAM
   ============================================================ */

CREATE TABLE BienTheSanPham (
    MaBienThe INT IDENTITY(1,1) PRIMARY KEY,

    MaSP INT NOT NULL,
    MaKichThuoc INT NOT NULL,
    MaMauSac INT NOT NULL,

    SKU VARCHAR(50) NOT NULL UNIQUE,

    GiaBanRieng DECIMAL(18,2) NULL,

    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_BT_SP
    FOREIGN KEY (MaSP)
    REFERENCES SanPham(MaSP),

    CONSTRAINT FK_BT_KichThuoc
    FOREIGN KEY (MaKichThuoc)
    REFERENCES KichThuoc(MaKichThuoc),

    CONSTRAINT FK_BT_MauSac
    FOREIGN KEY (MaMauSac)
    REFERENCES MauSac(MaMauSac),

    CONSTRAINT UQ_BienThe
    UNIQUE (MaSP, MaKichThuoc, MaMauSac),

    CONSTRAINT CK_BT_Gia
    CHECK (
        GiaBanRieng IS NULL
        OR GiaBanRieng >= 0
    )
);
GO


/* ============================================================
   10. HINH ANH SAN PHAM
   ============================================================ */

CREATE TABLE HinhAnhSanPham (
    MaHinh INT IDENTITY(1,1) PRIMARY KEY,

    MaSP INT NOT NULL,

    DuongDan NVARCHAR(500) NOT NULL,

    AnhChinh BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_HinhAnh_SP
    FOREIGN KEY (MaSP)
    REFERENCES SanPham(MaSP)
    ON DELETE CASCADE
);
GO


/* ============================================================
   11. KHO
   ============================================================ */

CREATE TABLE Kho (
    MaKho INT IDENTITY(1,1) PRIMARY KEY,

    TenKho NVARCHAR(100) NOT NULL,

    DiaChi NVARCHAR(255),

    TrangThai BIT NOT NULL DEFAULT 1
);
GO


/* ============================================================
   12. TON KHO
   ============================================================ */

CREATE TABLE TonKho (
    MaKho INT NOT NULL,
    MaBienThe INT NOT NULL,

    SoLuongTon INT NOT NULL DEFAULT 0,

    CONSTRAINT PK_TonKho
    PRIMARY KEY (MaKho, MaBienThe),

    CONSTRAINT FK_TonKho_Kho
    FOREIGN KEY (MaKho)
    REFERENCES Kho(MaKho),

    CONSTRAINT FK_TonKho_BienThe
    FOREIGN KEY (MaBienThe)
    REFERENCES BienTheSanPham(MaBienThe),

    CONSTRAINT CK_TonKho_SoLuong
    CHECK (SoLuongTon >= 0)
);
GO


/* ============================================================
   13. NHA CUNG CAP
   ============================================================ */

CREATE TABLE NhaCungCap (
    MaNCC INT IDENTITY(1,1) PRIMARY KEY,

    TenNCC NVARCHAR(150) NOT NULL,

    SoDienThoai VARCHAR(15),

    Email VARCHAR(150),

    DiaChi NVARCHAR(255),

    TrangThai BIT NOT NULL DEFAULT 1
);
GO


/* ============================================================
   14. PHIEU NHAP
   ============================================================ */

CREATE TABLE PhieuNhap (
    MaPhieuNhap INT IDENTITY(1,1) PRIMARY KEY,

    MaNCC INT NOT NULL,
    MaKho INT NOT NULL,
    MaNV INT NOT NULL,

    NgayNhap DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    TongTien DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    GhiChu NVARCHAR(500),

    CONSTRAINT FK_PN_NCC
    FOREIGN KEY (MaNCC)
    REFERENCES NhaCungCap(MaNCC),

    CONSTRAINT FK_PN_Kho
    FOREIGN KEY (MaKho)
    REFERENCES Kho(MaKho),

    CONSTRAINT FK_PN_NV
    FOREIGN KEY (MaNV)
    REFERENCES NhanVien(MaNV),

    CONSTRAINT CK_PN_TongTien
    CHECK (TongTien >= 0)
);
GO


/* ============================================================
   15. CHI TIET PHIEU NHAP
   ============================================================ */

CREATE TABLE ChiTietPhieuNhap (
    MaPhieuNhap INT NOT NULL,

    MaBienThe INT NOT NULL,

    SoLuong INT NOT NULL,

    GiaNhap DECIMAL(18,2) NOT NULL,

    CONSTRAINT PK_ChiTietPhieuNhap
    PRIMARY KEY (MaPhieuNhap, MaBienThe),

    CONSTRAINT FK_CTPN_PN
    FOREIGN KEY (MaPhieuNhap)
    REFERENCES PhieuNhap(MaPhieuNhap)
    ON DELETE CASCADE,

    CONSTRAINT FK_CTPN_BT
    FOREIGN KEY (MaBienThe)
    REFERENCES BienTheSanPham(MaBienThe),

    CONSTRAINT CK_CTPN_SL
    CHECK (SoLuong > 0),

    CONSTRAINT CK_CTPN_Gia
    CHECK (GiaNhap >= 0)
);
GO


/* ============================================================
   16. GIO HANG
   ============================================================ */

CREATE TABLE GioHang (
    MaGioHang INT IDENTITY(1,1) PRIMARY KEY,

    MaKH INT NOT NULL UNIQUE,

    NgayTao DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_GH_KH
    FOREIGN KEY (MaKH)
    REFERENCES KhachHang(MaKH)
);
GO


/* ============================================================
   17. CHI TIET GIO HANG
   ============================================================ */

CREATE TABLE ChiTietGioHang (
    MaGioHang INT NOT NULL,

    MaBienThe INT NOT NULL,

    SoLuong INT NOT NULL,

    CONSTRAINT PK_ChiTietGioHang
    PRIMARY KEY (MaGioHang, MaBienThe),

    CONSTRAINT FK_CTGH_GH
    FOREIGN KEY (MaGioHang)
    REFERENCES GioHang(MaGioHang)
    ON DELETE CASCADE,

    CONSTRAINT FK_CTGH_BT
    FOREIGN KEY (MaBienThe)
    REFERENCES BienTheSanPham(MaBienThe),

    CONSTRAINT CK_CTGH_SL
    CHECK (SoLuong > 0)
);
GO


/* ============================================================
   18. YEU THICH
   ============================================================ */

CREATE TABLE YeuThich (
    MaKH INT NOT NULL,

    MaSP INT NOT NULL,

    NgayThem DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_YeuThich
    PRIMARY KEY (MaKH, MaSP),

    CONSTRAINT FK_YT_KH
    FOREIGN KEY (MaKH)
    REFERENCES KhachHang(MaKH),

    CONSTRAINT FK_YT_SP
    FOREIGN KEY (MaSP)
    REFERENCES SanPham(MaSP)
);
GO


/* ============================================================
   19. KHUYEN MAI
   ============================================================ */

CREATE TABLE KhuyenMai (
    MaKM INT IDENTITY(1,1) PRIMARY KEY,

    MaCode VARCHAR(50) UNIQUE,

    TenKhuyenMai NVARCHAR(150) NOT NULL,

    LoaiGiam VARCHAR(20) NOT NULL,

    GiaTriGiam DECIMAL(18,2) NOT NULL,

    DonHangToiThieu DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    GiamToiDa DECIMAL(18,2) NULL,

    SoLuong INT NULL,

    NgayBatDau DATETIME2 NOT NULL,

    NgayKetThuc DATETIME2 NOT NULL,

    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_KM_Loai
    CHECK (
        LoaiGiam IN ('PHANTRAM', 'SOTIEN')
    ),

    CONSTRAINT CK_KM_GiaTri
    CHECK (GiaTriGiam > 0),

    CONSTRAINT CK_KM_Ngay
    CHECK (NgayKetThuc >= NgayBatDau),

    CONSTRAINT CK_KM_SL
    CHECK (
        SoLuong IS NULL
        OR SoLuong >= 0
    )
);
GO


/* ============================================================
   20. CHI TIET KHUYEN MAI
   ============================================================ */

CREATE TABLE ChiTietKhuyenMai (
    MaKM INT NOT NULL,

    MaSP INT NOT NULL,

    CONSTRAINT PK_ChiTietKhuyenMai
    PRIMARY KEY (MaKM, MaSP),

    CONSTRAINT FK_CTKM_KM
    FOREIGN KEY (MaKM)
    REFERENCES KhuyenMai(MaKM)
    ON DELETE CASCADE,

    CONSTRAINT FK_CTKM_SP
    FOREIGN KEY (MaSP)
    REFERENCES SanPham(MaSP)
);
GO


/* ============================================================
   21. DON HANG
   ============================================================ */

CREATE TABLE DonHang (
    MaDonHang INT IDENTITY(1,1) PRIMARY KEY,

    MaKH INT NOT NULL,

    MaKM INT NULL,

    NgayDat DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    HoTenNguoiNhan NVARCHAR(100) NOT NULL,

    SoDienThoaiNhan VARCHAR(15) NOT NULL,

    DiaChiGiaoHang NVARCHAR(500) NOT NULL,

    TongTienHang DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    TienGiam DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    PhiVanChuyen DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    TongThanhToan DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    TrangThai NVARCHAR(30)
        NOT NULL DEFAULT N'Chờ xác nhận',

    GhiChu NVARCHAR(500),

    CONSTRAINT FK_DH_KH
    FOREIGN KEY (MaKH)
    REFERENCES KhachHang(MaKH),

    CONSTRAINT FK_DH_KM
    FOREIGN KEY (MaKM)
    REFERENCES KhuyenMai(MaKM),

    CONSTRAINT CK_DH_TrangThai
    CHECK (
        TrangThai IN (
            N'Chờ xác nhận',
            N'Đã xác nhận',
            N'Đang chuẩn bị',
            N'Đang giao',
            N'Đã giao',
            N'Hoàn thành',
            N'Đã hủy'
        )
    ),

    CONSTRAINT CK_DH_Tien
    CHECK (
        TongTienHang >= 0
        AND TienGiam >= 0
        AND PhiVanChuyen >= 0
        AND TongThanhToan >= 0
    )
);
GO


/* ============================================================
   22. CHI TIET DON HANG
   ============================================================ */

CREATE TABLE ChiTietDonHang (
    MaCTDH INT IDENTITY(1,1) PRIMARY KEY,

    MaDonHang INT NOT NULL,

    MaBienThe INT NOT NULL,

    TenSanPham NVARCHAR(200) NOT NULL,

    KichThuoc NVARCHAR(30),

    MauSac NVARCHAR(50),

    SoLuong INT NOT NULL,

    DonGia DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_CTDH_DH
    FOREIGN KEY (MaDonHang)
    REFERENCES DonHang(MaDonHang)
    ON DELETE CASCADE,

    CONSTRAINT FK_CTDH_BT
    FOREIGN KEY (MaBienThe)
    REFERENCES BienTheSanPham(MaBienThe),

    CONSTRAINT CK_CTDH_SL
    CHECK (SoLuong > 0),

    CONSTRAINT CK_CTDH_Gia
    CHECK (DonGia >= 0)
);
GO


/* ============================================================
   23. THANH TOAN
   ============================================================ */

CREATE TABLE ThanhToan (
    MaThanhToan INT IDENTITY(1,1) PRIMARY KEY,

    MaDonHang INT NOT NULL,

    PhuongThuc NVARCHAR(30) NOT NULL,

    SoTien DECIMAL(18,2) NOT NULL,

    TrangThai NVARCHAR(30)
        NOT NULL DEFAULT N'Chưa thanh toán',

    MaGiaoDich VARCHAR(100),

    NgayThanhToan DATETIME2 NULL,

    CONSTRAINT FK_TT_DH
    FOREIGN KEY (MaDonHang)
    REFERENCES DonHang(MaDonHang),

    CONSTRAINT CK_TT_PhuongThuc
    CHECK (
        PhuongThuc IN (
            N'COD',
            N'Chuyển khoản',
            N'Ví điện tử'
        )
    ),

    CONSTRAINT CK_TT_TrangThai
    CHECK (
        TrangThai IN (
            N'Chưa thanh toán',
            N'Đã thanh toán',
            N'Thất bại',
            N'Đã hoàn tiền'
        )
    ),

    CONSTRAINT CK_TT_SoTien
    CHECK (SoTien >= 0)
);
GO


/* ============================================================
   24. LICH SU TRANG THAI DON HANG
   ============================================================ */

CREATE TABLE LichSuTrangThaiDonHang (
    MaLichSuTrangThai BIGINT
        IDENTITY(1,1) PRIMARY KEY,

    MaDonHang INT NOT NULL,

    TrangThai NVARCHAR(30) NOT NULL,

    ThoiGian DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    GhiChu NVARCHAR(500),

    CONSTRAINT FK_LSTT_DH
    FOREIGN KEY (MaDonHang)
    REFERENCES DonHang(MaDonHang),

    CONSTRAINT CK_LSTT_TrangThai
    CHECK (
        TrangThai IN (
            N'Chờ xác nhận',
            N'Đã xác nhận',
            N'Đang chuẩn bị',
            N'Đang giao',
            N'Đã giao',
            N'Hoàn thành',
            N'Đã hủy'
        )
    )
);
GO


/* ============================================================
   25. PHIEU TRA HANG
   ============================================================ */

CREATE TABLE PhieuTraHang (
    MaPhieuTra INT IDENTITY(1,1) PRIMARY KEY,

    MaDonHang INT NOT NULL,

    MaKH INT NOT NULL,

    MaNV INT NULL,

    NgayYeuCau DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    LyDo NVARCHAR(500) NOT NULL,

    TrangThai NVARCHAR(30)
        NOT NULL DEFAULT N'Chờ xử lý',

    TongTienHoan DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    NgayXuLy DATETIME2 NULL,

    GhiChu NVARCHAR(500),

    CONSTRAINT FK_PTH_DH
    FOREIGN KEY (MaDonHang)
    REFERENCES DonHang(MaDonHang),

    CONSTRAINT FK_PTH_KH
    FOREIGN KEY (MaKH)
    REFERENCES KhachHang(MaKH),

    CONSTRAINT FK_PTH_NV
    FOREIGN KEY (MaNV)
    REFERENCES NhanVien(MaNV),

    CONSTRAINT CK_PTH_TrangThai
    CHECK (
        TrangThai IN (
            N'Chờ xử lý',
            N'Đã duyệt',
            N'Từ chối',
            N'Đã nhận hàng',
            N'Đã hoàn tiền',
            N'Hoàn tất'
        )
    ),

    CONSTRAINT CK_PTH_Tien
    CHECK (TongTienHoan >= 0)
);
GO


/* ============================================================
   26. CHI TIET TRA HANG
   ============================================================ */

CREATE TABLE ChiTietTraHang (
    MaCTTra INT IDENTITY(1,1) PRIMARY KEY,

    MaPhieuTra INT NOT NULL,

    MaCTDH INT NOT NULL,

    SoLuongTra INT NOT NULL,

    LyDoChiTiet NVARCHAR(500),

    TinhTrangSanPham NVARCHAR(255),

    HinhAnh NVARCHAR(500),

    SoTienHoan DECIMAL(18,2)
        NOT NULL DEFAULT 0,

    CONSTRAINT FK_CTTH_PTH
    FOREIGN KEY (MaPhieuTra)
    REFERENCES PhieuTraHang(MaPhieuTra)
    ON DELETE CASCADE,

    CONSTRAINT FK_CTTH_CTDH
    FOREIGN KEY (MaCTDH)
    REFERENCES ChiTietDonHang(MaCTDH),

    CONSTRAINT CK_CTTH_SL
    CHECK (SoLuongTra > 0),

    CONSTRAINT CK_CTTH_Tien
    CHECK (SoTienHoan >= 0),

    CONSTRAINT UQ_CTTH
    UNIQUE (MaPhieuTra, MaCTDH)
);
GO


/* ============================================================
   27. DANH GIA
   ============================================================ */

CREATE TABLE DanhGia (
    MaDanhGia INT IDENTITY(1,1) PRIMARY KEY,

    MaKH INT NOT NULL,

    MaSP INT NOT NULL,

    SoSao INT NOT NULL,

    NoiDung NVARCHAR(1000),

    NgayDanhGia DATETIME2
        NOT NULL DEFAULT SYSDATETIME(),

    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_DG_KH
    FOREIGN KEY (MaKH)
    REFERENCES KhachHang(MaKH),

    CONSTRAINT FK_DG_SP
    FOREIGN KEY (MaSP)
    REFERENCES SanPham(MaSP),

    CONSTRAINT CK_DG_Sao
    CHECK (SoSao BETWEEN 1 AND 5),

    CONSTRAINT UQ_DanhGia
    UNIQUE (MaKH, MaSP)
);
GO


/* ============================================================
   INDEX
   ============================================================ */

CREATE INDEX IX_SanPham_TenSP
ON SanPham(TenSP);

CREATE INDEX IX_SanPham_DanhMuc
ON SanPham(MaDanhMuc);

CREATE INDEX IX_SanPham_ThuongHieu
ON SanPham(MaThuongHieu);

CREATE INDEX IX_BienThe_MaSP
ON BienTheSanPham(MaSP);

CREATE INDEX IX_DonHang_MaKH
ON DonHang(MaKH);

CREATE INDEX IX_DonHang_NgayDat
ON DonHang(NgayDat);

CREATE INDEX IX_DonHang_TrangThai
ON DonHang(TrangThai);

CREATE INDEX IX_LSTT_MaDonHang
ON LichSuTrangThaiDonHang(MaDonHang);

CREATE INDEX IX_PhieuTra_MaDonHang
ON PhieuTraHang(MaDonHang);
GO


/* ============================================================
   ============================================================
                    DỮ LIỆU MẪU FULL
   ============================================================
   ============================================================ */


/* ============================================================
   KHACH HANG
   ============================================================ */


CREATE UNIQUE INDEX UX_KhachHang_Phone ON KhachHang(SoDienThoai) WHERE SoDienThoai IS NOT NULL;
CREATE UNIQUE INDEX UX_NhanVien_Phone ON NhanVien(SoDienThoai) WHERE SoDienThoai IS NOT NULL;
GO

COMMIT;
GO
