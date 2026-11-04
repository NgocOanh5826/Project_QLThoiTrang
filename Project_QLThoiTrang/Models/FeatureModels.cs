// VAI TRÒ FILE: Entity và input của chat/OTP/giá vốn/công nợ/GHN, cùng cấu hình dịch vụ và dòng báo
// cáo lợi nhuận;
// mapping nằm trong Data/FeatureMapping.cs.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;

namespace Project_QLThoiTrang.Models;

// DỮ LIỆU: Một hội thoại theo CustomerId; lưu thời điểm cập nhật và mốc đã đọc riêng cho

// khách/nhân viên.
public class ChatThread
{
    // THUỘC TÍNH ChatThread.CustomerId (int): mã khách hàng dương mà đơn/giỏ/hội thoại/OTP thuộc
    // về, dùng giới hạn đúng chủ dữ liệu.
    public int CustomerId { get; set; }
    // THUỘC TÍNH ChatThread.UpdatedUtc (DateTime): thời điểm cập nhật gần nhất theo UTC, dùng sắp
    // xếp hoặc kiểm tra thời gian.
    public DateTime UpdatedUtc { get; set; }
    // THUỘC TÍNH ChatThread.CustomerReadId (long): mã tin cuối khách đã đọc, chỉ tăng và không vượt
    // tin lớn nhất trong hội thoại.
    public long CustomerReadId { get; set; }
    // THUỘC TÍNH ChatThread.StaffReadId (long): mã tin cuối phía nhân viên đã đọc, dùng tính số tin
    // khách chưa được đọc.
    public long StaffReadId { get; set; }
}

// DỮ LIỆU: Một tin nhắn có SenderId và ClientToken chống gửi trùng; Text bị giới hạn 2.000 ký tự.
public class ChatMessage
{
    // THUỘC TÍNH ChatMessage.Id (long): mã định danh của ChatMessage; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public long Id { get; set; }
    // THUỘC TÍNH ChatMessage.CustomerId (int): mã khách hàng dương mà đơn/giỏ/hội thoại/OTP thuộc
    // về, dùng giới hạn đúng chủ dữ liệu.
    public int CustomerId { get; set; }
    // THUỘC TÍNH ChatMessage.SenderId (int): id người gửi có dấu: khách dương, nhân viên âm; dùng
    // phân biệt hai phía chat.
    public int SenderId { get; set; }
    // THUỘC TÍNH ChatMessage.Text (string): nội dung văn bản của đối tượng, được kiểm tra độ dài
    // trong luồng nhập tương ứng.
    public string Text { get; set; } = "";
    // THUỘC TÍNH ChatMessage.ClientToken (Guid): Guid của lần gửi tin; cùng khách/token phải chỉ
    // tương ứng một nội dung và người gửi.
    public Guid ClientToken { get; set; }
    // THUỘC TÍNH ChatMessage.CreatedUtc (DateTime): thời điểm tạo theo UTC, dùng thời hạn OTP, giới
    // hạn tốc độ hoặc thứ tự tin.
    public DateTime CreatedUtc { get; set; }
}

// DỮ LIỆU: Challenge OTP: chỉ lưu hash, stamp mật khẩu, hạn dùng, số lần thử, cờ đã gửi và thời

// điểm đã dùng; không
// lưu OTP rõ.
public class ResetCode
{
    // THUỘC TÍNH ResetCode.Id (Guid): mã định danh của ResetCode; với mẫu nhập quản trị, 0 thường
    // biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public Guid Id { get; set; }
    // THUỘC TÍNH ResetCode.CustomerId (int): mã khách hàng dương mà đơn/giỏ/hội thoại/OTP thuộc về,
    // dùng giới hạn đúng chủ dữ liệu.
    public int CustomerId { get; set; }

    // OTP BẢO VỆ: lưu HMAC của OTP cùng mã challenge; không phải bản rõ OTP có thể gửi lại cho

