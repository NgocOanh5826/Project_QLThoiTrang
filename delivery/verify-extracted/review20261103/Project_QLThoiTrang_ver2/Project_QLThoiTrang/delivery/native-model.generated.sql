CREATE TABLE [DanhMuc] (
    [MaDanhMuc] int NOT NULL IDENTITY,
    [TenDanhMuc] nvarchar(100) NOT NULL,
    [MoTa] nvarchar(500) NULL,
    [TrangThai] bit NOT NULL DEFAULT (1),
    CONSTRAINT [PK_DanhMuc] PRIMARY KEY ([MaDanhMuc])
);
GO


CREATE TABLE [KhachHang] (
    [MaKH] int NOT NULL IDENTITY,
    [HoTen] nvarchar(100) NOT NULL,
    [Email] varchar(150) NOT NULL,
    [MatKhau] varchar(255) NOT NULL,
    [SoDienThoai] varchar(15) NULL,
    [GioiTinh] nvarchar(10) NULL,
    [NgaySinh] date NULL,
    [DiaChi] nvarchar(255) NULL,
    [NgayDangKy] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [TrangThai] bit NOT NULL DEFAULT (1),
    [QLegacyId] int NULL,
    CONSTRAINT [PK_KhachHang] PRIMARY KEY ([MaKH]),
    CONSTRAINT [CK_KhachHang_GioiTinh] CHECK (GioiTinh IS NULL
        OR GioiTinh IN (N'Nam', N'Nữ', N'Khác'))
);
GO


CREATE TABLE [Kho] (
    [MaKho] int NOT NULL IDENTITY,
    [TenKho] nvarchar(100) NOT NULL,
    [DiaChi] nvarchar(255) NULL,
    [TrangThai] bit NOT NULL DEFAULT (1),
    CONSTRAINT [PK_Kho] PRIMARY KEY ([MaKho])
);
GO


CREATE TABLE [KhuyenMai] (
    [MaKM] int NOT NULL IDENTITY,
    [MaCode] varchar(50) NULL,
    [TenKhuyenMai] nvarchar(150) NOT NULL,
    [LoaiGiam] varchar(20) NOT NULL,
    [GiaTriGiam] decimal(18,2) NOT NULL,
    [DonHangToiThieu] decimal(18,2) NOT NULL DEFAULT (0),
    [GiamToiDa] decimal(18,2) NULL,
    [SoLuong] int NULL,
    [NgayBatDau] datetime2 NOT NULL,
    [NgayKetThuc] datetime2 NOT NULL,
    [TrangThai] bit NOT NULL DEFAULT (1),
    CONSTRAINT [PK_KhuyenMai] PRIMARY KEY ([MaKM]),
    CONSTRAINT [CK_KM_GiaTri] CHECK (GiaTriGiam > 0),
    CONSTRAINT [CK_KM_Loai] CHECK (LoaiGiam IN ('PHANTRAM', 'SOTIEN')),
    CONSTRAINT [CK_KM_Ngay] CHECK (NgayKetThuc >= NgayBatDau),
    CONSTRAINT [CK_KM_SL] CHECK (SoLuong IS NULL
        OR SoLuong >= 0)
);
GO


CREATE TABLE [KichThuoc] (
    [MaKichThuoc] int NOT NULL IDENTITY,
    [TenKichThuoc] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_KichThuoc] PRIMARY KEY ([MaKichThuoc])
);
GO


CREATE TABLE [MauSac] (
    [MaMauSac] int NOT NULL IDENTITY,
    [TenMauSac] nvarchar(50) NOT NULL,
    [MaMau] varchar(20) NULL,
    CONSTRAINT [PK_MauSac] PRIMARY KEY ([MaMauSac])
);
GO


CREATE TABLE [NhaCungCap] (
    [MaNCC] int NOT NULL IDENTITY,
    [TenNCC] nvarchar(150) NOT NULL,
    [SoDienThoai] varchar(15) NULL,
    [Email] varchar(150) NULL,
    [DiaChi] nvarchar(255) NULL,
    [TrangThai] bit NOT NULL DEFAULT (1),
    CONSTRAINT [PK_NhaCungCap] PRIMARY KEY ([MaNCC])
);
GO


