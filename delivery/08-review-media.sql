-- VAI TRÒ SCRIPT SQL: Bổ sung bảng QOANH_ReviewMedia và ràng buộc để lưu byte ảnh/video của đánh giá.
-- LIÊN KẾT CODE: Data/Native/Mapping.cs, Data/FeatureMapping.cs và Data/ReviewMediaMapping.cs mô tả entity tương
-- ứng. Thêm/sửa/xóa từ giao diện được kiểm tra ở controller/service rồi EF gửi SQL; không sửa file này để cập
-- nhật từng đơn hoặc từng sản phẩm.

:ON ERROR EXIT USE [$(DatabaseName)];

GO


SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.DanhGia', N'U') IS NULL THROW 51000,'Missing DanhGia table; install the base schema first.',1;

IF OBJECT_ID(N'dbo.QOANH_ReviewMedia', N'U') IS NULL BEGIN
CREATE TABLE dbo.QOANH_ReviewMedia(Id uniqueidentifier NOT NULL CONSTRAINT PK_QOANH_ReviewMedia PRIMARY KEY, ReviewId int NOT NULL,
                                                                                                                          ContentType varchar(30) NOT NULL,
                                                                                                                                                  Content varbinary(max) NOT NULL,
                                                                                                                                                                         CONSTRAINT FK_QOANH_ReviewMedia_Review
                                   FOREIGN KEY(ReviewId) REFERENCES dbo.DanhGia(MaDanhGia), CONSTRAINT CK_QOANH_ReviewMedia_Type CHECK(ContentType IN ('image/jpeg',

                                                                                                                                                     'image/png',
                                                                                                                                                     'image/gif',
                                                                                                                                                     'image/webp',
                                                                                                                                                     'video/mp4',
                                                                                                                                                     'video/webm')),
                                                                                                                                 CONSTRAINT CK_QOANH_ReviewMedia_Size CHECK(DATALENGTH(Content)>0
                                                                                                                                                                            AND DATALENGTH(Content)<=20971520
                                                                                                                                                                            AND (ContentType LIKE 'video/%'
                                                                                                                                                                                 OR DATALENGTH(Content)<=5242880)));
CREATE INDEX IX_QOANH_ReviewMedia_ReviewId ON dbo.QOANH_ReviewMedia(ReviewId); END;


COMMIT;

GO
