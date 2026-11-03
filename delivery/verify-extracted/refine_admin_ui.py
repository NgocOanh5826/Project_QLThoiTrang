from pathlib import Path

root=Path(__file__).resolve().parents[2]
base=root/'Project_QLThoiTrang/Views/Admin'
def put(name, text): (base/(name+'.cshtml')).write_text(text,encoding='utf-8')

for name, title, back in [('Variant','biến thể','Variants'),('StaffMember','nhân viên','Staff')]:
    p=base/(name+'.cshtml');s=p.read_text(encoding='utf-8');form=s[s.index('<div class="admin-form-page">'):]
    form=form.replace('H?y / quay l?i','Hủy / quay lại')
    head='@model '+('VariantInput' if name=='Variant' else 'StaffInput')+'\n'
    head+='@{ViewData["Title"]=Model.Id==0?"Thêm '+title+'":"Chỉnh sửa '+title+'";'
    if name=='Variant':head+='var products=(List<Product>)ViewBag.Products;'
    head+='}\n'
    p.write_text(head+form,encoding='utf-8')

for name,title,url,label in [('Variants','Biến thể sản phẩm','Variant','Thêm biến thể'),('Staff','Nhân viên và phân quyền','StaffMember','Thêm nhân viên')]:
    p=base/(name+'.cshtml');s=p.read_text(encoding='utf-8');s=s[s.index('<partial'):]
    head='@{ViewData["Title"]="'+title+'";ViewData["CreateUrl"]="/Admin/'+url+'";ViewData["CreateLabel"]="'+label+'";'
    if name=='Variants':head+='var products=(List<Product>)ViewBag.Products;'
    p.write_text(head+'}\n'+s,encoding='utf-8')

