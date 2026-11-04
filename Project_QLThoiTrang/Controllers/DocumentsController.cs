using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_QLThoiTrang.Services;

namespace Project_QLThoiTrang.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DocumentsController(StoreService store, PdfDocuments pdf) : Controller
{
    int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    bool Sales => User.IsInRole("Admin") || User.IsInRole("Sales");

    // Order: Xuất PDF đơn hàng được phép xem.
    public IActionResult Order(string id)
    {
        var o = store.Read(d => d.Orders.FirstOrDefault(o => o.Id == id));

        if (o == null || !Sales && !(User.IsInRole("Customer") && o.CustomerId == Actor))
            return NotFound();
        var info = new List<string>
        {
            o.Created.ToString("dd/MM/yyyy HH:mm"),
            o.Name,
            o.Phone,
            o.Address,
            "Trạng thái: " + o.Status,
            "Thanh toán: " + o.Payment,
        };
        if (o.TransferReference != null)
            info.Add("Nội dung chuyển khoản: " + o.TransferReference);
        return File(
            pdf.Create(
                "ĐƠN HÀNG",
                o.Id,
                info,
                new[] { "Sản phẩm / size / màu", "SL", "Đơn giá", "Thành tiền" },
                o.Lines.Select(l =>
                        new[]
                        {
                            l.Name + " / " + l.Size + " / " + l.Color,
                            l.Quantity.ToString(),
                            l.Price.ToString("N0"),
                            (l.Price * l.Quantity).ToString("N0"),
                        }
                    )
                    .ToList(),
                new[]
                {
                    "Tiền hàng: " + o.Subtotal.ToString("N0") + " đ",
                    "Giảm giá: " + o.Discount.ToString("N0") + " đ",
                    "Vận chuyển: " + o.Shipping.ToString("N0") + " đ",
                    "Tổng cộng: " + o.Total.ToString("N0") + " đ",
                    "Đã nhận: "
                        + o.PaidAmount.ToString("N0")
                        + " đ; Đã hoàn: "
                        + o.RefundedAmount.ToString("N0")
                        + " đ",
                }
            ),
            "application/pdf",
            "DonHang-" + o.DatabaseId + ".pdf"
        );
    }

    // Receipt: Xuất PDF phiếu nhập kho.
    [Authorize(Roles = "Admin,Inventory")]
    public IActionResult Receipt(int id)
    {
        var p = store.NativeRead(db => db.PhieuNhap.SingleOrDefault(p => p.MaPhieuNhap == id));

        if (p == null)
            return NotFound();
        var lines = store.NativeRead(db =>
            db.ChiTietPhieuNhap.Where(l => l.MaPhieuNhap == id).ToList()
        );
        return File(
            pdf.Create(
                "PHIẾU NHẬP KHO",
                p.QReference ?? ("PN-" + p.MaPhieuNhap),
                new[]
                {
                    p.NgayNhap.ToString("dd/MM/yyyy HH:mm"),
                    $"Kho #{p.MaKho} / Nhà cung cấp #{p.MaNCC} / Nhân viên #{p.MaNV}",
                },
                new[] { "Biến thể", "SL", "Giá nhập", "Thành tiền" },
                lines
                    .Select(l =>
                        new[]
                        {
                            "SKU / biến thể #" + l.MaBienThe,
                            l.SoLuong.ToString(),
                            l.GiaNhap.ToString("N0"),
                            (l.SoLuong * l.GiaNhap).ToString("N0"),
                        }
                    )
                    .ToList(),
                new[]
                {
                    "Tổng chứng từ: " + p.TongTien.ToString("N0") + " đ",
                    "Tổng chi tiết: " + lines.Sum(l => l.SoLuong * l.GiaNhap).ToString("N0") + " đ",
                }
            ),
            "application/pdf",
            "PhieuNhap-" + id + ".pdf"
        );
    }

    // Return: Xuất PDF phiếu trả hàng.
    public IActionResult Return(int id)
    {
        var r = store.Read(d => d.Returns.FirstOrDefault(r => r.Id == id));

        if (r == null || !Sales && !(User.IsInRole("Customer") && r.CustomerId == Actor))
            return NotFound();
        var o = store.Read(d => d.Orders.Single(o => o.Id == r.OrderId));
        return File(
            pdf.Create(
                "PHIẾU TRẢ HÀNG",
                "PT-" + r.Id,
                new[]
                {
                    "Đơn: " + o.Id,
                    r.Created.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    "Lý do: " + r.Reason,
                    "Trạng thái: "
                        + (
                            r.Status == "Approved" ? "Đã duyệt"
                            : r.Status == "Rejected" ? "Từ chối"
                            : "Chờ xử lý"
                        ),
                    "Chứng từ: " + r.Reference,
                },
                new[] { "Sản phẩm", "SL trả", "Tiền hàng hoàn", "Tình trạng" },
                r.Lines.Select(l =>
                        new[]
                        {
                            o.Lines.Single(x => x.Id == l.OrderLineId).Name,
                            l.Quantity.ToString(),
                            l.Refund.ToString("N0"),
                            l.Condition,
                        }
                    )
                    .ToList(),
                new[]
                {
                    "Số tiền hoàn theo phiếu: " + r.Refund.ToString("N0") + " đ",
                    "Ghi chú: " + r.Resolution,
                }
            ),
            "application/pdf",
            "PhieuTra-" + id + ".pdf"
        );
    }
}
