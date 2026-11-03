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
BEGIN TRANSACTION;
GO
IF COL_LENGTH(N'dbo.KhachHang',N'QLegacyId') IS NULL ALTER TABLE [KhachHang] ADD [QLegacyId] int NULL;
GO
IF COL_LENGTH(N'dbo.NhanVien',N'QLegacyId') IS NULL ALTER TABLE [NhanVien] ADD [QLegacyId] int NULL;
GO
IF OBJECT_ID(N'dbo.QOANH_Configuration',N'U') IS NULL
BEGIN
CREATE TABLE [QOANH_Configuration] (
    [Id] int NOT NULL,
    [Settings_Name] nvarchar(80) NOT NULL,
    [Settings_Email] nvarchar(max) NOT NULL,
    [Settings_Phone] nvarchar(30) NOT NULL,
    [Settings_Address] nvarchar(250) NOT NULL,
    [ImportedFileHash] nvarchar(max) NULL,
    CONSTRAINT [PK_QOANH_Configuration] PRIMARY KEY ([Id])
);

END;
GO
IF OBJECT_ID(N'dbo.QOANH_PaymentLedger',N'U') IS NULL
BEGIN
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

END;
GO
IF COL_LENGTH(N'dbo.DonHang',N'QPublicId') IS NULL ALTER TABLE [DonHang] ADD [QPublicId] nvarchar(500) NULL;
GO
IF COL_LENGTH(N'dbo.DonHang',N'QCheckoutToken') IS NULL ALTER TABLE [DonHang] ADD [QCheckoutToken] nvarchar(64) NULL;
GO
IF COL_LENGTH(N'dbo.DonHang',N'QLegacyId') IS NULL ALTER TABLE [DonHang] ADD [QLegacyId] nvarchar(500) NULL;
GO
IF COL_LENGTH(N'dbo.PhieuNhap',N'QReference') IS NULL ALTER TABLE [PhieuNhap] ADD [QReference] nvarchar(500) NULL;
GO
IF COL_LENGTH(N'dbo.SanPham',N'QImage') IS NULL ALTER TABLE [SanPham] ADD [QImage] nvarchar(500) NULL;
GO
IF COL_LENGTH(N'dbo.SanPham',N'QIsNew') IS NULL ALTER TABLE [SanPham] ADD [QIsNew] bit NOT NULL DEFAULT 0 WITH VALUES;
GO
IF COL_LENGTH(N'dbo.SanPham',N'QOnSale') IS NULL ALTER TABLE [SanPham] ADD [QOnSale] bit NOT NULL DEFAULT 0 WITH VALUES;
GO
IF COL_LENGTH(N'dbo.SanPham',N'QLegacyId') IS NULL ALTER TABLE [SanPham] ADD [QLegacyId] int NULL;
GO
IF COL_LENGTH(N'dbo.PhieuTraHang',N'QReference') IS NULL ALTER TABLE [PhieuTraHang] ADD [QReference] nvarchar(500) NULL;
GO
IF OBJECT_ID(N'dbo.QOANH_StockMovements',N'U') IS NULL
BEGIN
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

END;
GO
IF COL_LENGTH(N'dbo.ChiTietDonHang',N'QImage') IS NULL ALTER TABLE [ChiTietDonHang] ADD [QImage] nvarchar(500) NULL;
GO
IF COL_LENGTH(N'dbo.ChiTietDonHang',N'QWarehouseId') IS NULL ALTER TABLE [ChiTietDonHang] ADD [QWarehouseId] int NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_DonHang_QCheckoutToken' AND object_id=OBJECT_ID(N'dbo.DonHang'))
CREATE UNIQUE INDEX [IX_DonHang_QCheckoutToken] ON [DonHang] ([QCheckoutToken]) WHERE [QCheckoutToken] IS NOT NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_DonHang_QPublicId' AND object_id=OBJECT_ID(N'dbo.DonHang'))
CREATE UNIQUE INDEX [IX_DonHang_QPublicId] ON [DonHang] ([QPublicId]) WHERE [QPublicId] IS NOT NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_PhieuNhap_QReference' AND object_id=OBJECT_ID(N'dbo.PhieuNhap'))
CREATE UNIQUE INDEX [IX_PhieuNhap_QReference] ON [PhieuNhap] ([QReference]) WHERE [QReference] IS NOT NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_QOANH_PaymentLedger_Reference' AND object_id=OBJECT_ID(N'dbo.QOANH_PaymentLedger'))
CREATE UNIQUE INDEX [IX_QOANH_PaymentLedger_Reference] ON [QOANH_PaymentLedger] ([Reference]);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_QOANH_StockMovements_ProductId' AND object_id=OBJECT_ID(N'dbo.QOANH_StockMovements'))
CREATE INDEX [IX_QOANH_StockMovements_ProductId] ON [QOANH_StockMovements] ([ProductId]);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_QOANH_StockMovements_Reference' AND object_id=OBJECT_ID(N'dbo.QOANH_StockMovements'))
CREATE UNIQUE INDEX [IX_QOANH_StockMovements_Reference] ON [QOANH_StockMovements] ([Reference]);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_ThanhToan_MaGiaoDich' AND object_id=OBJECT_ID(N'dbo.ThanhToan')) CREATE UNIQUE INDEX UX_ThanhToan_MaGiaoDich ON ThanhToan(MaGiaoDich) WHERE MaGiaoDich IS NOT NULL;
GO
INSERT INTO QOANH_StockMovements(Id,ProductId,[Before],Delta,[After],Reason,Reference,Supplier,UnitCost,ActorId,Created) SELECT (SELECT COALESCE(MAX(Id),0) FROM QOANH_StockMovements)+ROW_NUMBER() OVER(ORDER BY p.MaSP),p.MaSP,0,COALESCE(s.Qty,0),COALESCE(s.Qty,0),N'Số dư kho từ hai script SQL gốc',CONCAT('SQL-OPEN-',p.MaSP),N'',0,NULL,SYSUTCDATETIME() FROM SanPham p LEFT JOIN (SELECT b.MaSP,SUM(t.SoLuongTon) Qty FROM TonKho t JOIN BienTheSanPham b ON b.MaBienThe=t.MaBienThe GROUP BY b.MaSP) s ON s.MaSP=p.MaSP WHERE NOT EXISTS(SELECT 1 FROM QOANH_StockMovements m WHERE m.ProductId=p.MaSP);
GO
COMMIT;
GO