CREATE TABLE [NhanVien] (
    [MaNV] int NOT NULL IDENTITY,
    [HoTen] nvarchar(100) NOT NULL,
    [Email] varchar(150) NOT NULL,
    [MatKhau] varchar(255) NOT NULL,
    [SoDienThoai] varchar(15) NULL,
    [GioiTinh] nvarchar(10) NULL,
    [NgaySinh] date NULL,
    [DiaChi] nvarchar(255) NULL,
    [ChucVu] nvarchar(50) NOT NULL,
    [NgayVaoLam] date NOT NULL DEFAULT (GETDATE()),
    [TrangThai] bit NOT NULL DEFAULT (1),
    [QLegacyId] int NULL,
    CONSTRAINT [PK_NhanVien] PRIMARY KEY ([MaNV]),
    CONSTRAINT [CK_NhanVien_GioiTinh] CHECK (GioiTinh IS NULL
        OR GioiTinh IN (N'Nam', N'Nữ', N'Khác'))
);
GO


CREATE TABLE [QOANH_Configuration] (
    [Id] int NOT NULL,
    [Settings_Name] nvarchar(80) NOT NULL,
    [Settings_Email] nvarchar(max) NOT NULL,
    [Settings_Phone] nvarchar(30) NOT NULL,
    [Settings_Address] nvarchar(250) NOT NULL,
    [ImportedFileHash] nvarchar(max) NULL,
    CONSTRAINT [PK_QOANH_Configuration] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [QOANH_PaymentLedger] (
    [Id] int NOT NULL,
    [OrderId] nvarchar(max) NOT NULL,
    [Kind] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Reference] nvarchar(100) NOT NULL,
    [ActorId] int NOT NULL,
    [Created] datetime2 NOT NULL,
    CONSTRAINT [PK_QOANH_PaymentLedger] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_QPayment] CHECK ([Amount]>0 AND [Kind] IN ('Receipt','Refund'))
);
GO


CREATE TABLE [ThuongHieu] (
    [MaThuongHieu] int NOT NULL IDENTITY,
    [TenThuongHieu] nvarchar(100) NOT NULL,
    [MoTa] nvarchar(500) NULL,
    [TrangThai] bit NOT NULL DEFAULT (1),
    CONSTRAINT [PK_ThuongHieu] PRIMARY KEY ([MaThuongHieu])
);
GO


CREATE TABLE [GioHang] (
    [MaGioHang] int NOT NULL IDENTITY,
    [MaKH] int NOT NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_GioHang] PRIMARY KEY ([MaGioHang]),
    CONSTRAINT [FK_GioHang_KhachHang_MaKH] FOREIGN KEY ([MaKH]) REFERENCES [KhachHang] ([MaKH]) ON DELETE NO ACTION
);
GO


