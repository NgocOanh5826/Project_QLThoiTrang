// VAI TRÒ FILE: Dữ liệu khách/đơn/giỏ/ưu đãi/cấu hình, các ViewModel và mẫu nhập tài
// khoản/checkout. Các thuộc tính
// tính toán dùng giá server; DataAnnotations được ModelState/Validator áp dụng.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;

namespace Project_QLThoiTrang.Models;

// DỮ LIỆU: Tài khoản hiển thị chung: Id dương là khách, Id âm là nhân viên; Role quyết định quyền,

// Active quyết
// định còn dùng được, PasswordHash chỉ chứa mật khẩu đã băm.
public class Customer
{
    // THUỘC TÍNH Customer.LoginHistoryId (long): mã nhật ký đăng nhập để gắn vào cookie và ghi giờ
    // đăng xuất đúng phiên.
    public long LoginHistoryId { get; set; }

    // QUY ƯỚC MÃ: khách có Id > 0; nhân viên có Id < 0 và mã SQL NhanVien là -Id. Quy ước này giúp

    // tránh trùng
    // mã hai bảng.
    // THUỘC TÍNH Customer.Id (int): id tài khoản có dấu: dương là KhachHang, âm là NhanVien, không
    // phải cùng khóa SQL của hai bảng.
    public int Id { get; set; }
    // THUỘC TÍNH Customer.Name (string): họ tên tài khoản khách hoặc nhân viên được đưa vào cookie
    // hiển thị.
    public string Name { get; set; } = "";
    // THUỘC TÍNH Customer.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    public string Email { get; set; } = "";
    // THUỘC TÍNH Customer.Phone (string): số điện thoại lưu dạng chuỗi để giữ số 0 đầu và ký tự hợp
    // lệ.
    public string Phone { get; set; } = "";
    // THUỘC TÍNH Customer.Address (string): địa chỉ hồ sơ/cửa hàng/kho/NCC/người nhận tùy model,
    // không dùng làm khóa định danh.
    public string Address { get; set; } = "";

    // MẬT KHẨU: chỉ giữ Identity hash, không phải mật khẩu rõ; không đưa trường này vào HTML hoặc

    // form sửa hồ
    // sơ.
    // THUỘC TÍNH Customer.PasswordHash (string): hash Identity của mật khẩu; không phải mật khẩu rõ
    // và không gửi ra view.
    public string PasswordHash { get; set; } = "";

    // PHÂN QUYỀN: Customer, Admin, Catalog, Inventory, Sales hoặc Support; controller và service

    // cùng kiểm tra
    // Role khi thao tác.
    // THUỘC TÍNH Customer.Role (string): vai trò Customer/Admin/Catalog/Inventory/Sales/Support,
    // dùng kiểm tra quyền ở controller/service.
    public string Role { get; set; } = "Customer";
    // THUỘC TÍNH Customer.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối tượng;
    // không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
    // THUỘC TÍNH Customer.LegacyId (int?): mã từ dữ liệu cũ để đối chiếu khi nhập/chuyển đổi, khác
    // mã mới dùng ở bảng hiện tại.
    public int? LegacyId { get; set; }
}

// DỮ LIỆU: Đầu đơn và các dòng hàng; DatabaseId là khóa SQL, Id là mã công khai. Tổng tiền = tiền

// hàng + vận chuyển
// - giảm giá; tiền thu/hoàn và trạng thái được service kiểm tra.
public class Order
{
    // THUỘC TÍNH Order.BankQrImage (string?): ảnh QR được chụp vào đơn lúc chọn chuyển khoản để
    // không thay theo cấu hình sau này.
    public string? BankQrImage { get; set; }
    // THUỘC TÍNH Order.TransferReference (string?): nội dung chuyển khoản được tạo cho đơn để đối
    // soát tiền nhận.
    public string? TransferReference { get; set; }

    // KHÓA SQL: MaDonHang số nguyên trong bảng DonHang; khác mã công khai Id dùng ở URL và chứng

    // từ.
    // THUỘC TÍNH Order.DatabaseId (int): khóa nguyên MaDonHang trong SQL, khác Id công khai QH-...
    // dùng ở URL.
    public int DatabaseId { get; set; }

    // MÃ CÔNG KHAI: mã QH-... hoặc QH-SQL-... cho đơn cũ; service/controller tra mã này rồi đối

