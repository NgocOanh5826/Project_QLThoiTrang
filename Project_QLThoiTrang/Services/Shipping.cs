using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Project_QLThoiTrang.Models;

namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
    // ClaimShipment: Lưu và khóa yêu cầu tạo vận đơn để tránh gửi trùng.
    public Shipment ClaimShipment(
        string id,
        string requestHash,
        decimal expectedOutstanding,
        int actor
    ) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Sales");
            int legacyId =
                id.StartsWith("QH-SQL-") && int.TryParse(id[7..], out var parsed) ? parsed : 0;
            var o = db.DonHang.SingleOrDefault(o =>
                o.QPublicId == id || (o.QPublicId == null && o.MaDonHang == legacyId)
            );

            Require(o != null, "Đơn không tồn tại.");

            Require(
                o!.TrangThai is "Đã xác nhận" or "Đang chuẩn bị" or "Đang giao",
                "Đơn chưa đủ điều kiện tạo vận đơn."
            );
            var p = db.ThanhToan.SingleOrDefault(p => p.MaDonHang == o.MaDonHang);

            Require(
                (p?.TrangThai == "Đã thanh toán" ? 0 : o.TongThanhToan) == expectedOutstanding,
                "Số tiền đã thay đổi, tải lại trước khi tạo vận đơn."
            );

            Require(
                p?.PhuongThuc != "Chuyển khoản" || p.TrangThai == "Đã thanh toán",
                "Đơn chuyển khoản phải thu đủ trước khi tạo vận đơn."
            );
            var s = db.Shipments.SingleOrDefault(s => s.OrderId == o.MaDonHang);
            if (s == null)
            {
                s = new()
                {
                    OrderId = o.MaDonHang,
                    ClientCode = id,
                    RequestHash = requestHash,
                };
                db.Shipments.Add(s);
            }

            if (s.Status == "Rejected" && s.TrackingCode == null)
                s.RequestHash = requestHash;

            Require(
                s.RequestHash == requestHash,
                "Không đổi nội dung yêu cầu đã gửi GHN. Đối soát vận đơn trước."
            );

            Require(
                s.Status != "Requesting" || s.UpdatedUtc < DateTime.UtcNow.AddMinutes(-1),
                "Vận đơn đang được xử lý."
            );
            s.Status = "Requesting";
            s.UpdatedUtc = DateTime.UtcNow;
            return s;
        });

    // ShipmentResult: Lưu kết quả và trạng thái vận đơn.
    public void ShipmentResult(int orderId, string? code, string status, string error, int actor) =>
        NativeWrite(db =>
        {
            NativePermission(db, actor, "Sales");
            var s = db.Shipments.Single(s => s.OrderId == orderId);
            s.TrackingCode = code ?? s.TrackingCode;
            s.Status = Text(status, 1, 60, "Trạng thái vận đơn");
            s.Error = error.Length > 300 ? error[..300] : error;
            s.UpdatedUtc = DateTime.UtcNow;
            return 0;
        });
}

public sealed class ShippingService(StoreService store, IConfiguration config, HttpClient http)
{
    public bool Ready => (config.GetSection("Ghn").Get<GhnOptions>() ?? new()).Ready;

    // Options: Đọc và kiểm tra cấu hình GHN.
    GhnOptions Options()
    {
        var o = config.GetSection("Ghn").Get<GhnOptions>() ?? new();
        if (!o.Ready)
            throw new InvalidOperationException("GHN chưa được cấu hình.");
        if (
            o.BaseUrl
            is not (
                "https://dev-online-gateway.ghn.vn/shiip/public-api/"
                or "https://online-gateway.ghn.vn/shiip/public-api/"
            )
        )
            throw new InvalidOperationException("Endpoint GHN không hợp lệ.");
        return o;
    }

