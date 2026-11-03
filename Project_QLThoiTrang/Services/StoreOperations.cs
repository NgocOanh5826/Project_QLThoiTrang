using System.ComponentModel.DataAnnotations;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;
public sealed partial class StoreService
{
 static Order FindOrder(StoreData d,string id)=>d.Orders.FirstOrDefault(o=>o.Id==id)??throw new InvalidOperationException("Không tìm thấy đơn hàng.");
 static void Permission(StoreData d,int actor,params string[] roles)=>Require(d.Customers.Any(c=>c.Id==actor&&c.Active&&(c.Role=="Admin"||roles.Contains(c.Role))),"Không có quyền thực hiện thao tác này.");
 static string Text(string? text,int min,int max,string field){var v=text?.Trim()??"";Require(v.Length>=min&&v.Length<=max,field+" không hợp lệ.");return v;}
 static Variant GetVariant(StoreData d,CartItem c)
 {
  var p=d.Products.FirstOrDefault(p=>p.Id==c.ProductId&&p.Active);
  var v=d.Variants.FirstOrDefault(v=>v.ProductId==c.ProductId&&v.Active&&(c.VariantId>0?v.Id==c.VariantId:v.Size==c.Size&&(c.Color==""||v.Color==c.Color)));
  Require(p!=null&&v!=null,"Sản phẩm hoặc biến thể đã ngừng bán.");return v!;
 }
 static WarehouseStock StockRow(StoreData d,int variant,int warehouse)
 {var s=d.Stocks.FirstOrDefault(s=>s.VariantId==variant&&s.WarehouseId==warehouse);if(s!=null)return s;s=new(){VariantId=variant,WarehouseId=warehouse};d.Stocks.Add(s);return s;}
 static void Restore(StoreData d,OrderLine l,int quantity)
 {
  int warehouse=l.WarehouseId??d.Warehouses.First(w=>w.Active).Id;
  Require(d.Warehouses.Any(w=>w.Id==warehouse),"Kho gốc không tồn tại.");StockRow(d,l.VariantId,warehouse).Quantity+=quantity;d.Products.Single(p=>p.Id==l.ProductId).Stock+=quantity;
 }
 public Order PlaceOrder(int customerId,CheckoutInput input,List<CartItem> cart,List<OrderLine>? quote=null)=>Write(d=>
 {
  Validator.ValidateObject(input,new ValidationContext(input),true);Require(Guid.TryParseExact(input.CheckoutToken,"N",out _),"Phiên đặt hàng không hợp lệ.");
  var old=d.Orders.FirstOrDefault(o=>o.CheckoutToken==input.CheckoutToken);if(old!=null){Require(old.CustomerId==customerId,"Phiên đặt hàng không hợp lệ.");return old;}
  Require(d.Customers.Any(c=>c.Id==customerId&&c.Role=="Customer"&&c.Active),"Chỉ tài khoản khách hàng đang hoạt động được đặt hàng.");
  Require(input.Payment is "COD" or "Chuyển khoản","Phương thức thanh toán không hợp lệ.");
  if(input.Payment=="Chuyển khoản")Require(BankOptions.Enabled&&BankOptions.QrImage.StartsWith("/images/payments/",StringComparison.Ordinal),"Chuyển khoản chưa được cấu hình.");
  Require(cart.Count is >0 and <=100&&cart.All(c=>c.Quantity is >=1 and <=20),"Giỏ trống hoặc số lượng không hợp lệ.");
  var selected=cart.Select(c=>(item:c,variant:GetVariant(d,c))).ToList();
  Require(selected.GroupBy(x=>x.variant.Id).All(g=>g.Sum(x=>x.item.Quantity)<=20),"Tối đa 20 sản phẩm mỗi biến thể.");
  if(quote!=null)Require(cart.All(c=>quote.Any(l=>l.ProductId==c.ProductId&&l.Size==c.Size&&l.Quantity==c.Quantity&&(c.VariantId==0||l.VariantId==c.VariantId)))&&quote.Count==cart.Count,"Giỏ hàng đã thay đổi. Vui lòng kiểm tra lại.");
  var o=new Order{Id="QH-"+Guid.NewGuid().ToString("N").ToUpperInvariant(),CustomerId=customerId,CheckoutToken=input.CheckoutToken,Name=Text(input.Name,1,100,"Họ tên"),Phone=Text(input.Phone,9,15,"Điện thoại"),Address=Text(input.Address,1,300,"Địa chỉ"),Note=input.Note?.Trim()??""};
  foreach(var group in selected.GroupBy(x=>x.variant.Id))
  {
   var v=group.First().variant;var p=d.Products.Single(p=>p.Id==v.ProductId);int quantity=group.Sum(x=>x.item.Quantity);decimal price=v.Price??p.Price;
   if(quote!=null)Require(quote.Where(l=>l.ProductId==p.Id&&(l.VariantId==0||l.VariantId==v.Id)).All(l=>l.Price==price),"Giá sản phẩm đã thay đổi. Quay lại giỏ kiểm tra giá mới.");
   var stocks=d.Stocks.Where(s=>s.VariantId==v.Id&&d.Warehouses.Any(w=>w.Id==s.WarehouseId&&w.Active)).OrderBy(s=>s.WarehouseId).ToList();
   Require(stocks.Sum(s=>s.Quantity)>=quantity,"Biến thể không đủ tồn kho.");
   foreach(var s in stocks){int take=Math.Min(s.Quantity,quantity);if(take==0)continue;s.Quantity-=take;quantity-=take;p.Stock-=take;o.Lines.Add(new(){ProductId=p.Id,VariantId=v.Id,WarehouseId=s.WarehouseId,Name=p.Name,Image=p.Image,Size=v.Size,Color=v.Color,Price=price,Quantity=take});if(quantity==0)break;}
  }
  o.ShippingSnapshot=o.Shipping;
  o.Payment=input.Payment;
  if(input.Payment=="Chuyển khoản"){o.BankQrImage=BankOptions.QrImage;o.TransferReference="QO"+Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();}
  if(!string.IsNullOrWhiteSpace(input.PromotionCode))
  {
   var p=d.Promotions.FirstOrDefault(p=>p.Code.Equals(input.PromotionCode.Trim(),StringComparison.OrdinalIgnoreCase)&&p.Active&&p.From<=DateTime.Now&&p.Until.Date>=DateTime.Today&&(!p.Remaining.HasValue||p.Remaining>0));
   Require(p!=null&&o.Subtotal>=p.Minimum,"Mã ưu đãi không hợp lệ, hết lượt hoặc chưa đạt giá trị tối thiểu.");
   decimal eligible=o.Lines.Where(l=>p!.ProductIds.Count==0||p.ProductIds.Contains(l.ProductId)).Sum(l=>l.Price*l.Quantity);Require(eligible>0,"Mã không áp dụng cho sản phẩm trong đơn.");
   decimal value=p!.Value>0?p.Value:p.Percent;decimal discount=p.Kind=="SOTIEN"?value:eligible*value/100m;
   o.Discount=decimal.Round(Math.Min(eligible,Math.Min(discount,p.Maximum??decimal.MaxValue)),0,MidpointRounding.AwayFromZero);o.PromotionCode=p.Code;if(p.Remaining.HasValue)p.Remaining--;
  }
  d.Orders.Add(o);return o;
 },customerId,"Đặt hàng - trừ tồn biến thể");
 public void ChangeOrderStatus(string id,string status,int actor,bool customer=false)=>Write(d=>
 {
  var o=FindOrder(d,id);if(customer)Require(o.CustomerId==actor&&o.Status=="Chờ xác nhận"&&status=="Đã hủy","Chỉ hủy đơn chờ xác nhận của bạn.");else Permission(d,actor,"Sales");
  var allowed=o.Status switch{"Chờ xác nhận"=>new[]{"Đã xác nhận","Đã hủy"},"Đã xác nhận"=>new[]{"Đang chuẩn bị","Đang giao","Đã hủy"},"Đang chuẩn bị"=>new[]{"Đang giao","Đã hủy"},"Đang giao"=>new[]{"Đã giao"},"Đã giao"=>new[]{"Hoàn thành"},_=>Array.Empty<string>()};
  Require(allowed.Contains(status),"Không thể chuyển sang trạng thái này.");
  if(status is "Đã giao" or "Hoàn thành")Require(o.PaidAmount==o.Total,"Phải đối soát đủ tiền trước khi hoàn tất giao hàng.");
  if(o.Payment=="Chuyển khoản"&&status=="Đang giao")Require(o.PaidAmount==o.Total,"Đơn chuyển khoản cần nhận đủ tiền trước khi giao.");
  if(status=="Đã hủy"){Require(!d.Shipments.Any(s=>s.OrderId==o.DatabaseId&&s.Status is not ("cancel" or "Rejected")),"Cần hủy và đối soát vận đơn GHN trước khi hủy đơn.");Require(o.PaidAmount==0,"Đơn đã thu tiền cần hoàn tiền trước khi hủy.");foreach(var l in o.Lines)Restore(d,l,l.Quantity);if(o.PromotionCode!=""){var p=d.Promotions.Single(x=>x.Code==o.PromotionCode);if(p.Remaining.HasValue)p.Remaining++;}}
  o.Status=status;return 0;
 },actor,"Đơn "+id+": "+status);
 public void Receive(ReceiveInput input,int actor)=>ReceiveBatch(new(){input},actor);
 public void ReceiveBatch(List<ReceiveInput> inputs,int actor)=>Write(d=>
 {
  Permission(d,actor,"Inventory");Require(actor<0,"Phiếu nhập cần tài khoản nhân viên.");Require(inputs.Count is >0 and <=100,"Phiếu nhập cần từ 1 đến 100 dòng.");
  string reference="IN-"+Text(inputs[0].Reference,1,60,"Mã phiếu nhập");
  Require(!d.StockMovements.Any(m=>string.Equals(m.ReceiptReference,reference,StringComparison.OrdinalIgnoreCase)||m.Reference.Equals(reference,StringComparison.OrdinalIgnoreCase)),"Mã phiếu nhập đã sử dụng.");
  Require(inputs.All(i=>i.Reference.Trim()==inputs[0].Reference.Trim()&&i.WarehouseId==inputs[0].WarehouseId&&i.SupplierId==inputs[0].SupplierId),"Các dòng phải cùng phiếu, kho và nhà cung cấp.");
  var selected=new HashSet<int>();
  foreach(var i in inputs)
  {
   Validator.ValidateObject(i,new ValidationContext(i),true);var v=i.VariantId>0?d.Variants.SingleOrDefault(v=>v.Id==i.VariantId):d.Variants.FirstOrDefault(v=>v.ProductId==i.ProductId&&v.Size=="M");
   Require(v!=null&&selected.Add(v.Id),"Biến thể không tồn tại hoặc trùng dòng.");
   int warehouse=i.WarehouseId>0?i.WarehouseId:d.Warehouses.First(w=>w.Active).Id;Require(d.Warehouses.Any(w=>w.Id==warehouse&&w.Active),"Kho không hoạt động.");
   var supplier=i.SupplierId>0?d.Suppliers.FirstOrDefault(s=>s.Id==i.SupplierId&&s.Active):null;Require(i.SupplierId==0||supplier!=null,"Nhà cung cấp không hoạt động.");
   var p=d.Products.Single(p=>p.Id==v!.ProductId);int before=p.Stock;p.Stock=checked(p.Stock+i.Quantity);Require(p.Stock<=100000,"Tồn kho vượt giới hạn.");StockRow(d,v!.Id,warehouse).Quantity+=i.Quantity;
   d.StockMovements.Add(new(){Id=Next(d.StockMovements.Select(x=>x.Id)),ProductId=p.Id,VariantId=v.Id,WarehouseId=warehouse,Before=before,Delta=i.Quantity,After=p.Stock,Reason="Nhập kho",Reference=reference+"-"+v.Id,ReceiptReference=reference,Supplier=supplier?.Name??Text(i.Supplier,1,150,"Nhà cung cấp"),SupplierId=supplier?.Id,UnitCost=i.UnitCost,ActorId=actor});
  }return 0;
 },actor,"Nhập kho");
 public void AdjustStock(int id,int quantity,int expected,string reason,int actor)=>Write(d=>
 {
  Permission(d,actor,"Inventory");reason=Text(reason,5,300,"Lý do kiểm kê");var p=d.Products.SingleOrDefault(p=>p.Id==id)??throw new InvalidOperationException("Sản phẩm không tồn tại.");Require(p.Stock==expected,"Tồn kho đã thay đổi; tải lại trang trước khi kiểm kê.");Require(quantity is >=0 and <=100000,"Tồn kho không hợp lệ.");int old=p.Stock;p.Stock=quantity;d.StockMovements.Add(new(){Id=Next(d.StockMovements.Select(x=>x.Id)),ProductId=id,Before=old,Delta=quantity-old,After=quantity,Reason="Kiểm kê tổng (phân bổ M trước): "+reason,ActorId=actor,Reference="ADJ-"+Guid.NewGuid().ToString("N")});return 0;
 },actor,"Kiểm kê");
 public void AdjustVariantStock(int variantId,int warehouseId,int quantity,int expected,string reason,int actor)=>Write(d=>
 {
  Permission(d,actor,"Inventory");var v=d.Variants.SingleOrDefault(v=>v.Id==variantId)??throw new InvalidOperationException("Biến thể không tồn tại.");Require(d.Warehouses.Any(w=>w.Id==warehouseId&&w.Active),"Kho không hoạt động.");var stock=StockRow(d,variantId,warehouseId);Require(stock.Quantity==expected,"Tồn biến thể đã thay đổi; tải lại trang.");Require(quantity is >=0 and <=100000,"Tồn kho không hợp lệ.");var p=d.Products.Single(p=>p.Id==v.ProductId);int before=p.Stock;int delta=quantity-stock.Quantity;stock.Quantity=quantity;p.Stock+=delta;d.StockMovements.Add(new(){Id=Next(d.StockMovements.Select(x=>x.Id)),ProductId=p.Id,VariantId=v.Id,WarehouseId=warehouseId,Before=before,Delta=delta,After=p.Stock,ActorId=actor,Reason="Kiểm kê biến thể: "+Text(reason,5,300,"Lý do"),Reference="ADJ-"+Guid.NewGuid().ToString("N")});return 0;
 },actor,"Kiểm kê biến thể");
 public void RecordPayment(string id,decimal amount,string reference,int actor)=>Write(d=>
 {
  Permission(d,actor,"Sales");var o=FindOrder(d,id);reference=Text(reference,1,100,"Mã biên nhận");Require(!d.Shipments.Any(s=>s.OrderId==o.DatabaseId&&s.Status=="Requesting"),"Đang gửi vận đơn; chờ kết quả trước khi ghi nhận thêm tiền.");Require((o.Payment=="Chuyển khoản"&&o.Status=="Chờ xác nhận")||new[]{"Đã xác nhận","Đang chuẩn bị","Đang giao","Đã giao","Hoàn thành"}.Contains(o.Status),"Đơn chưa đủ điều kiện ghi nhận thanh toán.");Require(amount>0&&amount==o.Total-o.PaidAmount,"Số tiền phải đúng phần còn phải thu; không ghi nhận trùng.");Require(!d.Payments.Any(p=>p.Reference.Equals(reference,StringComparison.OrdinalIgnoreCase))&&!d.PaymentReferences.Contains(reference,StringComparer.OrdinalIgnoreCase),"Mã biên nhận đã sử dụng.");d.Payments.Add(new(){Id=Next(d.Payments.Select(x=>x.Id)),OrderId=id,Amount=amount,Reference=reference,ActorId=actor});o.PaidAmount+=amount;return 0;
 },actor,"Ghi nhận tiền thanh toán");
 static int Returned(StoreData d,string orderId,int lineId)=>d.Returns.Where(r=>r.OrderId==orderId&&r.Status=="Approved").SelectMany(r=>r.Lines).Where(l=>l.OrderLineId==lineId).Sum(l=>l.Quantity);
 public void RequestReturn(string id,string reason,int customerId,int lineId=0,int quantity=0)=>Write(d=>
 {
  var o=FindOrder(d,id);Require(o.CustomerId==customerId&&o.Status is "Đã giao" or "Hoàn thành","Chỉ yêu cầu trả đơn đã giao của bạn.");Require(!d.Returns.Any(r=>r.OrderId==id&&r.Status=="Pending"),"Đơn đang có yêu cầu chờ xử lý.");
  var r=new ReturnRecord{Id=Next(d.Returns.Select(x=>x.Id)),OrderId=id,CustomerId=customerId,Reason=Text(reason,5,500,"Lý do trả hàng")};
  foreach(var l in o.Lines.Where(l=>lineId==0||l.Id==lineId)){int remaining=l.Quantity-Returned(d,id,l.Id);int requested=lineId==0?remaining:quantity;if(requested==0&&lineId==0)continue;Require(requested>0&&requested<=remaining,"Số lượng trả vượt phần còn được trả.");decimal refund=decimal.Round(l.Price*requested*(o.Subtotal-o.Discount)/o.Subtotal,2,MidpointRounding.AwayFromZero);r.Lines.Add(new(){OrderLineId=l.Id,Quantity=requested,Refund=refund});}
  Require(r.Lines.Count>0,"Không có sản phẩm đủ điều kiện trả.");bool allRemaining=o.Lines.All(l=>Returned(d,id,l.Id)+r.Lines.Where(x=>x.OrderLineId==l.Id).Sum(x=>x.Quantity)==l.Quantity);
  r.Refund=allRemaining?o.Total-o.RefundedAmount:r.Lines.Sum(l=>l.Refund);Require(r.Refund>=0,"Số tiền hoàn không hợp lệ.");d.Returns.Add(r);return 0;
 },customerId,"Yêu cầu trả hàng");
 public void ResolveReturn(int returnId,bool approve,string resolution,string reference,int actor)=>Write(d=>
 {
  Permission(d,actor,"Sales");var r=d.Returns.SingleOrDefault(r=>r.Id==returnId)??throw new InvalidOperationException("Không tìm thấy yêu cầu.");Require(r.Status=="Pending","Yêu cầu đã xử lý.");var o=FindOrder(d,r.OrderId);Require(o.Status is "Đã giao" or "Hoàn thành","Đơn không còn đủ điều kiện trả.");r.Resolution=Text(resolution,5,500,"Ghi chú xử lý");r.ActorId=actor;r.Resolved=DateTime.UtcNow;
  if(approve)
  {
   Require(o.PaidAmount==o.Total&&r.Refund>0&&r.Refund<=o.PaidAmount-o.RefundedAmount,"Cần đối soát đủ tiền gốc và số tiền còn được hoàn.");reference=Text(reference,1,100,"Mã chứng từ hoàn tiền");Require(!d.Payments.Any(p=>p.Reference.Equals(reference,StringComparison.OrdinalIgnoreCase))&&!d.PaymentReferences.Contains(reference,StringComparer.OrdinalIgnoreCase),"Chứng từ đã sử dụng.");
   foreach(var returned in r.Lines){var l=o.Lines.Single(l=>l.Id==returned.OrderLineId);Require(returned.Quantity+Returned(d,o.Id,l.Id)<=l.Quantity,"Số lượng trả vượt số đã mua.");Restore(d,l,returned.Quantity);returned.Condition=r.Resolution;}
   d.Payments.Add(new(){Id=Next(d.Payments.Select(x=>x.Id)),OrderId=o.Id,Kind="Refund",Amount=r.Refund,Reference=reference,ActorId=actor});o.RefundedAmount+=r.Refund;r.Reference=reference;r.Status="Approved";
   if(o.Lines.All(l=>Returned(d,o.Id,l.Id)>=l.Quantity))o.Status="Đã hoàn hàng";
  }else r.Status="Rejected";return 0;
 },actor,"Nhận hàng trả và hoàn tiền");
}
