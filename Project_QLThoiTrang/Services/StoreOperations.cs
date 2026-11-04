// VAI TRÒ FILE: Điều kiện đặt đơn, chuyển trạng thái, nhập/kiểm kê, thu tiền và trả hàng.
// Controller chỉ nhận form;
// các Require tại đây là điều kiện bắt buộc cuối cùng trước lưu.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // HÀM FindOrder: Tìm đơn theo mã hiển thị trong StoreData; nếu không có thì ném lỗi nghiệp vụ
    // thay vì tiếp tục dùng giá trị null.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // TRẢ VỀ (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản tiền; ý
    // nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static Order FindOrder(StoreData d, string id) =>
        d.Orders.FirstOrDefault(o => o.Id == id)
        ?? throw new InvalidOperationException("Không tìm thấy đơn hàng.");

    // HÀM Permission: Kiểm tra tài khoản actor còn hoạt động và có quyền Admin hoặc một vai trò cho
    // phép. Dùng cho các thao tác qua StoreData, tương ứng NativePermission của luồng DbSet.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ roles (string[]): các vai trò được phép thực hiện thao tác; Admin vẫn được chấp nhận
    // theo helper quyền.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static void Permission(StoreData d, int actor, params string[] roles) =>
        Require(
            d.Customers.Any(c =>
                c.Id == actor && c.Active && (c.Role == "Admin" || roles.Contains(c.Role))
            ),
            "Không có quyền thực hiện thao tác này."
        );

    // HÀM Text: Trim chuỗi đầu vào rồi bắt buộc độ dài nằm trong min..max; field được ghép vào
    // thông báo lỗi để biết ô nhập nào chưa hợp lệ.
    // THAM SỐ text (string?): chuỗi đầu vào cần chuẩn hóa, so khớp hoặc chia dòng theo chức năng
    // của hàm.
    // THAM SỐ min (int): số ký tự tối thiểu của chuỗi sau Trim.
    // THAM SỐ max (int): số ký tự tối đa của chuỗi sau Trim.
    // THAM SỐ field (string): tên ô nhập dùng ghép vào thông báo lỗi, giúp chỉ rõ dữ liệu nào chưa
    // hợp lệ.
    // TRẢ VỀ (string): chuỗi ký tự; ý nghĩa được xác định bởi biểu thức return bên dưới.
    static string Text(string? text, int min, int max, string field)
    {
        // BIẾN v (string): chuỗi ký tự.
        var v = text?.Trim() ?? "";
        Require(v.Length >= min && v.Length <= max, field + " không hợp lệ.");
        return v;
    }

    // HÀM GetVariant: Đối chiếu dòng giỏ với sản phẩm đang bán và biến thể đang hoạt động; ưu tiên
    // VariantId, dữ liệu cũ có thể tra theo size. Không dùng giá bán do trình duyệt gửi lên.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // THAM SỐ c (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
    // quyết định giá bán; đối tượng đầu vào của hàm, không phải một ký tự viết tắt cố định cho mọi
    // file.
    // TRẢ VỀ (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; ý nghĩa được
    // xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static Variant GetVariant(StoreData d, CartItem c)
    {
        // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến thể;
        // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var p = d.Products.FirstOrDefault(p => p.Id == c.ProductId && p.Active);
        // BIẾN v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; kết quả
        // tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var v = d.Variants.FirstOrDefault(v =>
            v.ProductId == c.ProductId
            && v.Active
            && (
                c.VariantId > 0
                    ? v.Id == c.VariantId
                    : v.Size == c.Size && (c.Color == "" || v.Color == c.Color)
            )
        );
        // ĐIỀU KIỆN: Sản phẩm hoặc biến thể đã ngừng bán. Không đạt thì Require ném lỗi và dừng
        // luồng xử lý.
        Require(p != null && v != null, "Sản phẩm hoặc biến thể đã ngừng bán.");
        return v!;
    }

    // HÀM StockRow: Tìm tồn của một biến thể tại một kho; nếu chưa có thì tạo dòng WarehouseStock
    // với số lượng mặc định. Các hàm nhập/kiểm kê dùng chung helper này.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // THAM SỐ variant (int): mã biến thể cần tìm tồn tại kho, không phải đối tượng Variant khi tham
    // số là int.
    // THAM SỐ warehouse (int): mã kho cần tìm dòng WarehouseStock.
    // TRẢ VỀ (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản phẩm; ý
    // nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản phẩm;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static WarehouseStock StockRow(StoreData d, int variant, int warehouse)
    {
        // BIẾN s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản phẩm;
        // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
        var s = d.Stocks.FirstOrDefault(s => s.VariantId == variant && s.WarehouseId == warehouse);
        if (s != null)
            return s;
        s = new() { VariantId = variant, WarehouseId = warehouse };
        d.Stocks.Add(s);
        return s;
    }

    // HÀM Restore: Hoàn số lượng về biến thể/kho của dòng đơn và tăng tồn tổng sản phẩm; dùng khi
    // hủy đơn hoặc duyệt nhận hàng trả.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // THAM SỐ l (OrderLine): dòng đơn OrderLine lưu biến thể, kho xuất và số lượng gốc, dùng hoàn
    // hàng đúng nơi.
    // THAM SỐ quantity (int): số lượng cần cộng trở lại kho xuất của dòng đơn khi hủy/nhận trả.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
    // nhập/xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static void Restore(StoreData d, OrderLine l, int quantity)
    {
        // BIẾN warehouse (int): số nguyên 32 bit.
        int warehouse = l.WarehouseId ?? d.Warehouses.First(w => w.Active).Id;
        // ĐIỀU KIỆN: Kho gốc không tồn tại. Không đạt thì Require ném lỗi và dừng luồng xử lý.
        Require(d.Warehouses.Any(w => w.Id == warehouse), "Kho gốc không tồn tại.");
        StockRow(d, l.VariantId, warehouse).Quantity += quantity;
        d.Products.Single(p => p.Id == l.ProductId).Stock += quantity;
    }

    // HÀM PlaceOrder: Đặt hàng trong một Write: kiểm tra checkout token và trả lại đơn cũ nếu gửi
    // lại cùng token → xác thực khách/giỏ/phương thức → đối chiếu báo giá → trừ tồn kho đang hoạt
    // động → tính phí/ưu đãi → thêm đơn. Giá và tồn được lấy lại từ server.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ input (CheckoutInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ cart (List<CartItem>): các CartItem chuẩn bị đặt hàng; giá và tồn phải được kiểm tra
    // lại trên server.
    // THAM SỐ quote (List<OrderLine>?): báo giá OrderLine đã chụp khi mở checkout; null bỏ đối
    // chiếu báo giá, có dữ liệu thì kiểm tra giỏ và giá chưa đổi. Mặc định khi không truyền: null.
    // TRẢ VỀ (Order): Order đã được lưu/commit, hoặc đơn cũ đúng khách nếu gửi lại cùng token; lỗi
    // điều kiện ném ngoại lệ.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các khoản
    // tiền; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên, email,
    // vai trò và trạng thái; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA c (CartItem): dòng giỏ chứa mã sản phẩm/biến thể, size/màu và số lượng; không tự
    // quyết định giá bán; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x ((CartItem item, Variant variant)): (CartItem item, Variant variant); tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA g (IGrouping<int, (CartItem item, Variant variant)>): một nhóm phần tử có cùng
    // Key do GroupBy tạo ra; nhóm được duyệt để cộng hoặc lưu chung; tên này chỉ có nghĩa trong
    // lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng sản phẩm;
    // tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
    // nhập/xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
    // giới hạn lượt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public Order PlaceOrder(
        int customerId,
        CheckoutInput input,
        List<CartItem> cart,
        List<OrderLine>? quote = null
    ) =>
        Write(
            d =>
            {
                Validator.ValidateObject(input, new ValidationContext(input), true);
                // ĐIỀU KIỆN: Phiên đặt hàng không hợp lệ. Không đạt thì Require ném lỗi và dừng
                // luồng xử lý.
                Require(
                    Guid.TryParseExact(input.CheckoutToken, "N", out _),
                    "Phiên đặt hàng không hợp lệ."
                );
                // BIẾN old (Order): đơn đã có cùng CheckoutToken; nếu đúng khách thì trả lại để
                // không tạo/trừ tồn lần thứ hai.
                var old = d.Orders.FirstOrDefault(o => o.CheckoutToken == input.CheckoutToken);
                // CHỐNG GỬI TRÙNG: token đã tồn tại thì kiểm tra quyền/nội dung và trả kết quả cũ,
                // không thêm đơn/tin nhắn
                // lần hai.
                if (old != null)
                {
                    // ĐIỀU KIỆN: Phiên đặt hàng không hợp lệ. Không đạt thì Require ném lỗi và dừng
                    // luồng xử lý.
                    Require(old.CustomerId == customerId, "Phiên đặt hàng không hợp lệ.");
                    return old;
                }

                // ĐIỀU KIỆN: Chỉ tài khoản khách hàng đang hoạt động được đặt hàng. Không đạt thì

                // Require ném lỗi và dừng
                // luồng ghi.
                Require(
                    d.Customers.Any(c => c.Id == customerId && c.Role == "Customer" && c.Active),
                    "Chỉ tài khoản khách hàng đang hoạt động được đặt hàng."
                );
                // ĐIỀU KIỆN: Phương thức thanh toán không hợp lệ. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
                Require(
                    input.Payment is "COD" or "Chuyển khoản",
                    "Phương thức thanh toán không hợp lệ."
                );
                if (input.Payment == "Chuyển khoản")
                    // ĐIỀU KIỆN: Chuyển khoản chưa được cấu hình. Không đạt thì Require ném lỗi và
                    // dừng luồng xử lý.
                    Require(
                        BankOptions.Enabled
                            && BankOptions.QrImage.StartsWith(
                                "/images/payments/",
                                StringComparison.Ordinal
                            ),
                        "Chuyển khoản chưa được cấu hình."
                    );
                // ĐIỀU KIỆN: Giỏ trống hoặc số lượng không hợp lệ. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
                Require(
                    cart.Count is > 0 and <= 100 && cart.All(c => c.Quantity is >= 1 and <= 20),
                    "Giỏ trống hoặc số lượng không hợp lệ."
                );
                // BIẾN selected (List<(CartItem item, Variant variant)>): danh sách dòng giỏ đã
                // ghép đúng Variant để đặt hàng; trong nhập kho đây là HashSet mã đã gặp để phát
                // hiện trùng dòng.
                var selected = cart.Select(c => (item: c, variant: GetVariant(d, c))).ToList();
                // ĐIỀU KIỆN: Tối đa 20 sản phẩm mỗi biến thể. Không đạt thì Require ném lỗi và dừng
                // luồng xử lý.
                Require(
                    selected.GroupBy(x => x.variant.Id).All(g => g.Sum(x => x.item.Quantity) <= 20),
                    "Tối đa 20 sản phẩm mỗi biến thể."
                );
                if (quote != null)
                    // ĐIỀU KIỆN: Giỏ hàng đã thay đổi. Vui lòng kiểm tra lại. Không đạt thì Require
                    // ném lỗi và dừng luồng
                    // ghi.
                    Require(
                        cart.All(c =>
                            quote.Any(l =>
                                l.ProductId == c.ProductId
                                && l.Size == c.Size
                                && l.Quantity == c.Quantity
                                && (c.VariantId == 0 || l.VariantId == c.VariantId)
                            )
                        )
                            && quote.Count == cart.Count,
                        "Giỏ hàng đã thay đổi. Vui lòng kiểm tra lại."
                    );
                // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
                // khoản tiền; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa SQL đã
                // commit.
                var o = new Order
                {
                    Id = "QH-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                    CustomerId = customerId,
                    CheckoutToken = input.CheckoutToken,
                    Name = Text(input.Name, 1, 100, "Họ tên"),
                    Phone = Text(input.Phone, 9, 15, "Điện thoại"),
                    Address = Text(input.Address, 1, 300, "Địa chỉ"),
                    Note = input.Note?.Trim() ?? "",
                };
                // BIẾN LẶP group (IGrouping<int, (CartItem item, Variant variant)>): một nhóm phần
                // tử có cùng Key do GroupBy tạo ra; nhóm được duyệt để cộng hoặc lưu chung; mỗi
                // lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
                foreach (var group in selected.GroupBy(x => x.variant.Id))
                {
                    // BIẾN v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và
                    // tồn.
                    var v = group.First().variant;
                    // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các
                    // biến thể.
                    var p = d.Products.Single(p => p.Id == v.ProductId);
                    // BIẾN quantity (int): tổng lượng cần xuất cho một biến thể sau gộp các dòng
                    // giỏ; giảm dần khi lấy từ từng kho.
                    int quantity = group.Sum(x => x.item.Quantity);
                    // BIẾN price (decimal): giá bán server áp dụng: ưu tiên giá riêng biến thể, nếu
                    // null dùng giá mặc định sản phẩm.
                    decimal price = v.Price ?? p.Price;
                    if (quote != null)
                        // ĐIỀU KIỆN: Giá sản phẩm đã thay đổi. Quay lại giỏ kiểm tra giá mới. Không
                        // đạt thì Require ném lỗi
                        // và dừng luồng ghi.
                        Require(
                            quote
                                .Where(l =>
                                    l.ProductId == p.Id && (l.VariantId == 0 || l.VariantId == v.Id)
                                )
                                .All(l => l.Price == price),
                            "Giá sản phẩm đã thay đổi. Quay lại giỏ kiểm tra giá mới."
                        );
                    // BIẾN stocks (List<WarehouseStock>): các dòng tồn của biến thể ở kho đang hoạt
                    // động, sắp theo mã kho để phân bổ xuất nhất quán.
                    var stocks = d
                        .Stocks.Where(s =>
                            s.VariantId == v.Id
                            && d.Warehouses.Any(w => w.Id == s.WarehouseId && w.Active)
                        )
                        .OrderBy(s => s.WarehouseId)
                        .ToList();
                    // ĐIỀU KIỆN: Biến thể không đủ tồn kho. Không đạt thì Require ném lỗi và dừng
                    // luồng xử lý.
                    Require(stocks.Sum(s => s.Quantity) >= quantity, "Biến thể không đủ tồn kho.");
                    // BIẾN LẶP s (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn
                    // tổng sản phẩm; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong
                    // vòng foreach này.
                    foreach (var s in stocks)
                    {
                        // BIẾN take (int): số lượng lấy từ một kho trong lượt hiện tại, luôn bị
                        // giới hạn bởi số còn cần và số kho đang có.
                        int take = Math.Min(s.Quantity, quantity);
                        if (take == 0)
                            continue;
                        s.Quantity -= take;
                        quantity -= take;
                        p.Stock -= take;
                        o.Lines.Add(
                            new()
                            {
                                ProductId = p.Id,
                                VariantId = v.Id,
                                WarehouseId = s.WarehouseId,
                                Name = p.Name,
                                Image = p.Image,
                                Size = v.Size,
                                Color = v.Color,
                                Price = price,
                                Quantity = take,
                            }
                        );
                        if (quantity == 0)
                            break;
                    }
                }

                o.ShippingSnapshot = o.Shipping;
                o.Payment = input.Payment;
                if (input.Payment == "Chuyển khoản")
                {
                    o.BankQrImage = BankOptions.QrImage;
                    o.TransferReference =
                        "QO" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
                }

                if (!string.IsNullOrWhiteSpace(input.PromotionCode))
                {
                    // BIẾN p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm,
                    // giá trị và giới hạn lượt; kết quả tra cứu có thể null khi không khớp, xem
                    // nhánh kiểm tra hoặc ?? ngay sau.
                    var p = d.Promotions.FirstOrDefault(p =>
                        p.Code.Equals(
                            input.PromotionCode.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                        && p.Active
                        && p.From <= DateTime.Now
                        && p.Until.Date >= DateTime.Today
                        && (!p.Remaining.HasValue || p.Remaining > 0)
                    );
                    // ĐIỀU KIỆN: Mã ưu đãi không hợp lệ, hết lượt hoặc chưa đạt giá trị tối thiểu.
                    // Không đạt thì Require
                    // ném lỗi và dừng luồng ghi.
                    Require(
                        p != null && o.Subtotal >= p.Minimum,
                        "Mã ưu đãi không hợp lệ, hết lượt hoặc chưa đạt giá trị tối thiểu."
                    );
                    // BIẾN eligible (decimal): giá trị tiền hàng của các dòng đủ phạm vi ưu đãi,
                    // chưa phải số tiền được giảm.
                    decimal eligible = o
                        .Lines.Where(l =>
                            p!.ProductIds.Count == 0 || p.ProductIds.Contains(l.ProductId)
                        )
                        .Sum(l => l.Price * l.Quantity);
                    // ĐIỀU KIỆN: Mã không áp dụng cho sản phẩm trong đơn. Không đạt thì Require ném
                    // lỗi và dừng luồng xử lý.
                    Require(eligible > 0, "Mã không áp dụng cho sản phẩm trong đơn.");
                    // BIẾN value (decimal): giá trị cấu hình ưu đãi: dùng Value nếu dương, ngược
                    // lại dùng Percent để tương thích dữ liệu cũ.
                    decimal value = p!.Value > 0 ? p.Value : p.Percent;
                    // BIẾN discount (decimal): số tiền giảm tính từ loại SOTIEN hoặc phần trăm;
                    // bước gán vào đơn còn giới hạn bởi tiền hàng và trần giảm.
                    decimal discount = p.Kind == "SOTIEN" ? value : eligible * value / 100m;
                    o.Discount = decimal.Round(
                        Math.Min(eligible, Math.Min(discount, p.Maximum ?? decimal.MaxValue)),
                        0,
                        MidpointRounding.AwayFromZero
                    );
                    o.PromotionCode = p.Code;
                    if (p.Remaining.HasValue)
                        p.Remaining--;
                }

                d.Orders.Add(o);
                return o;
            },
            customerId,
            "Đặt hàng - trừ tồn biến thể"
        );

    // HÀM ChangeOrderStatus: Chuyển trạng thái đơn theo danh sách allowed bên dưới. Khách chỉ hủy
    // đơn chờ xác nhận của mình; nhân viên cần Sales/Admin. Giao/hoàn thành cần đủ tiền; hủy cần
    // đối soát vận đơn, chưa thu tiền và phải hoàn tồn/lượt ưu đãi.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ status (string): trạng thái đích muốn cập nhật; phải nằm trong tập chuyển trạng thái
    // được phép của luồng tương ứng.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // THAM SỐ customer (bool): true khi chính khách yêu cầu hủy đơn; false dùng luồng nhân viên và
    // kiểm tra quyền Sales/Admin. Mặc định khi không truyền: false.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và
    // mã vận đơn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản phẩm, giá trị và
    // giới hạn lượt; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void ChangeOrderStatus(string id, string status, int actor, bool customer = false) =>
        Write(
            d =>
            {
                // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
                // khoản tiền.
                var o = FindOrder(d, id);
                if (customer)
                    // ĐIỀU KIỆN: Chỉ hủy đơn chờ xác nhận của bạn. Không đạt thì Require ném lỗi và
                    // dừng luồng xử lý.
                    Require(
                        o.CustomerId == actor && o.Status == "Chờ xác nhận" && status == "Đã hủy",
                        "Chỉ hủy đơn chờ xác nhận của bạn."
                    );
                else
                    Permission(d, actor, "Sales");
                // BIẾN allowed (string[]): các trạng thái đích hợp lệ từ trạng thái hiện tại; trạng
                // thái ngoài tập bị Require từ chối.
                var allowed = o.Status switch
                {
                    "Chờ xác nhận" => new[] { "Đã xác nhận", "Đã hủy" },
                    "Đã xác nhận" => new[] { "Đang chuẩn bị", "Đang giao", "Đã hủy" },
                    "Đang chuẩn bị" => new[] { "Đang giao", "Đã hủy" },
                    "Đang giao" => new[] { "Đã giao" },
                    "Đã giao" => new[] { "Hoàn thành" },
                    _ => Array.Empty<string>(),
                };
                // ĐIỀU KIỆN: Không thể chuyển sang trạng thái này. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
                Require(allowed.Contains(status), "Không thể chuyển sang trạng thái này.");
                if (status is "Đã giao" or "Hoàn thành")
                    // ĐIỀU KIỆN: Phải đối soát đủ tiền trước khi hoàn tất giao hàng. Không đạt thì
                    // Require ném lỗi và dừng
                    // luồng ghi.
                    Require(
                        o.PaidAmount == o.Total,
                        "Phải đối soát đủ tiền trước khi hoàn tất giao hàng."
                    );
                if (o.Payment == "Chuyển khoản" && status == "Đang giao")
                    // ĐIỀU KIỆN: Đơn chuyển khoản cần nhận đủ tiền trước khi giao. Không đạt thì
                    // Require ném lỗi và dừng
                    // luồng ghi.
                    Require(
                        o.PaidAmount == o.Total,
                        "Đơn chuyển khoản cần nhận đủ tiền trước khi giao."
                    );
                // NHÁNH HỦY ĐƠN: phải kiểm tra vận đơn và tiền đã thu trước khi hoàn lại tồn, trả
                // lượt ưu đãi.
                if (status == "Đã hủy")
                {
                    // ĐIỀU KIỆN: Cần hủy và đối soát vận đơn GHN trước khi hủy đơn. Không đạt thì
                    // Require ném lỗi và dừng
                    // luồng ghi.
                    Require(
                        !d.Shipments.Any(s =>
                            s.OrderId == o.DatabaseId && s.Status is not ("cancel" or "Rejected")
                        ),
                        "Cần hủy và đối soát vận đơn GHN trước khi hủy đơn."
                    );
                    // ĐIỀU KIỆN: Đơn đã thu tiền cần hoàn tiền trước khi hủy. Không đạt thì Require
                    // ném lỗi và dừng luồng
                    // ghi.
                    Require(o.PaidAmount == 0, "Đơn đã thu tiền cần hoàn tiền trước khi hủy.");
                    // BIẾN LẶP l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời
                    // điểm mua; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng
                    // foreach này.
                    foreach (var l in o.Lines)
                        Restore(d, l, l.Quantity);
                    if (o.PromotionCode != "")
                    {
                        // BIẾN p (Promotion): chương trình ưu đãi có mã, thời gian, phạm vi sản
                        // phẩm, giá trị và giới hạn lượt.
                        var p = d.Promotions.Single(x => x.Code == o.PromotionCode);
                        if (p.Remaining.HasValue)
                            p.Remaining++;
                    }
                }

                o.Status = status;
                return 0;
            },
            actor,
            "Đơn " + id + ": " + status
        );

    // HÀM Receive: Lối nhập kho một dòng: đóng gói ReceiveInput thành danh sách rồi dùng
    // ReceiveBatch để áp dụng cùng quy tắc và transaction.
    // THAM SỐ input (ReceiveInput): đối tượng nhận dữ liệu form; thuộc tính được bind theo
    // name/asp-for, không được coi là đã hợp lệ trước ModelState và service.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // LIÊN KẾT XỬ LÝ: ReceiveBatch; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void Receive(ReceiveInput input, int actor) => ReceiveBatch(new() { input }, actor);

    // HÀM ReceiveBatch: Thêm phiếu nhập 1–100 dòng trong một Write: cần nhân viên Inventory/Admin,
    // mã phiếu chưa dùng, cùng kho/NCC, biến thể không trùng. Mỗi dòng tăng tồn kho và tồn tổng,
    // thêm StockMovement có giá nhập; PersistNative tạo phiếu/chi tiết nhập.
    // THAM SỐ inputs (List<ReceiveInput>): các ReceiveInput của cùng phiếu nhập; service kiểm tra
    // 1–100 dòng, chung kho/NCC/mã và không trùng biến thể.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA m (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA i (ReceiveInput): một dòng phiếu nhập gồm biến thể, kho, nhà cung cấp, số lượng,
    // giá nhập và mã phiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
    // nhập/xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA s (Supplier): nhà cung cấp dùng cho các dòng nhập hàng; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void ReceiveBatch(List<ReceiveInput> inputs, int actor) =>
        Write(
            d =>
            {
                Permission(d, actor, "Inventory");
                // ĐIỀU KIỆN: Phiếu nhập cần tài khoản nhân viên. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
                Require(actor < 0, "Phiếu nhập cần tài khoản nhân viên.");
                // ĐIỀU KIỆN: Phiếu nhập cần từ 1 đến 100 dòng. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
                Require(inputs.Count is > 0 and <= 100, "Phiếu nhập cần từ 1 đến 100 dòng.");
                // BIẾN reference (string): mã phiếu nhập đã chuẩn hóa có tiền tố IN-, dùng kiểm tra
                // phiếu chưa được ghi trước đó.
                string reference = "IN-" + Text(inputs[0].Reference, 1, 60, "Mã phiếu nhập");
                // ĐIỀU KIỆN: Mã phiếu nhập đã sử dụng. Không đạt thì Require ném lỗi và dừng luồng
                // xử lý.
                Require(
                    !d.StockMovements.Any(m =>
                        string.Equals(
                            m.ReceiptReference,
                            reference,
                            StringComparison.OrdinalIgnoreCase
                        ) || m.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase)
                    ),
                    "Mã phiếu nhập đã sử dụng."
                );
                // ĐIỀU KIỆN: Các dòng phải cùng phiếu, kho và nhà cung cấp. Không đạt thì Require
                // ném lỗi và dừng luồng
                // ghi.
                Require(
                    inputs.All(i =>
                        i.Reference.Trim() == inputs[0].Reference.Trim()
                        && i.WarehouseId == inputs[0].WarehouseId
                        && i.SupplierId == inputs[0].SupplierId
                    ),
                    "Các dòng phải cùng phiếu, kho và nhà cung cấp."
                );
                // BIẾN selected (HashSet<int>): HashSet mã biến thể đã xử lý trong phiếu; Add trả
                // false nghĩa là biến thể bị lặp dòng.
                var selected = new HashSet<int>();
                // BIẾN LẶP i (ReceiveInput): một dòng phiếu nhập gồm biến thể, kho, nhà cung cấp,
                // số lượng, giá nhập và mã phiếu; mỗi lượt nhận một phần tử của nguồn sau in, chỉ
                // dùng trong vòng foreach này.
                foreach (var i in inputs)
                {
                    Validator.ValidateObject(i, new ValidationContext(i), true);
                    // BIẾN v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và
                    // tồn; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
                    // ngay sau.
                    var v =
                        i.VariantId > 0
                            ? d.Variants.SingleOrDefault(v => v.Id == i.VariantId)
                            : d.Variants.FirstOrDefault(v =>
                                v.ProductId == i.ProductId && v.Size == "M"
                            );
                    // ĐIỀU KIỆN: Biến thể không tồn tại hoặc trùng dòng. Không đạt thì Require ném
                    // lỗi và dừng luồng xử lý.
                    Require(
                        v != null && selected.Add(v.Id),
                        "Biến thể không tồn tại hoặc trùng dòng."
                    );
                    // BIẾN warehouse (int): số nguyên 32 bit.
                    int warehouse =
                        i.WarehouseId > 0 ? i.WarehouseId : d.Warehouses.First(w => w.Active).Id;
                    // ĐIỀU KIỆN: Kho không hoạt động. Không đạt thì Require ném lỗi và dừng luồng
                    // xử lý.
                    Require(
                        d.Warehouses.Any(w => w.Id == warehouse && w.Active),
                        "Kho không hoạt động."
                    );
                    // BIẾN supplier (Supplier): nhà cung cấp dùng cho các dòng nhập hàng; kết quả
                    // tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
                    var supplier =
                        i.SupplierId > 0
                            ? d.Suppliers.FirstOrDefault(s => s.Id == i.SupplierId && s.Active)
                            : null;
                    // ĐIỀU KIỆN: Nhà cung cấp không hoạt động. Không đạt thì Require ném lỗi và
                    // dừng luồng xử lý.
                    Require(i.SupplierId == 0 || supplier != null, "Nhà cung cấp không hoạt động.");
                    // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các
                    // biến thể.
                    var p = d.Products.Single(p => p.Id == v!.ProductId);
                    // BIẾN before (int): tồn tổng sản phẩm trước cộng lượng nhập, dùng ghi
                    // Before/After của StockMovement.
                    int before = p.Stock;
                    p.Stock = checked(p.Stock + i.Quantity);
                    // ĐIỀU KIỆN: Tồn kho vượt giới hạn. Không đạt thì Require ném lỗi và dừng luồng
                    // xử lý.
                    Require(p.Stock <= 100000, "Tồn kho vượt giới hạn.");
                    StockRow(d, v!.Id, warehouse).Quantity += i.Quantity;
                    d.StockMovements.Add(
                        new()
                        {
                            Id = Next(d.StockMovements.Select(x => x.Id)),
                            ProductId = p.Id,
                            VariantId = v.Id,
                            WarehouseId = warehouse,
                            Before = before,
                            Delta = i.Quantity,
                            After = p.Stock,
                            Reason = "Nhập kho",
                            Reference = reference + "-" + v.Id,
                            ReceiptReference = reference,
                            Supplier = supplier?.Name ?? Text(i.Supplier, 1, 150, "Nhà cung cấp"),
                            SupplierId = supplier?.Id,
                            UnitCost = i.UnitCost,
                            ActorId = actor,
                        }
                    );
                }

                return 0;
            },
            actor,
            "Nhập kho"
        );

    // HÀM AdjustStock: Sửa tồn tổng bằng kiểm kê: quyền Inventory/Admin, lý do 5–300 ký tự,
    // quantity 0–100.000 và tồn hiện tại phải bằng expected. Ghi chênh lệch vào nhật ký;
    // PrepareStock phân bổ lại xuống tồn chi tiết.
    // THAM SỐ id (int): mã sản phẩm cần kiểm kê tồn tổng.
    // THAM SỐ quantity (int): số tồn thực tế muốn ghi sau kiểm kê (giá trị tuyệt đối), không phải
    // số tăng/giảm; phải 0–100.000.
    // THAM SỐ expected (int): số tồn người dùng đã thấy lúc mở form; so với tồn hiện tại để phát
    // hiện thay đổi và tránh ghi đè.
    // THAM SỐ reason (string): lý do nghiệp vụ do người dùng nhập; Text/attribute kiểm tra độ dài
    // trước khi ghi nhật ký hoặc phiếu.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void AdjustStock(int id, int quantity, int expected, string reason, int actor) =>
        Write(
            d =>
            {
                Permission(d, actor, "Inventory");
                reason = Text(reason, 5, 300, "Lý do kiểm kê");
                // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các
                // biến thể; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
                // ngay sau.
                var p =
                    d.Products.SingleOrDefault(p => p.Id == id)
                    ?? throw new InvalidOperationException("Sản phẩm không tồn tại.");
                // ĐIỀU KIỆN: Tồn kho đã thay đổi; tải lại trang trước khi kiểm kê. Không đạt thì
                // Require ném lỗi và dừng
                // luồng ghi.
                Require(
                    p.Stock == expected,
                    "Tồn kho đã thay đổi; tải lại trang trước khi kiểm kê."
                );
                // ĐIỀU KIỆN: Tồn kho không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử
                // lý.
                Require(quantity is >= 0 and <= 100000, "Tồn kho không hợp lệ.");
                // BIẾN old (int): số tồn tổng trước kiểm kê, dùng tính delta = quantity - old.
                int old = p.Stock;
                p.Stock = quantity;
                d.StockMovements.Add(
                    new()
                    {
                        Id = Next(d.StockMovements.Select(x => x.Id)),
                        ProductId = id,
                        Before = old,
                        Delta = quantity - old,
                        After = quantity,
                        Reason = "Kiểm kê tổng (phân bổ M trước): " + reason,
                        ActorId = actor,
                        Reference = "ADJ-" + Guid.NewGuid().ToString("N"),
                    }
                );
                return 0;
            },
            actor,
            "Kiểm kê"
        );

    // HÀM AdjustVariantStock: Kiểm kê một biến thể tại một kho hoạt động. expected phải bằng tồn
    // kho hiện tại để tránh ghi đè dữ liệu người khác; cập nhật tồn chi tiết, cộng chênh lệch vào
    // tồn tổng và thêm nhật ký.
    // THAM SỐ variantId (int): mã biến thể sản phẩm, dùng phân biệt đúng tổ hợp size/màu thay vì
    // chỉ mã sản phẩm.
    // THAM SỐ warehouseId (int): mã kho cần nhập/kiểm kê; service đối chiếu kho tồn tại và trạng
    // thái hoạt động.
    // THAM SỐ quantity (int): số tồn thực tế muốn ghi sau kiểm kê (giá trị tuyệt đối), không phải
    // số tăng/giảm; phải 0–100.000.
    // THAM SỐ expected (int): số tồn người dùng đã thấy lúc mở form; so với tồn hiện tại để phát
    // hiện thay đổi và tránh ghi đè.
    // THAM SỐ reason (string): lý do nghiệp vụ do người dùng nhập; Text/attribute kiểm tra độ dài
    // trước khi ghi nhật ký hoặc phiếu.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn; tên này
    // chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA w (Warehouse): thông tin kho và trạng thái kho được dùng trong kiểm tra
    // nhập/xuất; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các biến
    // thể; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (StockMovement): nhật ký trước/chênh lệch/sau của tồn, kèm lý do, người và
    // chứng từ; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void AdjustVariantStock(
        int variantId,
        int warehouseId,
        int quantity,
        int expected,
        string reason,
        int actor
    ) =>
        Write(
            d =>
            {
                Permission(d, actor, "Inventory");
                // BIẾN v (Variant): biến thể sản phẩm theo size/màu, chứa SKU, giá riêng và tồn;
                // kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ?? ngay sau.
                var v =
                    d.Variants.SingleOrDefault(v => v.Id == variantId)
                    ?? throw new InvalidOperationException("Biến thể không tồn tại.");
                // ĐIỀU KIỆN: Kho không hoạt động. Không đạt thì Require ném lỗi và dừng luồng xử
                // lý.
                Require(
                    d.Warehouses.Any(w => w.Id == warehouseId && w.Active),
                    "Kho không hoạt động."
                );
                // BIẾN stock (WarehouseStock): tồn của một biến thể tại một kho, khác với tồn tổng
                // sản phẩm.
                var stock = StockRow(d, variantId, warehouseId);
                // ĐIỀU KIỆN: Tồn biến thể đã thay đổi; tải lại trang. Không đạt thì Require ném lỗi
                // và dừng luồng xử lý.
                Require(stock.Quantity == expected, "Tồn biến thể đã thay đổi; tải lại trang.");
                // ĐIỀU KIỆN: Tồn kho không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng xử
                // lý.
                Require(quantity is >= 0 and <= 100000, "Tồn kho không hợp lệ.");
                // BIẾN p (Product): sản phẩm gồm thông tin bán, giá mặc định, trạng thái và các
                // biến thể.
                var p = d.Products.Single(p => p.Id == v.ProductId);
                // BIẾN before (int): tồn tổng sản phẩm trước kiểm kê biến thể, khác expected là tồn
                // của riêng biến thể/kho.
                int before = p.Stock;
                // BIẾN delta (int): chênh lệch số lượng cần cộng/trừ vào tồn; giá trị âm là giảm,
                // dương là tăng.
                int delta = quantity - stock.Quantity;
                stock.Quantity = quantity;
                p.Stock += delta;
                d.StockMovements.Add(
                    new()
                    {
                        Id = Next(d.StockMovements.Select(x => x.Id)),
                        ProductId = p.Id,
                        VariantId = v.Id,
                        WarehouseId = warehouseId,
                        Before = before,
                        Delta = delta,
                        After = p.Stock,
                        ActorId = actor,
                        Reason = "Kiểm kê biến thể: " + Text(reason, 5, 300, "Lý do"),
                        Reference = "ADJ-" + Guid.NewGuid().ToString("N"),
                    }
                );
                return 0;
            },
            actor,
            "Kiểm kê biến thể"
        );

    // HÀM RecordPayment: Sales/Admin ghi nhận tiền đã thu: đơn ở trạng thái cho phép, không đang
    // gửi GHN, amount phải đúng toàn bộ phần còn phải thu và mã biên nhận chưa dùng. Thêm
    // PaymentRecord rồi tăng PaidAmount; không gọi cổng thanh toán tại đây.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ amount (decimal): số tiền VND đã thu, phải dương và đúng toàn bộ phần Total -
    // PaidAmount còn thiếu của đơn.
    // THAM SỐ reference (string): mã chứng từ/biên nhận do người dùng cung cấp; các luồng tiền/nhập
    // kho kiểm tra chưa dùng để chống ghi trùng.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA s (Shipment): yêu cầu/vận đơn GHN đã lưu, gồm mã đơn, request hash, trạng thái và
    // mã vận đơn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void RecordPayment(string id, decimal amount, string reference, int actor) =>
        Write(
            d =>
            {
                Permission(d, actor, "Sales");
                // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
                // khoản tiền.
                var o = FindOrder(d, id);
                reference = Text(reference, 1, 100, "Mã biên nhận");
                // ĐIỀU KIỆN: Đang gửi vận đơn; chờ kết quả trước khi ghi nhận thêm tiền. Không đạt
                // thì Require ném lỗi và
                // dừng luồng ghi.
                Require(
                    !d.Shipments.Any(s => s.OrderId == o.DatabaseId && s.Status == "Requesting"),
                    "Đang gửi vận đơn; chờ kết quả trước khi ghi nhận thêm tiền."
                );
                // ĐIỀU KIỆN: Đơn chưa đủ điều kiện ghi nhận thanh toán. Không đạt thì Require ném
                // lỗi và dừng luồng xử lý.
                Require(
                    (o.Payment == "Chuyển khoản" && o.Status == "Chờ xác nhận")
                        || new[]
                        {
                            "Đã xác nhận",
                            "Đang chuẩn bị",
                            "Đang giao",
                            "Đã giao",
                            "Hoàn thành",
                        }.Contains(o.Status),
                    "Đơn chưa đủ điều kiện ghi nhận thanh toán."
                );
                // ĐIỀU KIỆN: Số tiền phải đúng phần còn phải thu; không ghi nhận trùng. Không đạt
                // thì Require ném lỗi và
                // dừng luồng ghi.
                Require(
                    amount > 0 && amount == o.Total - o.PaidAmount,
                    "Số tiền phải đúng phần còn phải thu; không ghi nhận trùng."
                );
                // ĐIỀU KIỆN: Mã biên nhận đã sử dụng. Không đạt thì Require ném lỗi và dừng luồng
                // xử lý.
                Require(
                    !d.Payments.Any(p =>
                        p.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase)
                    ) && !d.PaymentReferences.Contains(reference, StringComparer.OrdinalIgnoreCase),
                    "Mã biên nhận đã sử dụng."
                );
                d.Payments.Add(
                    new()
                    {
                        Id = Next(d.Payments.Select(x => x.Id)),
                        OrderId = id,
                        Amount = amount,
                        Reference = reference,
                        ActorId = actor,
                    }
                );
                o.PaidAmount += amount;
                return 0;
            },
            actor,
            "Ghi nhận tiền thanh toán"
        );

    // HÀM Returned: Tính tổng số lượng đã trả của một dòng đơn, chỉ cộng phiếu có trạng thái
    // Approved; phiếu Pending/Rejected không làm giảm số lượng còn được trả.
    // THAM SỐ d (StoreData): ảnh StoreData dùng trong thao tác hiện tại; các danh sách của d được
    // Load từ SQL trước callback.
    // THAM SỐ orderId (string): mã đơn công khai để lọc các phiếu trả thuộc đơn.
    // THAM SỐ lineId (int): mã một dòng chi tiết đơn; trong yêu cầu trả, 0 nghĩa là xử lý tất cả
    // dòng còn được trả.
    // TRẢ VỀ (int): số nguyên 32 bit; ý nghĩa được xác định bởi biểu thức return bên dưới.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn và tình
    // trạng hàng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    static int Returned(StoreData d, string orderId, int lineId) =>
        d
            .Returns.Where(r => r.OrderId == orderId && r.Status == "Approved")
            .SelectMany(r => r.Lines)
            .Where(l => l.OrderLineId == lineId)
            .Sum(l => l.Quantity);

    // HÀM RequestReturn: Khách tạo yêu cầu trả cho đơn của mình đã giao/hoàn thành và chưa có phiếu
    // Pending. lineId = 0 lấy mọi dòng còn được trả; nếu chọn một dòng thì kiểm tra quantity. Tính
    // hoàn tiền sau giảm giá và thêm ReturnRecord chờ duyệt.
    // THAM SỐ id (string): mã đơn công khai QH-... hoặc QH-SQL-..., dùng tìm đơn và kiểm tra chủ
    // đơn/quyền.
    // THAM SỐ reason (string): lý do nghiệp vụ do người dùng nhập; Text/attribute kiểm tra độ dài
    // trước khi ghi nhật ký hoặc phiếu.
    // THAM SỐ customerId (int): mã khách hàng dương; dùng giới hạn dữ liệu theo chủ tài khoản/hội
    // thoại/đơn tương ứng.
    // THAM SỐ lineId (int): mã một dòng chi tiết đơn; trong yêu cầu trả, 0 nghĩa là xử lý tất cả
    // dòng còn được trả. Mặc định khi không truyền: 0.
    // THAM SỐ quantity (int): số lượng muốn trả của dòng chọn; khi lineId = 0 service tự lấy toàn
    // bộ lượng còn được trả. Mặc định khi không truyền: 0.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn và tình
    // trạng hàng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng trả, tiền hoàn và tình
    // trạng hàng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void RequestReturn(
        string id,
        string reason,
        int customerId,
        int lineId = 0,
        int quantity = 0
    ) =>
        Write(
            d =>
            {
                // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
                // khoản tiền.
                var o = FindOrder(d, id);
                // ĐIỀU KIỆN: Chỉ yêu cầu trả đơn đã giao của bạn. Không đạt thì Require ném lỗi và
                // dừng luồng xử lý.
                Require(
                    o.CustomerId == customerId && o.Status is "Đã giao" or "Hoàn thành",
                    "Chỉ yêu cầu trả đơn đã giao của bạn."
                );
                // ĐIỀU KIỆN: Đơn đang có yêu cầu chờ xử lý. Không đạt thì Require ném lỗi và dừng
                // luồng xử lý.
                Require(
                    !d.Returns.Any(r => r.OrderId == id && r.Status == "Pending"),
                    "Đơn đang có yêu cầu chờ xử lý."
                );
                // BIẾN r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số
                // tiền hoàn; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa SQL đã
                // commit.
                var r = new ReturnRecord
                {
                    Id = Next(d.Returns.Select(x => x.Id)),
                    OrderId = id,
                    CustomerId = customerId,
                    Reason = Text(reason, 5, 500, "Lý do trả hàng"),
                };
                // BIẾN LẶP l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm
                // mua; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng trong vòng foreach này.
                foreach (var l in o.Lines.Where(l => lineId == 0 || l.Id == lineId))
                {
                    // BIẾN remaining (int): số lượng mua trừ số đã trả được duyệt, tức mức tối đa
                    // còn được trả của dòng này.
                    int remaining = l.Quantity - Returned(d, id, l.Id);
                    // BIẾN requested (int): lượng yêu cầu trả: toàn bộ remaining khi lineId = 0,
                    // hoặc quantity người dùng chọn khi trả một dòng.
                    int requested = lineId == 0 ? remaining : quantity;
                    if (requested == 0 && lineId == 0)
                        continue;
                    // ĐIỀU KIỆN: Số lượng trả vượt phần còn được trả. Không đạt thì Require ném lỗi
                    // và dừng luồng xử lý.
                    Require(
                        requested > 0 && requested <= remaining,
                        "Số lượng trả vượt phần còn được trả."
                    );
                    // BIẾN refund (decimal): tiền hoàn cho dòng trả sau phân bổ ưu đãi theo giá trị
                    // hàng, đã làm tròn tới 2 chữ số.
                    decimal refund = decimal.Round(
                        l.Price * requested * (o.Subtotal - o.Discount) / o.Subtotal,
                        2,
                        MidpointRounding.AwayFromZero
                    );
                    r.Lines.Add(
                        new()
                        {
                            OrderLineId = l.Id,
                            Quantity = requested,
                            Refund = refund,
                        }
                    );
                }

                // ĐIỀU KIỆN: Không có sản phẩm đủ điều kiện trả. Không đạt thì Require ném lỗi và

                // dừng luồng xử lý.
                Require(r.Lines.Count > 0, "Không có sản phẩm đủ điều kiện trả.");
                // BIẾN allRemaining (bool): true nếu phiếu hiện tại cùng các phiếu đã duyệt trả hết
                // mọi dòng, dùng tính nốt cả phần tiền còn lại gồm phí.
                bool allRemaining = o.Lines.All(l =>
                    Returned(d, id, l.Id)
                        + r.Lines.Where(x => x.OrderLineId == l.Id).Sum(x => x.Quantity)
                    == l.Quantity
                );
                r.Refund = allRemaining ? o.Total - o.RefundedAmount : r.Lines.Sum(l => l.Refund);
                // ĐIỀU KIỆN: Số tiền hoàn không hợp lệ. Không đạt thì Require ném lỗi và dừng luồng
                // xử lý.
                Require(r.Refund >= 0, "Số tiền hoàn không hợp lệ.");
                d.Returns.Add(r);
                return 0;
            },
            customerId,
            "Yêu cầu trả hàng"
        );

    // HÀM ResolveReturn: Sales/Admin duyệt hoặc từ chối phiếu Pending. Khi duyệt: kiểm tra tiền gốc
    // đã thu, hạn mức hoàn và chứng từ không trùng → hoàn tồn → ghi khoản Refund → tăng tiền đã
    // hoàn; trả hết các dòng thì đổi trạng thái đơn.
    // THAM SỐ returnId (int): mã phiếu yêu cầu trả hàng cần duyệt hoặc từ chối.
    // THAM SỐ approve (bool): true để duyệt trả hàng và thực hiện hoàn tồn/tiền; false để từ chối
    // yêu cầu.
    // THAM SỐ resolution (string): ghi chú kết quả kiểm tra và xử lý phiếu trả, lưu cùng người
    // duyệt và chứng từ.
    // THAM SỐ reference (string): mã chứng từ/biên nhận do người dùng cung cấp; các luồng tiền/nhập
    // kho kiểm tra chưa dùng để chống ghi trùng.
    // THAM SỐ actor (int): id tài khoản thực hiện thao tác: nhân viên thường là số âm; quyền và
    // trạng thái phải được service kiểm tra.
    // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ báo
    // lỗi cho caller.
    // BIẾN LAMBDA d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
    // không phải một bảng SQL riêng; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến
    // toàn cục.
    // BIẾN LAMBDA r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số tiền
    // hoàn; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA p (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời điểm mua; tên
    // này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // BIẾN LAMBDA x (PaymentRecord): bút toán thu hoặc hoàn tiền cho đơn, có số tiền và mã tham
    // chiếu; tên này chỉ có nghĩa trong lambda tương ứng, không phải biến toàn cục.
    // LIÊN KẾT XỬ LÝ: Write; xem định nghĩa các hàm này để theo tiếp bước đọc/ghi.
    public void ResolveReturn(
        int returnId,
        bool approve,
        string resolution,
        string reference,
        int actor
    ) =>
        Write(
            d =>
            {
                Permission(d, actor, "Sales");
                // BIẾN r (ReturnRecord): phiếu yêu cầu trả hàng với trạng thái, các dòng trả và số
                // tiền hoàn; kết quả tra cứu có thể null khi không khớp, xem nhánh kiểm tra hoặc ??
                // ngay sau.
                var r =
                    d.Returns.SingleOrDefault(r => r.Id == returnId)
                    ?? throw new InvalidOperationException("Không tìm thấy yêu cầu.");
                // ĐIỀU KIỆN: Yêu cầu đã xử lý. Không đạt thì Require ném lỗi và dừng luồng xử lý.
                Require(r.Status == "Pending", "Yêu cầu đã xử lý.");
                // BIẾN o (Order): đơn hàng gồm chủ đơn, người nhận, trạng thái, dòng hàng và các
                // khoản tiền.
                var o = FindOrder(d, r.OrderId);
                // ĐIỀU KIỆN: Đơn không còn đủ điều kiện trả. Không đạt thì Require ném lỗi và dừng
                // luồng xử lý.
                Require(o.Status is "Đã giao" or "Hoàn thành", "Đơn không còn đủ điều kiện trả.");
                r.Resolution = Text(resolution, 5, 500, "Ghi chú xử lý");
                r.ActorId = actor;
                r.Resolved = DateTime.UtcNow;
                // NHÁNH DUYỆT TRẢ: chỉ nhánh approve mới hoàn tồn và tiền; nhánh else đánh dấu từ
                // chối, không hoàn
                // hàng/tiền.
                if (approve)
                {
                    // ĐIỀU KIỆN: Cần đối soát đủ tiền gốc và số tiền còn được hoàn. Không đạt thì
                    // Require ném lỗi và dừng
                    // luồng ghi.
                    Require(
                        o.PaidAmount == o.Total
                            && r.Refund > 0
                            && r.Refund <= o.PaidAmount - o.RefundedAmount,
                        "Cần đối soát đủ tiền gốc và số tiền còn được hoàn."
                    );
                    reference = Text(reference, 1, 100, "Mã chứng từ hoàn tiền");
                    // ĐIỀU KIỆN: Chứng từ đã sử dụng. Không đạt thì Require ném lỗi và dừng luồng
                    // xử lý.
                    Require(
                        !d.Payments.Any(p =>
                            p.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase)
                        )
                            && !d.PaymentReferences.Contains(
                                reference,
                                StringComparer.OrdinalIgnoreCase
                            ),
                        "Chứng từ đã sử dụng."
                    );
                    // BIẾN LẶP returned (ReturnLine): dòng trả gắn với một dòng đơn, chứa lượng
                    // trả, tiền hoàn và tình trạng hàng; mỗi lượt nhận một phần tử của nguồn sau
                    // in, chỉ dùng trong vòng foreach này.
                    foreach (var returned in r.Lines)
                    {
                        // BIẾN l (OrderLine): dòng đơn chụp sản phẩm/giá/lượng/kho xuất tại thời
                        // điểm mua.
                        var l = o.Lines.Single(l => l.Id == returned.OrderLineId);
                        // ĐIỀU KIỆN: Số lượng trả vượt số đã mua. Không đạt thì Require ném lỗi và
                        // dừng luồng xử lý.
                        Require(
                            returned.Quantity + Returned(d, o.Id, l.Id) <= l.Quantity,
                            "Số lượng trả vượt số đã mua."
                        );
                        Restore(d, l, returned.Quantity);
                        returned.Condition = r.Resolution;
                    }

                    d.Payments.Add(
                        new()
                        {
                            Id = Next(d.Payments.Select(x => x.Id)),
                            OrderId = o.Id,
                            Kind = "Refund",
                            Amount = r.Refund,
                            Reference = reference,
                            ActorId = actor,
                        }
                    );
                    o.RefundedAmount += r.Refund;
                    r.Reference = reference;
                    r.Status = "Approved";
                    if (o.Lines.All(l => Returned(d, o.Id, l.Id) >= l.Quantity))
                        o.Status = "Đã hoàn hàng";
                }
                else
                    r.Status = "Rejected";
                return 0;
            },
            actor,
            "Nhận hàng trả và hoàn tiền"
        );
}