    // chiếu chủ
    // đơn/quyền.
    // THUỘC TÍNH Order.Id (string): mã đơn công khai QH-... hoặc QH-SQL-..., khác khóa số nguyên
    // DatabaseId.
    public string Id { get; set; } = "";
    // THUỘC TÍNH Order.CustomerId (int): mã khách hàng dương mà đơn/giỏ/hội thoại/OTP thuộc về,
    // dùng giới hạn đúng chủ dữ liệu.
    public int CustomerId { get; set; }
    // THUỘC TÍNH Order.Created (DateTime): thời điểm tạo bản ghi; model dùng DateTime.Now hoặc
    // UtcNow như giá trị khởi tạo/luồng gán tương ứng, không mặc định đều UTC.
    public DateTime Created { get; set; } = DateTime.Now;
    // THUỘC TÍNH Order.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo, khác mã
    // định danh Id.
    public string Name { get; set; } = "";
    // THUỘC TÍNH Order.Phone (string): số điện thoại lưu dạng chuỗi để giữ số 0 đầu và ký tự hợp
    // lệ.
    public string Phone { get; set; } = "";
    // THUỘC TÍNH Order.Address (string): địa chỉ hồ sơ/cửa hàng/kho/NCC/người nhận tùy model, không
    // dùng làm khóa định danh.
    public string Address { get; set; } = "";
    // THUỘC TÍNH Order.Note (string): ghi chú của thao tác/đơn; ở giá vốn và công nợ là căn cứ điều
    // chỉnh để đối soát.
    public string Note { get; set; } = "";
    // THUỘC TÍNH Order.Status (string): trạng thái đơn, chỉ chuyển theo allowed trong
    // ChangeOrderStatus; không cho client tự bỏ qua các bước.
    public string Status { get; set; } = "Chờ xác nhận";
    // THUỘC TÍNH Order.Payment (string): phương thức của đơn; form CheckoutInput nhận COD hoặc
    // Chuyển khoản theo kiểm tra service.
    public string Payment { get; set; } = "Thanh toán khi nhận hàng";
    // THUỘC TÍNH Order.Lines (List<OrderLine>): các dòng hàng chụp lúc đặt đơn; một biến thể có thể
    // tách nhiều dòng khi xuất từ nhiều kho.
    public List<OrderLine> Lines { get; set; } = new();

    // CÔNG THỨC: tổng (Price × Quantity) của các dòng; chưa cộng vận chuyển và chưa trừ ưu đãi.
    // THUỘC TÍNH Order.Subtotal (decimal): tiền hàng VND = tổng đơn giá × số lượng các dòng, chưa
    // tính vận chuyển/giảm giá.
    public decimal Subtotal => Lines.Sum(x => x.Price * x.Quantity);

    // CHỤP PHÍ: giữ phí lúc đặt để đơn cũ không thay theo cấu hình/giỏ hiện tại; null thì dùng

    // công thức mặc
    // định bên dưới.
    // THUỘC TÍNH Order.ShippingSnapshot (decimal?): phí vận chuyển đã chụp lúc đặt đơn; null thì
    // dùng công thức mặc định của model.
    public decimal? ShippingSnapshot { get; set; }

    // CÔNG THỨC MẶC ĐỊNH: tiền hàng >= 700.000đ miễn phí, còn lại 30.000đ; ShippingSnapshot được

    // ưu tiên nếu đã
    // lưu.
    // THUỘC TÍNH Order.Shipping (decimal): phí vận chuyển VND; dùng snapshot nếu có, mặc định 0 từ
    // 700.000đ tiền hàng và 30.000đ dưới ngưỡng.
    public decimal Shipping => ShippingSnapshot ?? (Subtotal >= 700000 ? 0 : 30000);
    // THUỘC TÍNH Order.Discount (decimal): số tiền VND được giảm trên đơn, không được vượt tiền
    // hàng theo kiểm tra Validate.
    public decimal Discount { get; set; }
    // THUỘC TÍNH Order.PromotionCode (string): mã ưu đãi người dùng nhập/đơn đã áp dụng, được đối
    // chiếu chương trình còn hạn và đủ điều kiện.
    public string PromotionCode { get; set; } = "";

    // CHỐNG TRÙNG: một lần mở checkout có một token; PlaceOrder trả đơn cũ khi token đã lưu của