put('Reference','''@model ReferenceInput
@{var titles=new Dictionary<string,string>{{"brands","thương hiệu"},{"sizes","kích thước"},{"colors","màu sắc"},{"warehouses","kho"},{"suppliers","nhà cung cấp"}};ViewData["Title"]=(Model.Id==0?"Thêm ":"Chỉnh sửa ")+titles[Model.Kind];ViewData["ReferenceKind"]=Model.Kind;}
<div class="admin-form-page"><form asp-action="Reference" method="post" class="panel form-stack">
<div asp-validation-summary="All" class="validation"></div><input asp-for="Id" type="hidden"/><input asp-for="Kind" type="hidden"/>
<label>Tên<input asp-for="Name" required maxlength="@(Model.Kind=="sizes"?30:Model.Kind=="colors"?50:100)" autofocus/></label>
@if(Model.Kind=="brands"){<label>Mô tả<textarea asp-for="Description" maxlength="500"></textarea></label>}
@if(Model.Kind=="colors"){<label>Mã màu<input asp-for="Description" maxlength="20" placeholder="#26372e"/></label>}
@if(Model.Kind is "warehouses" or "suppliers"){<label>Địa chỉ<input asp-for="Address" maxlength="255"/></label>}
@if(Model.Kind=="suppliers"){<label>Điện thoại<input asp-for="Phone" maxlength="15"/></label><label>Email<input asp-for="Email" type="email" maxlength="150"/></label>}
@if(Model.Kind is not ("sizes" or "colors")){<label class="checkbox"><input asp-for="Active"/>Đang hoạt động</label>}
<div class="actions"><button class="btn-black">Lưu</button><a href="/Admin/References?kind=@Model.Kind" class="button-secondary">Hủy / quay lại</a></div>
</form></div>
''')
put('References','''@model ReferencePage
@{var titles=new Dictionary<string,string>{{"brands","Thương hiệu"},{"sizes","Kích thước"},{"colors","Màu sắc"},{"warehouses","Kho"},{"suppliers","Nhà cung cấp"}};ViewData["Title"]=titles[Model.Input.Kind];ViewData["CreateUrl"]="/Admin/Reference?kind="+Model.Input.Kind;ViewData["CreateLabel"]="Thêm "+titles[Model.Input.Kind].ToLower();}
<nav class="admin-tabs" aria-label="Nhóm danh mục">@foreach(var x in titles){if(!User.IsInRole("Admin")&&((x.Key is "warehouses" or "suppliers")?!User.IsInRole("Inventory"):!User.IsInRole("Catalog"))){continue;}<a class="button-secondary @(Model.Input.Kind==x.Key?"is-active":"")" aria-current="@(Model.Input.Kind==x.Key?"page":null)" href="/Admin/References?kind=@x.Key">@x.Value</a>}</nav>
<partial name="_AdminListToolbar"/>
<div class="panel table-scroll"><table class="data-table"><thead><tr><th>Mã</th><th>Tên</th><th>Thông tin</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>@foreach(var r in Model.Rows){<tr><td>@r.Id</td><td><strong>@r.Name</strong></td><td>@r.Description @r.Address<br/>@r.Phone @r.Email</td><td><span class="status @(r.Active?"":"inactive")">@(r.Active?"Hoạt động":"Ngừng hoạt động")</span></td><td><a class="button-secondary compact" href="/Admin/Reference?kind=@r.Kind&id=@r.Id">Chỉnh sửa</a></td></tr>}</tbody></table></div>
''')
put('Category','''@model string
@{ViewData["Title"]="Thêm danh mục sản phẩm";}
<div class="admin-form-page"><form asp-action="Category" method="post" class="panel form-stack"><div asp-validation-summary="All" class="validation"></div><label>Tên danh mục<input name="name" value="@Model" minlength="2" maxlength="50" required autofocus/></label><div class="actions"><button class="btn-black">Lưu danh mục</button><a href="/Admin/Categories" class="button-secondary">Hủy / quay lại</a></div></form></div>
''')
p=base/'Categories.cshtml';s=p.read_text(encoding='utf-8');a=s.index('@{');b=s.index('}',a);s=s[:a]+'@{ViewData["Title"]="Danh mục sản phẩm";ViewData["CreateUrl"]="/Admin/Category";ViewData["CreateLabel"]="Thêm danh mục";}'+s[b+1:];p.write_text(s,encoding='utf-8')
put('Returns','''@model List<ReturnRecord>
@{ViewData["Title"]="Trả hàng và hoàn tiền";}
<partial name="_AdminListToolbar"/>
<div class="panel table-scroll"><table class="data-table"><thead><tr><th>Mã đơn</th><th>Ngày yêu cầu</th><th>Lý do</th><th>Tiền hoàn</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>@foreach(var r in Model){<tr><td>@r.OrderId</td><td>@r.Created.ToLocalTime().ToString("dd/MM/yyyy HH:mm")</td><td>@r.Reason</td><td>@r.Refund.ToString("N0") đ</td><td>@r.Status</td><td><a class="button-secondary compact" href="/Admin/ReturnRequest/@r.Id">@(r.Status=="Pending"?"Xử lý":"Xem chi tiết")</a></td></tr>}</tbody></table></div>
''')
p=base/'ReturnRequest.cshtml';s=p.read_text(encoding='utf-8');s=s.replace('Quay l?i danh s?ch','Quay lại danh sách');s=s.replace('ViewData["Title"]="Trả hàng và hoàn tiền"','ViewData["Title"]="Xử lý trả hàng"');p.write_text(s,encoding='utf-8')
put('StockCount','''@model StoreData
@{ViewData["Title"]="Kiểm kê tồn kho";int variantId=ViewBag.VariantId;int warehouseId=ViewBag.WarehouseId;var v=Model.Variants.Single(v=>v.Id==variantId);var w=Model.Warehouses.Single(w=>w.Id==warehouseId);var quantity=Model.Stocks.FirstOrDefault(s=>s.VariantId==variantId&&s.WarehouseId==warehouseId)?.Quantity??0;}
<div class="admin-form-page"><form asp-action="AdjustVariant" method="post" class="panel form-stack"><h2>@Model.Products.Single(p=>p.Id==v.ProductId).Name</h2><p>@w.Name · @v.Sku · @v.Size / @v.Color</p><input name="variantId" value="@v.Id" type="hidden"/><input name="warehouseId" value="@w.Id" type="hidden"/><input name="expected" value="@quantity" type="hidden"/><label>Tồn thực tế<input name="quantity" value="@quantity" type="number" min="0" max="100000" required/></label><label>Lý do kiểm kê<textarea name="reason" minlength="5" maxlength="300" required></textarea></label><div class="actions"><button class="btn-black">Ghi kiểm kê</button><a href="/Admin/VariantStock" class="button-secondary">Quay lại danh sách</a></div></form></div>
''')
for name in ['VariantStock','Inventory']:
    p=base/(name+'.cshtml');s=p.read_text(encoding='utf-8');s=s.replace('Ki?m k? theo bi?n th?','Kiểm kê theo biến thể').replace('Ki?m k?','Kiểm kê');p.write_text(s,encoding='utf-8')