    // khách.
    // THUỘC TÍNH ResetCode.Hash (string): HMAC của OTP đã ký bằng khóa cấu hình; không chứa mã OTP
    // rõ.
    public string Hash { get; set; } = "";
    // THUỘC TÍNH ResetCode.Stamp (string): SHA-256 của hash mật khẩu khi phát OTP; mật khẩu đổi làm
    // stamp cũ không còn khớp.
    public string Stamp { get; set; } = "";
    // THUỘC TÍNH ResetCode.CreatedUtc (DateTime): thời điểm tạo theo UTC, dùng thời hạn OTP, giới
    // hạn tốc độ hoặc thứ tự tin.
    public DateTime CreatedUtc { get; set; }
    // THUỘC TÍNH ResetCode.ExpiresUtc (DateTime): thời điểm UTC mã OTP hết hiệu lực; quá mốc này
    // không được đặt lại mật khẩu.
    public DateTime ExpiresUtc { get; set; }

    // GIỚI HẠN THỬ: mỗi lần so mã tăng 1; từ 5 lần trở lên ResetPassword từ chối tiếp.
    // THUỘC TÍNH ResetCode.Attempts (int): số lần đã thử OTP; đạt 5 lần thì từ chối thử tiếp với mã
    // đó.
    public int Attempts { get; set; }
    // THUỘC TÍNH ResetCode.Sent (bool): true khi SMTP đã gửi challenge thành công; mã chưa được
    // đánh dấu Sent không được dùng đổi mật khẩu.
    public bool Sent { get; set; }
    // THUỘC TÍNH ResetCode.UsedUtc (DateTime?): thời điểm mã OTP đã dùng/vô hiệu; null là chưa bị
    // đánh dấu dùng.
    public DateTime? UsedUtc { get; set; }
}

// DỮ LIỆU: Giá vốn bình quân của biến thể; null thể hiện chưa xác định, kèm người/lý do/thời điểm

// cập nhật.
public class InventoryValue
{
    // THUỘC TÍNH InventoryValue.VariantId (int): mã biến thể của sản phẩm theo tổ hợp size/màu;
    // dùng tìm đúng tồn, giá riêng hoặc giá vốn.
    public int VariantId { get; set; }

    // GIÁ VỐN BÌNH QUÂN: null là chưa biết; không thay bằng 0 vì sẽ làm lợi nhuận báo cáo sai ý

    // nghĩa.
    // THUỘC TÍNH InventoryValue.AverageCost (decimal?): giá vốn bình quân VND/đơn vị của biến thể;
    // null nghĩa chưa xác định.
    public decimal? AverageCost { get; set; }
    // THUỘC TÍNH InventoryValue.ActorId (int?): id tài khoản thực hiện thao tác để ghi vết; nhân
    // viên dùng id âm trong mô hình ứng dụng.
    public int? ActorId { get; set; }
    // THUỘC TÍNH InventoryValue.UpdatedUtc (DateTime): thời điểm cập nhật gần nhất theo UTC, dùng
    // sắp xếp hoặc kiểm tra thời gian.
    public DateTime UpdatedUtc { get; set; }
    // THUỘC TÍNH InventoryValue.Note (string): ghi chú của thao tác/đơn; ở giá vốn và công nợ là
    // căn cứ điều chỉnh để đối soát.
    public string Note { get; set; } = "";
}

