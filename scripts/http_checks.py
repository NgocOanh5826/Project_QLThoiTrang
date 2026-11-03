"""MVC smoke/integration checks against the isolated BusinessChecks test database.
Run after BusinessChecks and launch the app on port 5188 using that database.
Python standard library only. Writes test orders/receipts; never run against production.
"""
import json, re, sys, urllib.request, urllib.parse, urllib.error, http.cookiejar, uuid, traceback
from html import unescape
from pathlib import Path
from datetime import datetime, timezone
BASE = 'http://127.0.0.1:5188'
sys.stdout.reconfigure(encoding='utf-8')
results = []
def browser():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def request(client, path, data=None):
    req = urllib.request.Request(BASE+path, None if data is None else urllib.parse.urlencode(data).encode())
    try:
        with client.open(req, timeout=40) as r:
            return r.status, r.geturl(), r.read().decode('utf-8')
    except urllib.error.HTTPError as e:
        return e.code, e.geturl(), e.read().decode('utf-8')
def field(html, name):
    for tag in re.findall(r'<input\b[^>]*>', html):
        attrs = dict(re.findall(r'([\w-]+)="([^"]*)"', tag))
        if attrs.get('name') == name:
            return unescape(attrs.get('value', ''))
    raise AssertionError('Field missing: '+name)
def post(client, path, page, data):
    return request(client, path, dict(data, __RequestVerificationToken=field(page, '__RequestVerificationToken')))
def check(name, fn):
    try:
        fn(); results.append(dict(id=name, status='PASS', error='')); print('PASS',name)
    except Exception as e:
        results.append(dict(id=name, status='FAIL', error=traceback.format_exc())); print('FAIL',name,traceback.format_exc())
def ensure(x):
    if not x: raise AssertionError('Unexpected HTTP response/behavior')
def login(c, email, password):
    _,_,page=request(c,'/Account/Login')
    status,url,_=post(c,'/Account/Login',page,dict(Email=email,Password=password))
    ensure(status==200 and '/Account/Login' not in url)
guest, admin, customer = browser(), browser(), browser()
check('HTTP01-home', lambda: ensure(request(guest,'/')[0]==200))
check('HTTP02-guest-admin-redirect-login', lambda: ensure('/Account/Login' in request(guest,'/Admin')[1]))
check('HTTP03-antiforgery-required', lambda: ensure(request(guest,'/Shop/Add',dict(id=1,quantity=1,size='M'))[0]==400))
check('HTTP04-admin-login', lambda: login(admin,'admin@qoanh.local','Admin@12345'))
for path in ['/Admin','/Admin/Products','/Admin/Product','/Admin/Product/1','/Admin/Categories','/Admin/Orders','/Admin/Order/QH-DEMO01','/Admin/Customers','/Admin/Customer/2','/Admin/EditCustomer/2','/Admin/Inventory','/Admin/Receive','/Admin/Returns','/Admin/Promotions','/Admin/Promotion','/Admin/Promotion/1','/Admin/Reports','/Admin/Settings','/Admin/Export','/Admin/References?kind=brands','/Admin/References?kind=sizes','/Admin/References?kind=colors','/Admin/References?kind=warehouses','/Admin/References?kind=suppliers','/Admin/Variants','/Admin/VariantStock','/Admin/Staff','/Admin/Staff/1','/Admin/Receipts','/Admin/Reviews','/Admin/LoginHistory']:
    check('PAGE'+path,lambda p=path: ensure(request(admin,p)[0]==200))
check('HTTP05-customer-login',lambda: login(customer,'khach@qoanh.local','Khach@12345'))
check('HTTP06-customer-admin-denied',lambda: ensure('/Account/Denied' in request(customer,'/Admin')[1]))
for path in ['/Shop','/Shop/Detail/1','/Shop/Cart','/Shop/Wishlist','/Account','/Account/Password','/Account/Orders','/Account/Order/QH-DEMO01','/Home/Info/privacy']:
    check('PAGE'+path,lambda p=path: ensure(request(customer,p)[0]==200))
