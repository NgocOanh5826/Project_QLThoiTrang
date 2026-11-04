from pathlib import Path
p=Path('Project_QLThoiTrang/Views/Shared/_Layout.cshtml')
s=p.read_text(encoding='utf-8')
start=s.index('@if(User.Identity?.IsAuthenticated==true){<form')
end=s.index('</form>}',start)+len('</form>}')
logout=s[start:end].replace('class="button-secondary compact"','class="header-icon" aria-label="Đăng xuất" title="Đăng xuất"').replace('<span>Đăng xuất</span>','')
s=s[:start]+s[end:]
heart='<a href="/Shop/Wishlist" class="header-icon" aria-label="Yêu thích"><i class="bi bi-heart" aria-hidden="true"></i></a>'
assert heart in s
s=s.replace(heart,heart+logout)
p.write_text(s,encoding='utf-8')
