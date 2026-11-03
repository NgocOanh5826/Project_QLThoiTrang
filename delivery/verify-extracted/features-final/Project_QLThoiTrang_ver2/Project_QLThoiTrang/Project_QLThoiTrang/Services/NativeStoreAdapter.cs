using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Project_QLThoiTrang.Data;
using Project_QLThoiTrang.Data.Native;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;

public sealed partial class StoreService
{
 static string StaffRole(string title)=>title switch {"Admin"=>"Admin","Catalog"=>"Catalog","Inventory"=>"Inventory","Sales"=>"Sales","Support"=>"Support",_ when title.Contains("kho",StringComparison.OrdinalIgnoreCase)=>"Inventory",_ when title.Contains("sản phẩm",StringComparison.OrdinalIgnoreCase)=>"Catalog",_ when title.Contains("đơn",StringComparison.OrdinalIgnoreCase)||title.Contains("bán",StringComparison.OrdinalIgnoreCase)=>"Sales",_=>"Support"};
 static string PublicId(DonHangRow o)=>o.QPublicId??"QH-SQL-"+o.MaDonHang;
 StoreData Load(StoreDbContext db)
 {
  var cats=db.DanhMuc.ToList();var customers=db.KhachHang.ToList();var staff=db.NhanVien.ToList();
  var products=db.SanPham.ToList();var sizes=db.KichThuoc.ToList();var colors=db.MauSac.ToList();
  var variants=db.BienTheSanPham.ToList();var images=db.HinhAnhSanPham.ToList();var warehouses=db.Kho.ToList();var stock=db.TonKho.ToList();
  var promos=db.KhuyenMai.ToList();var promoItems=db.ChiTietKhuyenMai.ToList();var orders=db.DonHang.ToList();var lines=db.ChiTietDonHang.ToList();
  var payments=db.ThanhToan.ToList();var returns=db.PhieuTraHang.ToList();var returnLines=db.ChiTietTraHang.ToList();var history=db.LichSuTrangThaiDonHang.ToList();
  var config=db.Configuration.SingleOrDefault();
  var suppliers=db.NhaCungCap.ToList();db.PhieuNhap.Load();db.ChiTietPhieuNhap.Load();
  db.GioHang.Load();db.ChiTietGioHang.Load();
  var d=new StoreData{Shipments=db.Shipments.ToList(),Categories=cats.Where(c=>c.TrangThai).Select(c=>c.TenDanhMuc).ToList(),Settings=config?.Settings??new(),ImportedFileHash=config?.ImportedFileHash,
   StockMovements=db.StockMovements.ToList(),Payments=db.Payments.ToList(),Warehouses=warehouses.Select(w=>new Warehouse{Id=w.MaKho,Name=w.TenKho,Active=w.TrangThai}).ToList(),
   Stocks=stock.Select(s=>new WarehouseStock{WarehouseId=s.MaKho,VariantId=s.MaBienThe,Quantity=s.SoLuongTon}).ToList()};
  d.Variants=variants.Select(v=>new Variant{Id=v.MaBienThe,ProductId=v.MaSP,Size=sizes.Single(s=>s.MaKichThuoc==v.MaKichThuoc).TenKichThuoc,Color=colors.Single(c=>c.MaMauSac==v.MaMauSac).TenMauSac,Sku=v.SKU,Price=v.GiaBanRieng,Active=v.TrangThai,Stock=stock.Where(s=>s.MaBienThe==v.MaBienThe&&warehouses.Any(w=>w.MaKho==s.MaKho&&w.TrangThai)).Sum(s=>s.SoLuongTon)}).ToList();
  d.Suppliers=suppliers.Select(s=>new Supplier{Id=s.MaNCC,Name=s.TenNCC,Active=s.TrangThai}).ToList();
  d.PaymentReferences=payments.Where(p=>p.MaGiaoDich!=null).Select(p=>p.MaGiaoDich!).Concat(returns.Where(r=>r.QReference!=null).Select(r=>r.QReference!)).ToList();
  d.Products=products.Select(p=>new Product{Id=p.MaSP,LegacyId=p.QLegacyId,Name=p.TenSP,Category=cats.Single(c=>c.MaDanhMuc==p.MaDanhMuc).TenDanhMuc,BrandId=p.MaThuongHieu,Price=p.GiaBan,Description=p.MoTa??"",Active=p.TrangThai,IsNew=p.QIsNew,OnSale=p.QOnSale,
   Image=SafeImage(p.QImage??images.FirstOrDefault(i=>i.MaSP==p.MaSP&&i.AnhChinh)?.DuongDan,p.MaSP),Color=d.Variants.FirstOrDefault(v=>v.ProductId==p.MaSP)?.Color??"Trắng",Variants=d.Variants.Where(v=>v.ProductId==p.MaSP).ToList(),Stock=stock.Where(s=>variants.Any(v=>v.MaSP==p.MaSP&&v.MaBienThe==s.MaBienThe)).Sum(s=>s.SoLuongTon)}).ToList();
  d.Customers=customers.Select(c=>new Customer{Id=c.MaKH,LegacyId=c.QLegacyId,Name=c.HoTen,Email=c.Email,PasswordHash=c.MatKhau,Phone=c.SoDienThoai??"",Address=c.DiaChi??"",Active=c.TrangThai}).Concat(staff.Select(c=>new Customer{Id=-c.MaNV,LegacyId=c.QLegacyId,Name=c.HoTen,Email=c.Email,PasswordHash=c.MatKhau,Phone=c.SoDienThoai??"",Address=c.DiaChi??"",Active=c.TrangThai,Role=StaffRole(c.ChucVu)})).ToList();
  d.Promotions=promos.Select(p=>new Promotion{Id=p.MaKM,Code=p.MaCode??"",Name=p.TenKhuyenMai,Kind=p.LoaiGiam,Value=p.GiaTriGiam,Percent=p.LoaiGiam=="PHANTRAM"?(int)p.GiaTriGiam:10,Minimum=p.DonHangToiThieu,Maximum=p.GiamToiDa,Remaining=p.SoLuong,From=p.NgayBatDau,Until=p.NgayKetThuc,Active=p.TrangThai,ProductIds=promoItems.Where(x=>x.MaKM==p.MaKM).Select(x=>x.MaSP).ToList()}).ToList();
  d.Orders=orders.Select(o=>new Order{DatabaseId=o.MaDonHang,Id=PublicId(o),BankQrImage=o.QBankQrImage,TransferReference=o.QTransferReference,CustomerId=o.MaKH,Created=o.NgayDat,Name=o.HoTenNguoiNhan,Phone=o.SoDienThoaiNhan,Address=o.DiaChiGiaoHang,Note=o.GhiChu??"",Status=o.TrangThai,ShippingSnapshot=o.PhiVanChuyen,Discount=o.TienGiam,PromotionCode=promos.FirstOrDefault(p=>p.MaKM==o.MaKM)?.MaCode??"",CheckoutToken=o.QCheckoutToken,
   Payment=payments.FirstOrDefault(p=>p.MaDonHang==o.MaDonHang)?.PhuongThuc??"COD",PaidAmount=payments.Where(p=>p.MaDonHang==o.MaDonHang&&(p.TrangThai=="Đã thanh toán"||p.TrangThai=="Đã hoàn tiền")).Sum(p=>p.SoTien),RefundedAmount=returns.Where(r=>r.MaDonHang==o.MaDonHang&&(r.TrangThai=="Đã hoàn tiền"||r.TrangThai=="Hoàn tất")).Sum(r=>r.TongTienHoan),
   Lines=lines.Where(l=>l.MaDonHang==o.MaDonHang).Select(l=>new OrderLine{Id=l.MaCTDH,OrderId=PublicId(o),VariantId=l.MaBienThe,ProductId=variants.Single(v=>v.MaBienThe==l.MaBienThe).MaSP,Name=l.TenSanPham,Image=SafeImage(l.QImage,variants.Single(v=>v.MaBienThe==l.MaBienThe).MaSP),Size=l.KichThuoc??"",Color=l.MauSac??"",Quantity=l.SoLuong,Price=l.DonGia,UnitCost=l.QUnitCost,WarehouseId=l.QWarehouseId}).ToList()}).ToList();
  d.Returns=returns.Select(r=>new ReturnRecord{Id=r.MaPhieuTra,OrderId=PublicId(orders.Single(o=>o.MaDonHang==r.MaDonHang)),CustomerId=r.MaKH,ActorId=r.MaNV.HasValue?-r.MaNV:null,Created=r.NgayYeuCau,Resolved=r.NgayXuLy,Reason=r.LyDo,Resolution=r.GhiChu??"",Status=r.TrangThai is "Đã hoàn tiền" or "Hoàn tất"?"Approved":r.TrangThai=="Từ chối"?"Rejected":"Pending",Refund=r.TongTienHoan,Reference=r.QReference??"",
   Lines=returnLines.Where(l=>l.MaPhieuTra==r.MaPhieuTra).Select(l=>new ReturnLine{Id=l.MaCTTra,OrderLineId=l.MaCTDH,Quantity=l.SoLuongTra,Refund=l.SoTienHoan,Condition=l.TinhTrangSanPham??""}).ToList()}).ToList();
  foreach(var o in d.Orders)if(o.Lines.All(l=>d.Returns.Where(r=>r.OrderId==o.Id&&r.Status=="Approved").SelectMany(r=>r.Lines).Where(r=>r.OrderLineId==l.Id).Sum(r=>r.Quantity)>=l.Quantity))o.Status="Đã hoàn hàng";
  d.History=history.Select(h=>new OrderHistory{Id=h.MaLichSuTrangThai,OrderId=PublicId(orders.Single(o=>o.MaDonHang==h.MaDonHang)),Status=h.TrangThai,Created=h.ThoiGian,Note=h.GhiChu??""}).ToList();return d;
 }
 static string SafeImage(string? image,int id)=>image!=null&&Enumerable.Range(1,5).Any(i=>image==$"/images/products/aothun{i:00}.jpg")?image:$"/images/products/aothun{(id-1)%5+1:00}.jpg";
 static T Row<T>(StoreDbContext db,Func<T,bool> match,Func<T> create) where T:class
 {var r=db.Set<T>().Local.FirstOrDefault(match);if(r!=null)return r;r=create();db.Set<T>().Add(r);return r;}
 static void Flush<T>(StoreDbContext db) where T:class
 {
  bool identity=db.Model.FindEntityType(typeof(T))!.GetProperties().Any(p=>p.GetValueGenerationStrategy()==SqlServerValueGenerationStrategy.IdentityColumn);
  bool added=db.ChangeTracker.Entries<T>().Any(e=>e.State==EntityState.Added);
  string table=db.Model.FindEntityType(typeof(T))!.GetTableName()!;
  // Identifier comes only from the compiled EF model, never from request input.
  Require(System.Text.RegularExpressions.Regex.IsMatch(table,@"^[A-Za-z][A-Za-z0-9_]*$"),"Invalid mapped table name.");
  if(identity&&added)db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT ["+table+"] ON;");
  try{db.SaveChanges();}finally{if(identity&&added)db.Database.ExecuteSqlRaw("SET IDENTITY_INSERT ["+table+"] OFF;");}
 }
 static void PrepareStock(StoreData d)
 {
  if(d.Warehouses.Count==0)d.Warehouses.Add(new Warehouse{Id=1,Name="Kho chính"});
  foreach(var p in d.Products)
  {
   if(!d.Variants.Any(v=>v.ProductId==p.Id))foreach(string size in new[]{"S","M","L","XL"})d.Variants.Add(new Variant{Id=Next(d.Variants.Select(v=>v.Id)),ProductId=p.Id,Size=size,Color=p.Color,Sku=$"QH-{p.Id}-{size}"});
   var variants=d.Variants.Where(v=>v.ProductId==p.Id).ToList();
   foreach(var v in variants)if(!d.Stocks.Any(s=>s.VariantId==v.Id))d.Stocks.Add(new WarehouseStock{WarehouseId=d.Warehouses.First(w=>w.Active).Id,VariantId=v.Id});
   int delta=p.Stock-d.Stocks.Where(s=>variants.Any(v=>v.Id==s.VariantId)).Sum(s=>s.Quantity);
   if(delta>0){var v=variants.FirstOrDefault(v=>v.Size=="M")??variants[0];d.Stocks.First(s=>s.VariantId==v.Id).Quantity+=delta;}
   if(delta<0)foreach(var s in d.Stocks.Where(s=>variants.Any(v=>v.Id==s.VariantId)).OrderBy(s=>s.VariantId)){int take=Math.Min(s.Quantity,-delta);s.Quantity-=take;delta+=take;if(delta==0)break;}
   Require(d.Stocks.Where(s=>variants.Any(v=>v.Id==s.VariantId)).Sum(s=>s.Quantity)==p.Stock,"Không đủ tồn kho biến thể.");
   p.Variants=variants;foreach(var v in variants)v.Stock=d.Stocks.Where(s=>s.VariantId==v.Id&&d.Warehouses.Any(w=>w.Id==s.WarehouseId&&w.Active)).Sum(s=>s.Quantity);
  }
 }
 void PersistNative(StoreDbContext db,StoreData d)
 {
  foreach(string name in d.Categories)Row(db,(DanhMucRow c)=>c.TenDanhMuc==name,()=>new(){MaDanhMuc=Next(db.DanhMuc.Local.Select(c=>c.MaDanhMuc)),TenDanhMuc=name,TrangThai=true});
  foreach(var c in db.DanhMuc.Local)if(!d.Categories.Contains(c.TenDanhMuc)&&!d.Products.Any(p=>p.Category==c.TenDanhMuc))db.DanhMuc.Remove(c);Flush<DanhMucRow>(db);
  foreach(var c in d.Customers.Where(c=>c.Role=="Customer")){var r=Row(db,(KhachHangRow x)=>x.MaKH==c.Id,()=>new(){MaKH=c.Id,NgayDangKy=DateTime.Now});r.HoTen=c.Name;r.Email=c.Email;r.MatKhau=c.PasswordHash;r.SoDienThoai=string.IsNullOrWhiteSpace(c.Phone)?null:c.Phone;r.DiaChi=c.Address;r.TrangThai=c.Active;r.QLegacyId=c.LegacyId;}Flush<KhachHangRow>(db);
  foreach(var c in d.Customers.Where(c=>c.Role!="Customer")){var r=Row(db,(NhanVienRow x)=>x.MaNV==Math.Abs(c.Id),()=>new(){MaNV=Math.Abs(c.Id),NgayVaoLam=DateTime.Today,ChucVu=c.Role});r.HoTen=c.Name;r.Email=c.Email;r.MatKhau=c.PasswordHash;r.SoDienThoai=string.IsNullOrWhiteSpace(c.Phone)?null:c.Phone;r.DiaChi=c.Address;r.TrangThai=c.Active;r.QLegacyId=c.LegacyId;if(StaffRole(r.ChucVu)!=c.Role)r.ChucVu=c.Role;}Flush<NhanVienRow>(db);
  foreach(var p in d.Products){var r=Row(db,(SanPhamRow x)=>x.MaSP==p.Id,()=>new(){MaSP=p.Id,NgayTao=DateTime.Now});r.TenSP=p.Name;r.MaDanhMuc=db.DanhMuc.Local.Single(c=>c.TenDanhMuc==p.Category).MaDanhMuc;r.MaThuongHieu=p.BrandId;r.GiaBan=p.Price;r.MoTa=p.Description;r.TrangThai=p.Active;r.QImage=p.Image;r.QIsNew=p.IsNew;r.QOnSale=p.OnSale;r.QLegacyId=p.LegacyId;}Flush<SanPhamRow>(db);
  foreach(var v in d.Variants){Row(db,(KichThuocRow x)=>x.TenKichThuoc==v.Size,()=>new(){MaKichThuoc=Next(db.KichThuoc.Local.Select(x=>x.MaKichThuoc)),TenKichThuoc=v.Size});}Flush<KichThuocRow>(db);
  foreach(var v in d.Variants){Row(db,(MauSacRow x)=>x.TenMauSac==v.Color,()=>new(){MaMauSac=Next(db.MauSac.Local.Select(x=>x.MaMauSac)),TenMauSac=v.Color});}Flush<MauSacRow>(db);
  foreach(var v in d.Variants){var r=Row(db,(BienTheSanPhamRow x)=>x.MaBienThe==v.Id,()=>new(){MaBienThe=v.Id});r.MaSP=v.ProductId;r.MaKichThuoc=db.KichThuoc.Local.Single(x=>x.TenKichThuoc==v.Size).MaKichThuoc;r.MaMauSac=db.MauSac.Local.Single(x=>x.TenMauSac==v.Color).MaMauSac;r.SKU=v.Sku;r.GiaBanRieng=v.Price;r.TrangThai=v.Active;}Flush<BienTheSanPhamRow>(db);
  foreach(var w in d.Warehouses){var r=Row(db,(KhoRow x)=>x.MaKho==w.Id,()=>new(){MaKho=w.Id});r.TenKho=w.Name;r.TrangThai=w.Active;}Flush<KhoRow>(db);
  foreach(var s in d.Stocks){var r=Row(db,(TonKhoRow x)=>x.MaKho==s.WarehouseId&&x.MaBienThe==s.VariantId,()=>new(){MaKho=s.WarehouseId,MaBienThe=s.VariantId});r.SoLuongTon=s.Quantity;}Flush<TonKhoRow>(db);
  var receipts=d.StockMovements.Where(m=>m.Reason=="Nhập kho"&&!db.StockMovements.Local.Any(x=>x.Id==m.Id)).GroupBy(m=>m.ReceiptReference??m.Reference).ToList();
  foreach(var group in receipts)foreach(var m in group){if(!m.SupplierId.HasValue){var supplier=Row(db,(NhaCungCapRow x)=>x.TenNCC==m.Supplier,()=>new(){MaNCC=Next(db.NhaCungCap.Local.Select(s=>s.MaNCC)),TenNCC=m.Supplier,TrangThai=true});m.SupplierId=supplier.MaNCC;}}Flush<NhaCungCapRow>(db);
  foreach(var group in receipts){var m=group.First();var header=new PhieuNhapRow{MaPhieuNhap=Next(db.PhieuNhap.Local.Select(x=>x.MaPhieuNhap)),MaKho=m.WarehouseId!.Value,MaNCC=m.SupplierId!.Value,MaNV=-m.ActorId!.Value,NgayNhap=m.Created,TongTien=group.Sum(x=>x.Delta*x.UnitCost),QReference=group.Key,GhiChu="Phiếu nhập qua QOANH"};db.PhieuNhap.Add(header);}Flush<PhieuNhapRow>(db);
  foreach(var group in receipts){var header=db.PhieuNhap.Local.Single(p=>p.QReference==group.Key);db.SupplierAccounts.Add(new(){ReceiptId=header.MaPhieuNhap,Amount=header.TongTien,ActorId=group.First().ActorId??0,UpdatedUtc=DateTime.UtcNow,Note="New receipt"});}
  foreach(var group in receipts){var header=db.PhieuNhap.Local.Single(p=>p.QReference==group.Key);foreach(var m in group)db.ChiTietPhieuNhap.Add(new(){MaPhieuNhap=header.MaPhieuNhap,MaBienThe=m.VariantId!.Value,SoLuong=m.Delta,GiaNhap=m.UnitCost});}Flush<ChiTietPhieuNhapRow>(db);
  foreach(var p in d.Promotions){var r=Row(db,(KhuyenMaiRow x)=>x.MaKM==p.Id,()=>new(){MaKM=p.Id});r.MaCode=p.Code;r.TenKhuyenMai=p.Name;r.LoaiGiam=p.Kind;r.GiaTriGiam=p.Value>0?p.Value:p.Percent;r.DonHangToiThieu=p.Minimum;r.GiamToiDa=p.Maximum;r.SoLuong=p.Remaining;r.NgayBatDau=p.From;r.NgayKetThuc=p.Until;r.TrangThai=p.Active;}Flush<KhuyenMaiRow>(db);
  foreach(var row in db.ChiTietKhuyenMai.Local.ToList())if(d.Promotions.Any(p=>p.Id==row.MaKM&&!p.ProductIds.Contains(row.MaSP)))db.ChiTietKhuyenMai.Remove(row);
  foreach(var p in d.Promotions)foreach(int id in p.ProductIds)Row(db,(ChiTietKhuyenMaiRow x)=>x.MaKM==p.Id&&x.MaSP==id,()=>new(){MaKM=p.Id,MaSP=id});Flush<ChiTietKhuyenMaiRow>(db);
  var oldStatus=db.DonHang.Local.ToDictionary(o=>o.MaDonHang,o=>o.TrangThai);
  foreach(var o in d.Orders.Where(o=>o.DatabaseId==0)){var cart=db.GioHang.Local.FirstOrDefault(g=>g.MaKH==o.CustomerId);if(cart!=null)db.ChiTietGioHang.RemoveRange(db.ChiTietGioHang.Local.Where(l=>l.MaGioHang==cart.MaGioHang).ToList());}
  foreach(var o in d.Orders){if(o.DatabaseId==0)o.DatabaseId=Next(db.DonHang.Local.Select(x=>x.MaDonHang));var r=Row(db,(DonHangRow x)=>x.MaDonHang==o.DatabaseId,()=>new(){MaDonHang=o.DatabaseId});r.MaKH=o.CustomerId;r.MaKM=d.Promotions.FirstOrDefault(p=>p.Code==o.PromotionCode)?.Id;r.NgayDat=o.Created;r.HoTenNguoiNhan=o.Name;r.SoDienThoaiNhan=o.Phone;r.DiaChiGiaoHang=o.Address;r.TongTienHang=o.Subtotal;r.TienGiam=o.Discount;r.PhiVanChuyen=o.Shipping;r.TongThanhToan=o.Total;r.TrangThai=o.Status=="Đã hoàn hàng"?"Hoàn thành":o.Status;r.GhiChu=o.Note;r.QPublicId=o.Id;r.QCheckoutToken=o.CheckoutToken;r.QBankQrImage=o.BankQrImage;r.QTransferReference=o.TransferReference;}Flush<DonHangRow>(db);
  foreach(var o in d.Orders)foreach(var l in o.Lines){if(l.Id==0)l.Id=Next(db.ChiTietDonHang.Local.Select(x=>x.MaCTDH));if(l.VariantId==0)l.VariantId=d.Variants.First(v=>v.ProductId==l.ProductId&&v.Size==l.Size).Id;var r=Row(db,(ChiTietDonHangRow x)=>x.MaCTDH==l.Id,()=>new(){MaCTDH=l.Id});r.MaDonHang=o.DatabaseId;r.MaBienThe=l.VariantId;r.TenSanPham=l.Name;r.KichThuoc=l.Size;r.MauSac=string.IsNullOrWhiteSpace(l.Color)?d.Variants.Single(v=>v.Id==l.VariantId).Color:l.Color;r.SoLuong=l.Quantity;r.DonGia=l.Price;r.QImage=l.Image;r.QWarehouseId=l.WarehouseId;r.QUnitCost=l.UnitCost;}Flush<ChiTietDonHangRow>(db);
  foreach(var o in d.Orders){string status=o.Status=="Đã hoàn hàng"?"Hoàn thành":o.Status;if(oldStatus.GetValueOrDefault(o.DatabaseId)!=status)db.LichSuTrangThaiDonHang.Add(new(){MaLichSuTrangThai=db.LichSuTrangThaiDonHang.Local.Select(x=>x.MaLichSuTrangThai).DefaultIfEmpty().Max()+1,MaDonHang=o.DatabaseId,TrangThai=status,ThoiGian=DateTime.Now,GhiChu="Cập nhật qua QOANH"});}Flush<LichSuTrangThaiDonHangRow>(db);
  foreach(var o in d.Orders){var r=db.ThanhToan.Local.FirstOrDefault(p=>p.MaDonHang==o.DatabaseId);if(r==null){r=new(){MaThanhToan=Next(db.ThanhToan.Local.Select(x=>x.MaThanhToan)),MaDonHang=o.DatabaseId,PhuongThuc=o.Payment=="Chuyển khoản"?o.Payment:"COD"};db.ThanhToan.Add(r);}r.SoTien=o.Total;r.TrangThai=o.PaidAmount>=o.Total?(o.RefundedAmount>=o.Total?"Đã hoàn tiền":"Đã thanh toán"):"Chưa thanh toán";var receipt=d.Payments.LastOrDefault(p=>p.OrderId==o.Id&&p.Kind=="Receipt");if(receipt!=null){r.MaGiaoDich=receipt.Reference;r.NgayThanhToan=receipt.Created;}}Flush<ThanhToanRow>(db);
  foreach(var r in d.Returns){var o=d.Orders.Single(o=>o.Id==r.OrderId);var row=Row(db,(PhieuTraHangRow x)=>x.MaPhieuTra==r.Id,()=>new(){MaPhieuTra=r.Id});row.MaDonHang=o.DatabaseId;row.MaKH=r.CustomerId;row.MaNV=r.ActorId<0?-r.ActorId:null;row.NgayYeuCau=r.Created;row.LyDo=r.Reason;row.TrangThai=r.Status=="Approved"?"Đã hoàn tiền":r.Status=="Rejected"?"Từ chối":"Chờ xử lý";row.TongTienHoan=r.Refund;row.NgayXuLy=r.Resolved;row.GhiChu=r.Resolution;row.QReference=string.IsNullOrEmpty(r.Reference)?null:r.Reference;}Flush<PhieuTraHangRow>(db);
  foreach(var r in d.Returns)foreach(var l in r.Lines){if(l.Id==0)l.Id=Next(db.ChiTietTraHang.Local.Select(x=>x.MaCTTra));var row=Row(db,(ChiTietTraHangRow x)=>x.MaCTTra==l.Id,()=>new(){MaCTTra=l.Id});row.MaPhieuTra=r.Id;row.MaCTDH=l.OrderLineId;row.SoLuongTra=l.Quantity;row.SoTienHoan=l.Refund;row.LyDoChiTiet=r.Reason;row.TinhTrangSanPham=l.Condition;}Flush<ChiTietTraHangRow>(db);
  foreach(var m in d.StockMovements)if(!db.StockMovements.Local.Any(x=>x.Id==m.Id))db.StockMovements.Add(m);
  foreach(var p in d.Payments)if(!db.Payments.Local.Any(x=>x.Id==p.Id))db.Payments.Add(p);
  var cfg=db.Configuration.Local.SingleOrDefault();if(cfg==null)db.Configuration.Add(new(){Settings=d.Settings,ImportedFileHash=d.ImportedFileHash});else{db.Entry(cfg.Settings).CurrentValues.SetValues(d.Settings);cfg.ImportedFileHash=d.ImportedFileHash;}
  db.SaveChanges();
 }
}
