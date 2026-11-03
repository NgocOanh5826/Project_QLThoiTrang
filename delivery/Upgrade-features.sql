:ON ERROR EXIT
-- SQLCMD mode. Supply -v DatabaseName="QL_CuaHangThoiTrang".
-- Run from the solution directory. Existing original schema + extension 04 required.
USE [$(DatabaseName)];
GO
IF OBJECT_ID(N'dbo.DonHang',N'U') IS NULL OR OBJECT_ID(N'dbo.QOANH_Configuration',N'U') IS NULL
 THROW 51000,'Expected existing QOANH database with extension 04; upgrade refused.',1;
DECLARE @folder nvarchar(2000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(2000));
IF @folder IS NULL THROW 51000,'No SQL backup directory configured; aborting before schema changes.',1;
IF RIGHT(@folder,1)<>N'\' SET @folder+=N'\';
DECLARE @backup nvarchar(4000)=@folder+DB_NAME()+N'_before_features_'+CONVERT(nvarchar(8),GETDATE(),112)+N'_'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [$(DatabaseName)] TO DISK=@backup WITH COPY_ONLY,CHECKSUM;
RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM;
SELECT @backup AS VerifiedBackupFile;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'QOANH.Store.Write',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,'Could not obtain business lock; no schema changes committed.',1;
CREATE TABLE #BeforeUpgrade(TableName sysname NOT NULL PRIMARY KEY,RecordCount bigint NOT NULL);
CREATE TABLE #AfterUpgrade(TableName sysname NOT NULL PRIMARY KEY,RecordCount bigint NOT NULL);
DECLARE @table sysname,@sql nvarchar(max);
DECLARE before_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND is_ms_shipped=0;
OPEN before_cursor;FETCH NEXT FROM before_cursor INTO @table;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @sql=N'INSERT INTO #BeforeUpgrade SELECT @name,COUNT_BIG(*) FROM dbo.'+QUOTENAME(@table);
 EXEC sys.sp_executesql @sql,N'@name sysname',@name=@table;
 FETCH NEXT FROM before_cursor INTO @table;
END;
CLOSE before_cursor;DEALLOCATE before_cursor;
GO
:r .\delivery\07-customer-finance-features.sql
GO
DECLARE @table sysname,@sql nvarchar(max);
DECLARE after_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName FROM #BeforeUpgrade;
OPEN after_cursor;FETCH NEXT FROM after_cursor INTO @table;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @sql=N'INSERT INTO #AfterUpgrade SELECT @name,COUNT_BIG(*) FROM dbo.'+QUOTENAME(@table);
 EXEC sys.sp_executesql @sql,N'@name sysname',@name=@table;
 FETCH NEXT FROM after_cursor INTO @table;
END;
CLOSE after_cursor;DEALLOCATE after_cursor;
IF EXISTS(SELECT 1 FROM #BeforeUpgrade b LEFT JOIN #AfterUpgrade a ON a.TableName=b.TableName WHERE a.RecordCount IS NULL OR a.RecordCount<>b.RecordCount)
 THROW 51000,'Original row counts changed; rolling back schema upgrade.',1;
SELECT b.TableName,b.RecordCount AS BeforeCount,a.RecordCount AS AfterCount FROM #BeforeUpgrade b JOIN #AfterUpgrade a ON a.TableName=b.TableName ORDER BY b.TableName;
COMMIT;
GO
SELECT COL_LENGTH(N'dbo.DonHang',N'QBankQrImage') AS QBankQrImage,
 COL_LENGTH(N'dbo.DonHang',N'QTransferReference') AS QTransferReference,
 COL_LENGTH(N'dbo.ChiTietDonHang',N'QUnitCost') AS QUnitCost,
 (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS UserTableCount;