// DỮ LIỆU: Số phải trả đã đối soát cho một phiếu nhập; SupplierPayment ghi các lần chi thực tế.
public class SupplierAccount
{
    // THUỘC TÍNH SupplierAccount.ReceiptId (int): khóa MaPhieuNhap của phiếu nhập mà công nợ/chứng
    // từ chi này thuộc về.
    public int ReceiptId { get; set; }
    // THUỘC TÍNH SupplierAccount.Amount (decimal): số tiền VND của bút toán/chứng từ hoặc số phải
    // trả đã đối soát, tùy lớp chứa thuộc tính.
    public decimal Amount { get; set; }
    // THUỘC TÍNH SupplierAccount.Note (string): ghi chú của thao tác/đơn; ở giá vốn và công nợ là
    // căn cứ điều chỉnh để đối soát.
    public string Note { get; set; } = "";
    // THUỘC TÍNH SupplierAccount.ActorId (int): id tài khoản thực hiện thao tác để ghi vết; nhân
    // viên dùng id âm trong mô hình ứng dụng.
    public int ActorId { get; set; }
    // THUỘC TÍNH SupplierAccount.UpdatedUtc (DateTime): thời điểm cập nhật gần nhất theo UTC, dùng
    // sắp xếp hoặc kiểm tra thời gian.
    public DateTime UpdatedUtc { get; set; }
}

// DỮ LIỆU: Chứng từ chi cho công nợ phiếu nhập; Reference duy nhất và tổng chi không vượt số phải

// trả.
public class SupplierPayment
{
    // THUỘC TÍNH SupplierPayment.Id (long): mã định danh của SupplierPayment; với mẫu nhập quản
    // trị, 0 thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public long Id { get; set; }
    // THUỘC TÍNH SupplierPayment.ReceiptId (int): khóa MaPhieuNhap của phiếu nhập mà công nợ/chứng
    // từ chi này thuộc về.
    public int ReceiptId { get; set; }
    // THUỘC TÍNH SupplierPayment.Amount (decimal): số tiền VND của bút toán/chứng từ hoặc số phải
    // trả đã đối soát, tùy lớp chứa thuộc tính.
    public decimal Amount { get; set; }
    // THUỘC TÍNH SupplierPayment.Reference (string): mã tham chiếu hoặc chứng từ; luồng lưu kiểm
    // tra trùng theo loại nghiệp vụ.
    public string Reference { get; set; } = "";
    // THUỘC TÍNH SupplierPayment.ActorId (int): id tài khoản thực hiện thao tác để ghi vết; nhân
    // viên dùng id âm trong mô hình ứng dụng.
    public int ActorId { get; set; }
    // THUỘC TÍNH SupplierPayment.CreatedUtc (DateTime): thời điểm tạo theo UTC, dùng thời hạn OTP,
    // giới hạn tốc độ hoặc thứ tự tin.
    public DateTime CreatedUtc { get; set; }
}

// DỮ LIỆU: Trạng thái yêu cầu/vận đơn GHN theo đơn SQL; RequestHash bảo vệ nội dung đã gửi,

// Unknown yêu cầu đối
// soát.
public class Shipment
{
    // THUỘC TÍNH Shipment.OrderId (int): khóa nguyên SQL MaDonHang của vận đơn.
    public int OrderId { get; set; }
    // THUỘC TÍNH Shipment.ClientCode (string): mã đơn cửa hàng gửi GHN để tra cứu/đối soát cùng một
    // yêu cầu.
    public string ClientCode { get; set; } = "";

    // DẤU VẾT YÊU CẦU: hash nội dung gửi GHN, chặn sửa âm thầm payload đã gửi khi chưa đối soát.
    // THUỘC TÍNH Shipment.RequestHash (string): SHA-256 của nội dung GHN đã gửi; giữ nguyên để ngăn
    // thay payload khi thử lại.
    public string RequestHash { get; set; } = "";
    // THUỘC TÍNH Shipment.TrackingCode (string?): mã vận đơn GHN đã nhận, null khi chưa có kết quả
    // xác định.
    public string? TrackingCode { get; set; }
    // THUỘC TÍNH Shipment.Status (string): trạng thái gửi/đối soát GHN, có thể
    // Requesting/Unknown/Rejected hoặc trạng thái do GHN trả.
    public string Status { get; set; } = "Pending";
    // THUỘC TÍNH Shipment.UpdatedUtc (DateTime): thời điểm cập nhật gần nhất theo UTC, dùng sắp xếp
    // hoặc kiểm tra thời gian.
    public DateTime UpdatedUtc { get; set; }
    // THUỘC TÍNH Shipment.Error (string): nội dung lỗi gần nhất để phục vụ đối soát, không phải
    // bằng chứng giao hàng thành công.
    public string Error { get; set; } = "";
}