check('HTTP07-other-customer-order-hidden',lambda: ensure(request(customer,'/Account/Order/QH-47DEA8A753')[0]==404))
def checkout():
    _,_,page=request(customer,'/Shop/Detail/1')
    status,_,_=post(customer,'/Shop/Add',page,dict(id=1,size='M',quantity=1));ensure(status==200)
    status,_,page=request(customer,'/Shop/Checkout');ensure(status==200)
    values=dict(Name='HTTP test',Phone='0901234567',Address='Test address',CheckoutToken=field(page,'CheckoutToken'))
    status,url,body=post(customer,'/Shop/Checkout',page,values)
    ensure(status==200 and '/Shop/Success/' in url)
    order_id=url.rsplit('/',1)[1]
    # The HTTP repeat must never create a second order (a clear cart can show validation).
    status,_,_=post(customer,'/Shop/Checkout',page,values);ensure(status==200)
    _,_,detail=request(admin,'/Admin/Order/'+order_id)
    status,_,_=post(admin,'/Admin/OrderStatus',detail,dict(id=order_id,status='Đã xác nhận'));ensure(status==200)
    _,_,detail=request(admin,'/Admin/Order/'+order_id)
    amount=field(detail,'amount')
    status,_,body=post(admin,'/Admin/Payment',detail,dict(id=order_id,amount=amount,reference='HTTP-'+uuid.uuid4().hex));ensure(status==200 and 'Đã thanh toán' in unescape(body))
    for state in ['Đang giao','Đã giao']:
        _,_,detail=request(admin,'/Admin/Order/'+order_id)
        status,_,body=post(admin,'/Admin/OrderStatus',detail,dict(id=order_id,status=state));ensure(status==200)
    _,_,detail=request(customer,'/Account/Order/'+order_id)
    status,_,body=post(customer,'/Account/Return',detail,dict(id=order_id,reason='HTTP integration return'));ensure(status==200 and 'Pending' in body)
    _,_,page=request(admin,'/Admin/Returns')
    # Pick this order's section, independent of pre-existing requests.
    section=next(s for s in re.findall(r'<tr\b[^>]*>.*?</tr>',page,re.S) if order_id in s)
    return_id=re.search(r'/Admin/ReturnRequest/(\d+)',section)[1]
    _,_,page=request(admin,'/Admin/ReturnRequest/'+return_id)
    status,_,body=post(admin,'/Admin/ResolveReturn',page,dict(id=return_id,approve='true',resolution='Received goods and refunded HTTP',reference='HTTP-REF-'+uuid.uuid4().hex));ensure(status==200)
    _,_,body=request(customer,'/Account/Order/'+order_id)
    ensure('Đã hoàn hàng' in unescape(body) and 'Đã hoàn tiền' in unescape(body))
check('HTTP08-checkout-COD-deliver-full-return',checkout)
def receiving():
    _,_,page=request(admin,'/Admin/Receive')
    values=dict(ProductId=1,Quantity=3,UnitCost=100000,Supplier='HTTP supplier',Reference='HTTP-IN-'+uuid.uuid4().hex)
    status,url,body=post(admin,'/Admin/Receive',page,values)
    ensure(status==200 and '/Admin/Inventory' in url)
    _,_,page=request(admin,'/Admin/Receive')
    status,url,body=post(admin,'/Admin/Receive',page,values)
    ensure(status==200 and 'Mã phiếu nhập đã sử dụng.' in unescape(body))
check('HTTP09-receive-and-reject-duplicate',receiving)
inventory, catalog, sales, support = browser(),browser(),browser(),browser()
check('HTTP10-inventory-login',lambda:login(inventory,'kho@shop.com','123456'))
check('HTTP11-inventory-read-stock',lambda:ensure(request(inventory,'/Admin/VariantStock')[0]==200))
check('HTTP12-inventory-staff-denied',lambda:ensure('/Account/Denied' in request(inventory,'/Admin/Staff')[1]))
def forged_staff():
    _,_,page=request(inventory,'/Admin/Inventory')
    status,url,_=post(inventory,'/Admin/Staff',page,dict(Name='Blocked elevation',Email='blocked-http@example.test',Role='Admin',Password='Blocked@12345',Active='true'))
    ensure(status==200 and '/Account/Denied' in url)
