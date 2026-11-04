// VAI TRÒ FILE: Tác vụ nhập dữ liệu cũ hoặc dữ liệu mẫu có kiểm soát. Chỉ gọi từ các tham số quản
// trị trong
// Program.cs, không chạy trong yêu cầu web thông thường.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.Security.Cryptography;
using System.Text.Json;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // HÀM ImportJson: Đọc JSON cũ, tính SHA-256 và chuyển dữ liệu qua MergeLegacy để nhập có kiểm
    // soát. Đây là tác vụ quản trị được gọi bằng --import-json, không phải nguồn dữ liệu chạy
    // thường ngày.
    // THAM SỐ path (string): đường dẫn tệp JSON quản trị cần nhập; chỉ được dùng trong lệnh nhập dữ
    // liệu chủ động.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    public void ImportJson(string path)
    {
        // BIẾN bytes (byte[]): byte tệp JSON đang nhập.
        var bytes = File.ReadAllBytes(path);
        // BIẾN source (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng.
        var source =
            JsonSerializer.Deserialize<StoreData>(bytes)
            ?? throw new InvalidDataException("JSON trống.");
        MergeLegacy(source, Convert.ToHexString(SHA256.HashData(bytes)));
    }

    // HÀM SeedDemo: Nạp bộ dữ liệu mẫu qua MergeLegacy; chỉ thực hiện khi người vận hành chủ động
    // dùng --seed-demo.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    public void SeedDemo() => MergeLegacy(Seed(), "DEMO-SEED");

    // HÀM MergeLegacy: Ghép dữ liệu cũ/mẫu vào StoreData, kiểm tra dấu vết nhập và ánh xạ mã liên
    // quan trước khi Write lưu SQL; xem các Require bên dưới để biết điều kiện được phép nhập.
    // THAM SỐ source (StoreData): StoreData đọc từ dữ liệu cũ/mẫu để ghép vào dữ liệu SQL hiện có.
    // THAM SỐ hash (string): chuỗi hash mật khẩu đã lưu, dùng tạo stamp hoặc kiểm tra cấu trúc theo
    // hàm đang gọi.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA x (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
    // giới hạn lượt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write → PrepareStock; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    void MergeLegacy(StoreData source, string hash) =>
        Write(d =>
        {
            // ĐIỀU KIỆN: Database đã nhập JSON; từ chối nhập lặp hoặc ghi đè. Không đạt thì Require
            // ném lỗi và dừng
            // luồng ghi.
            Require(
                d.ImportedFileHash == null,
                "Database đã nhập JSON; từ chối nhập lặp hoặc ghi đè."
            );
            // BIẾN accounts (Dictionary<int, int>): bảng ánh xạ mã tài khoản dữ liệu cũ → mã mới để
            // nối lại đơn khi nhập dữ liệu.
            var accounts = new Dictionary<int, int>();
            // BIẾN products (Dictionary<int, int>): bảng ánh xạ mã sản phẩm cũ → mã mới, dùng nối
            // lại các dòng đơn khi nhập JSON.
            var products = new Dictionary<int, int>();
            // BIẾN LẶP c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên,
            // email, vai trò và trạng thái; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng
            // trong vòng foreach này.
            foreach (var c in source.Customers)
            {
                // BIẾN legacy (int): mã gốc trước khi được cấp/đổi mã trong quá trình ghép dữ liệu
                // cũ.
                int legacy = c.Id;
                // BIẾN existing (Customer): bản ghi/dòng đã tồn tại để gộp hoặc tránh tạo trùng;
                // tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email, vai trò và trạng
                // thái.
                var existing = d.Customers.FirstOrDefault(x =>
                    x.Email.Equals(c.Email.Trim(), StringComparison.OrdinalIgnoreCase)
                );
                if (existing != null)
                {
                    Require(
                        existing.Role == c.Role && existing.PasswordHash == c.PasswordHash,
                        "Email trùng dữ liệu SQL nhưng khác tài khoản: " + c.Email
                    );
                    accounts[legacy] = existing.Id;
                    continue;
                }

                c.LegacyId = legacy;
                c.Email = c.Email.Trim().ToLowerInvariant();
                c.Id =
                    c.Role == "Customer"
                        ? Next(d.Customers.Where(x => x.Id > 0).Select(x => x.Id))
                        : -Next(d.Customers.Where(x => x.Id < 0).Select(x => -x.Id));
                accounts[legacy] = c.Id;
                d.Customers.Add(c);
            }

            // BIẾN LẶP cat (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in, chỉ
            // dùng trong vòng foreach này.
            foreach (string cat in source.Categories)
                if (!d.Categories.Contains(cat, StringComparer.OrdinalIgnoreCase))
                    d.Categories.Add(cat);
            // BIẾN LẶP p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các
            // biến thể; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
            // này.
            foreach (var p in source.Products)
            {
                // BIẾN legacy (int): mã gốc trước khi được cấp/đổi mã trong quá trình ghép dữ liệu
                // cũ.
                int legacy = p.Id;
                p.Id = Next(d.Products.Select(x => x.Id));
                p.LegacyId = legacy;
                p.Variants.Clear();
                products[legacy] = p.Id;
                d.Products.Add(p);
            }

            PrepareStock(d);
            // BIẾN LẶP o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
            // khoản tiền; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
            // này.
            foreach (var o in source.Orders)
            {
                Require(!d.Orders.Any(x => x.Id == o.Id), "Mã đơn JSON trùng SQL: " + o.Id);
                o.DatabaseId = 0;
                o.CustomerId = accounts[o.CustomerId];
                o.ShippingSnapshot = o.Shipping;
                // BIẾN LẶP l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm
                // mua; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
                foreach (var l in o.Lines)
                {
                    l.Id = 0;
                    l.ProductId = products[l.ProductId];
                    // BIẾN v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và
                    // tồn.
                    var v = d.Variants.Single(v => v.ProductId == l.ProductId && v.Size == l.Size);
                    l.VariantId = v.Id;
                    l.Color = v.Color;
                }

                d.Orders.Add(o);
            }

            // BIẾN LẶP p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá
            // trị và giới hạn lượt; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
            // foreach này.
            foreach (var p in source.Promotions)
            {
                Require(
                    !d.Promotions.Any(x =>
                        x.Code.Equals(p.Code, StringComparison.OrdinalIgnoreCase)
                    ),
                    "Mã ưu đãi JSON trùng SQL: " + p.Code
                );
                p.Id = Next(d.Promotions.Select(x => x.Id));
                if (p.From > p.Until)
                    p.From = p.Until.Date;
                d.Promotions.Add(p);
            }

            // BIẾN LẶP p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các
            // biến thể; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach
            // này.
            foreach (var p in source.Products)
                d.StockMovements.Add(
                    new StockMovement
                    {
                        Id = Next(d.StockMovements.Select(x => x.Id)),
                        ProductId = p.Id,
                        Before = 0,
                        Delta = p.Stock,
                        After = p.Stock,
                        Reason = "JSON opening balance SHA256 " + hash,
                        Reference = "IMPORT-" + p.Id,
                    }
                );
            d.Settings = source.Settings;
            d.ImportedFileHash = hash;
            return 0;
        });
}
