// VAI TRÒ FILE: Mẫu nhập danh mục/nhân viên/biến thể/đổi mật khẩu và dữ liệu đánh giá. Input nhận
// từ form; các
// attribute đặt giới hạn và so khớp trường.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;

namespace Project_QLThoiTrang.Models;

// DỮ LIỆU: Mẫu danh mục dùng chung: Kind chọn bảng, Id = 0 thêm mới, khác 0 sửa; các trường tên/mô

// tả/liên hệ/trạng
// thái dùng theo từng loại.
public class ReferenceInput
{
    // THUỘC TÍNH ReferenceInput.Id (int): mã định danh của ReferenceInput; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH ReferenceInput.Kind (string): loại bảng danh mục cần lưu:
    // brands/sizes/colors/warehouses/suppliers.
    public string Kind { get; set; } = "brands";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReferenceInput.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo,
    // khác mã định danh Id.
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReferenceInput.Description (string?): mô tả sản phẩm/danh mục; trong
    // ReferenceInput màu có thể dùng chứa mã màu.
    [StringLength(500)]
    public string? Description { get; set; }

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReferenceInput.Address (string?): địa chỉ hồ sơ/cửa hàng/kho/NCC/người nhận tùy
    // model, không dùng làm khóa định danh.
    [StringLength(255)]
    public string? Address { get; set; }

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReferenceInput.Phone (string?): số điện thoại lưu dạng chuỗi để giữ số 0 đầu và ký
    // tự hợp lệ.
    [StringLength(15)]
    public string? Phone { get; set; }

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute; đúng định dạng email. MVC ghi

    // lỗi vào
    // ModelState; các luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH ReferenceInput.Email (string?): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }
    // THUỘC TÍNH ReferenceInput.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối
    // tượng; không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
}

// DỮ LIỆU: Ghép Input và Rows cho màn hình danh mục; controller chuẩn bị dữ liệu, view chỉ hiển

// thị và gửi form.
public class ReferencePage
{
    // THUỘC TÍNH ReferencePage.Input (ReferenceInput): mẫu nhập đang mở trong trang danh mục, được
    // bind khi người dùng gửi form.
    public ReferenceInput Input { get; set; } = new();
    // THUỘC TÍNH ReferencePage.Rows (List<ReferenceInput>): các bản ghi danh mục để liệt kê trong
    // view, không phải một bản ghi cần lưu.
    public List<ReferenceInput> Rows { get; set; } = new();
}

// DỮ LIỆU: Mẫu thêm/sửa nhân viên với quyền và trạng thái; SaveStaff kiểm tra bảo toàn Admin và

// băm mật khẩu mới.
public class StaffInput
{
    // THUỘC TÍNH StaffInput.Id (int): mã định danh của StaffInput; với mẫu nhập quản trị, 0 thường
    // biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StaffInput.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo, khác
    // mã định danh Id.
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute; đúng định dạng

    // email. MVC ghi
    // lỗi vào ModelState; các luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StaffInput.Email (string): địa chỉ email liên hệ hoặc tài khoản; attribute
    // EmailAddress kiểm tra định dạng nếu được đặt.
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StaffInput.Phone (string?): số điện thoại lưu dạng chuỗi để giữ số 0 đầu và ký tự
    // hợp lệ.
    [StringLength(15)]
    public string? Phone { get; set; }
    // THUỘC TÍNH StaffInput.Role (string): vai trò Customer/Admin/Catalog/Inventory/Sales/Support,
    // dùng kiểm tra quyền ở controller/service.
    public string Role { get; set; } = "Support";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH StaffInput.Password (string?): mật khẩu rõ đầu vào của form/cấu hình, chỉ dùng xác
    // minh hoặc băm; không ghi mật khẩu tài khoản rõ vào SQL.
    [StringLength(100)]
    public string? Password { get; set; }
    // THUỘC TÍNH StaffInput.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối
    // tượng; không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
}

// DỮ LIỆU: Mẫu biến thể gồm sản phẩm, size, màu, SKU, giá riêng và trạng thái; service cấm đổi bộ

