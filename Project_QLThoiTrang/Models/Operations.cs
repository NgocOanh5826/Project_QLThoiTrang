// VAI TRÒ FILE: Mô hình kho, biến thể, thanh toán, trả hàng và ReceiveInput. Đọc StoreOperations.cs
// để thấy nơi
// kiểm tra điều kiện và ghi nhật ký cho các đối tượng này.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;

namespace Project_QLThoiTrang.Models;

// DỮ LIỆU: Mô hình danh mục sản phẩm; việc thêm/xóa danh mục giao diện hiện tại xử lý qua danh

// sách
// StoreData.Categories.
public class Category
{
    // THUỘC TÍNH Category.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo, khác
    // mã định danh Id.
    [MaxLength(50)]
    public string Name { get; set; } = "";
}

// DỮ LIỆU: Bản ghi cấu hình QOANH chứa Settings và dấu vết nhập dữ liệu; cấu hình được

// PersistNative đọc/ghi.
public class StoreConfiguration
{
    // THUỘC TÍNH StoreConfiguration.Id (int): mã định danh của StoreConfiguration; với mẫu nhập
    // quản trị, 0 thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; } = 1;
    // THUỘC TÍNH StoreConfiguration.Settings (StoreSettings): thông tin cấu hình hiển thị cửa hàng
    // như tên, email, số điện thoại và địa chỉ.
    public StoreSettings Settings { get; set; } = new();
    // THUỘC TÍNH StoreConfiguration.ImportedFileHash (string?): dấu vết SHA-256/tên nhận diện lần
    // nhập dữ liệu cũ, dùng kiểm soát nhập lại.
    public string? ImportedFileHash { get; set; }
}

// DỮ LIỆU: Nhật ký biến động tồn: Before + Delta = After, kèm sản phẩm/biến thể/kho, người thao

// tác, lý do và mã
// chứng từ; không sửa số tồn bằng cách sửa nhật ký này.
public class StockMovement
{
    // THUỘC TÍNH StockMovement.VariantId (int?): mã biến thể của sản phẩm theo tổ hợp size/màu;
    // dùng tìm đúng tồn, giá riêng hoặc giá vốn.
    public int? VariantId { get; set; }
    // THUỘC TÍNH StockMovement.WarehouseId (int?): khóa kho liên quan; nullable khi bản ghi cũ hoặc
    // loại thao tác không chỉ rõ một kho.
    public int? WarehouseId { get; set; }
    // THUỘC TÍNH StockMovement.SupplierId (int?): khóa nhà cung cấp của lần nhập hàng, khác tên nhà
    // cung cấp dạng chuỗi.
    public int? SupplierId { get; set; }
    // THUỘC TÍNH StockMovement.ReceiptReference (string?): mã chung của đầu phiếu nhập, dùng gom
    // nhiều biến động biến thể thành một chứng từ.
    public string? ReceiptReference { get; set; }
    // THUỘC TÍNH StockMovement.Id (int): mã định danh của StockMovement; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH StockMovement.ProductId (int): mã sản phẩm gốc được biến thể/dòng đơn/nhật ký/đánh
    // giá tham chiếu.
    public int ProductId { get; set; }

    // TỒN TRƯỚC: ảnh số tồn trước giao dịch dùng để đối soát; ràng buộc SQL yêu cầu Before + Delta

    // = After.
    // THUỘC TÍNH StockMovement.Before (int): số tồn trước thao tác, dùng đối soát với Delta và
    // After.
    public int Before { get; set; }

