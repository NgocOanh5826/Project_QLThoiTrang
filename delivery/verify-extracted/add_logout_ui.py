from pathlib import Path
root=Path('Project_QLThoiTrang/Views')
confirm='data-confirm="Bạn có chắc muốn đăng xuất không?"'
for name in ['Account/Index.cshtml','Shared/_AdminLayout.cshtml']:
    p=root/name
    s=p.read_text(encoding='utf-8').replace('asp-action="Logout" method="post"','asp-action="Logout" method="post" '+confirm)
    p.write_text(s,encoding='utf-8')
p=root/'Shared/_Layout.cshtml'
s=p.read_text(encoding='utf-8')
needle='<a href="/Account" class="header-icon" aria-label="Tài khoản"><i class="bi bi-person" aria-hidden="true"></i></a>'
logout='@if(User.Identity?.IsAuthenticated==true){<form asp-controller="Account" asp-action="Logout" method="post" class="header-logout" '+confirm+'><button type="submit" class="button-secondary compact"><i class="bi bi-box-arrow-right" aria-hidden="true"></i><span>Đăng xuất</span></button></form>}'
assert needle in s
s=s.replace(needle,needle+logout)
p.write_text(s,encoding='utf-8')
p=root/'Account/Index.cshtml'
s=p.read_text(encoding='utf-8')
needle='    </nav>'
actions='''        <a href="/Account/Password" class="quick-link">
            <span class="quick-link-icon" aria-hidden="true"><i class="bi bi-key"></i></span>
            <span class="quick-link-copy"><strong>Đổi mật khẩu</strong><small>Cập nhật mật khẩu tài khoản</small></span>
            <i class="bi bi-chevron-right quick-arrow" aria-hidden="true"></i>
        </a>
'''
assert needle in s
s=s.replace(needle,actions+needle+'''
    <form asp-controller="Account" asp-action="Logout" method="post" class="account-logout" '''+confirm+'''>
        <button type="submit" class="button-secondary"><i class="bi bi-box-arrow-right" aria-hidden="true"></i> Đăng xuất</button>
    </form>''')
s=s.replace('<a class="button-secondary" href="/Account/Password">Đổi mật khẩu</a>','')
p.write_text(s,encoding='utf-8')