    // đúng khách.
    // THUỘC TÍNH Order.CheckoutToken (string?): Guid dạng N của một lần checkout, dùng đối chiếu
    // Session và chống tạo đơn trùng.
    public string? CheckoutToken { get; set; }

    // TIỀN THU THỰC GHI NHẬN: chỉ tăng qua RecordPayment, phải nằm trong 0..Total; hiển thị QR

    // không tự tăng số
    // này.
    // THUỘC TÍNH Order.PaidAmount (decimal): tổng tiền VND đã được ghi nhận thu của đơn; hiển thị
    // QR không tự tăng trường này.
    public decimal PaidAmount { get; set; }

    // TIỀN ĐÃ HOÀN: tăng khi duyệt phiếu trả, không vượt PaidAmount; phiếu chờ hoặc bị từ chối

    // chưa làm tăng
    // tiền hoàn.
    // THUỘC TÍNH Order.RefundedAmount (decimal): tổng tiền VND đã được duyệt hoàn của đơn, không
    // vượt số tiền đã thu.
    public decimal RefundedAmount { get; set; }

    // TRẠNG THÁI SUY RA: ưu tiên tiền hoàn, sau đó xét PaidAmount >= Total; không phải một ô nhập

    // tùy ý từ trình
    // duyệt.
    // THUỘC TÍNH Order.PaymentStatus (string): trạng thái hiển thị suy ra từ
    // PaidAmount/RefundedAmount/Total, không phải ô sửa tự do.
    public string PaymentStatus =>
        RefundedAmount > 0 ? (RefundedAmount < Total ? "Đã hoàn một phần" : "Đã hoàn tiền")
        : PaidAmount >= Total ? "Đã thanh toán"
        : "Chưa thanh toán";

    // CÔNG THỨC: Subtotal + Shipping - Discount; RecordPayment yêu cầu đúng phần Total -

    // PaidAmount khi thu.
    // THUỘC TÍNH Order.Total (decimal): tổng VND cần thu của đơn/giỏ sau cộng phí và trừ giảm giá
    // nếu model có Discount.
    public decimal Total => Subtotal + Shipping - Discount;

    // DOANH THU HÀNG THUẦN: tiền hàng trừ giảm giá và phần hoàn được giới hạn bằng tiền hàng sau

    // giảm; không bao
    // gồm phí vận chuyển.
    // THUỘC TÍNH Order.NetMerchandiseRevenue (decimal): doanh thu hàng sau giảm/hoàn, không gồm phí
    // vận chuyển.
    public decimal NetMerchandiseRevenue =>
        Math.Max(0, Subtotal - Discount - Math.Min(RefundedAmount, Subtotal - Discount));
}

// DỮ LIỆU: Ảnh chụp dòng hàng lúc mua: tên, giá, size/màu, biến thể, kho xuất và giá vốn. Một biến

// thể có thể tách
// nhiều dòng nếu xuất từ nhiều kho.
public class OrderLine
{
    // GIÁ VỐN CHỤP LÚC XUẤT: nullable để phân biệt chưa xác định với giá vốn 0; khai báo giá vốn
    // mới không sửa
    // ngược dòng đơn cũ.
    // THUỘC TÍNH OrderLine.UnitCost (decimal?): giá nhập/giá vốn VND cho một đơn vị; OrderLine giữ
    // ảnh chụp lúc mua, StockMovement/ReceiveInput ghi giá nhập của giao dịch.
    public decimal? UnitCost { get; set; }
    // THUỘC TÍNH OrderLine.VariantId (int): mã biến thể của sản phẩm theo tổ hợp size/màu; dùng tìm
    // đúng tồn, giá riêng hoặc giá vốn.
    public int VariantId { get; set; }
    // THUỘC TÍNH OrderLine.Color (string): tên màu hiển thị, khác ColorId/MaMauSac.
    public string Color { get; set; } = "";

