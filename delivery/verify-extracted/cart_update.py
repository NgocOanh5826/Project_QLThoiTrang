from pathlib import Path
p=Path('Project_QLThoiTrang/Views/Shop/Checkout.cshtml')
s=p.read_text(encoding='utf-8');start=s.index('<aside class="panel order-summary">');end=s.index('</aside>',start)+len('</aside>')
s=s[:start]+'<partial name="_CheckoutSummary" model="cart"/>'+s[end:]
p.write_text(s,encoding='utf-8')
p=Path('Project_QLThoiTrang/Controllers/ShopController.cs')
s=p.read_text(encoding='utf-8');start=s.index(' [HttpPost] public IActionResult UpdateCart(');end=s.index(' [Authorize(Roles="Customer"),HttpPost,RequestSizeLimit',start)
s=s[:start]+''' [HttpPost] public IActionResult UpdateCart(int id,string? size,int quantity,bool remove=false,int variantId=0,int newVariantId=0)
 {
  var items=SessionStore.Get<List<CartItem>>(HttpContext.Session,"cart");
  var item=items.FirstOrDefault(i=>i.ProductId==id&&(variantId>0?i.VariantId==variantId:i.Size==size));
  if(item==null)return RedirectToAction(nameof(Cart));
  if(!ModelState.IsValid){TempData["Error"]="Thông tin giỏ hàng không hợp lệ.";return RedirectToAction(nameof(Cart));}
  if(remove||quantity==0)items.Remove(item);
  else
  {
   var targetId=newVariantId>0?newVariantId:item.VariantId;
   var variant=store.Read(d=>d.Products.Any(p=>p.Id==id&&p.Active)?d.Variants.FirstOrDefault(v=>v.Id==targetId&&v.ProductId==id&&v.Active):null);
   var existing=items.FirstOrDefault(i=>i!=item&&i.VariantId==targetId);
   var total=(long)quantity+(existing?.Quantity??0);
   if(variant==null||quantity<1||total>Math.Min(20,variant.Stock)){TempData["Error"]="Màu, kích cỡ hoặc số lượng không hợp lệ; tổng trong giỏ không được vượt tồn kho và 20 sản phẩm mỗi phiên bản.";return RedirectToAction(nameof(Cart));}
   if(existing!=null){existing.Quantity=(int)total;items.Remove(item);}
   else{item.VariantId=variant.Id;item.Size=variant.Size;item.Color=variant.Color;item.Quantity=quantity;}
  }
  SessionStore.Set(HttpContext.Session,"cart",items);return RedirectToAction(nameof(Cart));
 }
'''+s[end:];p.write_text(s,encoding='utf-8')
