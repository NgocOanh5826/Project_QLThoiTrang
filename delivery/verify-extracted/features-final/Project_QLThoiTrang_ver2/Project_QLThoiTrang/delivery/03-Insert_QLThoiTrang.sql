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

-- Chay sau script goc, tren database moi tao voi du lieu mau ban dau.

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 6','kh06@example.com','AQAAAAIAAYagAAAAEINs2vE3W53XOnIvbKDBLrlsiAqYipq2lPY5VvubhPOil8GTV5XZDk4/o8qcPc5h3w==','09020000006',N'Nữ','2000-05-12',N'TP.HCM, phường 6');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 7','kh07@example.com','AQAAAAIAAYagAAAAENP/bpb3/dWwjkREskOsaiN0eOIjQRRyLGWfIgG+3Uj9CWJZ/iryV6K/D/7KPDm7WQ==','09020000007',N'Nam','2000-05-12',N'TP.HCM, phường 7');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 8','kh08@example.com','AQAAAAIAAYagAAAAEGhXobm1Kh5nK8QQSs7OGl6y/A44GA0b2LSTpGYqaY9QHnpZsmUe6C9l1VhBTCvBAg==','09020000008',N'Nữ','2000-05-12',N'TP.HCM, phường 8');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 9','kh09@example.com','AQAAAAIAAYagAAAAEOfQrDg0qbrwgLzEt8wC7HtcX3YKG4Pp3wvdTU+qdi5GHrIePNUZgpPaW0CSUOmHig==','09020000009',N'Nam','2000-05-12',N'TP.HCM, phường 9');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 10','kh10@example.com','AQAAAAIAAYagAAAAELl92fC7qnEVbkCeM5c+Ayi7tHcHsTbDzDBFROW+YHyZKEQpM2OP8jWXneCdWXDbig==','09020000010',N'Nữ','2000-05-12',N'TP.HCM, phường 10');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 11','kh11@example.com','AQAAAAIAAYagAAAAEDOZ7lR2mkGra7C60Z8IyVtdV/5lBOJdGQ9nrsXfuVcybs3pPefdcdZaAbUlNjncIw==','09020000011',N'Nam','2000-05-12',N'TP.HCM, phường 11');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 12','kh12@example.com','AQAAAAIAAYagAAAAEGGoBXkghoibZavRq5PC3Qiuct3tB4xSxYHti7A2aF1UC4W4mb281SLc5s6X0Jo3/g==','09020000012',N'Nữ','2000-05-12',N'TP.HCM, phường 12');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 13','kh13@example.com','AQAAAAIAAYagAAAAEFpzSLs6rNtKMlTDglrf7ATtiAGGw2qCQpwIlhG3nTx8+0liZapsYuAJydLiVzklcw==','09020000013',N'Nam','2000-05-12',N'TP.HCM, phường 13');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 14','kh14@example.com','AQAAAAIAAYagAAAAECn9Wx2XT+jzeel1wQ7gVRveKx+IPG46LW+jxHqRrULfgoXZb4Bl1b3c6uHmCpz+ig==','09020000014',N'Nữ','2000-05-12',N'TP.HCM, phường 14');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 15','kh15@example.com','AQAAAAIAAYagAAAAEEdwEnl0VeHFOR00Eviit9VkPzA1FuS75BJpaVuhkqFze9XPKsd8XssgFXe6s4rqhw==','09020000015',N'Nam','2000-05-12',N'TP.HCM, phường 15');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 16','kh16@example.com','AQAAAAIAAYagAAAAEItCO0U9e+gdzdRUT3lEGERoFkHaRUIlr1JXi/Hl8H1wreh6LaC7SrLRXEyNbPGWug==','09020000016',N'Nữ','2000-05-12',N'TP.HCM, phường 16');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 17','kh17@example.com','AQAAAAIAAYagAAAAEF2KUnFRFFDzHpphAcaCOqPChgWxWlX/eBSbpvWiZpTx4eWXK7aNrA6mGEw62g8+5A==','09020000017',N'Nam','2000-05-12',N'TP.HCM, phường 17');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 18','kh18@example.com','AQAAAAIAAYagAAAAEBgHX3aJAkNuof+NqcEXppYkHzolhdQzqlMmug8SSIVszf02V2JdwFxHhEKqK3J+SA==','09020000018',N'Nữ','2000-05-12',N'TP.HCM, phường 18');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 19','kh19@example.com','AQAAAAIAAYagAAAAEDYYTojYAV9QSf69soMNOtf4O5YYw7/kJrPEzqNPHdCjDFByQzDrPtYCUaZ/7LbOkw==','09020000019',N'Nam','2000-05-12',N'TP.HCM, phường 19');
GO