// sản phẩm/size/màu
// của biến thể đã tạo.
public class VariantInput
{
    // THUỘC TÍNH VariantInput.Id (int): mã định danh của VariantInput; với mẫu nhập quản trị, 0
    // thường biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH VariantInput.ProductId (int): mã sản phẩm gốc được biến thể/dòng đơn/nhật ký/đánh
    // giá tham chiếu.
    public int ProductId { get; set; }
    // THUỘC TÍNH VariantInput.SizeId (int): khóa MaKichThuoc của size được chọn, không phải chuỗi
    // tên size.
    public int SizeId { get; set; }
    // THUỘC TÍNH VariantInput.ColorId (int): khóa MaMauSac của màu được chọn, không phải chuỗi tên
    // màu.
    public int ColorId { get; set; }

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH VariantInput.Sku (string): mã nhận diện biến thể; service kiểm tra SKU và tổ hợp
    // sản phẩm/size/màu không trùng.
    [Required, StringLength(50)]
    public string Sku { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH VariantInput.Price (decimal?): giá riêng muốn lưu cho biến thể; bỏ trống để dùng
    // giá sản phẩm.
    [Range(typeof(decimal), "1000", "100000000")]
    public decimal? Price { get; set; }
    // THUỘC TÍNH VariantInput.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối
    // tượng; không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
}

// DỮ LIỆU: Dữ liệu đánh giá để hiển thị/kiểm duyệt, gồm khách, sao, nội dung, trạng thái và danh

// sách media; không
// chứa byte tệp trực tiếp.
public class ReviewItem
{
    // THUỘC TÍNH ReviewItem.Media (List<ReviewMediaInfo>): thông tin media thuộc một đánh giá; byte
    // thật được đọc qua endpoint kiểm tra quyền.
    public List<ReviewMediaInfo> Media { get; set; } = new();
    // THUỘC TÍNH ReviewItem.Id (int): mã định danh của ReviewItem; với mẫu nhập quản trị, 0 thường
    // biểu thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }
    // THUỘC TÍNH ReviewItem.ProductId (int): mã sản phẩm gốc được biến thể/dòng đơn/nhật ký/đánh
    // giá tham chiếu.
    public int ProductId { get; set; }
    // THUỘC TÍNH ReviewItem.Customer (string): tên khách được ghép để hiển thị đánh giá, không phải
    // đối tượng Customer đầy đủ.
    public string Customer { get; set; } = "";
    // THUỘC TÍNH ReviewItem.Stars (int): số sao đánh giá, chỉ nhận 1 đến 5 trong luồng thêm đánh
    // giá.
    public int Stars { get; set; }
    // THUỘC TÍNH ReviewItem.Text (string): nội dung văn bản của đối tượng, được kiểm tra độ dài
    // trong luồng nhập tương ứng.
    public string Text { get; set; } = "";
    // THUỘC TÍNH ReviewItem.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối
    // tượng; không mặc định xóa bản ghi SQL.
    public bool Active { get; set; }
    // THUỘC TÍNH ReviewItem.Created (DateTime): thời điểm tạo bản ghi; model dùng DateTime.Now hoặc
    // UtcNow như giá trị khởi tạo/luồng gán tương ứng, không mặc định đều UTC.
    public DateTime Created { get; set; }
}

// DỮ LIỆU: Mẫu đổi mật khẩu: mật khẩu hiện tại, mật khẩu mới và xác nhận; ChangePassword xác minh

// mật khẩu cũ trước
// lưu.
public class PasswordInput
{
    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập. MVC ghi lỗi vào ModelState; các luồng
    // Validator.ValidateObject cũng
    // kiểm tra các attribute này.
    // THUỘC TÍNH PasswordInput.CurrentPassword (string): mật khẩu hiện tại người dùng cung cấp để
    // xác minh trước khi đổi mật khẩu.
    [Required]
    public string CurrentPassword { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute; độ dài tối thiểu

    // theo
    // attribute; khớp mẫu RegularExpression. MVC ghi lỗi vào ModelState; các luồng
    // Validator.ValidateObject cũng
    // kiểm tra các attribute này.
    // THUỘC TÍNH PasswordInput.NewPassword (string): mật khẩu mới sẽ được băm nếu mẫu nhập và mật
    // khẩu hiện tại đều hợp lệ.
    [Required, MinLength(8), StringLength(100), RegularExpression(@"^(?=.*\p{L})(?=.*\d).+$")]
    public string NewPassword { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: trùng trường được chỉ định trong Compare. MVC ghi lỗi vào ModelState;

    // các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH PasswordInput.ConfirmPassword (string): mật khẩu nhập lại, Compare yêu cầu trùng
    // trường mật khẩu được chỉ định.
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = "";
}