    // KHO XUẤT: ghi lại kho đã trừ tồn; hủy/duyệt trả dùng thông tin này để hoàn hàng đúng kho.
    // THUỘC TÍNH OrderLine.WarehouseId (int?): khóa kho liên quan; nullable khi bản ghi cũ hoặc
    // loại thao tác không chỉ rõ một kho.
    public int? WarehouseId { get; set; }
    // THUỘC TÍNH OrderLine.Id (int): mã định danh của OrderLine; với mẫu nhập quản trị, 0 thường
    // biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH OrderLine.OrderId (string): mã đơn công khai mà đối tượng này liên kết.
    public string OrderId { get; set; } = "";
    // THUỘC TÍNH OrderLine.ProductId (int): mã sản phẩm gốc được biến thể/dòng đơn/nhật ký/đánh giá
    // tham chiếu.
    public int ProductId { get; set; }
    // THUỘC TÍNH OrderLine.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo, khác
    // mã định danh Id.
    public string Name { get; set; } = "";
    // THUỘC TÍNH OrderLine.Image (string): đường dẫn ảnh hiển thị của sản phẩm/dòng đơn; không phải
    // byte tệp đánh giá.
    public string Image { get; set; } = "";
    // THUỘC TÍNH OrderLine.Size (string): tên size như M/L; khác mã khóa SizeId/MaKichThuoc.
    public string Size { get; set; } = "M";
    // THUỘC TÍNH OrderLine.Quantity (int): số đơn vị mua từ kho của dòng đã chụp, dùng tính tiền và
    // mức được trả.
    public int Quantity { get; set; }
    // THUỘC TÍNH OrderLine.Price (decimal): đơn giá VND/đơn vị đã chụp lúc mua, không lấy lại giá
    // mới khi in/xem đơn.
    public decimal Price { get; set; }
}

// DỮ LIỆU: Dòng giỏ chỉ giữ mã sản phẩm/biến thể, size/màu và lượng; giá được lấy lại từ server

// khi dựng giỏ và đặt
// hàng.
public class CartItem
{
    // THUỘC TÍNH CartItem.ProductId (int): mã sản phẩm gốc được biến thể/dòng đơn/nhật ký/đánh giá
    // tham chiếu.
    public int ProductId { get; set; }
    // THUỘC TÍNH CartItem.VariantId (int): mã biến thể của sản phẩm theo tổ hợp size/màu; dùng tìm
    // đúng tồn, giá riêng hoặc giá vốn.
    public int VariantId { get; set; }
    // THUỘC TÍNH CartItem.Size (string): tên size như M/L; khác mã khóa SizeId/MaKichThuoc.
    public string Size { get; set; } = "M";
    // THUỘC TÍNH CartItem.Color (string): tên màu hiển thị, khác ColorId/MaMauSac.
    public string Color { get; set; } = "";
    // THUỘC TÍNH CartItem.Quantity (int): số đơn vị muốn mua của biến thể, 1–20 sau khi lưu giỏ.
    public int Quantity { get; set; } = 1;
}

// DỮ LIỆU: Điều kiện ưu đãi: PHANTRAM hoặc SOTIEN, thời gian From/Until, giá trị tối thiểu, trần

// giảm và số lượt
// còn lại. ProductIds rỗng nghĩa là không giới hạn theo danh sách sản phẩm.
public class Promotion
{
    // LOẠI GIẢM: PHANTRAM tính phần trăm trên hàng đủ điều kiện; SOTIEN giảm số tiền cố định, luôn
    // bị giới hạn
    // bởi hàng/trần giảm.
    // THUỘC TÍNH Promotion.Kind (string): PHANTRAM giảm theo tỷ lệ, SOTIEN giảm số VND cố định.
    public string Kind { get; set; } = "PHANTRAM";
    // THUỘC TÍNH Promotion.Value (decimal): mức ưu đãi: số tiền hoặc phần trăm tùy Kind; khi dữ
    // liệu cũ chưa có Value thì luồng tính có thể dùng Percent.
    public decimal Value { get; set; }
    // THUỘC TÍNH Promotion.Minimum (decimal): tiền hàng tối thiểu VND để mã ưu đãi được chấp nhận.
    public decimal Minimum { get; set; }
    // THUỘC TÍNH Promotion.Maximum (decimal?): trần tiền VND được giảm; null nghĩa không đặt trần
    // riêng ngoài giá trị hàng đủ điều kiện.
    public decimal? Maximum { get; set; }

    // LƯỢT: null nghĩa không đặt giới hạn; có số thì phải > 0 lúc áp dụng, đặt thành công giảm 1

