// VAI TRÒ FILE: CRUD trực tiếp qua DbSet: danh mục, biến thể, nhân viên, hồ sơ khách, đánh giá và
// tài khoản.
// NativeWrite dùng transaction/khóa; NativePermission kiểm tra người thao tác.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

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
    // HÀM CurrentAccount: Đọc tài khoản phục vụ xác thực cookie: id dương tra KhachHang, id âm tra
    // NhanVien với mã -id; trả null nếu không tồn tại. Nhân viên được đổi ChucVu sang Role bằng
    // StaffRole.
    // THAM SỐ id (int): id tài khoản có dấu: dương tra KhachHang, âm tra NhanVien bằng -id.
    // TRẢ VỀ (Customer?): Customer từ bảng khách/nhân viên theo id có dấu; null nếu không tìm thấy.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeRead; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public Customer? CurrentAccount(int id) =>
        NativeRead(db =>
        {
            if (id > 0)
            {
                // BIẾN c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; kết quả tra
                // cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
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

            // BIẾN s (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; kết quả
            // tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
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

    // HÀM NativeRead: Đọc trực tiếp các DbSet SQL, không qua StoreData; dùng NoTracking và
    // transaction Serializable. Callback phải lấy dữ liệu cần dùng trước khi DbContext bị dispose.
    // KIỂU T: kiểu kết quả do callback trả, được suy ra từ chỗ gọi; một hàm đọc/ghi có thể trả
    // entity, danh sách, bool hoặc số.
    // THAM SỐ read (Func<StoreDbContext, T>): hàm callback chỉ đọc, nhận StoreData hoặc DbContext
    // rồi trả kết quả kiểu T cho caller.
    // TRẢ VỀ (T): kết quả T do callback quyết định; chỉ trả sau khi transaction của hàm hoàn tất.
    public T NativeRead<T>(Func<StoreDbContext, T> read)
    {
        // BIẾN db (StoreDbContext): DbContext riêng cho lần đọc/ghi này; using bảo đảm giải phóng
        // kết nối khi kết thúc phạm vi.
        using var db = factory.CreateDbContext();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        // BIẾN tx (IDbContextTransaction): transaction SQL của lần xử lý; chỉ Commit sau khi mọi
        // kiểm tra và bước ghi cần thiết thành công.
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        // BIẾN r (T): kết quả kiểu T của callback read(db), được trả sau khi transaction đọc hoàn
        // tất.
        var r = read(db);
        // HOÀN TẤT GIAO DỊCH: chấp nhận toàn bộ thay đổi/ảnh đọc trong transaction; ngoại lệ trước
        // bước này không
        // được coi là thành công.
        tx.Commit();
        return r;
    }

    // HÀM NativeWrite: Ghi trực tiếp các entity SQL: mở transaction Serializable và khóa
    // QOANH.Store.Write, chạy callback, SaveChanges rồi Commit. Các hàm gọi phải tự kiểm tra quyền
    // và điều kiện nghiệp vụ.
    // KIỂU T: kiểu kết quả do callback trả, được suy ra từ chỗ gọi; một hàm đọc/ghi có thể trả
    // entity, danh sách, bool hoặc số.
    // THAM SỐ write (Func<StoreDbContext, T>): hàm callback thay đổi dữ liệu và trả T; transaction
    // bao ngoài chỉ hoàn tất sau SaveChanges/Commit.
    // TRẢ VỀ (T): kết quả T do callback quyết định; chỉ trả sau khi transaction của hàm hoàn tất.
    T NativeWrite<T>(Func<StoreDbContext, T> write)
    {
        // BIẾN db (StoreDbContext): DbContext riêng cho lần đọc/ghi này; using bảo đảm giải phóng
        // kết nối khi kết thúc phạm vi.
        using var db = factory.CreateDbContext();
        // BIẾN tx (IDbContextTransaction): transaction SQL của lần xử lý; chỉ Commit sau khi mọi
        // kiểm tra và bước ghi cần thiết thành công.
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        db.Database.ExecuteSqlRaw(
            (
                "DECLARE @r int; EXEC @r=sys.sp_getapplock "
                + "@Resource=N'QOANH.Store.Write',@LockMode='Exclusive',@Lo"
                + "ckOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW "
                + "51000,'Business lock timeout',1;"
            )
        );
        // BIẾN r (T): kết quả kiểu T của callback write(db), được trả sau SaveChanges và Commit.
        var r = write(db);
        // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
        // hoàn tất khi Commit
        // thành công.
        db.SaveChanges();
        // HOÀN TẤT GIAO DỊCH: chấp nhận toàn bộ thay đổi/ảnh đọc trong transaction; ngoại lệ trước
        // bước này không
        // được coi là thành công.
        tx.Commit();
        return r;
    }

    // HÀM NativePermission: Kiểm tra actor là nhân viên (id âm), còn hoạt động và có quyền Admin
    // hoặc một vai trò được truyền vào; không đạt thì chặn trước khi sửa SQL.
    // THAM SỐ db (StoreDbContext): DbContext đang dùng trong giao dịch; đọc DbSet để lấy dữ liệu,
    // sửa entity rồi SaveChanges để đẩy SQL.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ roles (string[]): các vai trò được phép thực hiện thao tác; Admin vẫn được chấp nhận
    // theo helper quyền.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA s (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static void NativePermission(StoreDbContext db, int actor, params string[] roles)
    {
        // ĐIỀU KIỆN: Cần tài khoản nhân viên. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(actor < 0, "Cần tài khoản nhân viên.");
        // BIẾN s (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; kết quả tra
        // cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var s = db.NhanVien.SingleOrDefault(s => s.MaNV == -actor && s.TrangThai);
        // ĐIỀU KIỆN: Không có quyền thực hiện thao tác này. Không đạt thì Require ném lỗi và dừng
        // luồng xử lý.
        Require(
            s != null && (StaffRole(s.ChucVu) == "Admin" || roles.Contains(StaffRole(s.ChucVu))),
            "Không có quyền thực hiện thao tác này."
        );
    }

    // HÀM References: Đọc danh mục theo kind: brands, sizes, colors, warehouses hoặc suppliers; đổi
    // các bảng tương ứng thành ReferenceInput. Kind ngoài danh sách bị từ chối.
    // THAM SỐ kind (string): loại danh mục: brands, sizes, colors, warehouses hoặc suppliers; quyết
    // định bảng SQL và nhóm quyền cần dùng.
    // TRẢ VỀ (List<ReferenceInput>): danh sách/truy vấn các phần tử: dữ liệu danh mục dùng chung;
    // Kind chọn loại, Id phân biệt thêm với sửa; có thể rỗng khi không có dữ liệu khớp.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA x (ThuongHieuRow): entity của bảng ThuongHieu, thương hiệu; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KichThuocRow): entity của bảng KichThuoc, danh sách size; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (MauSacRow): entity của bảng MauSac, tên và mã màu; tên này chỉ có nghĩa trong
    // lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KhoRow): entity của bảng Kho, thông tin kho; tên này chỉ có nghĩa trong lambda
    // tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung cấp hàng;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeRead; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
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

    // HÀM SaveReference: Thêm/sửa thương hiệu, size, màu, kho hoặc nhà cung cấp theo kind. Id = 0
    // tạo entity mới; Id khác 0 lấy entity hiện có. Kho/NCC cần quyền Inventory, các loại còn lại
    // cần Catalog; Admin được phép qua NativePermission.
    // THAM SỐ input (ReferenceInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA x (ThuongHieuRow): entity của bảng ThuongHieu, thương hiệu; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KichThuocRow): entity của bảng KichThuoc, danh sách size; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (MauSacRow): entity của bảng MauSac, tên và mã màu; tên này chỉ có nghĩa trong
    // lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KhoRow): entity của bảng Kho, thông tin kho; tên này chỉ có nghĩa trong lambda
    // tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung cấp hàng;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
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
                    // ĐIỀU KIỆN: Tên đã tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử lý.
                    Require(
                        !db.ThuongHieu.Any(x =>
                            x.MaThuongHieu != input.Id && x.TenThuongHieu == input.Name
                        ),
                        "Tên đã tồn tại."
                    );
                    // BIẾN b (ThuongHieuRow): entity của bảng ThuongHieu, thương hiệu.
                    var b =
                        input.Id == 0
                            ? new ThuongHieuRow()
                            : db.ThuongHieu.Single(x => x.MaThuongHieu == input.Id);
                    // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh
                    // còn lại xử lý bản ghi
                    // đã tồn tại nếu có.
                    if (input.Id == 0)
                        db.Add(b);
                    b.TenThuongHieu = input.Name;
                    b.MoTa = input.Description;
                    b.TrangThai = input.Active;
                    break;
                case "sizes":
                    // ĐIỀU KIỆN: Tên đã tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử lý.
                    Require(
                        !db.KichThuoc.Any(x =>
                            x.MaKichThuoc != input.Id && x.TenKichThuoc == input.Name
                        ),
                        "Tên đã tồn tại."
                    );
                    // BIẾN s (KichThuocRow): entity của bảng KichThuoc, danh sách size.
                    var s =
                        input.Id == 0
                            ? new KichThuocRow()
                            : db.KichThuoc.Single(x => x.MaKichThuoc == input.Id);
                    // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh
                    // còn lại xử lý bản ghi
                    // đã tồn tại nếu có.
                    if (input.Id == 0)
                        db.Add(s);
                    s.TenKichThuoc = input.Name;
                    break;
                case "colors":
                    // ĐIỀU KIỆN: Tên đã tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử lý.
                    Require(
                        !db.MauSac.Any(x => x.MaMauSac != input.Id && x.TenMauSac == input.Name),
                        "Tên đã tồn tại."
                    );
                    // ĐIỀU KIỆN: Mã màu tối đa 20 ký tự. Không đạt thì Require ném lỗi và dừng
                    // luồng xử lý.
                    Require((input.Description?.Length ?? 0) <= 20, "Mã màu tối đa 20 ký tự.");
                    // BIẾN c (MauSacRow): entity của bảng MauSac, tên và mã màu.
                    var c =
                        input.Id == 0
                            ? new MauSacRow()
                            : db.MauSac.Single(x => x.MaMauSac == input.Id);
                    // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh
                    // còn lại xử lý bản ghi
                    // đã tồn tại nếu có.
                    if (input.Id == 0)
                        db.Add(c);
                    c.TenMauSac = input.Name;
                    c.MaMau = input.Description;
                    break;
                case "warehouses":
                    // BIẾN w (KhoRow): entity của bảng Kho, thông tin kho.
                    var w = input.Id == 0 ? new KhoRow() : db.Kho.Single(x => x.MaKho == input.Id);
                    // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh
                    // còn lại xử lý bản ghi
                    // đã tồn tại nếu có.
                    if (input.Id == 0)
                        db.Add(w);
                    w.TenKho = input.Name;
                    w.DiaChi = input.Address;
                    w.TrangThai = input.Active;
                    break;
                case "suppliers":
                    // BIẾN n (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung
                    // cấp hàng.
                    var n =
                        input.Id == 0
                            ? new NhaCungCapRow()
                            : db.NhaCungCap.Single(x => x.MaNCC == input.Id);
                    // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh
                    // còn lại xử lý bản ghi
                    // đã tồn tại nếu có.
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

    // HÀM SaveVariant: Thêm/sửa biến thể sản phẩm: kiểm tra quyền Catalog, sản phẩm/size/màu tồn
    // tại và SKU/tổ hợp không trùng. Id = 0 thêm mới; khi sửa không được đổi sản phẩm, size, màu để
    // giữ lịch sử; chỉ sửa SKU, giá riêng và trạng thái.
    // THAM SỐ input (VariantInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA p (SanPhamRow): entity của bảng SanPham, thông tin sản phẩm gốc; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (KichThuocRow): entity của bảng KichThuoc, danh sách size; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (MauSacRow): entity của bảng MauSac, tên và mã màu; tên này chỉ có nghĩa trong
    // lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
    // và SKU; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void SaveVariant(VariantInput input, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Catalog");
            Validator.ValidateObject(input, new ValidationContext(input), true);
            // ĐIỀU KIỆN: Sản phẩm/size/màu không tồn tại. Không đạt thì Require ném lỗi và dừng
            // luồng xử lý.
            Require(
                db.SanPham.Any(p => p.MaSP == input.ProductId)
                    && db.KichThuoc.Any(s => s.MaKichThuoc == input.SizeId)
                    && db.MauSac.Any(s => s.MaMauSac == input.ColorId),
                "Sản phẩm/size/màu không tồn tại."
            );
            // ĐIỀU KIỆN: SKU hoặc tổ hợp sản phẩm/size/màu đã tồn tại. Không đạt thì Require ném
            // lỗi và dừng luồng xử lý.
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
            // BIẾN r (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
            // và SKU.
            var r =
                input.Id == 0
                    ? new BienTheSanPhamRow()
                    : db.BienTheSanPham.Single(v => v.MaBienThe == input.Id);
            // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh còn lại xử
            // lý bản ghi đã tồn
            // tại nếu có.
            if (input.Id == 0)
                db.Add(r);
            // NHÁNH SỬA: Id đã có; kiểm tra dữ liệu hiện tại trước khi cho thay đổi để giữ quan hệ
            // và lịch sử.
            if (input.Id != 0)
                // ĐIỀU KIỆN: Không thay định danh biến thể đã tạo; hãy thêm biến thể mới để giữ
                // lịch sử. Không đạt thì
                // Require ném lỗi và dừng luồng ghi.
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

    // HÀM Staff: Lấy danh sách tài khoản nhân viên từ StoreData theo quy ước Id âm; dữ liệu này
    // được Load từ bảng NhanVien.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (List<Customer>): danh sách/truy vấn các phần tử: tài khoản: Id dương là khách, Id âm
    // là nhân viên; có tên, email, vai trò và trạng thái; có thể rỗng khi không có dữ liệu khớp.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Read; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public List<Customer> Staff() => Read(d => d.Customers.Where(c => c.Id < 0).ToList());

    // HÀM SaveStaff: Admin thêm/sửa nhân viên và quyền: email không trùng khách hoặc nhân viên
    // khác; không tự khóa/hạ quyền Admin đang dùng, phải còn ít nhất một Admin hoạt động. Mật khẩu
    // mới phải có 8–100 ký tự, chữ và số, rồi được băm trước khi lưu.
    // THAM SỐ input (StaffInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void SaveStaff(StaffInput input, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor);
            Validator.ValidateObject(input, new ValidationContext(input), true);
            // ĐIỀU KIỆN: Vai trò không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(
                new[] { "Admin", "Catalog", "Inventory", "Sales", "Support" }.Contains(input.Role),
                "Vai trò không hợp lệ."
            );
            // BIẾN email (string): email đã Trim và chuẩn hóa chữ thường để so sánh/ghi nhận nhất
            // quán.
            string email = input.Email.Trim().ToLowerInvariant();
            // ĐIỀU KIỆN: Email đã sử dụng. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(
                !db.KhachHang.Any(c => c.Email == email)
                    && !db.NhanVien.Any(c => c.MaNV != input.Id && c.Email == email),
                "Email đã sử dụng."
            );
            if (input.Id == -actor)
                // ĐIỀU KIỆN: Không tự khóa hoặc hạ quyền tài khoản quản trị đang dùng. Không đạt
                // thì Require ném lỗi và
                // dừng luồng ghi.
                Require(
                    input.Active && input.Role == "Admin",
                    "Không tự khóa hoặc hạ quyền tài khoản quản trị đang dùng."
                );
            // BIẾN r (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ.
            var r =
                input.Id == 0
                    ? new NhanVienRow { NgayVaoLam = DateTime.Today }
                    : db.NhanVien.Single(x => x.MaNV == input.Id);
            // NHÁNH THÊM: Id = 0 biểu thị form chưa có bản ghi; tạo mã/entity mới. Nhánh còn lại xử
            // lý bản ghi đã tồn
            // tại nếu có.
            if (input.Id == 0)
                db.Add(r);
            if (
                StaffRole(r.ChucVu) == "Admin"
                && r.TrangThai
                && (!input.Active || input.Role != "Admin")
            )
                // ĐIỀU KIỆN: Phải còn ít nhất một Admin hoạt động. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
                Require(
                    db.NhanVien.Any(x => x.MaNV != r.MaNV && x.TrangThai && x.ChucVu == "Admin"),
                    "Phải còn ít nhất một Admin hoạt động."
                );
            if (!string.IsNullOrEmpty(input.Password))
            {
                // ĐIỀU KIỆN: Mật khẩu cần 8–100 ký tự, có chữ và số. Không đạt thì Require ném lỗi
                // và dừng luồng xử lý.
                Require(
                    System.Text.RegularExpressions.Regex.IsMatch(
                        input.Password,
                        @"^(?=.{8,100}$)(?=.*\p{L})(?=.*\d).+$"
                    ),
                    "Mật khẩu cần 8–100 ký tự, có chữ và số."
                );
                r.MatKhau = hasher.HashPassword(new Customer(), input.Password);
            }

            // ĐIỀU KIỆN: Nhập mật khẩu cho nhân viên mới. Không đạt thì Require ném lỗi và dừng

            // luồng xử lý.
            Require(r.MatKhau != "", "Nhập mật khẩu cho nhân viên mới.");
            r.HoTen = input.Name.Trim();
            r.Email = email;
            r.SoDienThoai = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
            r.ChucVu = input.Role;
            r.TrangThai = input.Active;
            return 0;
        });

    // HÀM UpdateCustomer: Nhân viên Support/Admin sửa tên, điện thoại, địa chỉ và trạng thái của
    // tài khoản có Role Customer; không sửa email, vai trò hay mật khẩu tại đây. Validate toàn bộ
    // dữ liệu diễn ra trong Write.
    // THAM SỐ id (int): mã bản ghi cần xử lý, được đối chiếu với khóa tương ứng trong thân hàm.
    // THAM SỐ input (ProfileInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ active (bool): trạng thái mới: true để hoạt động/hiển thị, false để khóa hoặc ẩn theo
    // đối tượng của hàm.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void UpdateCustomer(int id, ProfileInput input, bool active, int actor) =>
        Write(
            d =>
            {
                Permission(d, actor, "Support");
                Validator.ValidateObject(input, new ValidationContext(input), true);
                // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên,
                // email, vai trò và trạng thái; kết quả tra cứu có thể null khi không khớp, xem
                // nhánh kiểm tra hoặc ?? ngay sau.
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

    // HÀM Reviews: Đọc đánh giá kèm tên khách và danh sách mã ảnh/video. product lọc theo sản phẩm;
    // includeHidden = false chỉ lấy đánh giá đang hiển thị, true dùng cho màn hình kiểm duyệt.
    // THAM SỐ product (int?): mã sản phẩm dùng lọc đánh giá; null nghĩa lấy đánh giá mọi sản phẩm
    // theo chế độ hiển thị. Mặc định khi không truyền: null.
    // THAM SỐ includeHidden (bool): true lấy cả đánh giá ẩn cho quản trị; false chỉ lấy đánh giá
    // đang hiển thị. Mặc định khi không truyền: false.
    // TRẢ VỀ (List<ReviewItem>): danh sách/truy vấn các phần tử: đánh giá đã ghép tên khách và
    // thông tin media để hiển thị hoặc kiểm duyệt; có thể rỗng khi không có dữ liệu khớp.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA r (ReviewItem): đánh giá đã ghép tên khách và thông tin media để hiển thị hoặc
    // kiểm duyệt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA m (ReviewMedia): tệp đánh giá đã xác định loại nội dung, chứa mã Guid, ReviewId
    // và byte tệp; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA m (<anonymous type: Guid Id, int ReviewId, string ContentType>): đối tượng hoặc
    // danh sách tạm chỉ chứa các trường được liệt kê trong new { ... } của phép chiếu; tên này chỉ
    // có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeRead; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public List<ReviewItem> Reviews(int? product = null, bool includeHidden = false) =>
        NativeRead(db =>
        {
            // BIẾN reviews (List<ReviewItem>): danh sách/truy vấn các phần tử: đánh giá đã ghép tên
            // khách và thông tin media để hiển thị hoặc kiểm duyệt; ToList lấy kết quả ra danh sách
            // để dùng tiếp trong phạm vi này.
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
            // BIẾN ids (List<int>): các mã đánh giá vừa chọn, dùng truy vấn đúng media thuộc những
            // đánh giá này.
            var ids = reviews.Select(r => r.Id).ToList();
            // BIẾN media (kiểu tạm suy ra từ new { ... }): thông tin nhẹ Id/ReviewId/ContentType
            // của tệp, không lấy toàn bộ byte khi chỉ hiển thị danh sách.
            var media = db
                .ReviewMedia.Where(m => ids.Contains(m.ReviewId))
                .Select(m => new
                {
                    m.Id,
                    m.ReviewId,
                    m.ContentType,
                })
                .ToList();
            // BIẾN LẶP r (ReviewItem): đánh giá đã ghép tên khách và thông tin media để hiển thị
            // hoặc kiểm duyệt; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
            // foreach này.
            foreach (var r in reviews)
                r.Media = media
                    .Where(m => m.ReviewId == r.Id)
                    .Select(m => new ReviewMediaInfo(m.Id, m.ContentType.StartsWith("video/")))
                    .ToList();
            return reviews;
        });

    // HÀM Review: Thêm đánh giá sau mua: khách đang hoạt động, 1–5 sao, nội dung 5–1.000 ký tự, đã
    // mua sản phẩm trong đơn đã giao/hoàn thành và chưa từng đánh giá sản phẩm đó. Kiểm tra tệp,
    // lưu đánh giá để lấy khóa rồi gắn media trong cùng NativeWrite.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ productId (int): mã sản phẩm cần tra, đánh giá hoặc liên kết với biến thể/dòng hàng.
    // THAM SỐ stars (int): số sao đánh giá, bắt buộc trong khoảng 1 đến 5.
    // THAM SỐ text (string): nội dung đánh giá sau Trim, cần 5–1.000 ký tự.
    // THAM SỐ uploads (IReadOnlyList<ReviewUpload>?): danh sách tên tệp và byte của tệp đánh giá
    // trước khi xác minh chữ ký và giới hạn dung lượng. Mặc định khi không truyền: null.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (DanhGiaRow): entity của bảng DanhGia, nội dung/sao/trạng thái đánh giá; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void Review(
        int customerId,
        int productId,
        int stars,
        string text,
        IReadOnlyList<ReviewUpload>? uploads = null
    ) =>
        NativeWrite(db =>
        {
            // ĐIỀU KIỆN: Chỉ khách hàng được đánh giá. Không đạt thì Require ném lỗi và dừng luồng
            // xử lý.
            Require(
                customerId > 0 && db.KhachHang.Any(c => c.MaKH == customerId && c.TrangThai),
                "Chỉ khách hàng được đánh giá."
            );
            // ĐIỀU KIỆN: Số sao từ 1 đến 5. Không đạt thì Require ném lỗi và dừng luồng xử lý.
            Require(stars is >= 1 and <= 5, "Số sao từ 1 đến 5.");
            text = Text(text, 5, 1000, "Nội dung");
            // ĐIỀU KIỆN: Chỉ đánh giá sản phẩm đã mua và đã giao. Không đạt thì Require ném lỗi và
            // dừng luồng xử lý.
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
            // ĐIỀU KIỆN: Bạn đã đánh giá sản phẩm này. Không đạt thì Require ném lỗi và dừng luồng
            // xử lý.
            Require(
                !db.DanhGia.Any(r => r.MaKH == customerId && r.MaSP == productId),
                "Bạn đã đánh giá sản phẩm này."
            );
            // BIẾN attachments (List<ReviewMedia>): các ReviewMedia đã vượt kiểm tra đuôi/chữ
            // ký/dung lượng, chuẩn bị gắn với mã đánh giá.
            var attachments = ReviewUploads.Validate(uploads);
            // BIẾN review (DanhGiaRow): entity của bảng DanhGia, nội dung/sao/trạng thái đánh giá;
            // được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa SQL đã commit.
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
            // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
            // hoàn tất khi Commit
            // thành công.
            db.SaveChanges();
            // BIẾN LẶP media (ReviewMedia): tệp đánh giá đã xác định loại nội dung, chứa mã Guid,
            // ReviewId và byte tệp; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
            // foreach này.
            foreach (var media in attachments)
            {
                media.ReviewId = review.MaDanhGia;
                db.ReviewMedia.Add(media);
            }

            return 0;
        });

    // HÀM ModerateReview: Support/Admin ẩn hoặc hiện đánh giá bằng TrangThai = active; không xóa
    // bản ghi hay tệp đính kèm. Shop/ReviewMedia kiểm tra trạng thái này khi phục vụ nội dung.
    // THAM SỐ id (int): mã bản ghi cần xử lý, được đối chiếu với khóa tương ứng trong thân hàm.
    // THAM SỐ active (bool): trạng thái mới: true để hoạt động/hiển thị, false để khóa hoặc ẩn theo
    // đối tượng của hàm.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA r (DanhGiaRow): entity của bảng DanhGia, nội dung/sao/trạng thái đánh giá; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void ModerateReview(int id, bool active, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Support");
            // BIẾN r (DanhGiaRow): entity của bảng DanhGia, nội dung/sao/trạng thái đánh giá; kết
            // quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var r =
                db.DanhGia.SingleOrDefault(r => r.MaDanhGia == id)
                ?? throw new InvalidOperationException("Đánh giá không tồn tại.");
            r.TrangThai = active;
            return 0;
        });

    // HÀM LogLogin: Thêm nhật ký đăng nhập gồm khách/nhân viên, thời gian, IP và thiết bị đã cắt
    // theo giới hạn cột. SaveChanges lấy mã lịch sử để đưa vào claim login_history.
    // THAM SỐ c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai
    // trò và trạng thái; đối tượng đầu vào của hàm, không phải một ký tự viết tắt cố định cho mọi
    // file.
    // THAM SỐ success (bool): true ghi lần đăng nhập thành công, false ghi thất bại; không phải
    // trạng thái hoạt động của tài khoản.
    // THAM SỐ ip (string?): IP của yêu cầu đăng nhập, được cắt theo giới hạn cột trước lưu lịch sử.
    // THAM SỐ device (string?): chuỗi User-Agent mô tả trình duyệt/thiết bị, được cắt độ dài trước
    // lưu.
    // TRẢ VỀ (long): mã lịch sử đăng nhập vừa được SQL cấp, để gắn vào claim login_history.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public long LogLogin(Customer c, bool success, string? ip, string? device) =>
        NativeWrite(db =>
        {
            // BIẾN row (LichSuDangNhapRow): entity của bảng LichSuDangNhap, nhật ký đăng nhập/đăng
            // xuất; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa SQL đã commit.
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
            // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
            // hoàn tất khi Commit
            // thành công.
            db.SaveChanges();
            return row.MaLichSu;
        });

    // HÀM EndLogin: Ghi thời điểm đăng xuất cho bản ghi lịch sử thuộc đúng actor; không cập nhật
    // nhật ký của tài khoản khác.
    // THAM SỐ id (long): mã bản ghi LichSuDangNhap của phiên cần ghi thời gian đăng xuất.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // BIẾN LAMBDA l (LichSuDangNhapRow): entity của bảng LichSuDangNhap, nhật ký đăng nhập/đăng
    // xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void EndLogin(long id, int actor) =>
        NativeWrite(db =>
        {
            // BIẾN r (LichSuDangNhapRow): entity của bảng LichSuDangNhap, nhật ký đăng nhập/đăng
            // xuất; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay
            // sau.
            var r = db.LichSuDangNhap.SingleOrDefault(l =>
                l.MaLichSu == id && (actor > 0 ? l.MaKH == actor : l.MaNV == -actor)
            );
            if (r != null)
                r.ThoiGianDangXuat = DateTime.Now;
            return 0;
        });

    // HÀM ChangePassword: Kiểm tra mẫu đổi mật khẩu, xác minh mật khẩu hiện tại của tài khoản đang
    // hoạt động, rồi băm mật khẩu mới trong Write. Security stamp từ hash khiến cookie cũ bị từ
    // chối khi được kiểm tra tiếp.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ input (PasswordInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void ChangePassword(int actor, PasswordInput input) =>
        Write(
            d =>
            {
                Validator.ValidateObject(input, new ValidationContext(input), true);
                // BIẾN c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên,
                // email, vai trò và trạng thái.
                var c = d.Customers.Single(c => c.Id == actor && c.Active);
                // ĐIỀU KIỆN: Mật khẩu hiện tại không đúng. Không đạt thì Require ném lỗi và dừng
                // luồng xử lý.
                Require(
                    hasher.VerifyHashedPassword(c, c.PasswordHash, input.CurrentPassword)
                        != PasswordVerificationResult.Failed,
                    "Mật khẩu hiện tại không đúng."
                );
                c.PasswordHash = hasher.HashPassword(c, input.NewPassword);
                return 0;
            },
            actor,
            "Đổi mật khẩu"
        );

    // HÀM HashLegacyPasswords: Lệnh quản trị chuyển mật khẩu cũ trong KhachHang/NhanVien sang
    // Identity hash; bỏ qua bản ghi đã đúng định dạng. Chỉ được gọi khi chủ động chạy ứng dụng với
    // --hash-passwords.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (int): số tài khoản vừa được chuyển sang Identity hash, không tính bản ghi đã có hash
    // hợp lệ.
    // BIẾN LAMBDA db (StoreDbContext): phiên làm việc EF Core với database; cung cấp DbSet để truy
    // vấn và theo dõi thay đổi; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn
    // cục.
    // LIÊN KẾT XỬ LÝ: NativeWrite; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public int HashLegacyPasswords() =>
        NativeWrite(db =>
        {
            // BIẾN count (int): số bản ghi khách/nhân viên đã được chuyển từ mật khẩu cũ sang hash
            // Identity trong lần chạy.
            int count = 0;
            // BIẾN LẶP c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; mỗi lượt
            // nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
            foreach (var c in db.KhachHang)
            {
                if (PasswordStorage.IsIdentityHash(c.MatKhau))
                    continue;
                c.MatKhau = hasher.HashPassword(new Customer(), c.MatKhau);
                count++;
            }

            // BIẾN LẶP c (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; mỗi
            // lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
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
