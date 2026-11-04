// VAI TRÒ FILE: Cầu nối hai mô hình: Load đọc bảng SQL gốc thành StoreData; PersistNative ghi ngược
// lại. Tra
// Data/Native/Mapping.cs để biết tên bảng và khóa thật.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // HÀM StaffRole: Đổi chức vụ SQL sang vai trò ứng dụng theo switch bên dưới; các nhánh tương
    // thích xử lý cả tên chức vụ tiếng Việt của dữ liệu cũ.
    // THAM SỐ title (string): chức vụ SQL cần đổi sang vai trò ứng dụng.
    // TRẢ VỀ (string): chuỗi ký tự; ý nghĩa được xác định bởi biểu thức return bên dưới.
    static string StaffRole(string title) =>
        title switch
        {
            "Admin" => "Admin",
            "Catalog" => "Catalog",
            "Inventory" => "Inventory",
            "Sales" => "Sales",
            "Support" => "Support",
            _ when title.Contains("kho", StringComparison.OrdinalIgnoreCase) => "Inventory",
            _ when title.Contains("sản phẩm", StringComparison.OrdinalIgnoreCase) => "Catalog",
            _ when title.Contains("đơn", StringComparison.OrdinalIgnoreCase)
                    || title.Contains("bán", StringComparison.OrdinalIgnoreCase) => "Sales",
            _ => "Support",
        };

    // HÀM PublicId: Lấy QPublicId của đơn; nếu dữ liệu cũ chưa có mã công khai thì tạo mã hiển thị
    // QH-SQL-{MaDonHang} để vẫn truy cập được đơn.
    // THAM SỐ o (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL; đối tượng đầu vào của hàm,
    // không phải một ký tự viết tắt cố định cho mọi file.
    // TRẢ VỀ (string): chuỗi ký tự; ý nghĩa được xác định bởi biểu thức return bên dưới.
    static string PublicId(DonHangRow o) => o.QPublicId ?? "QH-SQL-" + o.MaDonHang;

    // HÀM Load: Bộ chuyển đổi SQL → StoreData: đọc các bảng gốc, ghép sản phẩm/biến
    // thể/tồn/đơn/thanh toán/trả hàng và cấu hình cho controller/view hiện có. Hàm này dựng dữ liệu
    // đọc, không phải nơi xử lý form thêm mới.
    // THAM SỐ db (StoreDbContext): DbContext đang dùng trong giao dịch; đọc DbSet để lấy dữ liệu,
    // sửa entity rồi SaveChanges để đẩy SQL.
    // TRẢ VỀ (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình; không
    // phải một bảng SQL riêng; ý nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA c (DanhMucRow): entity của bảng DanhMuc, danh mục sản phẩm; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA w (KhoRow): entity của bảng Kho, thông tin kho; tên này chỉ có nghĩa trong lambda
    // tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (TonKhoRow): entity của bảng TonKho, tồn của từng biến thể tại từng kho; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
    // và SKU; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (KichThuocRow): entity của bảng KichThuoc, danh sách size; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (MauSacRow): entity của bảng MauSac, tên và mã màu; tên này chỉ có nghĩa trong
    // lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung cấp hàng;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (ThanhToanRow): entity của bảng ThanhToan, thông tin phương thức/trạng thái
    // thanh toán gốc; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (PhieuTraHangRow): entity của bảng PhieuTraHang, đầu phiếu trả; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (SanPhamRow): entity của bảng SanPham, thông tin sản phẩm gốc; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA i (HinhAnhSanPhamRow): entity của bảng HinhAnhSanPham, ảnh gắn với sản phẩm; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (KhuyenMaiRow): entity của bảng KhuyenMai, cấu hình ưu đãi; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ChiTietKhuyenMaiRow): entity của bảng ChiTietKhuyenMai, liên kết ưu đãi với
    // sản phẩm; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA o (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ChiTietDonHangRow): entity của bảng ChiTietDonHang, các dòng hàng đã mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ChiTietTraHangRow): entity của bảng ChiTietTraHang, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA r (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn và tình
    // trạng hàng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA h (LichSuTrangThaiDonHangRow): entity của bảng LichSuTrangThaiDonHang, các mốc
    // đổi trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    StoreData Load(StoreDbContext db)
    {
        // BIẾN cats (List<DanhMucRow>): các dòng DanhMuc từ SQL, được đổi thành danh sách tên danh
        // mục trong StoreData.
        var cats = db.DanhMuc.ToList();
        // BIẾN customers (List<KhachHangRow>): danh sách/truy vấn các phần tử: entity của bảng
        // KhachHang, dữ liệu khách hàng; ToList lấy kết quả ra danh sách để dùng tiếp trong phạm vi
        // này.
        var customers = db.KhachHang.ToList();
        // BIẾN staff (List<NhanVienRow>): cờ cho phép xem media đánh giá bị ẩn vì người dùng là
        // Admin hoặc Support.
        var staff = db.NhanVien.ToList();
        // BIẾN products (List<SanPhamRow>): các hàng SanPham gốc từ SQL để ánh xạ sang Product cho
        // controller/view.
        var products = db.SanPham.ToList();
        // BIẾN sizes (List<KichThuocRow>): danh sách/truy vấn các phần tử: entity của bảng
        // KichThuoc, danh sách size; ToList lấy kết quả ra danh sách để dùng tiếp trong phạm vi
        // này.
        var sizes = db.KichThuoc.ToList();
        // BIẾN colors (List<MauSacRow>): danh sách/truy vấn các phần tử: entity của bảng MauSac,
        // tên và mã màu; ToList lấy kết quả ra danh sách để dùng tiếp trong phạm vi này.
        var colors = db.MauSac.ToList();
        // BIẾN variants (List<BienTheSanPhamRow>): danh sách/truy vấn các phần tử: entity của bảng
        // BienTheSanPham, tổ hợp sản phẩm/size/màu và SKU; ToList lấy kết quả ra danh sách để dùng
        // tiếp trong phạm vi này.
        var variants = db.BienTheSanPham.ToList();
        // BIẾN images (List<HinhAnhSanPhamRow>): danh sách/truy vấn các phần tử: entity của bảng
        // HinhAnhSanPham, ảnh gắn với sản phẩm; ToList lấy kết quả ra danh sách để dùng tiếp trong
        // phạm vi này.
        var images = db.HinhAnhSanPham.ToList();
        // BIẾN warehouses (List<KhoRow>): danh sách/truy vấn các phần tử: entity của bảng Kho,
        // thông tin kho; ToList lấy kết quả ra danh sách để dùng tiếp trong phạm vi này.
        var warehouses = db.Kho.ToList();
        // BIẾN stock (List<TonKhoRow>): các hàng TonKho theo biến thể/kho, dùng ghép tồn chi tiết
        // và tổng sản phẩm.
        var stock = db.TonKho.ToList();
        // BIẾN promos (List<KhuyenMaiRow>): danh sách/truy vấn các phần tử: entity của bảng
        // KhuyenMai, cấu hình ưu đãi; ToList lấy kết quả ra danh sách để dùng tiếp trong phạm vi
        // này.
        var promos = db.KhuyenMai.ToList();
        // BIẾN promoItems (List<ChiTietKhuyenMaiRow>): các liên kết KhuyenMai–SanPham từ SQL để xác
        // định ưu đãi áp dụng cho sản phẩm nào.
        var promoItems = db.ChiTietKhuyenMai.ToList();
        // BIẾN orders (List<DonHangRow>): danh sách/truy vấn các phần tử: entity của bảng DonHang,
        // đầu đơn hàng SQL; ToList lấy kết quả ra danh sách để dùng tiếp trong phạm vi này.
        var orders = db.DonHang.ToList();
        // BIẾN lines (List<ChiTietDonHangRow>): các ChiTietDonHang SQL, được ghép vào đúng đơn theo
        // khóa MaDonHang.
        var lines = db.ChiTietDonHang.ToList();
        // BIẾN payments (List<ThanhToanRow>): danh sách/truy vấn các phần tử: entity của bảng
        // ThanhToan, thông tin phương thức/trạng thái thanh toán gốc; ToList lấy kết quả ra danh
        // sách để dùng tiếp trong phạm vi này.
        var payments = db.ThanhToan.ToList();
        // BIẾN returns (List<PhieuTraHangRow>): danh sách/truy vấn các phần tử: entity của bảng
        // PhieuTraHang, đầu phiếu trả; ToList lấy kết quả ra danh sách để dùng tiếp trong phạm vi
        // này.
        var returns = db.PhieuTraHang.ToList();
        // BIẾN returnLines (List<ChiTietTraHangRow>): chi tiết SQL của các phiếu trả, dùng ghép
        // lượng và tiền hoàn vào ReturnRecord.
        var returnLines = db.ChiTietTraHang.ToList();
        // BIẾN history (List<LichSuTrangThaiDonHangRow>): danh sách/truy vấn các phần tử: entity
        // của bảng LichSuTrangThaiDonHang, các mốc đổi trạng thái; ToList lấy kết quả ra danh sách
        // để dùng tiếp trong phạm vi này.
        var history = db.LichSuTrangThaiDonHang.ToList();
        // BIẾN config (StoreConfiguration): bản ghi cấu hình SQL hiện có; thiếu thì Load dùng cấu
        // hình mặc định theo nhánh khởi tạo.
        var config = db.Configuration.SingleOrDefault();
        // BIẾN suppliers (List<NhaCungCapRow>): danh sách/truy vấn các phần tử: entity của bảng
        // NhaCungCap, thông tin đối tác cung cấp hàng; ToList lấy kết quả ra danh sách để dùng tiếp
        // trong phạm vi này.
        var suppliers = db.NhaCungCap.ToList();
        db.PhieuNhap.Load();
        db.ChiTietPhieuNhap.Load();
        db.GioHang.Load();
        db.ChiTietGioHang.Load();
        // BIẾN d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa
        // SQL đã commit.
        var d = new StoreData
        {
            Shipments = db.Shipments.ToList(),
            Categories = cats.Where(c => c.TrangThai).Select(c => c.TenDanhMuc).ToList(),
            Settings = config?.Settings ?? new(),
            ImportedFileHash = config?.ImportedFileHash,
            StockMovements = db.StockMovements.ToList(),
            Payments = db.Payments.ToList(),
            Warehouses = warehouses
                .Select(w => new Warehouse
                {
                    Id = w.MaKho,
                    Name = w.TenKho,
                    Active = w.TrangThai,
                })
                .ToList(),
            Stocks = stock
                .Select(s => new WarehouseStock
                {
                    WarehouseId = s.MaKho,
                    VariantId = s.MaBienThe,
                    Quantity = s.SoLuongTon,
                })
                .ToList(),
        };
        d.Variants = variants
            .Select(v => new Variant
            {
                Id = v.MaBienThe,
                ProductId = v.MaSP,
                Size = sizes.Single(s => s.MaKichThuoc == v.MaKichThuoc).TenKichThuoc,
                Color = colors.Single(c => c.MaMauSac == v.MaMauSac).TenMauSac,
                Sku = v.SKU,
                Price = v.GiaBanRieng,
                Active = v.TrangThai,
                Stock = stock
                    .Where(s =>
                        s.MaBienThe == v.MaBienThe
                        && warehouses.Any(w => w.MaKho == s.MaKho && w.TrangThai)
                    )
                    .Sum(s => s.SoLuongTon),
            })
            .ToList();
        d.Suppliers = suppliers
            .Select(s => new Supplier
            {
                Id = s.MaNCC,
                Name = s.TenNCC,
                Active = s.TrangThai,
            })
            .ToList();
        d.PaymentReferences = payments
            .Where(p => p.MaGiaoDich != null)
            .Select(p => p.MaGiaoDich!)
            .Concat(returns.Where(r => r.QReference != null).Select(r => r.QReference!))
            .ToList();
        d.Products = products
            .Select(p => new Product
            {
                Id = p.MaSP,
                LegacyId = p.QLegacyId,
                Name = p.TenSP,
                Category = cats.Single(c => c.MaDanhMuc == p.MaDanhMuc).TenDanhMuc,
                BrandId = p.MaThuongHieu,
                Price = p.GiaBan,
                Description = p.MoTa ?? "",
                Active = p.TrangThai,
                IsNew = p.QIsNew,
                OnSale = p.QOnSale,
                Image = SafeImage(
                    p.QImage
                        ?? images.FirstOrDefault(i => i.MaSP == p.MaSP && i.AnhChinh)?.DuongDan,
                    p.MaSP
                ),
                Color = d.Variants.FirstOrDefault(v => v.ProductId == p.MaSP)?.Color ?? "Trắng",
                Variants = d.Variants.Where(v => v.ProductId == p.MaSP).ToList(),
                Stock = stock
                    .Where(s => variants.Any(v => v.MaSP == p.MaSP && v.MaBienThe == s.MaBienThe))
                    .Sum(s => s.SoLuongTon),
            })
            .ToList();
        d.Customers = customers
            .Select(c => new Customer
            {
                Id = c.MaKH,
                LegacyId = c.QLegacyId,
                Name = c.HoTen,
                Email = c.Email,
                PasswordHash = c.MatKhau,
                Phone = c.SoDienThoai ?? "",
                Address = c.DiaChi ?? "",
                Active = c.TrangThai,
            })
            .Concat(
                staff.Select(c => new Customer
                {
                    Id = -c.MaNV,
                    LegacyId = c.QLegacyId,
                    Name = c.HoTen,
                    Email = c.Email,
                    PasswordHash = c.MatKhau,
                    Phone = c.SoDienThoai ?? "",
                    Address = c.DiaChi ?? "",
                    Active = c.TrangThai,
                    Role = StaffRole(c.ChucVu),
                })
            )
            .ToList();
        d.Promotions = promos
            .Select(p => new Promotion
            {
                Id = p.MaKM,
                Code = p.MaCode ?? "",
                Name = p.TenKhuyenMai,
                Kind = p.LoaiGiam,
                Value = p.GiaTriGiam,
                Percent = p.LoaiGiam == "PHANTRAM" ? (int)p.GiaTriGiam : 10,
                Minimum = p.DonHangToiThieu,
                Maximum = p.GiamToiDa,
                Remaining = p.SoLuong,
                From = p.NgayBatDau,
                Until = p.NgayKetThuc,
                Active = p.TrangThai,
                ProductIds = promoItems.Where(x => x.MaKM == p.MaKM).Select(x => x.MaSP).ToList(),
            })
            .ToList();
        d.Orders = orders
            .Select(o => new Order
            {
                DatabaseId = o.MaDonHang,
                Id = PublicId(o),
                BankQrImage = o.QBankQrImage,
                TransferReference = o.QTransferReference,
                CustomerId = o.MaKH,
                Created = o.NgayDat,
                Name = o.HoTenNguoiNhan,
                Phone = o.SoDienThoaiNhan,
                Address = o.DiaChiGiaoHang,
                Note = o.GhiChu ?? "",
                Status = o.TrangThai,
                ShippingSnapshot = o.PhiVanChuyen,
                Discount = o.TienGiam,
                PromotionCode = promos.FirstOrDefault(p => p.MaKM == o.MaKM)?.MaCode ?? "",
                CheckoutToken = o.QCheckoutToken,
                Payment =
                    payments.FirstOrDefault(p => p.MaDonHang == o.MaDonHang)?.PhuongThuc ?? "COD",
                PaidAmount = payments
                    .Where(p =>
                        p.MaDonHang == o.MaDonHang
                        && (p.TrangThai == "Đã thanh toán" || p.TrangThai == "Đã hoàn tiền")
                    )
                    .Sum(p => p.SoTien),
                RefundedAmount = returns
                    .Where(r =>
                        r.MaDonHang == o.MaDonHang
                        && (r.TrangThai == "Đã hoàn tiền" || r.TrangThai == "Hoàn tất")
                    )
                    .Sum(r => r.TongTienHoan),
                Lines = lines
                    .Where(l => l.MaDonHang == o.MaDonHang)
                    .Select(l => new OrderLine
                    {
                        Id = l.MaCTDH,
                        OrderId = PublicId(o),
                        VariantId = l.MaBienThe,
                        ProductId = variants.Single(v => v.MaBienThe == l.MaBienThe).MaSP,
                        Name = l.TenSanPham,
                        Image = SafeImage(
                            l.QImage,
                            variants.Single(v => v.MaBienThe == l.MaBienThe).MaSP
                        ),
                        Size = l.KichThuoc ?? "",
                        Color = l.MauSac ?? "",
                        Quantity = l.SoLuong,
                        Price = l.DonGia,
                        UnitCost = l.QUnitCost,
                        WarehouseId = l.QWarehouseId,
                    })
                    .ToList(),
            })
            .ToList();
        d.Returns = returns
            .Select(r => new ReturnRecord
            {
                Id = r.MaPhieuTra,
                OrderId = PublicId(orders.Single(o => o.MaDonHang == r.MaDonHang)),
                CustomerId = r.MaKH,
                ActorId = r.MaNV.HasValue ? -r.MaNV : null,
                Created = r.NgayYeuCau,
                Resolved = r.NgayXuLy,
                Reason = r.LyDo,
                Resolution = r.GhiChu ?? "",
                Status =
                    r.TrangThai is "Đã hoàn tiền" or "Hoàn tất" ? "Approved"
                    : r.TrangThai == "Từ chối" ? "Rejected"
                    : "Pending",
                Refund = r.TongTienHoan,
                Reference = r.QReference ?? "",
                Lines = returnLines
                    .Where(l => l.MaPhieuTra == r.MaPhieuTra)
                    .Select(l => new ReturnLine
                    {
                        Id = l.MaCTTra,
                        OrderLineId = l.MaCTDH,
                        Quantity = l.SoLuongTra,
                        Refund = l.SoTienHoan,
                        Condition = l.TinhTrangSanPham ?? "",
                    })
                    .ToList(),
            })
            .ToList();
        // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var o in d.Orders)
            if (
                o.Lines.All(l =>
                    d.Returns.Where(r => r.OrderId == o.Id && r.Status == "Approved")
                        .SelectMany(r => r.Lines)
                        .Where(r => r.OrderLineId == l.Id)
                        .Sum(r => r.Quantity) >= l.Quantity
                )
            )
                o.Status = "Đã hoàn hàng";
        d.History = history
            .Select(h => new OrderHistory
            {
                Id = h.MaLichSuTrangThai,
                OrderId = PublicId(orders.Single(o => o.MaDonHang == h.MaDonHang)),
                Status = h.TrangThai,
                Created = h.ThoiGian,
                Note = h.GhiChu ?? "",
            })
            .ToList();
        return d;
    }

    // HÀM SafeImage: Chỉ dùng 5 ảnh sản phẩm có sẵn trong thư viện; đường dẫn ngoài danh sách được
    // thay bằng ảnh mặc định tính theo id.
    // THAM SỐ image (string?): đường dẫn ảnh đề xuất; SafeImage chỉ giữ đường dẫn thuộc thư viện
    // cho phép.
    // THAM SỐ id (int): mã sản phẩm để chọn ảnh mặc định khi đường dẫn ảnh không thuộc thư viện cho
    // phép.
    // TRẢ VỀ (string): chuỗi ký tự; ý nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA i (int): số nguyên 32 bit; tên này chỉ có nghĩa trong lambda tương ứng, không
    // phải biến toàn cục.
    static string SafeImage(string? image, int id) =>
        image != null
        && Enumerable.Range(1, 5).Any(i => image == $"/images/products/aothun{i:00}.jpg")
            ? image
            : $"/images/products/aothun{(id - 1) % 5 + 1:00}.jpg";

    // HÀM Row: Tìm entity đã nạp trong DbSet.Local theo match; chưa có thì gọi create và đánh dấu
    // thêm mới. Việc ghi SQL thực tế diễn ra ở Flush/SaveChanges của luồng lưu.
    // KIỂU T: lớp entity EF đang thao tác, ví dụ SanPhamRow; T quyết định DbSet/bảng cần tìm, thêm
    // hoặc ghi.
    // THAM SỐ db (StoreDbContext): DbContext đang dùng trong giao dịch; đọc DbSet để lấy dữ liệu,
    // sửa entity rồi SaveChanges để đẩy SQL.
    // THAM SỐ match (Func<T, bool>): điều kiện tìm entity T trong DbSet.Local đã nạp, thường là so
    // khóa chính hoặc khóa ghép.
    // THAM SỐ create (Func<T>): hàm tạo entity T khi không tìm được dòng phù hợp; Row đánh dấu thêm
    // entity mới vào DbContext.
    // TRẢ VỀ (T): kết quả T do callback quyết định; chỉ trả sau khi transaction của hàm hoàn tất.
    static T Row<T>(StoreDbContext db, Func<T, bool> match, Func<T> create)
        where T : class
    {
        // BIẾN r (T): kết quả tổng quát T do hàm callback/caller quyết định, không cố định là một
        // entity SQL; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay
        // sau.
        var r = db.Set<T>().Local.FirstOrDefault(match);
        if (r != null)
            return r;
        r = create();
        db.Set<T>().Add(r);
        return r;
    }

    // HÀM Flush: Lưu một nhóm entity theo thứ tự bảng; khi cần thêm mã tường minh vào cột IDENTITY
    // thì bật/tắt IDENTITY_INSERT quanh SaveChanges. Phải chạy trong transaction của luồng ghi.
    // KIỂU T: lớp entity EF đang thao tác, ví dụ SanPhamRow; T quyết định DbSet/bảng cần tìm, thêm
    // hoặc ghi.
    // THAM SỐ db (StoreDbContext): DbContext đang dùng trong giao dịch; đọc DbSet để lấy dữ liệu,
    // sửa entity rồi SaveChanges để đẩy SQL.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA p (IProperty): IProperty; tên này chỉ có nghĩa trong lambda tương ứng, không phải
    // biến toàn cục.
    // BIẾN LAMBDA e (EntityEntry<T>): EntityEntry<T>; tên này chỉ có nghĩa trong lambda tương ứng,
    // không phải biến toàn cục.
    static void Flush<T>(StoreDbContext db)
        where T : class
    {
        // BIẾN identity (bool): true nếu EF xác định bảng có cột IDENTITY, cần xử lý
        // IDENTITY_INSERT khi thêm mã tường minh.
        bool identity = db
            .Model.FindEntityType(typeof(T))!
            .GetProperties()
            .Any(p =>
                p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn
            );
        // BIẾN added (bool): true nếu có entity T đang ở trạng thái Added; trong Flush dùng quyết
        // định có cần bật IDENTITY_INSERT.
        bool added = db.ChangeTracker.Entries<T>().Any(e => e.State == EntityState.Added);
        // BIẾN table (string): tên bảng từ metadata EF đã biên dịch, được kiểm tra tên hợp lệ trước
        // khi ghép lệnh IDENTITY_INSERT.
        string table = db.Model.FindEntityType(typeof(T))!.GetTableName()!;
        // Identifier comes only from the compiled EF model, never from request input.
        // ĐIỀU KIỆN: Invalid mapped table name. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(
            System.Text.RegularExpressions.Regex.IsMatch(table, @"^[A-Za-z][A-Za-z0-9_]*$"),
            "Invalid mapped table name."
        );
        if (identity && added)
            db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [" + table + "] ON;");
        try
        {
            // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ
            // hoàn tất khi
            // Commit thành công.
            db.SaveChanges();
        }
        finally
        {
            if (identity && added)
                db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [" + table + "] OFF;");
        }
    }

    // HÀM PrepareStock: Đồng bộ tồn tổng, biến thể và tồn từng kho; tạo dữ liệu biến thể/tồn còn
    // thiếu và phân bổ chênh lệch theo các nhánh bên dưới. Đây là bước chuẩn hóa sau callback
    // Write, không thay quyền kiểm kê của người dùng.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản phẩm;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
    // nhập/xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static void PrepareStock(StoreData d)
    {
        if (d.Warehouses.Count == 0)
            d.Warehouses.Add(new Warehouse { Id = 1, Name = "Kho chính" });
        // BIẾN LẶP p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
        // thể; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var p in d.Products)
        {
            if (!d.Variants.Any(v => v.ProductId == p.Id))
                // BIẾN LẶP size (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in,
                // chỉ dùng trong vòng foreach này.
                foreach (string size in new[] { "S", "M", "L", "XL" })
                    d.Variants.Add(
                        new Variant
                        {
                            Id = Next(d.Variants.Select(v => v.Id)),
                            ProductId = p.Id,
                            Size = size,
                            Color = p.Color,
                            Sku = $"QH-{p.Id}-{size}",
                        }
                    );
            // BIẾN variants (List<Variant>): danh sách/truy vấn các phần tử: biến thể sản phẩm theo
            // size/màu, chứa SKU, giá riêng và tồn; ToList lấy kết quả ra danh sách để dùng tiếp
            // trong phạm vi này.
            var variants = d.Variants.Where(v => v.ProductId == p.Id).ToList();
            // BIẾN LẶP v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn;
            // mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
            foreach (var v in variants)
                if (!d.Stocks.Any(s => s.VariantId == v.Id))
                    d.Stocks.Add(
                        new WarehouseStock
                        {
                            WarehouseId = d.Warehouses.First(w => w.Active).Id,
                            VariantId = v.Id,
                        }
                    );
            // BIẾN delta (int): chênh lệch số lượng cần cộng/trừ vào tồn; giá trị âm là giảm, dương
            // là tăng.
            int delta =
                p.Stock
                - d.Stocks.Where(s => variants.Any(v => v.Id == s.VariantId)).Sum(s => s.Quantity);
            if (delta > 0)
            {
                // BIẾN v (Variant): biến thể ưu tiên nhận chênh lệch tồn: size M nếu có, nếu không
                // dùng biến thể đầu tiên.
                var v = variants.FirstOrDefault(v => v.Size == "M") ?? variants[0];
                d.Stocks.First(s => s.VariantId == v.Id).Quantity += delta;
            }

            if (delta < 0)
                // BIẾN LẶP s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng
                // sản phẩm; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
                // này.
                foreach (
                    var s in d
                        .Stocks.Where(s => variants.Any(v => v.Id == s.VariantId))
                        .OrderBy(s => s.VariantId)
                )
                {
                    // BIẾN take (int): số lượng lấy từ một kho trong lượt hiện tại, luôn bị giới
                    // hạn bởi số còn cần và số kho đang có.
                    int take = Math.Min(s.Quantity, -delta);
                    s.Quantity -= take;
                    delta += take;
                    if (delta == 0)
                        break;
                }

            // ĐIỀU KIỆN: Không đủ tồn kho biến thể. Không đạt thì Require ném lỗi và dừng luồng xử

            // lý.
            Require(
                d.Stocks.Where(s => variants.Any(v => v.Id == s.VariantId)).Sum(s => s.Quantity)
                    == p.Stock,
                "Không đủ tồn kho biến thể."
            );
            p.Variants = variants;
            // BIẾN LẶP v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn;
            // mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
            foreach (var v in variants)
                v.Stock = d
                    .Stocks.Where(s =>
                        s.VariantId == v.Id
                        && d.Warehouses.Any(w => w.Id == s.WarehouseId && w.Active)
                    )
                    .Sum(s => s.Quantity);
        }
    }

    // HÀM PersistNative: Bộ chuyển đổi StoreData → các bảng SQL gốc: cập nhật/thêm danh mục, tài
    // khoản, sản phẩm, biến thể, tồn, phiếu nhập, ưu đãi, đơn, thanh toán, trả hàng và nhật ký. Ghi
    // theo thứ tự phụ thuộc khóa; Commit do Write quản lý.
    // THAM SỐ db (StoreDbContext): DbContext đang dùng trong giao dịch; đọc DbSet để lấy dữ liệu,
    // sửa entity rồi SaveChanges để đẩy SQL.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA c (DanhMucRow): entity của bảng DanhMuc, danh mục sản phẩm; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (SanPhamRow): entity của bảng SanPham, thông tin sản phẩm gốc; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KichThuocRow): entity của bảng KichThuoc, danh sách size; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (MauSacRow): entity của bảng MauSac, tên và mã màu; tên này chỉ có nghĩa trong
    // lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
    // và SKU; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KhoRow): entity của bảng Kho, thông tin kho; tên này chỉ có nghĩa trong lambda
    // tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (TonKhoRow): entity của bảng TonKho, tồn của từng biến thể tại từng kho; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung cấp hàng;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung cấp hàng;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (PhieuNhapRow): entity của bảng PhieuNhap, đầu phiếu nhập kho; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (KhuyenMaiRow): entity của bảng KhuyenMai, cấu hình ưu đãi; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
    // giới hạn lượt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ChiTietKhuyenMaiRow): entity của bảng ChiTietKhuyenMai, liên kết ưu đãi với
    // sản phẩm; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA o (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA g (GioHangRow): entity của bảng GioHang, đầu giỏ theo khách; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ChiTietGioHangRow): entity của bảng ChiTietGioHang, các dòng biến thể trong
    // giỏ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ChiTietDonHangRow): entity của bảng ChiTietDonHang, các dòng hàng đã mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (LichSuTrangThaiDonHangRow): entity của bảng LichSuTrangThaiDonHang, các mốc
    // đổi trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (ThanhToanRow): entity của bảng ThanhToan, thông tin phương thức/trạng thái
    // thanh toán gốc; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ThanhToanRow): entity của bảng ThanhToan, thông tin phương thức/trạng thái
    // thanh toán gốc; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (PhieuTraHangRow): entity của bảng PhieuTraHang, đầu phiếu trả; tên này chỉ có
    // nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ChiTietTraHangRow): entity của bảng ChiTietTraHang, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    void PersistNative(StoreDbContext db, StoreData d)
    {
        // BIẾN LẶP name (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng
        // trong vòng foreach này.
        foreach (string name in d.Categories)
            Row(
                db,
                (DanhMucRow c) => c.TenDanhMuc == name,
                () =>
                    new()
                    {
                        MaDanhMuc = Next(db.DanhMuc.Local.Select(c => c.MaDanhMuc)),
                        TenDanhMuc = name,
                        TrangThai = true,
                    }
            );
        // BIẾN LẶP c (DanhMucRow): entity của bảng DanhMuc, danh mục sản phẩm; mỗi lượt nhận một
        // phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var c in db.DanhMuc.Local)
            if (
                !d.Categories.Contains(c.TenDanhMuc)
                && !d.Products.Any(p => p.Category == c.TenDanhMuc)
            )
                db.DanhMuc.Remove(c);
        Flush<DanhMucRow>(db);
        // BIẾN LẶP c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
        // vai trò và trạng thái; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
        // foreach này.
        foreach (var c in d.Customers.Where(c => c.Role == "Customer"))
        {
            // BIẾN r (KhachHangRow): entity của bảng KhachHang, dữ liệu khách hàng.
            var r = Row(
                db,
                (KhachHangRow x) => x.MaKH == c.Id,
                () => new() { MaKH = c.Id, NgayDangKy = DateTime.Now }
            );
            r.HoTen = c.Name;
            r.Email = c.Email;
            r.MatKhau = c.PasswordHash;
            r.SoDienThoai = string.IsNullOrWhiteSpace(c.Phone) ? null : c.Phone;
            r.DiaChi = c.Address;
            r.TrangThai = c.Active;
            r.QLegacyId = c.LegacyId;
        }

        Flush<KhachHangRow>(db);
        // BIẾN LẶP c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
        // vai trò và trạng thái; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
        // foreach này.
        foreach (var c in d.Customers.Where(c => c.Role != "Customer"))
        {
            // BIẾN r (NhanVienRow): entity của bảng NhanVien, dữ liệu nhân viên và chức vụ.
            var r = Row(
                db,
                (NhanVienRow x) => x.MaNV == Math.Abs(c.Id),
                () =>
                    new()
                    {
                        MaNV = Math.Abs(c.Id),
                        NgayVaoLam = DateTime.Today,
                        ChucVu = c.Role,
                    }
            );
            r.HoTen = c.Name;
            r.Email = c.Email;
            r.MatKhau = c.PasswordHash;
            r.SoDienThoai = string.IsNullOrWhiteSpace(c.Phone) ? null : c.Phone;
            r.DiaChi = c.Address;
            r.TrangThai = c.Active;
            r.QLegacyId = c.LegacyId;
            if (StaffRole(r.ChucVu) != c.Role)
                r.ChucVu = c.Role;
        }

        Flush<NhanVienRow>(db);
        // BIẾN LẶP p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
        // thể; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var p in d.Products)
        {
            // BIẾN r (SanPhamRow): entity của bảng SanPham, thông tin sản phẩm gốc.
            var r = Row(
                db,
                (SanPhamRow x) => x.MaSP == p.Id,
                () => new() { MaSP = p.Id, NgayTao = DateTime.Now }
            );
            r.TenSP = p.Name;
            r.MaDanhMuc = db.DanhMuc.Local.Single(c => c.TenDanhMuc == p.Category).MaDanhMuc;
            r.MaThuongHieu = p.BrandId;
            r.GiaBan = p.Price;
            r.MoTa = p.Description;
            r.TrangThai = p.Active;
            r.QImage = p.Image;
            r.QIsNew = p.IsNew;
            r.QOnSale = p.OnSale;
            r.QLegacyId = p.LegacyId;
        }

        Flush<SanPhamRow>(db);
        // BIẾN LẶP v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; mỗi
        // lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var v in d.Variants)
        {
            Row(
                db,
                (KichThuocRow x) => x.TenKichThuoc == v.Size,
                () =>
                    new()
                    {
                        MaKichThuoc = Next(db.KichThuoc.Local.Select(x => x.MaKichThuoc)),
                        TenKichThuoc = v.Size,
                    }
            );
        }

        Flush<KichThuocRow>(db);
        // BIẾN LẶP v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; mỗi
        // lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var v in d.Variants)
        {
            Row(
                db,
                (MauSacRow x) => x.TenMauSac == v.Color,
                () =>
                    new()
                    {
                        MaMauSac = Next(db.MauSac.Local.Select(x => x.MaMauSac)),
                        TenMauSac = v.Color,
                    }
            );
        }

        Flush<MauSacRow>(db);
        // BIẾN LẶP v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; mỗi
        // lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var v in d.Variants)
        {
            // BIẾN r (BienTheSanPhamRow): entity của bảng BienTheSanPham, tổ hợp sản phẩm/size/màu
            // và SKU.
            var r = Row(
                db,
                (BienTheSanPhamRow x) => x.MaBienThe == v.Id,
                () => new() { MaBienThe = v.Id }
            );
            r.MaSP = v.ProductId;
            r.MaKichThuoc = db.KichThuoc.Local.Single(x => x.TenKichThuoc == v.Size).MaKichThuoc;
            r.MaMauSac = db.MauSac.Local.Single(x => x.TenMauSac == v.Color).MaMauSac;
            r.SKU = v.Sku;
            r.GiaBanRieng = v.Price;
            r.TrangThai = v.Active;
        }

        Flush<BienTheSanPhamRow>(db);
        // BIẾN LẶP w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
        // nhập/xuất; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var w in d.Warehouses)
        {
            // BIẾN r (KhoRow): entity của bảng Kho, thông tin kho.
            var r = Row(db, (KhoRow x) => x.MaKho == w.Id, () => new() { MaKho = w.Id });
            r.TenKho = w.Name;
            r.TrangThai = w.Active;
        }

        Flush<KhoRow>(db);
        // BIẾN LẶP s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản
        // phẩm; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var s in d.Stocks)
        {
            // BIẾN r (TonKhoRow): entity của bảng TonKho, tồn của từng biến thể tại từng kho.
            var r = Row(
                db,
                (TonKhoRow x) => x.MaKho == s.WarehouseId && x.MaBienThe == s.VariantId,
                () => new() { MaKho = s.WarehouseId, MaBienThe = s.VariantId }
            );
            r.SoLuongTon = s.Quantity;
        }

        Flush<TonKhoRow>(db);
        // BIẾN receipts (List<IGrouping<string, StockMovement>>): các biến động nhập kho chưa lưu,
        // đã nhóm theo mã phiếu để tạo chung đầu phiếu/chi tiết.
        var receipts = d
            .StockMovements.Where(m =>
                m.Reason == "Nhập kho" && !db.StockMovements.Local.Any(x => x.Id == m.Id)
            )
            .GroupBy(m => m.ReceiptReference ?? m.Reference)
            .ToList();
        // BIẾN LẶP group (IGrouping<string, StockMovement>): một nhóm phần tử có cùng Key do
        // GroupBy tạo ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi lượt nhận một phần tử của
        // nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var group in receipts)
        // BIẾN LẶP m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
        // chứng từ; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var m in group)
        {
            if (!m.SupplierId.HasValue)
            {
                // BIẾN supplier (NhaCungCapRow): entity của bảng NhaCungCap, thông tin đối tác cung
                // cấp hàng.
                var supplier = Row(
                    db,
                    (NhaCungCapRow x) => x.TenNCC == m.Supplier,
                    () =>
                        new()
                        {
                            MaNCC = Next(db.NhaCungCap.Local.Select(s => s.MaNCC)),
                            TenNCC = m.Supplier,
                            TrangThai = true,
                        }
                );
                m.SupplierId = supplier.MaNCC;
            }
        }

        Flush<NhaCungCapRow>(db);
        // BIẾN LẶP group (IGrouping<string, StockMovement>): một nhóm phần tử có cùng Key do
        // GroupBy tạo ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi lượt nhận một phần tử của
        // nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var group in receipts)
        {
            // BIẾN m (StockMovement): một StockMovement đại diện nhóm nhập, cung cấp thông tin đầu
            // phiếu như kho/NCC/người nhập.
            var m = group.First();
            // BIẾN header (PhieuNhapRow): đầu PhieuNhap của một nhóm chung mã phiếu; các dòng chi
            // tiết gắn vào mã phiếu này.
            var header = new PhieuNhapRow
            {
                MaPhieuNhap = Next(db.PhieuNhap.Local.Select(x => x.MaPhieuNhap)),
                MaKho = m.WarehouseId!.Value,
                MaNCC = m.SupplierId!.Value,
                MaNV = -m.ActorId!.Value,
                NgayNhap = m.Created,
                TongTien = group.Sum(x => x.Delta * x.UnitCost),
                QReference = group.Key,
                GhiChu = "Phiếu nhập qua QOANH",
            };
            db.PhieuNhap.Add(header);
        }

        Flush<PhieuNhapRow>(db);
        // BIẾN LẶP group (IGrouping<string, StockMovement>): một nhóm phần tử có cùng Key do
        // GroupBy tạo ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi lượt nhận một phần tử của
        // nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var group in receipts)
        {
            // BIẾN header (PhieuNhapRow): đầu PhieuNhap của một nhóm chung mã phiếu; các dòng chi
            // tiết gắn vào mã phiếu này.
            var header = db.PhieuNhap.Local.Single(p => p.QReference == group.Key);
            db.SupplierAccounts.Add(
                new()
                {
                    ReceiptId = header.MaPhieuNhap,
                    Amount = header.TongTien,
                    ActorId = group.First().ActorId ?? 0,
                    UpdatedUtc = DateTime.UtcNow,
                    Note = "New receipt",
                }
            );
        }

        // BIẾN LẶP group (IGrouping<string, StockMovement>): một nhóm phần tử có cùng Key do
        // GroupBy tạo ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi lượt nhận một phần tử của
        // nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var group in receipts)
        {
            // BIẾN header (PhieuNhapRow): đầu PhieuNhap của một nhóm chung mã phiếu; các dòng chi
            // tiết gắn vào mã phiếu này.
            var header = db.PhieuNhap.Local.Single(p => p.QReference == group.Key);
            // BIẾN LẶP m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
            // chứng từ; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
            // này.
            foreach (var m in group)
                db.ChiTietPhieuNhap.Add(
                    new()
                    {
                        MaPhieuNhap = header.MaPhieuNhap,
                        MaBienThe = m.VariantId!.Value,
                        SoLuong = m.Delta,
                        GiaNhap = m.UnitCost,
                    }
                );
        }

        Flush<ChiTietPhieuNhapRow>(db);
        // BIẾN LẶP p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị
        // và giới hạn lượt; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
        // này.
        foreach (var p in d.Promotions)
        {
            // BIẾN r (KhuyenMaiRow): entity của bảng KhuyenMai, cấu hình ưu đãi.
            var r = Row(db, (KhuyenMaiRow x) => x.MaKM == p.Id, () => new() { MaKM = p.Id });
            r.MaCode = p.Code;
            r.TenKhuyenMai = p.Name;
            r.LoaiGiam = p.Kind;
            r.GiaTriGiam = p.Value > 0 ? p.Value : p.Percent;
            r.DonHangToiThieu = p.Minimum;
            r.GiamToiDa = p.Maximum;
            r.SoLuong = p.Remaining;
            r.NgayBatDau = p.From;
            r.NgayKetThuc = p.Until;
            r.TrangThai = p.Active;
        }

        Flush<KhuyenMaiRow>(db);
        // BIẾN LẶP row (ChiTietKhuyenMaiRow): entity của bảng ChiTietKhuyenMai, liên kết ưu đãi với
        // sản phẩm; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var row in db.ChiTietKhuyenMai.Local.ToList())
            if (d.Promotions.Any(p => p.Id == row.MaKM && !p.ProductIds.Contains(row.MaSP)))
                db.ChiTietKhuyenMai.Remove(row);
        // BIẾN LẶP p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị
        // và giới hạn lượt; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
        // này.
        foreach (var p in d.Promotions)
        // BIẾN LẶP id (int): số nguyên 32 bit; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng
        // trong vòng foreach này.
        foreach (int id in p.ProductIds)
            Row(
                db,
                (ChiTietKhuyenMaiRow x) => x.MaKM == p.Id && x.MaSP == id,
                () => new() { MaKM = p.Id, MaSP = id }
            );
        Flush<ChiTietKhuyenMaiRow>(db);
        // BIẾN oldStatus (Dictionary<int, string>): bảng tra khóa SQL đơn → trạng thái cũ, dùng
        // phát hiện thay đổi để thêm lịch sử.
        var oldStatus = db.DonHang.Local.ToDictionary(o => o.MaDonHang, o => o.TrangThai);
        // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var o in d.Orders.Where(o => o.DatabaseId == 0))
        {
            // BIẾN cart (GioHangRow): entity của bảng GioHang, đầu giỏ theo khách; kết quả tra cứu
            // có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
            var cart = db.GioHang.Local.FirstOrDefault(g => g.MaKH == o.CustomerId);
            if (cart != null)
                db.ChiTietGioHang.RemoveRange(
                    db.ChiTietGioHang.Local.Where(l => l.MaGioHang == cart.MaGioHang).ToList()
                );
        }

        // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var o in d.Orders)
        {
            if (o.DatabaseId == 0)
                o.DatabaseId = Next(db.DonHang.Local.Select(x => x.MaDonHang));
            // BIẾN r (DonHangRow): entity của bảng DonHang, đầu đơn hàng SQL.
            var r = Row(
                db,
                (DonHangRow x) => x.MaDonHang == o.DatabaseId,
                () => new() { MaDonHang = o.DatabaseId }
            );
            r.MaKH = o.CustomerId;
            r.MaKM = d.Promotions.FirstOrDefault(p => p.Code == o.PromotionCode)?.Id;
            r.NgayDat = o.Created;
            r.HoTenNguoiNhan = o.Name;
            r.SoDienThoaiNhan = o.Phone;
            r.DiaChiGiaoHang = o.Address;
            r.TongTienHang = o.Subtotal;
            r.TienGiam = o.Discount;
            r.PhiVanChuyen = o.Shipping;
            r.TongThanhToan = o.Total;
            r.TrangThai = o.Status == "Đã hoàn hàng" ? "Hoàn thành" : o.Status;
            r.GhiChu = o.Note;
            r.QPublicId = o.Id;
            r.QCheckoutToken = o.CheckoutToken;
            r.QBankQrImage = o.BankQrImage;
            r.QTransferReference = o.TransferReference;
        }

        Flush<DonHangRow>(db);
        // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var o in d.Orders)
        // BIẾN LẶP l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; mỗi
        // lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var l in o.Lines)
        {
            if (l.Id == 0)
                l.Id = Next(db.ChiTietDonHang.Local.Select(x => x.MaCTDH));
            if (l.VariantId == 0)
                l.VariantId = d
                    .Variants.First(v => v.ProductId == l.ProductId && v.Size == l.Size)
                    .Id;
            // BIẾN r (ChiTietDonHangRow): entity của bảng ChiTietDonHang, các dòng hàng đã mua.
            var r = Row(
                db,
                (ChiTietDonHangRow x) => x.MaCTDH == l.Id,
                () => new() { MaCTDH = l.Id }
            );
            r.MaDonHang = o.DatabaseId;
            r.MaBienThe = l.VariantId;
            r.TenSanPham = l.Name;
            r.KichThuoc = l.Size;
            r.MauSac = string.IsNullOrWhiteSpace(l.Color)
                ? d.Variants.Single(v => v.Id == l.VariantId).Color
                : l.Color;
            r.SoLuong = l.Quantity;
            r.DonGia = l.Price;
            r.QImage = l.Image;
            r.QWarehouseId = l.WarehouseId;
            r.QUnitCost = l.UnitCost;
        }

        Flush<ChiTietDonHangRow>(db);
        // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var o in d.Orders)
        {
            // BIẾN status (string): trạng thái sẽ ghi vào bảng đơn gốc; nhánh adapter chuyển Đã
            // hoàn hàng thành Hoàn thành cho schema tương thích.
            string status = o.Status == "Đã hoàn hàng" ? "Hoàn thành" : o.Status;
            if (oldStatus.GetValueOrDefault(o.DatabaseId) != status)
                db.LichSuTrangThaiDonHang.Add(
                    new()
                    {
                        MaLichSuTrangThai =
                            db.LichSuTrangThaiDonHang.Local.Select(x => x.MaLichSuTrangThai)
                                .DefaultIfEmpty()
                                .Max() + 1,
                        MaDonHang = o.DatabaseId,
                        TrangThai = status,
                        ThoiGian = DateTime.Now,
                        GhiChu = "Cập nhật qua QOANH",
                    }
                );
        }

        Flush<LichSuTrangThaiDonHangRow>(db);
        // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
        // tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var o in d.Orders)
        {
            // BIẾN r (ThanhToanRow): entity của bảng ThanhToan, thông tin phương thức/trạng thái
            // thanh toán gốc; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc
            // ?? ngay sau.
            var r = db.ThanhToan.Local.FirstOrDefault(p => p.MaDonHang == o.DatabaseId);
            if (r == null)
            {
                r = new()
                {
                    MaThanhToan = Next(db.ThanhToan.Local.Select(x => x.MaThanhToan)),
                    MaDonHang = o.DatabaseId,
                    PhuongThuc = o.Payment == "Chuyển khoản" ? o.Payment : "COD",
                };
                db.ThanhToan.Add(r);
            }

            r.SoTien = o.Total;
            r.TrangThai =
                o.PaidAmount >= o.Total
                    ? (o.RefundedAmount >= o.Total ? "Đã hoàn tiền" : "Đã thanh toán")
                    : "Chưa thanh toán";
            // BIẾN receipt (PaymentRecord): bút toán thu tiền gần nhất của đơn, dùng cập nhật dữ
            // liệu thanh toán tương thích trong bảng gốc.
            var receipt = d.Payments.LastOrDefault(p => p.OrderId == o.Id && p.Kind == "Receipt");
            if (receipt != null)
            {
                r.MaGiaoDich = receipt.Reference;
                r.NgayThanhToan = receipt.Created;
            }
        }

        Flush<ThanhToanRow>(db);
        // BIẾN LẶP r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
        // hoàn; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var r in d.Returns)
        {
            // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
            // tiền.
            var o = d.Orders.Single(o => o.Id == r.OrderId);
            // BIẾN row (PhieuTraHangRow): entity của bảng PhieuTraHang, đầu phiếu trả.
            var row = Row(
                db,
                (PhieuTraHangRow x) => x.MaPhieuTra == r.Id,
                () => new() { MaPhieuTra = r.Id }
            );
            row.MaDonHang = o.DatabaseId;
            row.MaKH = r.CustomerId;
            row.MaNV = r.ActorId < 0 ? -r.ActorId : null;
            row.NgayYeuCau = r.Created;
            row.LyDo = r.Reason;
            row.TrangThai =
                r.Status == "Approved" ? "Đã hoàn tiền"
                : r.Status == "Rejected" ? "Từ chối"
                : "Chờ xử lý";
            row.TongTienHoan = r.Refund;
            row.NgayXuLy = r.Resolved;
            row.GhiChu = r.Resolution;
            row.QReference = string.IsNullOrEmpty(r.Reference) ? null : r.Reference;
        }

        Flush<PhieuTraHangRow>(db);
        // BIẾN LẶP r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
        // hoàn; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var r in d.Returns)
        // BIẾN LẶP l (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn và tình
        // trạng hàng; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var l in r.Lines)
        {
            if (l.Id == 0)
                l.Id = Next(db.ChiTietTraHang.Local.Select(x => x.MaCTTra));
            // BIẾN row (ChiTietTraHangRow): entity của bảng ChiTietTraHang, các dòng trả và số tiền
            // hoàn.
            var row = Row(
                db,
                (ChiTietTraHangRow x) => x.MaCTTra == l.Id,
                () => new() { MaCTTra = l.Id }
            );
            row.MaPhieuTra = r.Id;
            row.MaCTDH = l.OrderLineId;
            row.SoLuongTra = l.Quantity;
            row.SoTienHoan = l.Refund;
            row.LyDoChiTiet = r.Reason;
            row.TinhTrangSanPham = l.Condition;
        }

        Flush<ChiTietTraHangRow>(db);
        // BIẾN LẶP m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
        // chứng từ; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var m in d.StockMovements)
            if (!db.StockMovements.Local.Any(x => x.Id == m.Id))
                db.StockMovements.Add(m);
        // BIẾN LẶP p (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
        // chiếu; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
        foreach (var p in d.Payments)
            if (!db.Payments.Local.Any(x => x.Id == p.Id))
                db.Payments.Add(p);
        // BIẾN cfg (StoreConfiguration): bản ghi cấu hình cửa hàng đang có trong DbContext.Local,
        // sẽ được cập nhật hoặc tạo nếu chưa có.
        var cfg = db.Configuration.Local.SingleOrDefault();
        if (cfg == null)
            db.Configuration.Add(
                new() { Settings = d.Settings, ImportedFileHash = d.ImportedFileHash }
            );
        else
        {
            db.Entry(cfg.Settings).CurrentValues.SetValues(d.Settings);
            cfg.ImportedFileHash = d.ImportedFileHash;
        }

        // GHI SQL: đẩy thay đổi đang được EF theo dõi xuống database; transaction bao ngoài chỉ

        // hoàn tất khi Commit
        // thành công.
        db.SaveChanges();
    }
}
