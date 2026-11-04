from pathlib import Path
p=Path('Project_QLThoiTrang/Views/Shop/Detail.cshtml')
s=p.read_text(encoding='utf-8')
s='\n'.join(line for line in s.split('\n') if 'var colorOptions=' not in line and 'string? ColorHex(' not in line)
s=s.replace(' data-color-hex="@ColorHex(v.Color)"','').replace('size-options product-colors','size-options product-color-options')
p.write_text(s,encoding='utf-8')
p=Path('Project_QLThoiTrang/Controllers/ShopController.cs')
s=p.read_text(encoding='utf-8').replace('ViewBag.ColorOptions=store.References("colors");','')
p.write_text(s,encoding='utf-8')
