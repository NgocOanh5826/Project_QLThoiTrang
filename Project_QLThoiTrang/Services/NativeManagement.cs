using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // CurrentAccount: Lấy tài khoản khách hoặc nhân viên theo mã đăng nhập.
    public Customer? CurrentAccount(int id) =>
        NativeRead(db =>
        {
            if (id > 0)
            {
                var c = db.KhachHang.SingleOrDefault(c => c.MaKH == id);
                return c == null
                    ? null
                    : new Customer
                    {
                        Id = id,
                        Name = c.HoTen,
                        Email = c.Email,
                        PasswordHash = c.MatKhau,
                        Active = c.TrangThai,
                    };
            }

            var s = db.NhanVien.SingleOrDefault(s => s.MaNV == -id);
            return s == null
                ? null
                : new Customer
                {
                    Id = id,
                    Name = s.HoTen,
                    Email = s.Email,
                    PasswordHash = s.MatKhau,
                    Active = s.TrangThai,
                    Role = StaffRole(s.ChucVu),
                };
        });

    // NativeRead: Đọc trực tiếp entity SQL trong transaction.
    public T NativeRead<T>(Func<StoreDbContext, T> read)
    {
        using var db = factory.CreateDbContext();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        var r = read(db);

        tx.Commit();
        return r;
    }

    // NativeWrite: Lưu trực tiếp entity SQL trong transaction.
    T NativeWrite<T>(Func<StoreDbContext, T> write)
    {
        using var db = factory.CreateDbContext();
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        db.Database.ExecuteSqlRaw(
            (
                "DECLARE @r int; EXEC @r=sys.sp_getapplock "
                + "@Resource=N'QOANH.Store.Write',@LockMode='Exclusive',@Lo"
                + "ckOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW "
                + "51000,'Business lock timeout',1;"
            )
        );
        var r = write(db);

        db.SaveChanges();

        tx.Commit();
        return r;
    }

    // NativePermission: Kiểm tra quyền và trạng thái nhân viên.
    static void NativePermission(StoreDbContext db, int actor, params string[] roles)
    {
        Require(actor < 0, "Cần tài khoản nhân viên.");
        var s = db.NhanVien.SingleOrDefault(s => s.MaNV == -actor && s.TrangThai);

        Require(
            s != null && (StaffRole(s.ChucVu) == "Admin" || roles.Contains(StaffRole(s.ChucVu))),
            "Không có quyền thực hiện thao tác này."
        );
    }

    // References: Lấy danh mục thương hiệu, size, màu, kho hoặc nhà cung cấp.
    public List<ReferenceInput> References(string kind) =>
        NativeRead(db =>
            kind switch
            {
                "brands" => db
                    .ThuongHieu.Select(x => new ReferenceInput
                    {
                        Id = x.MaThuongHieu,
                        Name = x.TenThuongHieu,
                        Description = x.MoTa,
                        Active = x.TrangThai,
                        Kind = kind,
                    })
                    .ToList(),
                "sizes" => db
                    .KichThuoc.Select(x => new ReferenceInput
                    {
                        Id = x.MaKichThuoc,
                        Name = x.TenKichThuoc,
                        Kind = kind,
                    })
                    .ToList(),
                "colors" => db
                    .MauSac.Select(x => new ReferenceInput
                    {
                        Id = x.MaMauSac,
                        Name = x.TenMauSac,
                        Description = x.MaMau,
                        Kind = kind,
                    })
                    .ToList(),
                "warehouses" => db
                    .Kho.Select(x => new ReferenceInput
                    {
                        Id = x.MaKho,
                        Name = x.TenKho,
                        Address = x.DiaChi,
                        Active = x.TrangThai,
                        Kind = kind,
                    })
                    .ToList(),
                "suppliers" => db
                    .NhaCungCap.Select(x => new ReferenceInput
                    {
                        Id = x.MaNCC,
                        Name = x.TenNCC,
                        Address = x.DiaChi,
                        Phone = x.SoDienThoai,
                        Email = x.Email,
                        Active = x.TrangThai,
                        Kind = kind,
                    })
                    .ToList(),
                _ => throw new InvalidOperationException("Loại danh mục không hợp lệ."),
            }
        );

    // SaveReference: Thêm hoặc sửa danh mục dùng chung.
    public void SaveReference(ReferenceInput input, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(
                db,
                actor,
                input.Kind is "warehouses" or "suppliers" ? "Inventory" : "Catalog"
            );
            Validator.ValidateObject(input, new ValidationContext(input), true);
            input.Name = Text(
                input.Name,
                1,
                input.Kind == "sizes" ? 30
                    : input.Kind == "colors" ? 50
                    : 100,
                "Tên"
            );
            switch (input.Kind)
            {
                case "brands":

                    Require(
                        !db.ThuongHieu.Any(x =>
                            x.MaThuongHieu != input.Id && x.TenThuongHieu == input.Name
                        ),
                        "Tên đã tồn tại."
                    );
                    var b =
                        input.Id == 0
                            ? new ThuongHieuRow()
                            : db.ThuongHieu.Single(x => x.MaThuongHieu == input.Id);

                    if (input.Id == 0)
                        db.Add(b);
                    b.TenThuongHieu = input.Name;
                    b.MoTa = input.Description;
                    b.TrangThai = input.Active;
                    break;
                case "sizes":

                    Require(
                        !db.KichThuoc.Any(x =>
                            x.MaKichThuoc != input.Id && x.TenKichThuoc == input.Name
                        ),
                        "Tên đã tồn tại."
                    );
                    var s =
                        input.Id == 0
                            ? new KichThuocRow()
                            : db.KichThuoc.Single(x => x.MaKichThuoc == input.Id);

                    if (input.Id == 0)
                        db.Add(s);
                    s.TenKichThuoc = input.Name;
                    break;
                case "colors":

                    Require(
                        !db.MauSac.Any(x => x.MaMauSac != input.Id && x.TenMauSac == input.Name),
                        "Tên đã tồn tại."
                    );

                    Require((input.Description?.Length ?? 0) <= 20, "Mã màu tối đa 20 ký tự.");
                    var c =
                        input.Id == 0
                            ? new MauSacRow()
                            : db.MauSac.Single(x => x.MaMauSac == input.Id);

                    if (input.Id == 0)
                        db.Add(c);
                    c.TenMauSac = input.Name;
                    c.MaMau = input.Description;
                    break;
                case "warehouses":
                    var w = input.Id == 0 ? new KhoRow() : db.Kho.Single(x => x.MaKho == input.Id);

                    if (input.Id == 0)
                        db.Add(w);
                    w.TenKho = input.Name;
                    w.DiaChi = input.Address;
                    w.TrangThai = input.Active;
                    break;
                case "suppliers":
                    var n =
                        input.Id == 0
                            ? new NhaCungCapRow()
                            : db.NhaCungCap.Single(x => x.MaNCC == input.Id);

                    if (input.Id == 0)
                        db.Add(n);
                    n.TenNCC = input.Name;
                    n.DiaChi = input.Address;
                    n.SoDienThoai = input.Phone;
                    n.Email = input.Email;
                    n.TrangThai = input.Active;
                    break;
                default:
                    throw new InvalidOperationException("Loại không hợp lệ.");
            }

            return 0;
        });

    // SaveVariant: Thêm hoặc sửa biến thể sản phẩm.
    public void SaveVariant(VariantInput input, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Catalog");
            Validator.ValidateObject(input, new ValidationContext(input), true);

            Require(
                db.SanPham.Any(p => p.MaSP == input.ProductId)
                    && db.KichThuoc.Any(s => s.MaKichThuoc == input.SizeId)
                    && db.MauSac.Any(s => s.MaMauSac == input.ColorId),
                "Sản phẩm/size/màu không tồn tại."
            );

            Require(
                !db.BienTheSanPham.Any(v =>
                    v.MaBienThe != input.Id
                    && (
                        v.SKU == input.Sku
                        || (
                            v.MaSP == input.ProductId
                            && v.MaKichThuoc == input.SizeId
                            && v.MaMauSac == input.ColorId
                        )
                    )
                ),
                "SKU hoặc tổ hợp sản phẩm/size/màu đã tồn tại."
            );
            var r =
                input.Id == 0
                    ? new BienTheSanPhamRow()
                    : db.BienTheSanPham.Single(v => v.MaBienThe == input.Id);

            if (input.Id == 0)
                db.Add(r);

            if (input.Id != 0)
                Require(
                    r.MaSP == input.ProductId
                        && r.MaKichThuoc == input.SizeId
                        && r.MaMauSac == input.ColorId,
                    "Không thay định danh biến thể đã tạo; hãy thêm biến thể mới để giữ lịch sử."
                );
            r.MaSP = input.ProductId;
            r.MaKichThuoc = input.SizeId;
            r.MaMauSac = input.ColorId;
            r.SKU = input.Sku.Trim();
            r.GiaBanRieng = input.Price;
            r.TrangThai = input.Active;
            return 0;
        });

    // Staff: Lấy danh sách tài khoản nhân viên.
    public List<Customer> Staff() => Read(d => d.Customers.Where(c => c.Id < 0).ToList());

    // SaveStaff: Lưu tài khoản, mật khẩu và quyền nhân viên.
    public void SaveStaff(StaffInput input, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor);
            Validator.ValidateObject(input, new ValidationContext(input), true);

            Require(
                new[] { "Admin", "Catalog", "Inventory", "Sales", "Support" }.Contains(input.Role),
                "Vai trò không hợp lệ."
            );
            string email = input.Email.Trim().ToLowerInvariant();

            Require(
                !db.KhachHang.Any(c => c.Email == email)
                    && !db.NhanVien.Any(c => c.MaNV != input.Id && c.Email == email),
                "Email đã sử dụng."
            );
            if (input.Id == -actor)
                Require(
                    input.Active && input.Role == "Admin",
                    "Không tự khóa hoặc hạ quyền tài khoản quản trị đang dùng."
                );
            var r =
                input.Id == 0
                    ? new NhanVienRow { NgayVaoLam = DateTime.Today }
                    : db.NhanVien.Single(x => x.MaNV == input.Id);

            if (input.Id == 0)
                db.Add(r);
            if (
                StaffRole(r.ChucVu) == "Admin"
                && r.TrangThai
                && (!input.Active || input.Role != "Admin")
            )
                Require(
                    db.NhanVien.Any(x => x.MaNV != r.MaNV && x.TrangThai && x.ChucVu == "Admin"),
                    "Phải còn ít nhất một Admin hoạt động."
                );
            if (!string.IsNullOrEmpty(input.Password))
            {
                Require(
                    System.Text.RegularExpressions.Regex.IsMatch(
                        input.Password,
                        @"^(?=.{8,100}$)(?=.*\p{L})(?=.*\d).+$"
                    ),
                    "Mật khẩu cần 8–100 ký tự, có chữ và số."
                );
                r.MatKhau = hasher.HashPassword(new Customer(), input.Password);
            }

            Require(r.MatKhau != "", "Nhập mật khẩu cho nhân viên mới.");
            r.HoTen = input.Name.Trim();
            r.Email = email;
            r.SoDienThoai = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            r.ChucVu = input.Role;
            r.TrangThai = input.Active;
            return 0;
        });

    // UpdateCustomer: Cập nhật hồ sơ và trạng thái khách hàng.
    public void UpdateCustomer(int id, ProfileInput input, bool active, int actor) =>
        Write(
            d =>
            {
                Permission(d, actor, "Support");
                Validator.ValidateObject(input, new ValidationContext(input), true);
                var c =
                    d.Customers.SingleOrDefault(c => c.Id == id && c.Role == "Customer")
                    ?? throw new InvalidOperationException("Khách không tồn tại.");
                c.Name = input.Name.Trim();
                c.Phone = input.Phone?.Trim() ?? "";
                c.Address = input.Address?.Trim() ?? "";
                c.Active = active;
                return 0;
            },
            actor,
            "Cập nhật khách hàng"
        );

    // Reviews: Lấy đánh giá sản phẩm kèm thông tin media.
    public List<ReviewItem> Reviews(int? product = null, bool includeHidden = false) =>
        NativeRead(db =>
        {
            var reviews = (
                from r in db.DanhGia
                join c in db.KhachHang on r.MaKH equals c.MaKH
                where (!product.HasValue || r.MaSP == product) && (includeHidden || r.TrangThai)
                select new ReviewItem
                {
                    Id = r.MaDanhGia,
                    ProductId = r.MaSP,
                    Customer = c.HoTen,
                    Stars = r.SoSao,
                    Text = r.NoiDung ?? "",
                    Active = r.TrangThai,
                    Created = r.NgayDanhGia,
                }
            )
                .OrderByDescending(r => r.Created)
                .ToList();
            var ids = reviews.Select(r => r.Id).ToList();
            var media = db
                .ReviewMedia.Where(m => ids.Contains(m.ReviewId))
                .Select(m => new
                {
                    m.Id,
                    m.ReviewId,
                    m.ContentType,
                })
                .ToList();
            foreach (var r in reviews)
                r.Media = media
                    .Where(m => m.ReviewId == r.Id)
                    .Select(m => new ReviewMediaInfo(m.Id, m.ContentType.StartsWith("video/")))
                    .ToList();
            return reviews;
        });

    // Review: Lưu đánh giá của khách đã mua sản phẩm.
    public void Review(
        int customerId,
        int productId,
        int stars,
        string text,
        IReadOnlyList<ReviewUpload>? uploads = null
    ) =>
        NativeWrite(db =>
        {
            Require(
                customerId > 0 && db.KhachHang.Any(c => c.MaKH == customerId && c.TrangThai),
                "Chỉ khách hàng được đánh giá."
            );

            Require(stars is >= 1 and <= 5, "Số sao từ 1 đến 5.");
            text = Text(text, 5, 1000, "Nội dung");

            Require(
                (
                    from o in db.DonHang
                    join l in db.ChiTietDonHang on o.MaDonHang equals l.MaDonHang
                    join v in db.BienTheSanPham on l.MaBienThe equals v.MaBienThe
                    where
                        o.MaKH == customerId
                        && v.MaSP == productId
                        && (o.TrangThai == "Đã giao" || o.TrangThai == "Hoàn thành")
                    select o.MaDonHang
                ).Any(),
                "Chỉ đánh giá sản phẩm đã mua và đã giao."
            );

            Require(
                !db.DanhGia.Any(r => r.MaKH == customerId && r.MaSP == productId),
                "Bạn đã đánh giá sản phẩm này."
            );
            var attachments = ReviewUploads.Validate(uploads);
            var review = new DanhGiaRow
            {
                MaKH = customerId,
                MaSP = productId,
                SoSao = stars,
                NoiDung = text,
                NgayDanhGia = DateTime.Now,
                TrangThai = true,
            };
            db.DanhGia.Add(review);

            db.SaveChanges();
            foreach (var media in attachments)
            {
                media.ReviewId = review.MaDanhGia;
                db.ReviewMedia.Add(media);
            }

            return 0;
        });

    // ModerateReview: Ẩn hoặc hiện đánh giá sản phẩm.
    public void ModerateReview(int id, bool active, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Support");
            var r =
                db.DanhGia.SingleOrDefault(r => r.MaDanhGia == id)
                ?? throw new InvalidOperationException("Đánh giá không tồn tại.");
            r.TrangThai = active;
            return 0;
        });

    // LogLogin: Ghi lịch sử đăng nhập thành công hoặc thất bại.
    public long LogLogin(Customer c, bool success, string? ip, string? device) =>
        NativeWrite(db =>
        {
            var row = new LichSuDangNhapRow
            {
                MaKH = c.Id > 0 ? c.Id : null,
                MaNV = c.Id < 0 ? -c.Id : null,
                ThoiGianDangNhap = DateTime.Now,
                DiaChiIP = (ip ?? "")[..Math.Min(ip?.Length ?? 0, 45)],
                ThietBi = (device ?? "")[..Math.Min(device?.Length ?? 0, 255)],
                TrangThai = success ? "Thành công" : "Thất bại",
            };
            db.Add(row);

            db.SaveChanges();
            return row.MaLichSu;
        });

    // EndLogin: Ghi thời điểm đăng xuất của tài khoản.
    public void EndLogin(long id, int actor) =>
        NativeWrite(db =>
        {
            var r = db.LichSuDangNhap.SingleOrDefault(l =>
                l.MaLichSu == id && (actor > 0 ? l.MaKH == actor : l.MaNV == -actor)
            );
            if (r != null)
                r.ThoiGianDangXuat = DateTime.Now;
            return 0;
        });

    // ChangePassword: Xác minh mật khẩu cũ và lưu mật khẩu mới đã băm.
    public void ChangePassword(int actor, PasswordInput input) =>
        Write(
            d =>
            {
                Validator.ValidateObject(input, new ValidationContext(input), true);
                var c =
                    d.Customers.SingleOrDefault(c => c.Id == actor && c.Active)
                    ?? throw new InvalidOperationException("Tài khoản không còn hoạt động.");

                Require(
                    hasher.VerifyHashedPassword(c, c.PasswordHash, input.CurrentPassword)
                        != PasswordVerificationResult.Failed,
                    "Mật khẩu hiện tại không đúng."
                );

                Require(
                    hasher.VerifyHashedPassword(c, c.PasswordHash, input.NewPassword)
                        == PasswordVerificationResult.Failed,
                    "Mật khẩu mới phải khác mật khẩu hiện tại."
                );

                c.PasswordHash = hasher.HashPassword(c, input.NewPassword);
                return 0;
            },
            actor,
            "Đổi mật khẩu"
        );

    // HashLegacyPasswords: Chuyển mật khẩu cũ sang hash ASP.NET Identity.
    public int HashLegacyPasswords() =>
        NativeWrite(db =>
        {
            int count = 0;
            foreach (var c in db.KhachHang)
            {
                if (PasswordStorage.IsIdentityHash(c.MatKhau))
                    continue;
                c.MatKhau = hasher.HashPassword(new Customer(), c.MatKhau);
                count++;
            }

            foreach (var c in db.NhanVien)
            {
                if (PasswordStorage.IsIdentityHash(c.MatKhau))
                    continue;
                c.MatKhau = hasher.HashPassword(new Customer(), c.MatKhau);
                count++;
            }

            return count;
        });
}