check('HTTP13-inventory-cannot-create-admin',forged_staff)
check('HTTP14-catalog-login',lambda:login(catalog,'sanpham@shop.com','123456'))
check('HTTP15-catalog-can-products',lambda:ensure(request(catalog,'/Admin/Products')[0]==200))
check('HTTP16-catalog-receive-denied',lambda:ensure('/Account/Denied' in request(catalog,'/Admin/Receive')[1]))
check('HTTP17-sales-login',lambda:login(sales,'donhang@shop.com','123456'))
check('HTTP18-sales-can-orders',lambda:ensure(request(sales,'/Admin/Orders')[0]==200))
check('HTTP19-sales-settings-denied',lambda:ensure('/Account/Denied' in request(sales,'/Admin/Settings')[1]))
def staff_revocation():
    email='revoke-'+uuid.uuid4().hex[:8]+'@example.test'
    _,_,page=request(admin,'/Admin/StaffMember')
    status,url,_=post(admin,'/Admin/Staff',page,dict(Name='HTTP revoke test',Email=email,Role='Support',Password='Verify@12345',Active='true'))
    ensure(status==200 and '/Admin/Staff' in url)
    login(support,email,'Verify@12345')
    ensure(request(support,'/Admin/Reviews')[0]==200)
    _,_,page=request(admin,'/Admin/Staff')
    edit=next(section for section in re.findall(r'<tr\b[^>]*>.*?</tr>',page,re.S) if email in section)
    staff_id=re.search(r'/Admin/StaffMember/(\d+)',edit)[1]
    _,_,editpage=request(admin,'/Admin/Staff/'+staff_id)
    status,_,_=post(admin,'/Admin/Staff',editpage,dict(Id=staff_id,Name='HTTP revoke test',Email=email,Role='Support',Active='false'))
    ensure(status==200)
    ensure('/Account/Login' in request(support,'/Admin/Reviews')[1])
check('HTTP20-lock-account-revokes-existing-cookie',staff_revocation)
def persistent_guest():
    c=browser()
    _,_,page=request(c,'/Shop/Detail/1')
    status,_,_=post(c,'/Shop/Add',page,dict(id=1,size='M',quantity=1));ensure(status==200)
    login(c,'khach@qoanh.local','Khach@12345')
    status,_,page=request(c,'/Shop/Cart');ensure(status==200 and 'name="variantId"' in page)
    _,_,page=request(c,'/Account')
    status,_,_=post(c,'/Account/Logout',page,{});ensure(status==200)
    login(c,'khach@qoanh.local','Khach@12345')
    status,_,page=request(c,'/Shop/Cart');ensure(status==200 and 'name="variantId"' in page)
check('HTTP21-guest-cart-merged-and-survives-logout',persistent_guest)
def rate_limit():
    c=browser();saw=False
    for _ in range(23):
        status,_,page=request(c,'/Account/Login')
        status,_,_=post(c,'/Account/Login',page,dict(Email='unknown@example.test',Password='wrong'))
        if status==429:saw=True;break
    ensure(saw)
def profile_optional_fields():
    _,_,page=request(customer,'/Account')
    name=field(page,'Name')
    status,url,body=post(customer,'/Account',page,dict(Name=name,Phone='',Address=''))
    ensure(status==200 and '/Account/Login' not in url)
    _,_,page=request(customer,'/Account')
    ensure(field(page,'Phone')=='' and field(page,'Address')=='')
check('HTTP23-profile-empty-optional-fields',profile_optional_fields)

def profile_duplicate_phone():
    _,_,details=request(admin,'/Admin/Customer/1')
    phones=re.findall(r'0\d{9}',details)
    ensure(bool(phones))
    _,_,page=request(customer,'/Account')
    name=field(page,'Name')
    status,_,body=post(customer,'/Account',page,dict(Name=name,Phone=phones[0],Address=''))
    ensure(status==200 and 'Duplicate phone number.' in unescape(body))
    _,_,saved=request(customer,'/Account')
    ensure(field(saved,'Phone')=='')
check('HTTP24-profile-duplicate-phone-validation-rollback',profile_duplicate_phone)
check('HTTP22-login-rate-limit-429',rate_limit)
Path('delivery/http-check-results.json').write_text(json.dumps(dict(executedUtc=datetime.now(timezone.utc).isoformat(),baseUrl=BASE,total=len(results),failed=sum(r['status']=='FAIL' for r in results),results=results),ensure_ascii=False,indent=2),encoding='utf-8')
failed=sum(r['status']=='FAIL' for r in results)
print(f'{len(results)-failed}/{len(results)} passed')
sys.exit(bool(failed))
