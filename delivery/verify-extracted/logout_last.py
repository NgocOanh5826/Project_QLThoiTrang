from pathlib import Path
p=Path('Project_QLThoiTrang/Views/Shared/_Layout.cshtml')
s=p.read_text(encoding='utf-8')
start=s.index('@if(User.Identity?.IsAuthenticated==true){<form')
end=s.index('</form>}',start)+len('</form>}')
logout=s[start:end]
s=s[:start]+s[end:]
cart='<span>@cart.Sum(i=>i.Quantity)</span></a>}'
assert cart in s
s=s.replace(cart,cart+logout)
p.write_text(s,encoding='utf-8')