for name in ['AdminController.cs','AdminExtensions.cs']:
    p=root/'Project_QLThoiTrang/Controllers'/name;s=p.read_text(encoding='utf-8')
    for a,b in [('?? l?u danh m?c.','Đã lưu danh mục.'),('?? l?u bi?n th?.','Đã lưu biến thể.'),('?? l?u nh?n vi?n v? quy?n.','Đã lưu nhân viên và quyền.'),('T?n danh m?c c?n 2–50 k? t?.','Tên danh mục cần 2–50 ký tự.'),('T?n danh m?c c?n 2?50 k? t?.','Tên danh mục cần 2–50 ký tự.'),('?? th?m danh m?c.','Đã thêm danh mục.'),('Danh m?c ?? t?n t?i.','Danh mục đã tồn tại.')]:s=s.replace(a,b)
    p.write_text(s,encoding='utf-8')

# Sidebar active state is rendered by the server, including pages opened directly.
p=root/'Project_QLThoiTrang/Views/Shared/_AdminLayout.cshtml';s=p.read_text(encoding='utf-8')
old='@{var name=Store.Read(d=>d.Settings.Name);var current=ViewContext.RouteData.Values["action"]?.ToString();}'
new='''@{
 var name=Store.Read(d=>d.Settings.Name);var current=ViewContext.RouteData.Values["action"]?.ToString();
 var kind=ViewData["ReferenceKind"]?.ToString()??Context.Request.Query["kind"].ToString();
 var selected=current switch{
 "Index"=>"", "Product" or "DeleteProduct"=>"Products", "Category" or "AddCategory"=>"Categories",
 "Variant"=>"Variants", "StaffMember"=>"Staff", "StockCount" or "AdjustVariant"=>"VariantStock",
 "Reference" or "References"=>"References?kind="+(kind is "suppliers" or "warehouses"?kind:"brands"),
 "Order"=>"Orders", "Customer" or "EditCustomer"=>"Customers", "Promotion"=>"Promotions",
 "ReturnRequest" or "ResolveReturn"=>"Returns", _=>current};
 bool IsCurrent(string target)=>selected==target;
}
'''
s=s.replace(old,new)
s=s.replace('<a href="/Admin">Tổng quan</a>','<a class="@(IsCurrent("")?"active":"")" aria-current="@(IsCurrent("")?"page":null)" href="/Admin">Tổng quan</a>')
s=s.replace('<a href="/Admin/@item.Item1">','<a class="@(IsCurrent(item.Item1)?"active":"")" aria-current="@(IsCurrent(item.Item1)?"page":null)" href="/Admin/@item.Item1">')
for action in ['Customers','Reviews','Staff','LoginHistory','Settings']:
    s=s.replace('<a href="/Admin/'+action+'">','<a class="@(IsCurrent("'+action+'")?"active":"")" aria-current="@(IsCurrent("'+action+'")?"page":null)" href="/Admin/'+action+'">')
p.write_text(s,encoding='utf-8')
