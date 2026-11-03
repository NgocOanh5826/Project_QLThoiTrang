:ON ERROR EXIT
-- SQLCMD mode. Supply -v DatabaseName="QL_CuaHangThoiTrang".
USE [master];
GO
IF DB_ID(N'$(DatabaseName)') IS NULL
    EXEC(N'CREATE DATABASE [$(DatabaseName)]');
GO