    // CHÊNH LỆCH: dương là tăng tồn, âm là giảm tồn; đi cùng Before/After, lý do và mã tham chiếu.
    // THUỘC TÍNH StockMovement.Delta (int): chênh lệch tồn có dấu; tăng dương, giảm âm; Before +
    // Delta phải bằng After.
    public int Delta { get; set; }
    // THUỘC TÍNH StockMovement.After (int): số tồn sau thao tác; phải không âm và khớp Before +
    // Delta.
    public int After { get; set; }
    // THUỘC TÍNH StockMovement.Reason (string): lý do nhập/kiểm kê/trả hàng theo model, được lưu để
    // đối soát thao tác.
    public string Reason { get; set; } = "";
    // THUỘC TÍNH StockMovement.Reference (string): mã tham chiếu hoặc chứng từ; luồng lưu kiểm tra
    // trùng theo loại nghiệp vụ.
    public string Reference { get; set; } = "";
    // THUỘC TÍNH StockMovement.Supplier (string): tên nhà cung cấp dùng hiển thị hoặc nhánh nhập dữ
    // liệu tương thích.
    public string Supplier { get; set; } = "";
    // THUỘC TÍNH StockMovement.UnitCost (decimal): giá nhập/giá vốn VND cho một đơn vị; OrderLine
    // giữ ảnh chụp lúc mua, StockMovement/ReceiveInput ghi giá nhập của giao dịch.
    public decimal UnitCost { get; set; }
    // THUỘC TÍNH StockMovement.ActorId (int?): id tài khoản thực hiện biến động tồn; nullable nếu
    // tác vụ không truyền actor.
    public int? ActorId { get; set; }
    // THUỘC TÍNH StockMovement.Created (DateTime): thời điểm tạo bản ghi; model dùng DateTime.Now
    // hoặc UtcNow như giá trị khởi tạo/luồng gán tương ứng, không mặc định đều UTC.
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

// DỮ LIỆU: Sổ thu/hoàn tiền của đơn, Kind phân biệt Receipt/Refund; Amount dương và Reference duy

// nhất để chống ghi
// nhận cùng chứng từ hai lần.
public class PaymentRecord
{
    // THUỘC TÍNH PaymentRecord.Id (int): mã định danh của PaymentRecord; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH PaymentRecord.OrderId (string): mã đơn công khai mà đối tượng này liên kết.
    public string OrderId { get; set; } = "";
    // THUỘC TÍNH PaymentRecord.Kind (string): Receipt là thu tiền, Refund là hoàn tiền; Amount vẫn
    // là số dương.
    public string Kind { get; set; } = "Receipt";
    // THUỘC TÍNH PaymentRecord.Amount (decimal): số tiền VND của bút toán/chứng từ hoặc số phải trả
    // đã đối soát, tùy lớp chứa thuộc tính.
    public decimal Amount { get; set; }
    // THUỘC TÍNH PaymentRecord.Reference (string): mã tham chiếu hoặc chứng từ; luồng lưu kiểm tra
    // trùng theo loại nghiệp vụ.
    public string Reference { get; set; } = "";
    // THUỘC TÍNH PaymentRecord.ActorId (int): id tài khoản thực hiện thao tác để ghi vết; nhân viên
    // dùng id âm trong mô hình ứng dụng.
    public int ActorId { get; set; }
    // THUỘC TÍNH PaymentRecord.Created (DateTime): thời điểm tạo bản ghi; model dùng DateTime.Now
    // hoặc UtcNow như giá trị khởi tạo/luồng gán tương ứng, không mặc định đều UTC.
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

// DỮ LIỆU: Phiếu yêu cầu trả hàng với Pending/Approved/Rejected, các dòng, tổng tiền hoàn và thông

// tin người
// duyệt/chứng từ.
public class ReturnRecord
{
    // THUỘC TÍNH ReturnRecord.Id (int): mã định danh của ReturnRecord; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH ReturnRecord.OrderId (string): mã đơn công khai mà đối tượng này liên kết.
    public string OrderId { get; set; } = "";
    // THUỘC TÍNH ReturnRecord.CustomerId (int): mã khách hàng dương mà đơn/giỏ/hội thoại/OTP thuộc
    // về, dùng giới hạn đúng chủ dữ liệu.
    public int CustomerId { get; set; }
    // THUỘC TÍNH ReturnRecord.Reason (string): lý do nhập/kiểm kê/trả hàng theo model, được lưu để
    // đối soát thao tác.
    public string Reason { get; set; } = "";
    // THUỘC TÍNH ReturnRecord.Status (string): Pending chờ duyệt, Approved đã duyệt, Rejected từ
    // chối; chỉ phiếu Pending được xử lý tiếp.
    public string Status { get; set; } = "Pending";
    // THUỘC TÍNH ReturnRecord.Resolution (string): ghi chú kết quả xử lý phiếu trả, gắn với người
    // duyệt và chứng từ.
    public string Resolution { get; set; } = "";
    // THUỘC TÍNH ReturnRecord.ActorId (int?): id tài khoản thực hiện thao tác để ghi vết; nhân viên
    // dùng id âm trong mô hình ứng dụng.
    public int? ActorId { get; set; }
    // THUỘC TÍNH ReturnRecord.Created (DateTime): thời điểm tạo bản ghi; model dùng DateTime.Now
    // hoặc UtcNow như giá trị khởi tạo/luồng gán tương ứng, không mặc định đều UTC.
    public DateTime Created { get; set; } = DateTime.UtcNow;
    // THUỘC TÍNH ReturnRecord.Resolved (DateTime?): thời điểm UTC phiếu trả được duyệt/từ chối;
    // null khi chưa xử lý.
    public DateTime? Resolved { get; set; }
    // THUỘC TÍNH ReturnRecord.Lines (List<ReturnLine>): các dòng trả tham chiếu tới OrderLine gốc.
    public List<ReturnLine> Lines { get; set; } = new();
    // THUỘC TÍNH ReturnRecord.Refund (decimal): tiền VND dự kiến/đã duyệt hoàn theo phiếu hoặc dòng
    // trả, đã xét ưu đãi theo luồng tính.
    public decimal Refund { get; set; }
    // THUỘC TÍNH ReturnRecord.Reference (string): mã tham chiếu hoặc chứng từ; luồng lưu kiểm tra
    // trùng theo loại nghiệp vụ.
    public string Reference { get; set; } = "";
}

// DỮ LIỆU: Chi tiết lượng trả và tiền hoàn cho một OrderLine; lượng không được vượt phần chưa trả

// của dòng mua ban
// đầu.
public class ReturnLine
{
    // THUỘC TÍNH ReturnLine.Id (int): mã định danh của ReturnLine; với mẫu nhập quản trị, 0 thường
    // biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH ReturnLine.OrderLineId (int): mã dòng ChiTietDonHang mà một dòng trả tham chiếu.
    public int OrderLineId { get; set; }
    // THUỘC TÍNH ReturnLine.Quantity (int): số lượng trả của một dòng, không vượt lượng mua trừ các
    // lượt đã duyệt trả.
    public int Quantity { get; set; }
    // THUỘC TÍNH ReturnLine.Refund (decimal): tiền VND dự kiến/đã duyệt hoàn theo phiếu hoặc dòng
    // trả, đã xét ưu đãi theo luồng tính.
    public decimal Refund { get; set; }
    // THUỘC TÍNH ReturnLine.Condition (string): ghi chú tình trạng hàng trả nhận được sau khi kiểm
    // tra.
    public string Condition { get; set; } = "";
}

// DỮ LIỆU: Một tổ hợp sản phẩm/size/màu với SKU, giá riêng có thể null và tồn cộng từ các kho;

// Active cho biết có
// được lựa chọn mua hay không.
public class Variant
{
    // THUỘC TÍNH Variant.Id (int): mã định danh của Variant; với mẫu nhập quản trị, 0 thường biểu
    // thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH Variant.ProductId (int): mã sản phẩm gốc được biến thể/dòng đơn/nhật ký/đánh giá
    // tham chiếu.
    public int ProductId { get; set; }
    // THUỘC TÍNH Variant.Size (string): tên size như M/L; khác mã khóa SizeId/MaKichThuoc.
    public string Size { get; set; } = "";
    // THUỘC TÍNH Variant.Color (string): tên màu hiển thị, khác ColorId/MaMauSac.
    public string Color { get; set; } = "";
    // THUỘC TÍNH Variant.Sku (string): mã nhận diện biến thể; service kiểm tra SKU và tổ hợp sản
    // phẩm/size/màu không trùng.
    public string Sku { get; set; } = "";
    // THUỘC TÍNH Variant.Price (decimal?): giá bán riêng VND/đơn vị của biến thể; null dùng giá mặc
    // định sản phẩm.
    public decimal? Price { get; set; }
    // THUỘC TÍNH Variant.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối tượng;
    // không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
    // THUỘC TÍNH Variant.Stock (int): số lượng tồn; Product là tồn tổng, Variant là tổng của biến
    // thể; không phải lượng người dùng muốn mua.
    public int Stock { get; set; }
}

// DỮ LIỆU: Tồn một biến thể tại một kho; nhập, kiểm kê, đặt hàng và hoàn hàng cùng cập nhật dữ

// liệu này.
public class WarehouseStock
{
    // THUỘC TÍNH WarehouseStock.WarehouseId (int): khóa kho liên quan; nullable khi bản ghi cũ hoặc
    // loại thao tác không chỉ rõ một kho.
    public int WarehouseId { get; set; }
    // THUỘC TÍNH WarehouseStock.VariantId (int): mã biến thể của sản phẩm theo tổ hợp size/màu;
    // dùng tìm đúng tồn, giá riêng hoặc giá vốn.
    public int VariantId { get; set; }
    // THUỘC TÍNH WarehouseStock.Quantity (int): số đơn vị tồn của đúng biến thể tại đúng kho.
    public int Quantity { get; set; }
}

// DỮ LIỆU: Thông tin kho và cờ hoạt động; luồng đặt/nhập/kiểm kê kiểm tra kho đang hoạt động theo

// quy tắc ở
// service.
public class Warehouse
{
    // THUỘC TÍNH Warehouse.Id (int): mã định danh của Warehouse; với mẫu nhập quản trị, 0 thường
    // biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH Warehouse.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo, khác
    // mã định danh Id.
    public string Name { get; set; } = "";
    // THUỘC TÍNH Warehouse.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối
    // tượng; không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
}

// DỮ LIỆU: Lịch sử trạng thái đơn và thời điểm thay đổi phục vụ theo dõi; được ghép từ bảng lịch

// sử khi Load.
public class OrderHistory
{
    // THUỘC TÍNH OrderHistory.Id (long): mã định danh của OrderHistory; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public long Id { get; set; }
    // THUỘC TÍNH OrderHistory.OrderId (string): mã đơn công khai mà đối tượng này liên kết.
    public string OrderId { get; set; } = "";
    // THUỘC TÍNH OrderHistory.Status (string): trạng thái nghiệp vụ; OrderHistory lưu mốc đã xảy
    // ra, luồng đổi mới vẫn phải qua service kiểm tra.
    public string Status { get; set; } = "";
    // THUỘC TÍNH OrderHistory.Created (DateTime): thời điểm tạo bản ghi; model dùng DateTime.Now
    // hoặc UtcNow như giá trị khởi tạo/luồng gán tương ứng, không mặc định đều UTC.
    public DateTime Created { get; set; }
    // THUỘC TÍNH OrderHistory.Note (string): ghi chú của thao tác/đơn; ở giá vốn và công nợ là căn
    // cứ điều chỉnh để đối soát.
    public string Note { get; set; } = "";
}

// DỮ LIỆU: Một dòng phiếu nhập: biến thể/sản phẩm, số lượng, giá nhập, kho, nhà cung cấp và mã

// phiếu; ReceiveBatch
// kiểm tra các dòng chung đầu phiếu.
public class ReceiveInput
{
    // THUỘC TÍNH ReceiveInput.VariantId (int): mã biến thể của sản phẩm theo tổ hợp size/màu; dùng
    // tìm đúng tồn, giá riêng hoặc giá vốn.
    public int VariantId { get; set; }
    // THUỘC TÍNH ReceiveInput.WarehouseId (int): khóa kho liên quan; nullable khi bản ghi cũ hoặc
    // loại thao tác không chỉ rõ một kho.
    public int WarehouseId { get; set; }
    // THUỘC TÍNH ReceiveInput.SupplierId (int): khóa nhà cung cấp của lần nhập hàng, khác tên nhà
    // cung cấp dạng chuỗi.
    public int SupplierId { get; set; }
    // THUỘC TÍNH ReceiveInput.ProductId (int): mã sản phẩm gốc được biến thể/dòng đơn/nhật ký/đánh
    // giá tham chiếu.
    public int ProductId { get; set; }

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReceiveInput.Quantity (int): số đơn vị nhập thêm trong dòng phiếu, không phải tồn
    // sau kiểm kê.
    [Range(1, 100000)]
    public int Quantity { get; set; }

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReceiveInput.UnitCost (decimal): giá nhập/giá vốn VND cho một đơn vị; OrderLine
    // giữ ảnh chụp lúc mua, StockMovement/ReceiveInput ghi giá nhập của giao dịch.
    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal UnitCost { get; set; }

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReceiveInput.Supplier (string): tên nhà cung cấp dùng hiển thị hoặc nhánh nhập dữ
    // liệu tương thích.
    [StringLength(150)]
    public string Supplier { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReceiveInput.Reference (string): mã tham chiếu hoặc chứng từ; luồng lưu kiểm tra
    // trùng theo loại nghiệp vụ.
    [Required, StringLength(100)]
    public string Reference { get; set; } = "";
}

// DỮ LIỆU: Nhà cung cấp dùng khi nhập kho; dữ liệu được chuyển từ bảng NhaCungCap.
public class Supplier
{
    // THUỘC TÍNH Supplier.Id (int): mã định danh của Supplier; với mẫu nhập quản trị, 0 thường biểu
    // thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH Supplier.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo, khác
    // mã định danh Id.
    public string Name { get; set; } = "";
    // THUỘC TÍNH Supplier.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối tượng;
    // không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
}
