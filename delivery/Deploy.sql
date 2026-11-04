-- VAI TRÒ SCRIPT SQL: Script triển khai tổng hợp cho database mới. Chạy có chủ đích trên database đích;
-- Program.cs không tự chạy file này khi mở website.
-- LIÊN KẾT CODE: Data/Native/Mapping.cs, Data/FeatureMapping.cs và Data/ReviewMediaMapping.cs mô tả entity tương
-- ứng. Thêm/sửa/xóa từ giao diện được kiểm tra ở controller/service rồi EF gửi SQL; không sửa file này để cập
-- nhật từng đơn hoặc từng sản phẩm.

:ON ERROR EXIT -- SQLCMD mode, run from solution root; use -v DatabaseName="QL_CuaHangThoiTrang".
:r .\delivery\00-database.sql :r .\delivery\01-schema.sql :r .\delivery\02-sample-data.sql :r .\delivery\03-Insert_QLThoiTrang.sql :r .\delivery\04-qoanh-extensions.sql :r .\delivery\05-views-procedures.sql :r .\delivery\07-customer-finance-features.sql :r .\delivery\08-review-media.sql
