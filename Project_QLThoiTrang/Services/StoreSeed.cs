// VAI TRÒ FILE: Dữ liệu mẫu được dựng trong bộ nhớ cho --seed-demo; chưa lưu SQL cho tới khi
// MergeLegacy/Write được
// gọi.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // HÀM Seed: Tạo đối tượng dữ liệu mẫu trong bộ nhớ cho SeedDemo; việc lưu database thuộc
    // MergeLegacy/Write.
    // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân hàm sử
    // dụng.
    // TRẢ VỀ (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình; không
    // phải một bảng SQL riêng; ý nghĩa được xác định bởi biểu thức return bên dưới.
    StoreData Seed()
    {
        // BIẾN d (StoreData): ảnh dữ liệu nghiệp vụ gồm sản phẩm, khách, đơn, kho và cấu hình;
        // không phải một bảng SQL riêng; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa
        // SQL đã commit.
        var d = new StoreData();
        // BIẾN names (string[]): các tên sản phẩm mẫu dùng khi dựng dữ liệu demo.
        string[] names =
        {
            "Áo thun Cotton Basic",
            "Áo thun Essential",
            "Áo thun Premium",
            "Áo thun Unisex",
            "Áo thun Everyday",
            "Áo thun Junior",
            "Áo thun Weekend",
            "Áo thun Relaxed",
        };
        // BIẾN i (int): chỉ số vòng lặp bắt đầu từ giá trị khai báo, dùng truy cập phần tử theo
        // điều kiện for bên dưới.
        for (int i = 0; i < names.Length; i++)
            d.Products.Add(
                new Product
                {
                    Id = i + 1,
                    Name = names[i],
                    Category =
                        i == 5 ? "Trẻ em"
                        : i % 2 == 0 ? "Nữ"
                        : "Nam",
                    Price = 299000 + i * 50000,
                    Image = $"/images/products/aothun{i % 5 + 1:00}.jpg",
                    Color = i % 2 == 0 ? "Trắng" : "Đen",
                    Stock = 15 + i * 3,
                    OnSale = i == 2,
                    IsNew = i >= 4,
                }
            );
        // BIẾN admin (Customer): tài khoản quản trị mẫu chưa hoàn tất lưu; mật khẩu được băm trong
        // phần khởi tạo tương ứng.
        var admin = new Customer
        {
            Id = 1,
            Name = "Quản trị cửa hàng",
            Email = "admin@qoanh.local",
            Role = "Admin",
        };
        admin.PasswordHash = (
            "AQAAAAIAAYagAAAAEPf4HCeGeaRarlp8AHMXzK9Pzj6bHPeuK0bTCMvZ"
            + "WdF+E3oeI1/eLTpTEbIZbbifcw=="
        );
        // BIẾN customer (Customer): tài khoản: Id dương là khách, Id âm là nhân viên; có tên,
        // email, vai trò và trạng thái; được khởi tạo mới trong bộ nhớ tại đây, không đồng nghĩa
        // SQL đã commit.
        var customer = new Customer
        {
            Id = 2,
            Name = "Khách hàng demo",
            Email = "khach@qoanh.local",
            Phone = "0901234567",
            Address = "TP. Hồ Chí Minh",
        };
        customer.PasswordHash = (
            "AQAAAAIAAYagAAAAEOjmujh1ybT/DhQChP3kvV7Z+4s/Q8ACNyRvPNts"
            + "dbwvHHFwFj8+6SSKKU0U3H3Ckw=="
        );
        d.Customers.AddRange(new[] { admin, customer });
        d.Orders.Add(
            new Order
            {
                Id = "QH-DEMO01",
                CustomerId = 2,
                Name = customer.Name,
                Phone = customer.Phone,
                Address = customer.Address,
                Status = "Đã giao",
                Created = DateTime.Now.AddDays(-3),
                Lines = new()
                {
                    new OrderLine
                    {
                        ProductId = 1,
                        Name = names[0],
                        Image = d.Products[0].Image,
                        Price = d.Products[0].Price,
                        Quantity = 2,
                    },
                },
            }
        );
        d.Promotions.Add(
            new Promotion
            {
                Id = 1,
                Name = "Ưu đãi bộ sưu tập mới",
                Code = "HELLO10",
                Percent = 10,
            }
        );
        return d;
    }
}