    // Send: Gửi yêu cầu API GHN và kiểm tra phản hồi.
    async Task<JsonElement> Send(
        HttpMethod method,
        string route,
        object? body,
        CancellationToken token
    )
    {
        var o = Options();
        using var request = new HttpRequestMessage(method, o.BaseUrl + route);
        request.Headers.Add("Token", o.Token);
        request.Headers.Add("ShopId", o.ShopId.ToString());
        if (body != null)
            request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, token);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var root = json.RootElement;
        if ((int)response.StatusCode is >= 400 and < 500)
            throw new GhnRejectedException(
                "GHN từ chối yêu cầu. Kiểm tra địa chỉ, quyền tài khoản hoặc cấu hình."
            );
        if (
            !response.IsSuccessStatusCode
            || !root.TryGetProperty("code", out var status)
            || status.GetInt32() != 200
        )
            throw new InvalidOperationException(
                "Chưa xác định được kết quả GHN. Đối soát trước khi thử lại."
            );
        return root.GetProperty("data").Clone();
    }

    // Create: Tạo vận đơn GHN và lưu kết quả.
    public async Task Create(ShipmentInput input, int actor, CancellationToken token)
    {
        Options();
        Validator.ValidateObject(input, new ValidationContext(input), true);
        var user = store.CurrentAccount(actor);
        if (user?.Role is not ("Admin" or "Sales") || !user.Active)
            throw new InvalidOperationException("Không có quyền tạo vận đơn.");
        var order = store.Read(d => d.Orders.Single(o => o.Id == input.Id));
        var existing = store.NativeRead(db =>
            db.Shipments.SingleOrDefault(s => s.OrderId == order.DatabaseId)
        );
        if (existing?.TrackingCode != null)
        {
            await Sync(input.Id, actor, token);
            return;
        }

        decimal cod = order.Total - order.PaidAmount;
        if (cod != decimal.Truncate(cod))
            throw new InvalidOperationException("GHN requires an integer VND collection amount.");
        if (cod > 50000000m)
            throw new InvalidOperationException("COD vượt giới hạn GHN 50 triệu đồng.");
        var body = new
        {
            to_name = order.Name,
            to_phone = order.Phone,
            to_address = order.Address,
            to_province_name = input.Province.Trim(),
            to_ward_name = input.Ward.Trim(),
            is_new_to_address = true,
            client_order_code = order.Id,
            weight = input.Weight,
            length = input.Length,
            width = input.Width,
            height = input.Height,
            service_type_id = 2,
            payment_type_id = 1,
            required_note = "CHOXEMHANGKHONGTHU",
            cod_amount = (long)cod,
            insurance_value = (long)Math.Min(order.Subtotal, 5000000),
            items = order
                .Lines.Select(l => new
                {
                    name = l.Name,
                    code = l.VariantId.ToString(),
                    quantity = l.Quantity,
                })
                .ToArray(),
        };
        string hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)))
        );
        var claim = store.ClaimShipment(input.Id, hash, cod, actor);
        try
        {
            var data = await Send(HttpMethod.Post, "v2/shipping-order/create", body, token);
            string code =
                data.GetProperty("order_code").GetString()
                ?? throw new InvalidOperationException("GHN thiếu mã vận đơn.");
            store.ShipmentResult(claim.OrderId, code, "ready_to_pick", "", actor);
        }
        catch (GhnRejectedException ex)
        {
            bool uncertain = existing?.Status is "Unknown" or "Requesting";
            store.ShipmentResult(
                claim.OrderId,
                null,
                uncertain ? "Unknown" : "Rejected",
                uncertain
                    ? "Lần gửi trước chưa xác định kết quả; cần đồng bộ/đối soát GHN."
                    : ex.Message,
                actor
            );
            throw;
        }
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or OperationCanceledException
                        or JsonException
                        or InvalidOperationException
            )
        {
            store.ShipmentResult(
                claim.OrderId,
                null,
                "Unknown",
                "Chưa xác định kết quả. Đồng bộ/đối soát GHN trước khi gửi lại.",
                actor
            );
            throw new InvalidOperationException(
                (
                    "Chưa xác định kết quả GHN. Không tạo đơn mới bằng mã "
                    + "khác; đồng bộ để đối soát."
                )
            );
        }
    }

    // Sync: Đồng bộ vận đơn GHN theo mã đơn đã lưu.
    public async Task Sync(string id, int actor, CancellationToken token)
    {
        Options();
        var user = store.CurrentAccount(actor);
        if (user?.Role is not ("Admin" or "Sales") || !user.Active)
            throw new InvalidOperationException("Không có quyền đồng bộ.");
        var order = store.Read(d => d.Orders.Single(o => o.Id == id));
        var s =
            store.NativeRead(db => db.Shipments.SingleOrDefault(s => s.OrderId == order.DatabaseId))
            ?? throw new InvalidOperationException("Đơn chưa gửi GHN.");
        var data = await Send(
            HttpMethod.Get,
            "v2/shipping-order/detail-by-client-code?client_order_code="
                + Uri.EscapeDataString(s.ClientCode),
            null,
            token
        );
        if (data.GetProperty("client_order_code").GetString() != s.ClientCode)
            throw new InvalidOperationException("GHN trả sai mã đơn.");
        store.ShipmentResult(
            order.DatabaseId,
            data.GetProperty("order_code").GetString(),
            data.GetProperty("status").GetString() ?? "Unknown",
            "",
            actor
        );
    }
}

public sealed class GhnRejectedException(string message) : InvalidOperationException(message);
