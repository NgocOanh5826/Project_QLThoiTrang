:ON ERROR EXIT
-- Target: existing local QL_CuaHangThoiTrang. Never DROP or recreate the database.
USE [master];
GO
IF DB_ID(N'QL_CuaHangThoiTrang') IS NULL THROW 51000,'Existing project database not found; aborting.',1;
DECLARE @directory nvarchar(2000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(2000));
IF @directory IS NULL THROW 51000,'SQL default backup directory unavailable; aborting before changes.',1;
IF RIGHT(@directory,1)<>N'\' SET @directory+=N'\';
DECLARE @backup nvarchar(4000)=@directory+N'QL_CuaHangThoiTrang_before_QOANH_'+CONVERT(nvarchar(8),GETDATE(),112)+N'_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [QL_CuaHangThoiTrang] TO DISK=@backup WITH COPY_ONLY,CHECKSUM;
RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM;
SELECT @backup AS VerifiedDatabaseBackup;
GO
USE [QL_CuaHangThoiTrang];
GO
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
-- Allow more than one customer/staff without a phone; retain unique real phones.
DECLARE @sql nvarchar(max)=N'';
SELECT @sql+=N'ALTER TABLE '+QUOTENAME(OBJECT_SCHEMA_NAME(k.parent_object_id))+N'.'+QUOTENAME(t.name)+N' DROP CONSTRAINT '+QUOTENAME(k.name)+N';'
FROM sys.key_constraints k
JOIN sys.tables t ON t.object_id=k.parent_object_id
WHERE k.type='UQ' AND t.name IN(N'KhachHang',N'NhanVien')
AND (SELECT COUNT(*) FROM sys.index_columns ic WHERE ic.object_id=k.parent_object_id AND ic.index_id=k.unique_index_id AND ic.key_ordinal>0)=1
AND EXISTS(SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=k.parent_object_id AND ic.index_id=k.unique_index_id AND c.name=N'SoDienThoai');
IF @sql<>N'' EXEC sys.sp_executesql @sql;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'KhachHang') AND name=N'UX_KhachHang_Phone') CREATE UNIQUE INDEX UX_KhachHang_Phone ON KhachHang(SoDienThoai) WHERE SoDienThoai IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'NhanVien') AND name=N'UX_NhanVien_Phone') CREATE UNIQUE INDEX UX_NhanVien_Phone ON NhanVien(SoDienThoai) WHERE SoDienThoai IS NOT NULL;
GO
:r .\delivery\04-qoanh-extensions.sql
COMMIT;
GO
