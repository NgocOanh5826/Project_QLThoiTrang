"""HTTP checks for the split administration pages on the isolated local host."""
import http.cookiejar
import json
import re
import sys
import urllib.request
import urllib.parse
from html import unescape
from html.parser import HTMLParser
from datetime import datetime, timezone
from pathlib import Path
BASE='http://127.0.0.1:5188'
results=[]
class Page(HTMLParser):
    def __init__(self, html):
        super().__init__();self.main=False;self.sidebar=False;self.links=[];self.forms=[];self.main_html='';self.has_table=False;self.has_filter=False;self.feed(html)
    def handle_starttag(self, tag, attrs):
        a=dict(attrs)
        if tag=='aside':self.sidebar=True
        if tag=='main':self.main=True
        if self.sidebar and tag=='a':self.links.append(a)
        if self.main and tag=='form':self.forms.append(a)
        if self.main and tag=='table':self.has_table=True
        if self.main and 'data-admin-list-filter' in a:self.has_filter=True
    def handle_endtag(self, tag):
        if tag=='aside':self.sidebar=False
        if tag=='main':self.main=False
def browser():return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def request(c,path,data=None):
    with c.open(BASE+path,None if data is None else urllib.parse.urlencode(data).encode(),timeout=40) as r:return r.geturl(),r.read().decode('utf-8')
def token(page):
    tag=re.search(r'<input[^>]*name="__RequestVerificationToken"[^>]*>',page).group()
    return unescape(re.search(r'value="([^"]+)"',tag).group(1))
def login(c,email,password):
    _,page=request(c,'/Account/Login');url,_=request(c,'/Account/Login',dict(Email=email,Password=password,__RequestVerificationToken=token(page)));assert '/Account/Login' not in url
def check(name,action):
    try:action();results.append(dict(id=name,status='PASS',error=''));print('PASS',name)
    except Exception as e:results.append(dict(id=name,status='FAIL',error=str(e)));print('FAIL',name,str(e))
def ensure(value):assert value
admin=browser();login(admin,'admin@qoanh.local','Admin@12345')
def listing(path,selected):
    _,html=request(admin,path);page=Page(html)
    assert page.has_filter and page.has_table
    assert not any(f.get('method','').lower()=='post' and 'form-stack' in f.get('class','') for f in page.forms)
    active=[a for a in page.links if a.get('aria-current')=='page']
    assert len(active)==1 and unescape(active[0]['href'])==selected
def edit(path,action,selected):
    _,html=request(admin,path);page=Page(html)
    assert not page.has_table
    assert any(f.get('action') in [action, action+'/1'] and f.get('method','').lower()=='post' for f in page.forms)
    active=[a for a in page.links if a.get('aria-current')=='page']
    assert len(active)==1 and unescape(active[0]['href'])==selected
for kind in ['brands','sizes','colors','warehouses','suppliers']:
    selected='/Admin/References?kind='+('brands' if kind in ['brands','sizes','colors'] else kind)
    check('LIST-reference-'+kind,lambda k=kind,s=selected:listing('/Admin/References?kind='+k,s))
    check('CREATE-reference-'+kind,lambda k=kind,s=selected:edit('/Admin/Reference?kind='+k,'/Admin/Reference',s))
for name,new,selected in [('Variants','Variant','Variants'),('Staff','StaffMember','Staff'),('Categories','Category','Categories')]:
    check('LIST-'+name,lambda n=name,s=selected:listing('/Admin/'+n,'/Admin/'+s))
    check('CREATE-'+new,lambda n=new,s=selected:edit('/Admin/'+n,'/Admin/'+n,'/Admin/'+s))
for name in ['Inventory','VariantStock','Returns','Receipts','Reviews','LoginHistory','Promotions']:
    check('LIST-'+name,lambda n=name:listing('/Admin/'+n,'/Admin/'+('Inventory' if n=='VariantStock' else n)))
