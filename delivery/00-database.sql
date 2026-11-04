-- VAI TRÒ SCRIPT SQL: Khởi tạo database theo thiết lập SQL trong file; bước đầu của quy trình triển khai mới.
-- LIÊN KẾT CODE: Data/Native/Mapping.cs, Data/FeatureMapping.cs và Data/ReviewMediaMapping.cs mô tả entity tương
-- ứng. Thêm/sửa/xóa từ giao diện được kiểm tra ở controller/service rồi EF gửi SQL; không sửa file này để cập
-- nhật từng đơn hoặc từng sản phẩm.

:ON ERROR EXIT -- SQLCMD mode. Supply -v DatabaseName="QL_CuaHangThoiTrang".
USE [master];

GO

IF DB_ID(N'$(DatabaseName)') IS NULL EXEC(N'CREATE DATABASE [$(DatabaseName)]');

GO