INSERT INTO KhachHang(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi) VALUES (N'Khách hàng mẫu 20','kh20@example.com','AQAAAAIAAYagAAAAEPxWIqgYUruX2lrP9wdg2fUrtmj9DibWthk74hPWW6o6POLwuXoIWt75VGKCfeJ4KA==','09020000020',N'Nữ','2000-05-12',N'TP.HCM, phường 20');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 5','nv05@example.com','AQAAAAIAAYagAAAAEFgQEWIBdk6Erge3v3OG4UOv971VqfWwUHAlYRpZcXUXnt2luMe1y+KckkKQSFXz/g==','09120000005',N'Nam','1998-06-15',N'TP.HCM',N'Nhân viên kho');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 6','nv06@example.com','AQAAAAIAAYagAAAAECiREZHbM5/x51Fnddpg6FMGl+ZSpEckAVPjXZKZyqgn11k+Ij2FS7Fs8femlk4MdA==','09120000006',N'Nữ','1998-06-15',N'TP.HCM',N'Chăm sóc khách hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 7','nv07@example.com','AQAAAAIAAYagAAAAEA56p0NWjA4hlY80AxyFXHzqF227rWfHOETLWQUGosr/azobL5RK1+rsKR/FxN2E0Q==','09120000007',N'Nam','1998-06-15',N'TP.HCM',N'Quản lý đơn hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 8','nv08@example.com','AQAAAAIAAYagAAAAED+/DpAAMxvgnrnuSZduUp11Tbzrdv6k+rtJyT7HeydVOG12B3nGcF0OCpNATe5Msg==','09120000008',N'Nữ','1998-06-15',N'TP.HCM',N'Nhân viên bán hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 9','nv09@example.com','AQAAAAIAAYagAAAAEDqStG6Wx3oOF0uOepe98HyMGS1PzOVdmYUjBSQAqgA2vhRn9LPjR8QMmxSOUYZUTg==','09120000009',N'Nam','1998-06-15',N'TP.HCM',N'Nhân viên kho');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 10','nv10@example.com','AQAAAAIAAYagAAAAELb6dXCRJSsAq70rFvvaqICd51w6UGFi6bwLa32wX64DCfCpnRiL12mtay0iPAI2gA==','09120000010',N'Nữ','1998-06-15',N'TP.HCM',N'Chăm sóc khách hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 11','nv11@example.com','AQAAAAIAAYagAAAAEJZgEErsb+5SodDEDyX9sW9m5+xm1K9sGLe71Bkk/rx3mi2BlGyCiK1jVgwTDh4XRw==','09120000011',N'Nam','1998-06-15',N'TP.HCM',N'Quản lý đơn hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 12','nv12@example.com','AQAAAAIAAYagAAAAEFDp5A+cZ4es0ct76Js/3P6cd1falNOegA2Z/hA4FAyyGGyWctCirwKJEDvEWpY/jA==','09120000012',N'Nữ','1998-06-15',N'TP.HCM',N'Nhân viên bán hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 13','nv13@example.com','AQAAAAIAAYagAAAAEHt35/B79nfEwEQDSTahg3W8KrtkIunQdrUy8csYPzIk9IDF4z7UQp4Es/LlUNQckg==','09120000013',N'Nam','1998-06-15',N'TP.HCM',N'Nhân viên kho');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 14','nv14@example.com','AQAAAAIAAYagAAAAEA7W9r9esq92n2SqKvNBrTufrARWqdqNwCodK0uHdsiU15b0K54WCHgf1AqN9iXDWQ==','09120000014',N'Nữ','1998-06-15',N'TP.HCM',N'Chăm sóc khách hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 15','nv15@example.com','AQAAAAIAAYagAAAAEDra/Pq4vL34IfBzxVaEQkXz/7s6YRIh1MtXF4yqyvTfyx+Fv6TdoJtFafMOQSOI3A==','09120000015',N'Nam','1998-06-15',N'TP.HCM',N'Quản lý đơn hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 16','nv16@example.com','AQAAAAIAAYagAAAAEMhBwhVsgjol9JC9nzMqidS9X8DZ5RXsB//D3JtuJLpc+DP9+W6rdWxeMLGIZ+0T/w==','09120000016',N'Nữ','1998-06-15',N'TP.HCM',N'Nhân viên bán hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 17','nv17@example.com','AQAAAAIAAYagAAAAEGm0Bvnl89wdLQrVpSDF/rnMr/qRqXpxHpRQsH1Co1mqIYYmesK35FP/qFDOABSDuw==','09120000017',N'Nam','1998-06-15',N'TP.HCM',N'Nhân viên kho');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 18','nv18@example.com','AQAAAAIAAYagAAAAEKbtQmSMYGSRvl48ddu6F6wvZ7RG1/pqG3HBmCQe5hgOg8nHSKe+TZy8cxA40d5F2A==','09120000018',N'Nữ','1998-06-15',N'TP.HCM',N'Chăm sóc khách hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 19','nv19@example.com','AQAAAAIAAYagAAAAEAF3bX7+9ibTKsUCGJBXQia1HZG5Rb8OlVwoY5phClscAkY/v+BQm/gSNmqpaF06CQ==','09120000019',N'Nam','1998-06-15',N'TP.HCM',N'Quản lý đơn hàng');
GO

INSERT INTO NhanVien(HoTen,Email,MatKhau,SoDienThoai,GioiTinh,NgaySinh,DiaChi,ChucVu) VALUES (N'Nhân viên 20','nv20@example.com','AQAAAAIAAYagAAAAEHHGF2r+FoHEL8/wFi3wQKyWc8E9fPnF4i0cZT2CKphPMWMP8LzpG1SOeQH2OxBF1g==','09120000020',N'Nữ','1998-06-15',N'TP.HCM',N'Nhân viên bán hàng');
GO
COMMIT;
GO