// DỮ LIỆU: Email đầu vào yêu cầu cấp OTP; controller không tiết lộ tài khoản có tồn tại hay không.
public class ForgotInput
{
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute; đúng định dạng
    // email. MVC ghi
    // lỗi vào ModelState; các luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ForgotInput.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";
}

// DỮ LIỆU: Email, OTP và mật khẩu mới của form khôi phục; thời hạn/số lần thử được kiểm tra trên

// server.
public class ResetInput
{
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; đúng định dạng email. MVC ghi lỗi vào ModelState; các
    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ResetInput.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; khớp mẫu RegularExpression. MVC ghi lỗi vào ModelState;

    // các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ResetInput.Code (string): OTP 6 chữ số người dùng nhận qua email để đặt lại mật
    // khẩu.
    [Required, RegularExpression(@"^\d{6}$")]
    public string Code { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute; độ dài tối thiểu

    // theo
    // attribute; khớp mẫu RegularExpression. MVC ghi lỗi vào ModelState; các luồng
    // Validator.ValidateObject cũng
    // kiểm tra các attribute này.
    // THUỘC TÍNH ResetInput.Password (string): mật khẩu rõ đầu vào của form/cấu hình, chỉ dùng xác
    // minh hoặc băm; không ghi mật khẩu tài khoản rõ vào SQL.
    [Required, MinLength(8), StringLength(100), RegularExpression(@"^(?=.*\p{L})(?=.*\d).+$")]
    public string Password { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: trùng trường được chỉ định trong Compare. MVC ghi lỗi vào ModelState;

    // các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ResetInput.ConfirmPassword (string): mật khẩu nhập lại, Compare yêu cầu trùng
    // trường mật khẩu được chỉ định.
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";
}

// DỮ LIỆU: Mẫu địa chỉ GHN, trọng lượng và kích thước; DataAnnotations kiểm tra giới hạn trước khi

// gọi API.
public class ShipmentInput
{
    // THUỘC TÍNH ShipmentInput.Id (string): mã đơn công khai cần tạo vận đơn, không phải Guid hay
    // khóa số vận đơn.
    public string Id { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ShipmentInput.Province (string): tên tỉnh/thành gửi GHN theo form địa chỉ, không
    // phải mã tỉnh trong dữ liệu JSON của bộ chọn cửa hàng.
    [Required, StringLength(100)]
    public string Province { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ShipmentInput.Ward (string): tên phường/xã gửi GHN theo form, được kiểm tra bắt
    // buộc và giới hạn độ dài.
    [Required, StringLength(100)]
    public string Ward { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ShipmentInput.Weight (int): khối lượng kiện gửi theo gram; ShipmentInput giới hạn
    // 1–19.999 gram.
    [Range(1, 19999)]
    public int Weight { get; set; } = 500;

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ShipmentInput.Length (int): chiều dài kiện theo centimet, ShipmentInput giới hạn
    // 1–200 cm.
    [Range(1, 200)]
    public int Length { get; set; } = 20;

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ShipmentInput.Width (int): chiều rộng kiện theo centimet, ShipmentInput giới hạn
    // 1–200 cm.
    [Range(1, 200)]
    public int Width { get; set; } = 15;

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ShipmentInput.Height (int): chiều cao kiện theo centimet, ShipmentInput giới hạn
    // 1–200 cm.
    [Range(1, 200)]
    public int Height { get; set; } = 10;
}

// DỮ LIỆU: Cấu hình bật chuyển khoản và ảnh QR nội bộ; khi đặt đơn thông tin được chụp vào đơn để

// không thay theo
// cấu hình sau này.
public class BankTransferOptions
{
    // THUỘC TÍNH BankTransferOptions.Enabled (bool): cờ bật tính năng cấu hình; các điều kiện
    // Ready/Options còn kiểm tra đủ cấu hình cần thiết.
    public bool Enabled { get; set; }
    // THUỘC TÍNH BankTransferOptions.QrImage (string): đường dẫn ảnh QR chuyển khoản nội bộ, không
    // phải URL tự xác nhận tiền đã thu.
    public string QrImage { get; set; } = "/images/payments/transfer-qr.png";
}

// DỮ LIỆU: Cấu hình SMTP và cờ Ready; SmtpMailSender yêu cầu TLS trước gửi OTP.
public class SmtpOptions
{
    // THUỘC TÍNH SmtpOptions.Host (string): tên máy chủ SMTP dùng gửi OTP.
    public string Host { get; set; } = "";
    // THUỘC TÍNH SmtpOptions.Port (int): cổng SMTP để kết nối gửi email.
    public int Port { get; set; } = 587;
    // THUỘC TÍNH SmtpOptions.User (string): tên đăng nhập dịch vụ SMTP, khác tài khoản khách hàng
    // của website.
    public string User { get; set; } = "";
    // THUỘC TÍNH SmtpOptions.Password (string): mật khẩu dịch vụ SMTP đọc từ cấu hình, không phải
    // mật khẩu tài khoản khách.
    public string Password { get; set; } = "";
    // THUỘC TÍNH SmtpOptions.From (string): địa chỉ email người gửi OTP, khác tài khoản đăng nhập
    // SMTP User.
    public string From { get; set; } = "";
    // THUỘC TÍNH SmtpOptions.EnableSsl (bool): cờ yêu cầu TLS cho SMTP; bộ gửi OTP từ chối gửi khi
    // chưa bật.
    public bool EnableSsl { get; set; } = true;
    // THUỘC TÍNH SmtpOptions.Ready (bool): giá trị suy ra từ cấu hình bắt buộc đã có; không tự
    // chứng minh dịch vụ ngoài đang kết nối thành công.
    public bool Ready => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}

// DỮ LIỆU: Cấu hình token, shop và gateway GHN; ShippingService còn đối chiếu URL cho phép.
public class GhnOptions
{
    // THUỘC TÍNH GhnOptions.Enabled (bool): cờ bật tính năng cấu hình; các điều kiện Ready/Options
    // còn kiểm tra đủ cấu hình cần thiết.
    public bool Enabled { get; set; }
    // THUỘC TÍNH GhnOptions.Token (string): token xác thực GHN từ cấu hình, được gửi header của
    // API, không đưa vào form công khai.
    public string Token { get; set; } = "";
    // THUỘC TÍNH GhnOptions.ShopId (int): mã shop trên GHN, được gửi header ShopId khi gọi API.
    public int ShopId { get; set; }
    // THUỘC TÍNH GhnOptions.BaseUrl (string): gateway GHN, phải khớp URL được
    // ShippingService.Options cho phép.
    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api/";
    // THUỘC TÍNH GhnOptions.Ready (bool): giá trị suy ra từ cấu hình bắt buộc đã có; không tự chứng
    // minh dịch vụ ngoài đang kết nối thành công.
    public bool Ready => Enabled && Token.Length > 0 && ShopId > 0;
}

// DỮ LIỆU: Dòng báo cáo doanh thu và giá vốn; giá vốn nullable giúp phân biệt chưa biết chi phí

// với chi phí bằng 0.
public record ProfitLine(
    string OrderId,
    DateTime Date,
    string Product,
    int Quantity,
    decimal Revenue,
    decimal? Cost
)
{
    // THUỘC TÍNH ProfitLine.Profit (decimal?): lợi nhuận gộp = Revenue - Cost khi đã có Cost, nếu
    // thiếu Cost thì giữ null.
    public decimal? Profit => Cost.HasValue ? Revenue - Cost : null;
}