check('EDIT-stock-count',lambda:edit('/Admin/StockCount?variantId=1&warehouseId=1','/Admin/AdjustVariant','/Admin/Inventory'))
def invalid_reference():
    _,html=request(admin,'/Admin/Reference?kind=brands')
    url,body=request(admin,'/Admin/Reference',dict(Id=0,Kind='brands',Name='',__RequestVerificationToken=token(html)))
    page=Page(body);assert '/Admin/Reference' in url and not page.has_table and 'validation-summary-errors' in body
check('VALIDATION-reference-stays-on-form',invalid_reference)
def save_reference():
    name='Layout check '+datetime.now(timezone.utc).strftime('%Y%m%d%H%M%S%f')
    _,html=request(admin,'/Admin/Reference?kind=brands')
    url,body=request(admin,'/Admin/Reference',dict(Id=0,Kind='brands',Name=name,Active='true',__RequestVerificationToken=token(html)))
    assert '/Admin/References?kind=brands' in url and name in unescape(body)
check('SAVE-reference-returns-to-list',save_reference)
check('EDIT-reference',lambda:edit('/Admin/Reference?kind=brands&id=1','/Admin/Reference','/Admin/References?kind=brands'))
check('EDIT-variant',lambda:edit('/Admin/Variant/1','/Admin/Variant','/Admin/Variants'))
check('EDIT-staff',lambda:edit('/Admin/StaffMember/1','/Admin/StaffMember','/Admin/Staff'))
def detail(path, selected):
    _,html=request(admin,path);active=[a for a in Page(html).links if a.get('aria-current')=='page']
    assert len(active)==1 and active[0]['href']==selected
check('DETAIL-receipt',lambda:detail('/Admin/Receipt/1','/Admin/Receipts'))
check('DETAIL-return',lambda:detail('/Admin/ReturnRequest/1','/Admin/Returns'))
check('LEGACY-variant-link',lambda:ensure('/Admin/Variant/1' in request(admin,'/Admin/Variants/1')[0]))
def legacy_reference():
    url,_=request(admin,'/Admin/References?kind=brands&id=1');parsed=urllib.parse.urlparse(url);query=urllib.parse.parse_qs(parsed.query)
    assert query.get('kind')==['brands'] and (parsed.path=='/Admin/Reference/1' or parsed.path=='/Admin/Reference' and query.get('id')==['1'])
check('LEGACY-reference-link',legacy_reference)
inventory,catalog,sales=browser(),browser(),browser()
login(inventory,'kho@shop.com','123456');login(catalog,'sanpham@shop.com','123456');login(sales,'donhang@shop.com','123456')
check('AUTH-inventory-warehouse-form',lambda:ensure('/Account/Denied' not in request(inventory,'/Admin/Reference?kind=warehouses')[0]))
check('AUTH-inventory-brand-form-denied',lambda:ensure('/Account/Denied' in request(inventory,'/Admin/Reference?kind=brands')[0]))
check('AUTH-catalog-warehouse-form-denied',lambda:ensure('/Account/Denied' in request(catalog,'/Admin/Reference?kind=warehouses')[0]))
check('AUTH-catalog-category-form',lambda:ensure('/Account/Denied' not in request(catalog,'/Admin/Category')[0]))
check('AUTH-sales-staff-form-denied',lambda:ensure('/Account/Denied' in request(sales,'/Admin/StaffMember')[0]))
check('AUTH-sales-stock-form-denied',lambda:ensure('/Account/Denied' in request(sales,'/Admin/StockCount?variantId=1&warehouseId=1')[0]))
failed=sum(r['status']=='FAIL' for r in results)
Path('delivery/admin-layout-results.json').write_text(json.dumps(dict(executedUtc=datetime.now(timezone.utc).isoformat(),total=len(results),failed=failed,results=results),indent=2),encoding='utf-8')
print(f'{len(results)-failed}/{len(results)} passed');sys.exit(bool(failed))
