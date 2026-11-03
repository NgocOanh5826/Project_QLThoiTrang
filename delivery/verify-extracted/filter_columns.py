from pathlib import Path
root=Path('Project_QLThoiTrang/Views/Admin')
p=root/'Customers.cshtml'
s=p.read_text(encoding='utf-8').replace('<th>Điện thoại</th><th></th>','<th>Điện thoại</th><th>Trạng thái</th><th></th>').replace('<td>@c.Phone</td><td class="table-actions-cell">','<td>@c.Phone</td><td><span class="status">@(c.Active?"Đang hoạt động":"Đã khóa")</span></td><td class="table-actions-cell">')
p.write_text(s,encoding='utf-8')
p=root/'Orders.cshtml'
s=p.read_text(encoding='utf-8').replace('<th>Trạng thái</th><th>Tổng tiền</th>','<th>Trạng thái</th><th>Thanh toán</th><th>Tổng tiền</th>').replace('<td>@o.Total.ToString("N0") đ</td>','<td>@o.Payment</td><td>@o.Total.ToString("N0") đ</td>')
p.write_text(s,encoding='utf-8')
