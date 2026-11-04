from pathlib import Path
p=Path('Project_QLThoiTrang/Views/Shared/_Layout.cshtml')
s=p.read_text(encoding='utf-8').replace('<body>','<body class="storefront-body">')
s=s.replace('<a href="/Chat" class="text-link">Chat hỗ trợ</a>','<a href="/Chat" class="text-link header-support" aria-label="Chat hỗ trợ" title="Chat hỗ trợ"><i class="bi bi-headset" aria-hidden="true"></i><span>Chat hỗ trợ</span></a>')
s=s.replace('<a href="/Account" class="header-icon"','<div class="header-actions"><a href="/Account" class="header-icon"',1)
needle='</button></form>}</div>\n</header>'
assert needle in s
s=s.replace(needle,'</button></form>}</div></div>\n</header>',1)
p.write_text(s,encoding='utf-8')