CREATE TABLE [DonHang] (
    [MaDonHang] int NOT NULL IDENTITY,
    [MaKH] int NOT NULL,
    [MaKM] int NULL,
    [NgayDat] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [HoTenNguoiNhan] nvarchar(100) NOT NULL,
    [SoDienThoaiNhan] varchar(15) NOT NULL,
    [DiaChiGiaoHang] nvarchar(500) NOT NULL,
    [TongTienHang] decimal(18,2) NOT NULL DEFAULT (0),
    [TienGiam] decimal(18,2) NOT NULL DEFAULT (0),
    [PhiVanChuyen] decimal(18,2) NOT NULL DEFAULT (0),
    [TongThanhToan] decimal(18,2) NOT NULL DEFAULT (0),
    [TrangThai] nvarchar(30) NOT NULL DEFAULT (N'Chờ xác nhận'),
    [GhiChu] nvarchar(500) NULL,
    [QPublicId] nvarchar(500) NULL,
    [QCheckoutToken] nvarchar(64) NULL,
    [QLegacyId] nvarchar(500) NULL,
    CONSTRAINT [PK_DonHang] PRIMARY KEY ([MaDonHang]),
    CONSTRAINT [CK_DH_Tien] CHECK (TongTienHang >= 0
        AND TienGiam >= 0
        AND PhiVanChuyen >= 0
        AND TongThanhToan >= 0),
    CONSTRAINT [CK_DH_TrangThai] CHECK (TrangThai IN (
            N'Chờ xác nhận',
            N'Đã xác nhận',
            N'Đang chuẩn bị',
            N'Đang giao',
            N'Đã giao',
            N'Hoàn thành',
            N'Đã hủy'
        )),
    CONSTRAINT [FK_DonHang_KhachHang_MaKH] FOREIGN KEY ([MaKH]) REFERENCES [KhachHang] ([MaKH]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DonHang_KhuyenMai_MaKM] FOREIGN KEY ([MaKM]) REFERENCES [KhuyenMai] ([MaKM]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LichSuDangNhap] (
    [MaLichSu] bigint NOT NULL IDENTITY,
    [MaKH] int NULL,
    [MaNV] int NULL,
    [ThoiGianDangNhap] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [ThoiGianDangXuat] datetime2 NULL,
    [DiaChiIP] varchar(45) NULL,
    [ThietBi] nvarchar(255) NULL,
    [TrangThai] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_LichSuDangNhap] PRIMARY KEY ([MaLichSu]),
    CONSTRAINT [CK_LSDN_NguoiDung] CHECK ((MaKH IS NOT NULL AND MaNV IS NULL)
        OR
        (MaKH IS NULL AND MaNV IS NOT NULL)),
    CONSTRAINT [CK_LSDN_TrangThai] CHECK (TrangThai IN (N'Thành công', N'Thất bại')),
    CONSTRAINT [FK_LichSuDangNhap_KhachHang_MaKH] FOREIGN KEY ([MaKH]) REFERENCES [KhachHang] ([MaKH]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LichSuDangNhap_NhanVien_MaNV] FOREIGN KEY ([MaNV]) REFERENCES [NhanVien] ([MaNV]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PhieuNhap] (
    [MaPhieuNhap] int NOT NULL IDENTITY,
    [MaNCC] int NOT NULL,
    [MaKho] int NOT NULL,
    [MaNV] int NOT NULL,
    [NgayNhap] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [TongTien] decimal(18,2) NOT NULL DEFAULT (0),
    [GhiChu] nvarchar(500) NULL,
    [QReference] nvarchar(500) NULL,
    CONSTRAINT [PK_PhieuNhap] PRIMARY KEY ([MaPhieuNhap]),
    CONSTRAINT [CK_PN_TongTien] CHECK (TongTien >= 0),
    CONSTRAINT [FK_PhieuNhap_Kho_MaKho] FOREIGN KEY ([MaKho]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhieuNhap_NhaCungCap_MaNCC] FOREIGN KEY ([MaNCC]) REFERENCES [NhaCungCap] ([MaNCC]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhieuNhap_NhanVien_MaNV] FOREIGN KEY ([MaNV]) REFERENCES [NhanVien] ([MaNV]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SanPham] (
    [MaSP] int NOT NULL IDENTITY,
    [MaDanhMuc] int NOT NULL,
    [MaThuongHieu] int NULL,
    [TenSP] nvarchar(200) NOT NULL,
    [MoTa] nvarchar(max) NULL,
    [GiaBan] decimal(18,2) NOT NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [TrangThai] bit NOT NULL DEFAULT (1),
    [QImage] nvarchar(500) NULL,
    [QIsNew] bit NOT NULL,
    [QOnSale] bit NOT NULL,
    [QLegacyId] int NULL,
    CONSTRAINT [PK_SanPham] PRIMARY KEY ([MaSP]),
    CONSTRAINT [CK_SP_GiaBan] CHECK (GiaBan >= 0),
    CONSTRAINT [FK_SanPham_DanhMuc_MaDanhMuc] FOREIGN KEY ([MaDanhMuc]) REFERENCES [DanhMuc] ([MaDanhMuc]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SanPham_ThuongHieu_MaThuongHieu] FOREIGN KEY ([MaThuongHieu]) REFERENCES [ThuongHieu] ([MaThuongHieu]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LichSuTrangThaiDonHang] (
    [MaLichSuTrangThai] bigint NOT NULL IDENTITY,
    [MaDonHang] int NOT NULL,
    [TrangThai] nvarchar(30) NOT NULL,
    [ThoiGian] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [GhiChu] nvarchar(500) NULL,
    CONSTRAINT [PK_LichSuTrangThaiDonHang] PRIMARY KEY ([MaLichSuTrangThai]),
    CONSTRAINT [CK_LSTT_TrangThai] CHECK (TrangThai IN (
            N'Chờ xác nhận',
            N'Đã xác nhận',
            N'Đang chuẩn bị',
            N'Đang giao',
            N'Đã giao',
            N'Hoàn thành',
            N'Đã hủy'
        )),
    CONSTRAINT [FK_LichSuTrangThaiDonHang_DonHang_MaDonHang] FOREIGN KEY ([MaDonHang]) REFERENCES [DonHang] ([MaDonHang]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PhieuTraHang] (
    [MaPhieuTra] int NOT NULL IDENTITY,
    [MaDonHang] int NOT NULL,
    [MaKH] int NOT NULL,
    [MaNV] int NULL,
    [NgayYeuCau] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [LyDo] nvarchar(500) NOT NULL,
    [TrangThai] nvarchar(30) NOT NULL DEFAULT (N'Chờ xử lý'),
    [TongTienHoan] decimal(18,2) NOT NULL DEFAULT (0),
    [NgayXuLy] datetime2 NULL,
    [GhiChu] nvarchar(500) NULL,
    [QReference] nvarchar(500) NULL,
    CONSTRAINT [PK_PhieuTraHang] PRIMARY KEY ([MaPhieuTra]),
    CONSTRAINT [CK_PTH_Tien] CHECK (TongTienHoan >= 0),
    CONSTRAINT [CK_PTH_TrangThai] CHECK (TrangThai IN (
            N'Chờ xử lý',
            N'Đã duyệt',
            N'Từ chối',
            N'Đã nhận hàng',
            N'Đã hoàn tiền',
            N'Hoàn tất'
        )),
    CONSTRAINT [FK_PhieuTraHang_DonHang_MaDonHang] FOREIGN KEY ([MaDonHang]) REFERENCES [DonHang] ([MaDonHang]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhieuTraHang_KhachHang_MaKH] FOREIGN KEY ([MaKH]) REFERENCES [KhachHang] ([MaKH]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PhieuTraHang_NhanVien_MaNV] FOREIGN KEY ([MaNV]) REFERENCES [NhanVien] ([MaNV]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ThanhToan] (
    [MaThanhToan] int NOT NULL IDENTITY,
    [MaDonHang] int NOT NULL,
    [PhuongThuc] nvarchar(30) NOT NULL,
    [SoTien] decimal(18,2) NOT NULL,
    [TrangThai] nvarchar(30) NOT NULL DEFAULT (N'Chưa thanh toán'),
    [MaGiaoDich] varchar(100) NULL,
    [NgayThanhToan] datetime2 NULL,
    CONSTRAINT [PK_ThanhToan] PRIMARY KEY ([MaThanhToan]),
    CONSTRAINT [CK_TT_PhuongThuc] CHECK (PhuongThuc IN (
            N'COD',
            N'Chuyển khoản',
            N'Ví điện tử'
        )),
    CONSTRAINT [CK_TT_SoTien] CHECK (SoTien >= 0),
    CONSTRAINT [CK_TT_TrangThai] CHECK (TrangThai IN (
            N'Chưa thanh toán',
            N'Đã thanh toán',
            N'Thất bại',
            N'Đã hoàn tiền'
        )),
    CONSTRAINT [FK_ThanhToan_DonHang_MaDonHang] FOREIGN KEY ([MaDonHang]) REFERENCES [DonHang] ([MaDonHang]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BienTheSanPham] (
    [MaBienThe] int NOT NULL IDENTITY,
    [MaSP] int NOT NULL,
    [MaKichThuoc] int NOT NULL,
    [MaMauSac] int NOT NULL,
    [SKU] varchar(50) NOT NULL,
    [GiaBanRieng] decimal(18,2) NULL,
    [TrangThai] bit NOT NULL DEFAULT (1),
    CONSTRAINT [PK_BienTheSanPham] PRIMARY KEY ([MaBienThe]),
    CONSTRAINT [CK_BT_Gia] CHECK (GiaBanRieng IS NULL
        OR GiaBanRieng >= 0),
    CONSTRAINT [FK_BienTheSanPham_KichThuoc_MaKichThuoc] FOREIGN KEY ([MaKichThuoc]) REFERENCES [KichThuoc] ([MaKichThuoc]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BienTheSanPham_MauSac_MaMauSac] FOREIGN KEY ([MaMauSac]) REFERENCES [MauSac] ([MaMauSac]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BienTheSanPham_SanPham_MaSP] FOREIGN KEY ([MaSP]) REFERENCES [SanPham] ([MaSP]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ChiTietKhuyenMai] (
    [MaKM] int NOT NULL,
    [MaSP] int NOT NULL,
    CONSTRAINT [PK_ChiTietKhuyenMai] PRIMARY KEY ([MaKM], [MaSP]),
    CONSTRAINT [FK_ChiTietKhuyenMai_KhuyenMai_MaKM] FOREIGN KEY ([MaKM]) REFERENCES [KhuyenMai] ([MaKM]) ON DELETE CASCADE,
    CONSTRAINT [FK_ChiTietKhuyenMai_SanPham_MaSP] FOREIGN KEY ([MaSP]) REFERENCES [SanPham] ([MaSP]) ON DELETE NO ACTION
);
GO


CREATE TABLE [DanhGia] (
    [MaDanhGia] int NOT NULL IDENTITY,
    [MaKH] int NOT NULL,
    [MaSP] int NOT NULL,
    [SoSao] int NOT NULL,
    [NoiDung] nvarchar(1000) NULL,
    [NgayDanhGia] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    [TrangThai] bit NOT NULL DEFAULT (1),
    CONSTRAINT [PK_DanhGia] PRIMARY KEY ([MaDanhGia]),
    CONSTRAINT [CK_DG_Sao] CHECK (SoSao BETWEEN 1 AND 5),
    CONSTRAINT [FK_DanhGia_KhachHang_MaKH] FOREIGN KEY ([MaKH]) REFERENCES [KhachHang] ([MaKH]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DanhGia_SanPham_MaSP] FOREIGN KEY ([MaSP]) REFERENCES [SanPham] ([MaSP]) ON DELETE NO ACTION
);
GO


CREATE TABLE [HinhAnhSanPham] (
    [MaHinh] int NOT NULL IDENTITY,
    [MaSP] int NOT NULL,
    [DuongDan] nvarchar(500) NOT NULL,
    [AnhChinh] bit NOT NULL DEFAULT (0),
    CONSTRAINT [PK_HinhAnhSanPham] PRIMARY KEY ([MaHinh]),
    CONSTRAINT [FK_HinhAnhSanPham_SanPham_MaSP] FOREIGN KEY ([MaSP]) REFERENCES [SanPham] ([MaSP]) ON DELETE CASCADE
);
GO


CREATE TABLE [QOANH_StockMovements] (
    [Id] int NOT NULL,
    [VariantId] int NULL,
    [WarehouseId] int NULL,
    [SupplierId] int NULL,
    [ReceiptReference] nvarchar(max) NULL,
    [ProductId] int NOT NULL,
    [Before] int NOT NULL,
    [Delta] int NOT NULL,
    [After] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Reference] nvarchar(100) NOT NULL,
    [Supplier] nvarchar(max) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [ActorId] int NULL,
    [Created] datetime2 NOT NULL,
    CONSTRAINT [PK_QOANH_StockMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_QStockBalance] CHECK ([Before]>=0 AND [After]>=0 AND [After]=[Before]+[Delta]),
    CONSTRAINT [FK_QOANH_StockMovements_SanPham_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [SanPham] ([MaSP]) ON DELETE NO ACTION
);
GO


CREATE TABLE [YeuThich] (
    [MaKH] int NOT NULL,
    [MaSP] int NOT NULL,
    [NgayThem] datetime2 NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_YeuThich] PRIMARY KEY ([MaKH], [MaSP]),
    CONSTRAINT [FK_YeuThich_KhachHang_MaKH] FOREIGN KEY ([MaKH]) REFERENCES [KhachHang] ([MaKH]) ON DELETE NO ACTION,
    CONSTRAINT [FK_YeuThich_SanPham_MaSP] FOREIGN KEY ([MaSP]) REFERENCES [SanPham] ([MaSP]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ChiTietDonHang] (
    [MaCTDH] int NOT NULL IDENTITY,
    [MaDonHang] int NOT NULL,
    [MaBienThe] int NOT NULL,
    [TenSanPham] nvarchar(200) NOT NULL,
    [KichThuoc] nvarchar(30) NULL,
    [MauSac] nvarchar(50) NULL,
    [SoLuong] int NOT NULL,
    [DonGia] decimal(18,2) NOT NULL,
    [QImage] nvarchar(500) NULL,
    [QWarehouseId] int NULL,
    CONSTRAINT [PK_ChiTietDonHang] PRIMARY KEY ([MaCTDH]),
    CONSTRAINT [CK_CTDH_Gia] CHECK (DonGia >= 0),
    CONSTRAINT [CK_CTDH_SL] CHECK (SoLuong > 0),
    CONSTRAINT [FK_ChiTietDonHang_BienTheSanPham_MaBienThe] FOREIGN KEY ([MaBienThe]) REFERENCES [BienTheSanPham] ([MaBienThe]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChiTietDonHang_DonHang_MaDonHang] FOREIGN KEY ([MaDonHang]) REFERENCES [DonHang] ([MaDonHang]) ON DELETE CASCADE
);
GO


CREATE TABLE [ChiTietGioHang] (
    [MaGioHang] int NOT NULL,
    [MaBienThe] int NOT NULL,
    [SoLuong] int NOT NULL,
    CONSTRAINT [PK_ChiTietGioHang] PRIMARY KEY ([MaGioHang], [MaBienThe]),
    CONSTRAINT [CK_CTGH_SL] CHECK (SoLuong > 0),
    CONSTRAINT [FK_ChiTietGioHang_BienTheSanPham_MaBienThe] FOREIGN KEY ([MaBienThe]) REFERENCES [BienTheSanPham] ([MaBienThe]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChiTietGioHang_GioHang_MaGioHang] FOREIGN KEY ([MaGioHang]) REFERENCES [GioHang] ([MaGioHang]) ON DELETE CASCADE
);
GO


CREATE TABLE [ChiTietPhieuNhap] (
    [MaPhieuNhap] int NOT NULL,
    [MaBienThe] int NOT NULL,
    [SoLuong] int NOT NULL,
    [GiaNhap] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_ChiTietPhieuNhap] PRIMARY KEY ([MaPhieuNhap], [MaBienThe]),
    CONSTRAINT [CK_CTPN_Gia] CHECK (GiaNhap >= 0),
    CONSTRAINT [CK_CTPN_SL] CHECK (SoLuong > 0),
    CONSTRAINT [FK_ChiTietPhieuNhap_BienTheSanPham_MaBienThe] FOREIGN KEY ([MaBienThe]) REFERENCES [BienTheSanPham] ([MaBienThe]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChiTietPhieuNhap_PhieuNhap_MaPhieuNhap] FOREIGN KEY ([MaPhieuNhap]) REFERENCES [PhieuNhap] ([MaPhieuNhap]) ON DELETE CASCADE
);
GO


CREATE TABLE [TonKho] (
    [MaKho] int NOT NULL,
    [MaBienThe] int NOT NULL,
    [SoLuongTon] int NOT NULL DEFAULT (0),
    CONSTRAINT [PK_TonKho] PRIMARY KEY ([MaKho], [MaBienThe]),
    CONSTRAINT [CK_TonKho_SoLuong] CHECK (SoLuongTon >= 0),
    CONSTRAINT [FK_TonKho_BienTheSanPham_MaBienThe] FOREIGN KEY ([MaBienThe]) REFERENCES [BienTheSanPham] ([MaBienThe]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TonKho_Kho_MaKho] FOREIGN KEY ([MaKho]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ChiTietTraHang] (
    [MaCTTra] int NOT NULL IDENTITY,
    [MaPhieuTra] int NOT NULL,
    [MaCTDH] int NOT NULL,
    [SoLuongTra] int NOT NULL,
    [LyDoChiTiet] nvarchar(500) NULL,
    [TinhTrangSanPham] nvarchar(255) NULL,
    [HinhAnh] nvarchar(500) NULL,
    [SoTienHoan] decimal(18,2) NOT NULL DEFAULT (0),
    CONSTRAINT [PK_ChiTietTraHang] PRIMARY KEY ([MaCTTra]),
    CONSTRAINT [CK_CTTH_SL] CHECK (SoLuongTra > 0),
    CONSTRAINT [CK_CTTH_Tien] CHECK (SoTienHoan >= 0),
    CONSTRAINT [FK_ChiTietTraHang_ChiTietDonHang_MaCTDH] FOREIGN KEY ([MaCTDH]) REFERENCES [ChiTietDonHang] ([MaCTDH]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChiTietTraHang_PhieuTraHang_MaPhieuTra] FOREIGN KEY ([MaPhieuTra]) REFERENCES [PhieuTraHang] ([MaPhieuTra]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_BienTheSanPham_MaKichThuoc] ON [BienTheSanPham] ([MaKichThuoc]);
GO


CREATE INDEX [IX_BienTheSanPham_MaMauSac] ON [BienTheSanPham] ([MaMauSac]);
GO


CREATE UNIQUE INDEX [IX_BienTheSanPham_MaSP_MaKichThuoc_MaMauSac] ON [BienTheSanPham] ([MaSP], [MaKichThuoc], [MaMauSac]);
GO


CREATE UNIQUE INDEX [IX_BienTheSanPham_SKU] ON [BienTheSanPham] ([SKU]);
GO


CREATE INDEX [IX_ChiTietDonHang_MaBienThe] ON [ChiTietDonHang] ([MaBienThe]);
GO


CREATE INDEX [IX_ChiTietDonHang_MaDonHang] ON [ChiTietDonHang] ([MaDonHang]);
GO


CREATE INDEX [IX_ChiTietGioHang_MaBienThe] ON [ChiTietGioHang] ([MaBienThe]);
GO


CREATE INDEX [IX_ChiTietKhuyenMai_MaSP] ON [ChiTietKhuyenMai] ([MaSP]);
GO


CREATE INDEX [IX_ChiTietPhieuNhap_MaBienThe] ON [ChiTietPhieuNhap] ([MaBienThe]);
GO


CREATE INDEX [IX_ChiTietTraHang_MaCTDH] ON [ChiTietTraHang] ([MaCTDH]);
GO


CREATE UNIQUE INDEX [IX_ChiTietTraHang_MaPhieuTra_MaCTDH] ON [ChiTietTraHang] ([MaPhieuTra], [MaCTDH]);
GO


CREATE UNIQUE INDEX [IX_DanhGia_MaKH_MaSP] ON [DanhGia] ([MaKH], [MaSP]);
GO


CREATE INDEX [IX_DanhGia_MaSP] ON [DanhGia] ([MaSP]);
GO


CREATE UNIQUE INDEX [IX_DanhMuc_TenDanhMuc] ON [DanhMuc] ([TenDanhMuc]);
GO


CREATE INDEX [IX_DonHang_MaKH] ON [DonHang] ([MaKH]);
GO


CREATE INDEX [IX_DonHang_MaKM] ON [DonHang] ([MaKM]);
GO


CREATE UNIQUE INDEX [IX_DonHang_QCheckoutToken] ON [DonHang] ([QCheckoutToken]) WHERE [QCheckoutToken] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_DonHang_QPublicId] ON [DonHang] ([QPublicId]) WHERE [QPublicId] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_GioHang_MaKH] ON [GioHang] ([MaKH]);
GO


CREATE INDEX [IX_HinhAnhSanPham_MaSP] ON [HinhAnhSanPham] ([MaSP]);
GO


CREATE UNIQUE INDEX [IX_KhachHang_Email] ON [KhachHang] ([Email]);
GO


CREATE UNIQUE INDEX [IX_KhachHang_SoDienThoai] ON [KhachHang] ([SoDienThoai]) WHERE [SoDienThoai] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_KhuyenMai_MaCode] ON [KhuyenMai] ([MaCode]) WHERE [MaCode] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_KichThuoc_TenKichThuoc] ON [KichThuoc] ([TenKichThuoc]);
GO


CREATE INDEX [IX_LichSuDangNhap_MaKH] ON [LichSuDangNhap] ([MaKH]);
GO


CREATE INDEX [IX_LichSuDangNhap_MaNV] ON [LichSuDangNhap] ([MaNV]);
GO


CREATE INDEX [IX_LichSuTrangThaiDonHang_MaDonHang] ON [LichSuTrangThaiDonHang] ([MaDonHang]);
GO


CREATE UNIQUE INDEX [IX_MauSac_TenMauSac] ON [MauSac] ([TenMauSac]);
GO


CREATE UNIQUE INDEX [IX_NhanVien_Email] ON [NhanVien] ([Email]);
GO


CREATE UNIQUE INDEX [IX_NhanVien_SoDienThoai] ON [NhanVien] ([SoDienThoai]) WHERE [SoDienThoai] IS NOT NULL;
GO


CREATE INDEX [IX_PhieuNhap_MaKho] ON [PhieuNhap] ([MaKho]);
GO


CREATE INDEX [IX_PhieuNhap_MaNCC] ON [PhieuNhap] ([MaNCC]);
GO


CREATE INDEX [IX_PhieuNhap_MaNV] ON [PhieuNhap] ([MaNV]);
GO


CREATE UNIQUE INDEX [IX_PhieuNhap_QReference] ON [PhieuNhap] ([QReference]) WHERE [QReference] IS NOT NULL;
GO


CREATE INDEX [IX_PhieuTraHang_MaDonHang] ON [PhieuTraHang] ([MaDonHang]);
GO


CREATE INDEX [IX_PhieuTraHang_MaKH] ON [PhieuTraHang] ([MaKH]);
GO


CREATE INDEX [IX_PhieuTraHang_MaNV] ON [PhieuTraHang] ([MaNV]);
GO


CREATE UNIQUE INDEX [IX_QOANH_PaymentLedger_Reference] ON [QOANH_PaymentLedger] ([Reference]);
GO


CREATE INDEX [IX_QOANH_StockMovements_ProductId] ON [QOANH_StockMovements] ([ProductId]);
GO


CREATE UNIQUE INDEX [IX_QOANH_StockMovements_Reference] ON [QOANH_StockMovements] ([Reference]);
GO


CREATE INDEX [IX_SanPham_MaDanhMuc] ON [SanPham] ([MaDanhMuc]);
GO


CREATE INDEX [IX_SanPham_MaThuongHieu] ON [SanPham] ([MaThuongHieu]);
GO


CREATE INDEX [IX_ThanhToan_MaDonHang] ON [ThanhToan] ([MaDonHang]);
GO


CREATE UNIQUE INDEX [IX_ThanhToan_MaGiaoDich] ON [ThanhToan] ([MaGiaoDich]) WHERE [MaGiaoDich] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_ThuongHieu_TenThuongHieu] ON [ThuongHieu] ([TenThuongHieu]);
GO


CREATE INDEX [IX_TonKho_MaBienThe] ON [TonKho] ([MaBienThe]);
GO


CREATE INDEX [IX_YeuThich_MaSP] ON [YeuThich] ([MaSP]);
GO


