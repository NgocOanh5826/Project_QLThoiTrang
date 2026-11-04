// VAI TRÒ FILE: Mô hình sản phẩm dùng chung form và hiển thị; DataAnnotations kiểm tra
// tên/giá/màu/tồn. Controller
// thêm mới ép Stock = 0; thay tồn phải qua nhập kho hoặc kiểm kê.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using System.ComponentModel.DataAnnotations;

namespace Project_QLThoiTrang.Models;

public class Product
{
    // THUỘC TÍNH Product.BrandId (int?): khóa thương hiệu của sản phẩm; null khi sản phẩm chưa gắn
    // thương hiệu.
    public int? BrandId { get; set; }
    // THUỘC TÍNH Product.LegacyId (int?): mã từ dữ liệu cũ để đối chiếu khi nhập/chuyển đổi, khác
    // mã mới dùng ở bảng hiện tại.
    public int? LegacyId { get; set; }

    // BIẾN THỂ: danh sách size/màu/SKU của sản phẩm; việc tạo/sửa thực hiện ở Admin/Variant và

    // SaveVariant.
    // THUỘC TÍNH Product.Variants (List<Variant>): các biến thể của sản phẩm hoặc toàn cửa hàng
    // theo model chứa thuộc tính.
    public List<Variant> Variants { get; set; } = new();

    // TỒN CÓ THỂ CHỌN: cộng tồn của các biến thể đang Active; khi đặt hàng service còn kiểm tra

    // tồn ở kho đang
    // hoạt động.
    // THUỘC TÍNH Product.AvailableStock (int): tổng tồn của biến thể đang Active, tính từ Variants
    // chứ không phải ô nhập riêng.
    public int AvailableStock => Variants.Where(v => v.Active).Sum(v => v.Stock);

    // THÊM/SỬA: form mới có Id = 0; POST Admin/Product cấp mã và thêm mới. Id đã tồn tại dùng tìm

    // dòng cần cập
    // nhật.
    // THUỘC TÍNH Product.Id (int): mã định danh của Product; với mẫu nhập quản trị, 0 thường biểu
    // thị chưa tạo và khác 0 là sửa bản ghi đã có.
    public int Id { get; set; }

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Product.Name (string): tên hiển thị của đối tượng thuộc lớp đang khai báo, khác mã
    // định danh Id.
    [Required(ErrorMessage = "Nhập tên sản phẩm"), StringLength(120)]
    public string Name { get; set; } = "";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập. MVC ghi lỗi vào ModelState; các luồng

    // Validator.ValidateObject cũng
    // kiểm tra các attribute này.
    // THUỘC TÍNH Product.Category (string): tên danh mục sản phẩm hoặc lựa chọn danh mục đang lọc
    // trong view model.
    [Required]
    public string Category { get; set; } = "Nữ";

    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào

    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Product.Price (decimal): giá bán mặc định VND/đơn vị, từ 1.000 đến 100 triệu; biến
    // thể có thể đặt giá riêng.
    [Range(1000, 100000000, ErrorMessage = "Giá phải từ 1.000 đến 100.000.000 đ")]
    public decimal Price { get; set; }
    // THUỘC TÍNH Product.Image (string): đường dẫn ảnh hiển thị của sản phẩm/dòng đơn; không phải
    // byte tệp đánh giá.
    public string Image { get; set; } = "/images/products/aothun01.jpg";

    // RÀNG BUỘC MẪU NHẬP: bắt buộc nhập; giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào

    // ModelState; các
    // luồng Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Product.Color (string): tên màu hiển thị, khác ColorId/MaMauSac.
    [Required, StringLength(60)]
    public string Color { get; set; } = "Trắng";

    // RÀNG BUỘC MẪU NHẬP: giới hạn độ dài theo tham số attribute. MVC ghi lỗi vào ModelState; các

    // luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Product.Description (string): mô tả sản phẩm/danh mục; trong ReferenceInput màu có
    // thể dùng chứa mã màu.
    [StringLength(2000)]
    public string Description { get; set; } =
        (
            "Cotton mềm mại, phom dễ mặc. Phù hợp với những ngày bạn "
            + "muốn thoải mái mà vẫn chỉn chu."
        );

    // TỒN TỔNG: không dùng ô sửa sản phẩm để thay tồn. Admin/Product giữ tồn cũ khi sửa và ép 0

    // khi thêm;
    // nhập/kiểm kê thay đổi tồn qua service.
    // RÀNG BUỘC MẪU NHẬP: giá trị nằm trong khoảng min..max ghi bên dưới. MVC ghi lỗi vào
    // ModelState; các luồng
    // Validator.ValidateObject cũng kiểm tra các attribute này.
    // THUỘC TÍNH Product.Stock (int): số lượng tồn; Product là tồn tổng, Variant là tổng của biến
    // thể; không phải lượng người dùng muốn mua.
    [Range(0, 100000)]
    public int Stock { get; set; } = 20;

    // XÓA MỀM: false làm sản phẩm bị lọc khỏi cửa hàng; Admin/DeleteProduct không xóa bản ghi vì

    // đơn cũ vẫn cần
    // tham chiếu.
    // THUỘC TÍNH Product.Active (bool): true hoạt động/hiển thị, false khóa/ẩn tùy loại đối tượng;
    // không mặc định xóa bản ghi SQL.
    public bool Active { get; set; } = true;
    // THUỘC TÍNH Product.IsNew (bool): cờ đưa sản phẩm vào bộ sưu tập hàng mới.
    public bool IsNew { get; set; } = true;
    // THUỘC TÍNH Product.OnSale (bool): cờ đưa sản phẩm vào bộ sưu tập ưu đãi; mức giảm thực vẫn do
    // Promotion khi đặt đơn.
    public bool OnSale { get; set; }
}