    // và hủy đơn có
    // thể trả lại 1 lượt.
    // THUỘC TÍNH Promotion.Remaining (int?): lượt ưu đãi còn lại; null không giới hạn, có số phải
    // còn dương lúc đặt.
    public int? Remaining { get; set; }
    // THUỘC TÍNH Promotion.From (DateTime): ngày bắt đầu hiệu lực ưu đãi; không được sau Until.
    public DateTime From { get; set; } = DateTime.Today;

    // PHẠM VI ÁP DỤNG: danh sách rỗng áp dụng không giới hạn theo sản phẩm; có mã thì chỉ cộng các

    // dòng thuộc
    // danh sách.
    // THUỘC TÍNH Promotion.ProductIds (List<int>): các mã sản phẩm được ưu đãi áp dụng; danh sách
    // rỗng không giới hạn theo sản phẩm.
    public List<int> ProductIds { get; set; } = new();
    // THUỘC TÍNH Promotion.Id (int): mã định danh của Promotion; với mẫu nhập quản trị, 0 thường
    // biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Promotion.Name (string): tên hiển thị chương trình ưu đãi.
    [Required, StringLength(120)]
    public string Name { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Promotion.Code (string): mã ưu đãi khách nhập; POST chuẩn hóa chữ hoa và kiểm tra
    // chưa trùng chương trình khác.
    [Required, StringLength(30)]
    public string Code { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Promotion.Percent (int): phần trăm giảm của cấu hình ưu đãi tương thích cũ, có
    // attribute giới hạn 1–100.
    [Range(1, 100)]
    public int Percent { get; set; } = 10;
    // THUỘC TÍNH Promotion.Until (DateTime): ngày kết thúc hiệu lực ưu đãi, so sánh phần Date khi
    // áp dụng.
    public DateTime Until { get; set; } = DateTime.Today.AddDays(30);
    // THUỘC TÍNH Promotion.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối
    // tượng; không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
}

// DỮ LIỆU: Thông tin cửa hàng có kiểm tra bắt buộc, độ dài và định dạng email; form quản trị

// Settings cập nhật đối
// tượng này.
public class StoreSettings
{
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào
    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StoreSettings.Name (string): tên cửa hàng hiển thị ở layout và thông tin liên hệ.
    [Required, StringLength(80)]
    public string Name { get; set; } = "QOANH";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; đúng định dạng email. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StoreSettings.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    [Required, EmailAddress]
    public string Email { get; set; } = "hello@qoanh.example";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StoreSettings.Phone (string): số điện thoại lưu dạng chuỗi để giữ số 0 đầu và ký
    // tự hợp lệ.
    [Required, StringLength(30)]
    public string Phone { get; set; } = "0900 123 456";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StoreSettings.Address (string): địa chỉ hồ sơ/cửa hàng/kho/NCC/người nhận tùy
    // model, không dùng làm khóa định danh.
    [Required, StringLength(250)]
    public string Address { get; set; } = "TP. Hồ Chí Minh";
}

// DỮ LIỆU: Ảnh dữ liệu nghiệp vụ được Load từ SQL; callback Read/Write thao tác các danh sách này.

// PersistNative
// đồng bộ về bảng thật trước Commit.
public class StoreData
{
    // THUỘC TÍNH StoreData.Shipments (List<Shipment>): các vận đơn được Load vào ảnh StoreData,
    // dùng kiểm tra trạng thái GHN liên quan tới đơn.
    public List<Shipment> Shipments { get; set; } = new();
    // THUỘC TÍNH StoreData.ImportedFileHash (string?): dấu vết SHA-256/tên nhận diện lần nhập dữ
    // liệu cũ, dùng kiểm soát nhập lại.
    public string? ImportedFileHash { get; set; }
    // THUỘC TÍNH StoreData.Variants (List<Variant>): các biến thể của sản phẩm hoặc toàn cửa hàng
    // theo model chứa thuộc tính.
    public List<Variant> Variants { get; set; } = new();
    // THUỘC TÍNH StoreData.Stocks (List<WarehouseStock>): tồn chi tiết theo biến thể/kho, nguồn để
    // phân bổ xuất hoặc điều chỉnh tồn.
    public List<WarehouseStock> Stocks { get; set; } = new();
    // THUỘC TÍNH StoreData.Warehouses (List<Warehouse>): danh sách kho đã Load; các luồng chọn kho
    // còn phải kiểm tra Active.
    public List<Warehouse> Warehouses { get; set; } = new();
    // THUỘC TÍNH StoreData.Suppliers (List<Supplier>): danh sách nhà cung cấp đã Load từ SQL.
    public List<Supplier> Suppliers { get; set; } = new();
    // THUỘC TÍNH StoreData.History (List<OrderHistory>): các mốc lịch sử thay đổi trạng thái đơn.
    public List<OrderHistory> History { get; set; } = new();
    // THUỘC TÍNH StoreData.Products (List<Product>): danh sách sản phẩm, có thể là toàn dữ liệu
    // StoreData hoặc tập đã lọc của CatalogViewModel.
    public List<Product> Products { get; set; } = new();
    // THUỘC TÍNH StoreData.Customers (List<Customer>): danh sách tài khoản đã đổi sang mô hình
    // Customer dùng chung, gồm khách dương/nhân viên âm.
    public List<Customer> Customers { get; set; } = new();
    // THUỘC TÍNH StoreData.Orders (List<Order>): danh sách đơn hàng nghiệp vụ đã ghép chi
    // tiết/tiền/trạng thái.
    public List<Order> Orders { get; set; } = new();
    // THUỘC TÍNH StoreData.Promotions (List<Promotion>): các chương trình ưu đãi được Load để kiểm
    // tra khi đặt hàng.
    public List<Promotion> Promotions { get; set; } = new();
    // THUỘC TÍNH StoreData.Categories (List<string>): các tên danh mục để hiển thị/lọc/chọn trong
    // form.
    public List<string> Categories { get; set; } = new() { "Nữ", "Nam", "Trẻ em", "Phụ kiện" };
    // THUỘC TÍNH StoreData.Settings (StoreSettings): thông tin cấu hình hiển thị cửa hàng như tên,
    // email, số điện thoại và địa chỉ.
    public StoreSettings Settings { get; set; } = new();
    // THUỘC TÍNH StoreData.StockMovements (List<StockMovement>): nhật ký biến động tồn hiện có và
    // các dòng callback vừa thêm trước PersistNative.
    public List<StockMovement> StockMovements { get; set; } = new();
    // THUỘC TÍNH StoreData.Payments (List<PaymentRecord>): các bút toán thu/hoàn tiền nghiệp vụ của
    // đơn.
    public List<PaymentRecord> Payments { get; set; } = new();
    // THUỘC TÍNH StoreData.PaymentReferences (List<string>): các mã chứng từ đã dùng để kiểm tra
    // chống ghi trùng với dữ liệu thanh toán cũ.
    public List<string> PaymentReferences { get; set; } = new();
    // THUỘC TÍNH StoreData.Returns (List<ReturnRecord>): các phiếu trả và chi tiết của chúng, dùng
    // tính lượng/tiền còn được trả.
    public List<ReturnRecord> Returns { get; set; } = new();
}

// DỮ LIỆU: Dữ liệu danh sách bán hàng và trạng thái lọc/sắp xếp để view giữ lại lựa chọn khi tải

// trang.
public class CatalogViewModel
{
    // THUỘC TÍNH CatalogViewModel.Products (List<Product>): danh sách sản phẩm, có thể là toàn dữ
    // liệu StoreData hoặc tập đã lọc của CatalogViewModel.
    public List<Product> Products { get; set; } = new();
    // THUỘC TÍNH CatalogViewModel.Categories (List<string>): các tên danh mục để hiển thị/lọc/chọn
    // trong form.
    public List<string> Categories { get; set; } = new();
    // THUỘC TÍNH CatalogViewModel.Query (string?): từ khóa tìm kiếm cần giữ lại khi render
    // form/danh sách.
    public string? Query { get; set; }
    // THUỘC TÍNH CatalogViewModel.Category (string?): tên danh mục sản phẩm hoặc lựa chọn danh mục
    // đang lọc trong view model.
    public string? Category { get; set; }
    // THUỘC TÍNH CatalogViewModel.Sort (string): giá trị lựa chọn sắp xếp để view và controller
    // thống nhất thứ tự sản phẩm.
    public string Sort { get; set; } = "new";
    // THUỘC TÍNH CatalogViewModel.Collection (string?): bộ sưu tập new/sale đang được lọc; null
    // không chọn riêng.
    public string? Collection { get; set; }
}

// DỮ LIỆU: Giỏ để hiển thị: cộng đơn giá × số lượng; phí 30.000đ nếu giỏ có hàng và tiền hàng dưới

// 700.000đ, ngược
// lại miễn phí.
public class CartViewModel
{
    // THUỘC TÍNH CartViewModel.Lines (List<OrderLine>): các dòng giỏ đã ghép sản phẩm/biến thể/giá
    // để hiển thị checkout.
    public List<OrderLine> Lines { get; set; } = new();
    // THUỘC TÍNH CartViewModel.Subtotal (decimal): tiền hàng VND = tổng đơn giá × số lượng các
    // dòng, chưa tính vận chuyển/giảm giá.
    public decimal Subtotal => Lines.Sum(x => x.Price * x.Quantity);
    // THUỘC TÍNH CartViewModel.Shipping (decimal): phí giỏ VND: 0 nếu giỏ rỗng hoặc tiền hàng từ
    // 700.000đ; còn lại 30.000đ.
    public decimal Shipping => Lines.Count == 0 || Subtotal >= 700000 ? 0 : 30000;

    // TỔNG GIỎ HIỂN THỊ: Subtotal + Shipping; ưu đãi được kiểm tra và trừ trong PlaceOrder khi tạo

    // đơn.
    // THUỘC TÍNH CartViewModel.Total (decimal): tổng VND cần thu của đơn/giỏ sau cộng phí và trừ
    // giảm giá nếu model có Discount.
    public decimal Total => Subtotal + Shipping;
}

// DỮ LIỆU: Mẫu form đặt hàng: người nhận, điện thoại, địa chỉ, ghi chú, ưu đãi, phương thức và

// checkout token chống
// gửi trùng; service còn kiểm tra lại giỏ/giá/tồn.
public class CheckoutInput
{
    // THUỘC TÍNH CheckoutInput.Payment (string): phương thức của đơn; form CheckoutInput nhận COD
    // hoặc Chuyển khoản theo kiểm tra service.
    public string Payment { get; set; } = "COD";

    // TOKEN FORM: phải trùng Session checkout-token; service còn kiểm tra định dạng Guid và đối

    // chiếu đơn đã có
    // để chống bấm đặt hai lần.
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập. MVC ghi lỗi vào ModelState; các luồng
    // Validator.ValidateObject cũng
    // kiểm tra các attribute này.
    // THUỘC TÍNH CheckoutInput.CheckoutToken (string): Guid dạng N của một lần checkout, dùng đối
    // chiếu Session và chống tạo đơn trùng.
    [Required]
    public string CheckoutToken { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH CheckoutInput.PromotionCode (string?): mã ưu đãi người dùng nhập/đơn đã áp dụng,
    // được đối chiếu chương trình còn hạn và đủ điều kiện.
    [StringLength(30)]
    public string? PromotionCode { get; set; }

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH CheckoutInput.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo,
    // khác mã định danh Id.
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)]
    public string Name { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; khớp mẫu RegularExpression. MVC ghi lỗi vào ModelState;

    // các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH CheckoutInput.Phone (string): số điện thoại lưu dạng chuỗi để giữ số 0 đầu và ký
    // tự hợp lệ.
    [
        Required(ErrorMessage = "Nhập số điện thoại"),
        RegularExpression(@"[0-9+ ()-]{9,20}", ErrorMessage = "Số điện thoại chưa hợp lệ")
    ]
    public string Phone { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH CheckoutInput.Address (string): địa chỉ hồ sơ/cửa hàng/kho/NCC/người nhận tùy
    // model, không dùng làm khóa định danh.
    [Required(ErrorMessage = "Nhập địa chỉ giao hàng"), StringLength(300)]
    public string Address { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH CheckoutInput.Note (string): ghi chú của thao tác/đơn; ở giá vốn và công nợ là căn
    // cứ điều chỉnh để đối soát.
    [StringLength(500)]
    public string Note { get; set; } = "";
}

// DỮ LIỆU: Mẫu email/mật khẩu đăng nhập, tùy chọn ghi nhớ và URL quay về; controller chỉ chấp nhận

// ReturnUrl nội
// bộ.
public class LoginInput
{
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; đúng định dạng email. MVC ghi lỗi vào ModelState; các
    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH LoginInput.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    [
        Required(ErrorMessage = "Nhập email của bạn"),
        EmailAddress(ErrorMessage = "Email chưa đúng định dạng")
    ]
    public string Email { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập. MVC ghi lỗi vào ModelState; các luồng

    // Validator.ValidateObject cũng
    // kiểm tra các attribute này.
    // THUỘC TÍNH LoginInput.Password (string): mật khẩu rõ đầu vào của form/cấu hình, chỉ dùng xác
    // minh hoặc băm; không ghi mật khẩu tài khoản rõ vào SQL.
    [Required(ErrorMessage = "Nhập mật khẩu")]
    public string Password { get; set; } = "";
    // THUỘC TÍNH LoginInput.RememberMe (bool): true cho phép đăng nhập ghi nhớ, quyết định cookie
    // có lưu lâu và thời hạn của SignIn.
    public bool RememberMe { get; set; }
    // THUỘC TÍNH LoginInput.ReturnUrl (string?): đường dẫn nội bộ muốn quay lại sau đăng nhập/đăng
    // ký; được controller kiểm tra IsLocalUrl.
    public string? ReturnUrl { get; set; }
}

// DỮ LIỆU: Mẫu đăng ký: tên/email hợp lệ, mật khẩu 8–100 ký tự có chữ/số và ConfirmPassword phải

// trùng. Không cho
// nhập vai trò từ form.
public class RegisterInput
{
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào
    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH RegisterInput.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo,
    // khác mã định danh Id.
    [
        Required(ErrorMessage = "Nhập họ tên"),
        StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 100 ký tự")
    ]
    public string Name { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute; đúng định dạng

    // email. MVC ghi
    // lỗi vào ModelState; các luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH RegisterInput.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    [
        Required(ErrorMessage = "Nhập email"),
        EmailAddress(ErrorMessage = "Email chưa đúng định dạng"),
        StringLength(150)
    ]
    public string Email { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute; độ dài tối thiểu

    // theo
    // attribute; khớp mẫu RegularExpression. MVC ghi lỗi vào ModelState; các luồng
    // Validator.ValidateObject cũng
    // kiểm tra các attribute này.
    // THUỘC TÍNH RegisterInput.Password (string): mật khẩu rõ đầu vào của form/cấu hình, chỉ dùng
    // xác minh hoặc băm; không ghi mật khẩu tài khoản rõ vào SQL.
    [
        Required(ErrorMessage = "Tạo mật khẩu"),
        MinLength(8, ErrorMessage = "Mật khẩu cần ít nhất 8 ký tự"),
        StringLength(100, ErrorMessage = "Mật khẩu không được quá 100 ký tự"),
        RegularExpression(
            @"^(?=.*\p{L})(?=.*\d).+$",
            ErrorMessage = "Mật khẩu cần có ít nhất một chữ cái và một chữ số"
        )
    ]
    public string Password { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; trùng trường được chỉ định trong Compare. MVC ghi lỗi vào

    // ModelState;
    // các luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH RegisterInput.ConfirmPassword (string): mật khẩu nhập lại, Compare yêu cầu trùng
    // trường mật khẩu được chỉ định.
    [
        Required(ErrorMessage = "Nhập lại mật khẩu"),
        Compare("Password", ErrorMessage = "Mật khẩu xác nhận chưa khớp")
    ]
    public string ConfirmPassword { get; set; } = "";
    // THUỘC TÍNH RegisterInput.ReturnUrl (string?): đường dẫn nội bộ muốn quay lại sau đăng
    // nhập/đăng ký; được controller kiểm tra IsLocalUrl.
    public string? ReturnUrl { get; set; }
}

// DỮ LIỆU: Mẫu sửa hồ sơ chỉ gồm tên, điện thoại và địa chỉ; không chứa trường email, vai trò hoặc

// mật khẩu.
public class ProfileInput
{
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào
    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ProfileInput.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo,
    // khác mã định danh Id.
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ProfileInput.Phone (string?): số điện thoại lưu dạng chuỗi để giữ số 0 đầu và ký
    // tự hợp lệ.
    [StringLength(15)]
    public string? Phone { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ProfileInput.Address (string?): địa chỉ hồ sơ/cửa hàng/kho/NCC/người nhận tùy
    // model, không dùng làm khóa định danh.
    [StringLength(255)]
    public string? Address { get; set; } = "";
}
