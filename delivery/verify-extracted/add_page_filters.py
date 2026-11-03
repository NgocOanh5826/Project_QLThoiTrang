from pathlib import Path
base=Path('Project_QLThoiTrang/Views')
choice=lambda key,label,items:'AdminListFilter.Choice("'+key+'","'+label+'",'+','.join('("'+v+'","'+t+'")' for v,t in items)+')'
auto=lambda key,label:'new AdminListFilter("'+key+'","'+label+'")'
state=choice('state','Trạng thái',[('active','Đang hoạt động'),('inactive','Ngừng hoạt động')])
stock=choice('stock','Tồn kho',[('empty','Hết hàng'),('low','Sắp hết (1–5)'),('normal','Còn hàng (>5)')])
config={
'Admin/Promotions.cshtml':[choice('state','Trạng thái',[('active','Đang hiệu lực'),('upcoming','Sắp diễn ra'),('expired','Hết hạn'),('inactive','Đã tắt')]),choice('kind','Loại giảm',[('PHANTRAM','Phần trăm'),('SOTIEN','Số tiền')]),auto('month','Tháng kết thúc')],
'Admin/Variants.cshtml':[auto('product','Sản phẩm'),auto('size','Kích thước'),auto('color','Màu sắc'),state,stock],
'Admin/Staff.cshtml':[state,choice('role','Vai trò',[('Admin','Quản trị'),('Catalog','Sản phẩm'),('Inventory','Kho'),('Sales','Bán hàng'),('Support','Hỗ trợ')])],
'Admin/Categories.cshtml':[choice('usage','Sử dụng',[('used','Đã có sản phẩm'),('unused','Chưa có sản phẩm')])],
'Admin/Inventory.cshtml':[auto('category','Danh mục'),stock],
'Admin/VariantStock.cshtml':[auto('warehouse','Kho'),auto('product','Sản phẩm'),auto('size','Kích thước'),auto('color','Màu sắc'),stock],
'Admin/Receipts.cshtml':[auto('warehouse','Kho nhận'),auto('supplier','Nhà cung cấp'),auto('staff','Nhân viên nhập'),auto('month','Tháng nhập')],
'Admin/Returns.cshtml':[choice('state','Trạng thái',[('Pending','Chờ xử lý'),('Approved','Đã duyệt'),('Rejected','Từ chối')]),auto('month','Tháng yêu cầu')],
'Admin/Reviews.cshtml':[choice('state','Hiển thị',[('active','Đang hiển thị'),('inactive','Đã ẩn')]),auto('stars','Số sao'),auto('product','Sản phẩm')],
'Admin/LoginHistory.cshtml':[choice('actor','Loại tài khoản',[('customer','Khách hàng'),('staff','Nhân viên')]),auto('result','Kết quả'),auto('month','Tháng đăng nhập')],
'Finance/Suppliers.cshtml':[auto('supplier','Nhà cung cấp'),choice('debt','Công nợ',[('unknown','Chưa đối soát'),('owing','Còn nợ'),('settled','Đã thanh toán đủ')]),auto('month','Tháng nhập')],
'Valuation/Index.cshtml':[auto('category','Danh mục'),choice('cost','Giá vốn',[('known','Đã xác định'),('unknown','Chưa xác định')]),choice('stock','Tồn kho',[('empty','Hết hàng'),('available','Còn hàng')])],
'Chat/Inbox.cshtml':[choice('unread','Tin nhắn',[('unread','Có tin chưa đọc'),('read','Đã đọc hết')]),auto('month','Tháng cập nhật')]
}
row_attrs={
'Admin/Promotions.cshtml':'data-filter-state="@(!p.Active?"inactive":p.Until.Date<DateTime.Today?"expired":p.From>DateTime.Now?"upcoming":"active")" data-filter-kind="@p.Kind" data-filter-month="@p.Until.ToString("yyyy-MM")"',
'Admin/Variants.cshtml':'data-filter-product="@v.ProductId" data-filter-product-label="@products.First(p=>p.Id==v.ProductId).Name" data-filter-size="@v.Size" data-filter-color="@v.Color" data-filter-stock="@(v.Stock==0?"empty":v.Stock<=5?"low":"normal")"',
'Admin/Staff.cshtml':'data-filter-role="@c.Role"',
'Admin/Categories.cshtml':'data-filter-usage="@(Model.Products.Any(p=>p.Category==category)?"used":"unused")"',
'Admin/Inventory.cshtml':'data-filter-category="@p.Category" data-filter-stock="@(p.Stock==0?"empty":p.Stock<=5?"low":"normal")"',
'Admin/VariantStock.cshtml':'data-filter-warehouse="@w.Id" data-filter-warehouse-label="@w.Name" data-filter-product="@v.ProductId" data-filter-product-label="@Model.Products.Single(p=>p.Id==v.ProductId).Name" data-filter-size="@v.Size" data-filter-color="@v.Color" data-filter-stock="@(quantity==0?"empty":quantity<=5?"low":"normal")"',
'Admin/Receipts.cshtml':'data-filter-warehouse="@r.MaKho" data-filter-warehouse-label="@(((Dictionary<int,string>)ViewBag.WarehouseNames).GetValueOrDefault(r.MaKho))" data-filter-supplier="@r.MaNCC" data-filter-supplier-label="@(((Dictionary<int,string>)ViewBag.SupplierNames).GetValueOrDefault(r.MaNCC))" data-filter-staff="@r.MaNV" data-filter-staff-label="@(((Dictionary<int,string>)ViewBag.StaffNames).GetValueOrDefault(r.MaNV))" data-filter-month="@r.NgayNhap.ToString("yyyy-MM")"',
'Admin/Returns.cshtml':'data-filter-state="@r.Status" data-filter-month="@r.Created.ToLocalTime().ToString("yyyy-MM")"',
'Admin/Reviews.cshtml':'data-filter-state="@(r.Active?"active":"inactive")" data-filter-stars="@r.Stars" data-filter-product="@r.ProductId" data-filter-product-label="@(((Dictionary<int,string>)ViewBag.ProductNames).GetValueOrDefault(r.ProductId))"',
'Admin/LoginHistory.cshtml':'data-filter-actor="@(r.MaNV.HasValue?"staff":"customer")" data-filter-result="@r.TrangThai" data-filter-month="@r.ThoiGianDangNhap.ToString("yyyy-MM")"',
'Finance/Suppliers.cshtml':'data-filter-supplier="@p.MaNCC" data-filter-supplier-label="@suppliers.FirstOrDefault(s=>s.Id==p.MaNCC)?.Name" data-filter-debt="@(a==null?"unknown":a.Amount>amount?"owing":"settled")" data-filter-month="@p.NgayNhap.ToString("yyyy-MM")"',
'Valuation/Index.cshtml':'data-filter-category="@Model.Products.Single(p=>p.Id==v.ProductId).Category" data-filter-cost="@(a?.AverageCost.HasValue==true?"known":"unknown")" data-filter-stock="@(Model.Stocks.Where(s=>s.VariantId==v.Id).Sum(s=>s.Quantity)==0?"empty":"available")"',
'Chat/Inbox.cshtml':'data-filter-unread="@(t.Unread>0?"unread":"read")" data-filter-month="@t.Updated.ToLocalTime().ToString("yyyy-MM")"'
}
for name,filters in config.items():
 p=base/name;s=p.read_text(encoding='utf-8');marker='<partial name="_AdminListToolbar"/>';assert marker in s,name
 s=s.replace(marker,'@{ViewData["TableFilters"]=new[]{'+','.join(filters)+'};}\n'+marker,1)
 attrs=row_attrs[name]
 if '<tr data-state=' in s:s=s.replace('<tr data-state=','<tr '+attrs+' data-state=',1)
 else:s=s.replace('<tr><td>','<tr '+attrs+'><td>',1)
 if name=='Admin/Promotions.cshtml':s=s.replace('@(p.Active&&p.Until.Date>=DateTime.Today?"Đang bật":"Ngừng / hết hạn")','@(!p.Active?"Đã tắt":p.Until.Date<DateTime.Today?"Hết hạn":p.From>DateTime.Now?"Sắp diễn ra":"Đang hiệu lực")')
 p.write_text(s,encoding='utf-8')
