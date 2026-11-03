:ON ERROR EXIT
USE [$(DatabaseName)];
GO
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.DonHang',N'QBankQrImage') IS NULL ALTER TABLE dbo.DonHang ADD QBankQrImage nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.DonHang',N'QTransferReference') IS NULL ALTER TABLE dbo.DonHang ADD QTransferReference nvarchar(30) NULL;
IF COL_LENGTH(N'dbo.ChiTietDonHang',N'QUnitCost') IS NULL ALTER TABLE dbo.ChiTietDonHang ADD QUnitCost decimal(18,2) NULL;
IF OBJECT_ID(N'dbo.QOANH_ChatThreads',N'U') IS NULL
CREATE TABLE dbo.QOANH_ChatThreads(CustomerId int NOT NULL PRIMARY KEY,UpdatedUtc datetime2 NOT NULL,CustomerReadId bigint NOT NULL,StaffReadId bigint NOT NULL,CONSTRAINT FK_QChat_Customer FOREIGN KEY(CustomerId) REFERENCES dbo.KhachHang(MaKH));
IF OBJECT_ID(N'dbo.QOANH_ChatMessages',N'U') IS NULL
CREATE TABLE dbo.QOANH_ChatMessages(Id bigint IDENTITY PRIMARY KEY,CustomerId int NOT NULL,SenderId int NOT NULL,Text nvarchar(2000) NOT NULL,ClientToken uniqueidentifier NOT NULL,CreatedUtc datetime2 NOT NULL,CONSTRAINT FK_QMessage_Thread FOREIGN KEY(CustomerId) REFERENCES dbo.QOANH_ChatThreads(CustomerId),CONSTRAINT UQ_QMessage_Token UNIQUE(CustomerId,ClientToken),CONSTRAINT CK_QMessage_Sender CHECK(SenderId<>0));
IF OBJECT_ID(N'dbo.QOANH_ResetCodes',N'U') IS NULL
CREATE TABLE dbo.QOANH_ResetCodes(Id uniqueidentifier NOT NULL PRIMARY KEY,CustomerId int NOT NULL,Hash nvarchar(64) NOT NULL,Stamp nvarchar(64) NOT NULL,CreatedUtc datetime2 NOT NULL,ExpiresUtc datetime2 NOT NULL,Attempts int NOT NULL,Sent bit NOT NULL,UsedUtc datetime2 NULL,CONSTRAINT FK_QReset_Customer FOREIGN KEY(CustomerId) REFERENCES dbo.KhachHang(MaKH),CONSTRAINT CK_QReset_Attempts CHECK(Attempts BETWEEN 0 AND 5));
IF OBJECT_ID(N'dbo.QOANH_InventoryValues',N'U') IS NULL
CREATE TABLE dbo.QOANH_InventoryValues(VariantId int NOT NULL PRIMARY KEY,AverageCost decimal(18,2) NULL,ActorId int NULL,UpdatedUtc datetime2 NOT NULL,Note nvarchar(300) NOT NULL,CONSTRAINT FK_QValue_Variant FOREIGN KEY(VariantId) REFERENCES dbo.BienTheSanPham(MaBienThe),CONSTRAINT CK_QValue_Cost CHECK(AverageCost IS NULL OR AverageCost>=0));
IF OBJECT_ID(N'dbo.QOANH_SupplierAccounts',N'U') IS NULL
CREATE TABLE dbo.QOANH_SupplierAccounts(ReceiptId int NOT NULL PRIMARY KEY,Amount decimal(18,2) NOT NULL,Note nvarchar(300) NOT NULL,ActorId int NOT NULL,UpdatedUtc datetime2 NOT NULL,CONSTRAINT FK_QAccount_Receipt FOREIGN KEY(ReceiptId) REFERENCES dbo.PhieuNhap(MaPhieuNhap),CONSTRAINT CK_QAccount_Amount CHECK(Amount>=0));
IF OBJECT_ID(N'dbo.QOANH_SupplierPayments',N'U') IS NULL
CREATE TABLE dbo.QOANH_SupplierPayments(Id bigint IDENTITY PRIMARY KEY,ReceiptId int NOT NULL,Amount decimal(18,2) NOT NULL,Reference nvarchar(100) NOT NULL,ActorId int NOT NULL,CreatedUtc datetime2 NOT NULL,CONSTRAINT FK_QSupplierPay_Account FOREIGN KEY(ReceiptId) REFERENCES dbo.QOANH_SupplierAccounts(ReceiptId),CONSTRAINT UQ_QSupplierPay_Reference UNIQUE(Reference),CONSTRAINT CK_QSupplierPay_Amount CHECK(Amount>0));
IF OBJECT_ID(N'dbo.QOANH_Shipments',N'U') IS NULL
CREATE TABLE dbo.QOANH_Shipments(OrderId int NOT NULL PRIMARY KEY,ClientCode nvarchar(50) NOT NULL,RequestHash nvarchar(64) NOT NULL,TrackingCode nvarchar(50) NULL,Status nvarchar(60) NOT NULL,UpdatedUtc datetime2 NOT NULL,Error nvarchar(300) NOT NULL,CONSTRAINT FK_QShipment_Order FOREIGN KEY(OrderId) REFERENCES dbo.DonHang(MaDonHang),CONSTRAINT UQ_QShipment_Client UNIQUE(ClientCode));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_QReset_CustomerCreated' AND object_id=OBJECT_ID(N'dbo.QOANH_ResetCodes')) CREATE INDEX IX_QReset_CustomerCreated ON dbo.QOANH_ResetCodes(CustomerId,CreatedUtc);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_QMessage_CustomerId' AND object_id=OBJECT_ID(N'dbo.QOANH_ChatMessages')) CREATE INDEX IX_QMessage_CustomerId ON dbo.QOANH_ChatMessages(CustomerId,Id);
COMMIT;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_QTransferReference' AND object_id=OBJECT_ID(N'dbo.DonHang')) CREATE UNIQUE INDEX UX_QTransferReference ON dbo.DonHang(QTransferReference) WHERE QTransferReference IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_QTracking' AND object_id=OBJECT_ID(N'dbo.QOANH_Shipments')) CREATE UNIQUE INDEX UX_QTracking ON dbo.QOANH_Shipments(TrackingCode) WHERE TrackingCode IS NOT NULL;
GO
