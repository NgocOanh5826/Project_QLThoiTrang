from pathlib import Path
import re
root=Path('Project_QLThoiTrang/Views')
changed=[]
for folder in ['Admin','Finance','Valuation','Chat']:
 for p in (root/folder).glob('*.cshtml'):
  original=p.read_text(encoding='utf-8');s=original
  if p.name=='Staff.cshtml':s=s.replace('>Sửa / khóa / đặt lại mật khẩu</a>',' title="Sửa, khóa hoặc đặt lại mật khẩu nhân viên">Sửa</a>')
  if p.name=='Reviews.cshtml':s=s.replace('<button class="button-secondary">','<button class="table-action @(r.Active?"table-action--danger":"")">')
  def cell(match):
   content=match.group(1)
   if not any(token in content for token in ['button-secondary','button-danger','table-action','>Sửa</a>']):return match.group(0)
   def control(m):
    tag=m.group(0)
    if 'class=' in tag:
     # Existing action classes are static here; conditional review styling is retained.
     if 'table-action' not in tag:
      extra=' table-action'+(' table-action--danger' if 'button-danger' in tag else '')
      tag=re.sub(r'class="([^"]*)"',lambda c:'class="'+c.group(1)+extra+'"',tag,count=1)
    else:tag=tag[:-1]+' class="table-action">'
    return tag
   content=re.sub(r'<(?:a|button)\b[^>]*>',control,content)
   content=content.replace('>Chỉnh sửa</a>','>Sửa</a>').replace('>Xem hồ sơ</a>','>Chi tiết</a>')
   if 'class="actions"' in content:content=content.replace('class="actions"','class="table-actions"')
   else:content='<div class="table-actions">'+content+'</div>'
   return '<td class="table-actions-cell">'+content+'</td>'
  s=re.sub(r'<td>(.*?)</td>',cell,s,flags=re.S)
  if s!=original:p.write_text(s,encoding='utf-8');changed.append(str(p))
print('\n'.join(changed));print('Updated',len(changed),'table views')