# Reference lists use state only where the original schema supports it.
p=base/'Admin/References.cshtml';s=p.read_text(encoding='utf-8');filters='''@{var usedReferences=(HashSet<int>)ViewBag.ReferenceUsage;var referenceFilters=new List<AdminListFilter>{AdminListFilter.Choice("usage","Sử dụng",("used","Đang được sử dụng"),("unused","Chưa được sử dụng"))};if(Model.Input.Kind is not ("sizes" or "colors")){referenceFilters.Insert(0,AdminListFilter.Choice("state","Trạng thái",("active","Đang hoạt động"),("inactive","Ngừng hoạt động")));}ViewData["TableFilters"]=referenceFilters;}
''';s=s.replace('<partial name="_AdminListToolbar"/>',filters+'<partial name="_AdminListToolbar"/>',1);s=s.replace('<tr data-state=','<tr data-filter-usage="@(usedReferences.Contains(r.Id)?"used":"unused")" data-state=',1);p.write_text(s,encoding='utf-8')
# Keep server-side filtering for the three existing searchable lists.
headers={
'Products':'''@model List<Product>
@{ViewData["Title"]="Quản lý sản phẩm";}
<form method="get" class="filter-bar admin-list-toolbar" data-admin-server-filter><label class="admin-list-search">Tìm sản phẩm<input name="q" value="@ViewBag.Query" placeholder="Tên hoặc màu sản phẩm"/></label><label>Danh mục<select name="category"><option value="">Tất cả</option>@foreach(var c in (List<string>)ViewBag.Categories){<option value="@c" selected="@(ViewBag.Category==c)">@c</option>}</select></label><label>Thương hiệu<select name="brandId"><option value="">Tất cả</option><option value="0" selected="@(ViewBag.BrandId==0)">Không thương hiệu</option>@foreach(var b in (List<ReferenceInput>)ViewBag.Brands){<option value="@b.Id" selected="@(ViewBag.BrandId==b.Id)">@b.Name</option>}</select></label><label>Hiển thị<select name="state"><option value="">Tất cả</option><option value="active" selected="@(ViewBag.State=="active")">Đang bán</option><option value="inactive" selected="@(ViewBag.State=="inactive")">Đã ẩn</option></select></label><button class="button-secondary">Lọc</button><a href="/Admin/Products" class="button-secondary">Xóa bộ lọc</a><span class="muted small">@Model.Count / @ViewBag.Total bản ghi</span><a href="/Admin/Product" class="btn-black">Thêm sản phẩm</a></form>
''',
'Customers':'''@model List<Customer>
@{ViewData["Title"]="Khách hàng";}
<form method="get" class="filter-bar admin-list-toolbar" data-admin-server-filter><label class="admin-list-search">Tìm khách hàng<input name="q" value="@ViewBag.Query" placeholder="Họ tên, email hoặc điện thoại"/></label><label>Trạng thái<select name="state"><option value="">Tất cả</option><option value="active" selected="@(ViewBag.State=="active")">Đang hoạt động</option><option value="inactive" selected="@(ViewBag.State=="inactive")">Đã khóa</option></select></label><label>Đơn hàng<select name="hasOrders"><option value="">Tất cả</option><option value="yes" selected="@(ViewBag.HasOrders=="yes")">Đã có đơn</option><option value="no" selected="@(ViewBag.HasOrders=="no")">Chưa có đơn</option></select></label><button class="button-secondary">Lọc</button><a href="/Admin/Customers" class="button-secondary">Xóa bộ lọc</a><span class="muted small">@Model.Count / @ViewBag.Total bản ghi</span></form>
''',
'Orders':'''@model List<Order>
@{ViewData["Title"]="Quản lý đơn hàng";}
<form method="get" class="filter-bar admin-list-toolbar" data-admin-server-filter><label class="admin-list-search">Tìm đơn<input name="q" value="@ViewBag.Query" placeholder="Mã đơn, người nhận hoặc điện thoại"/></label><label>Trạng thái<select name="status"><option value="">Tất cả</option>@foreach(var status in new[]{"Chờ xác nhận","Đã xác nhận","Đang chuẩn bị","Đang giao","Đã giao","Hoàn thành","Đã hủy","Đã hoàn hàng"}){<option value="@status" selected="@(ViewBag.Status==status)">@status</option>}</select></label><label>Thanh toán<select name="payment"><option value="">Tất cả</option>@foreach(var method in new[]{"COD","Chuyển khoản","Ví điện tử"}){<option value="@method" selected="@(ViewBag.Payment==method)">@method</option>}</select></label><label>Thời gian<select name="period"><option value="">Toàn bộ</option><option value="today" selected="@(ViewBag.Period=="today")">Hôm nay</option><option value="last7" selected="@(ViewBag.Period=="last7")">7 ngày gần nhất</option><option value="last30" selected="@(ViewBag.Period=="last30")">30 ngày gần nhất</option></select></label><button class="button-secondary">Lọc</button><a href="/Admin/Orders" class="button-secondary">Xóa bộ lọc</a><span class="muted small">@Model.Count / @ViewBag.Total bản ghi</span><a href="/Admin/Export" class="button-secondary">Xuất CSV</a></form>
'''
}
for name,head in headers.items():
 p=base/'Admin'/(name+'.cshtml');s=p.read_text(encoding='utf-8');table=s[s.index('<div class="panel table-scroll">'):];p.write_text(head+table,encoding='utf-8')
print('Updated filters on',len(config)+4,'list pages')
