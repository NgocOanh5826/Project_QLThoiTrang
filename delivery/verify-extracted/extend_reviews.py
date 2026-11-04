from pathlib import Path
p=Path('Project_QLThoiTrang/Models/ManagementModels.cs')
s=p.read_text(encoding='utf-8').replace('public class ReviewItem {','public class ReviewItem { public List<ReviewMediaInfo> Media { get; set; }=new();')
p.write_text(s,encoding='utf-8')
p=Path('Project_QLThoiTrang/Services/NativeManagement.cs')
s=p.read_text(encoding='utf-8')
start=s.index(' public List<ReviewItem> Reviews(')
end=s.index(' public void Review(',start)
s=s[:start]+''' public List<ReviewItem> Reviews(int? product=null,bool includeHidden=false)=>NativeRead(db=>
 {
  var reviews=(from r in db.DanhGia join c in db.KhachHang on r.MaKH equals c.MaKH where (!product.HasValue||r.MaSP==product)&&(includeHidden||r.TrangThai) select new ReviewItem{Id=r.MaDanhGia,ProductId=r.MaSP,Customer=c.HoTen,Stars=r.SoSao,Text=r.NoiDung??"",Active=r.TrangThai,Created=r.NgayDanhGia}).OrderByDescending(r=>r.Created).ToList();
  var ids=reviews.Select(r=>r.Id).ToList();
  var media=db.ReviewMedia.Where(m=>ids.Contains(m.ReviewId)).Select(m=>new{m.Id,m.ReviewId,m.ContentType}).ToList();
  foreach(var r in reviews)r.Media=media.Where(m=>m.ReviewId==r.Id).Select(m=>new ReviewMediaInfo(m.Id,m.ContentType.StartsWith("video/"))).ToList();
  return reviews;
 });
'''+s[end:]
s=s.replace('int stars,string text)=>NativeWrite(db=>','int stars,string text,IReadOnlyList<ReviewUpload>? uploads=null)=>NativeWrite(db=>')
needle='db.DanhGia.Add(new(){MaKH=customerId,MaSP=productId,SoSao=stars,NoiDung=text,NgayDanhGia=DateTime.Now,TrangThai=true});return 0;'
assert needle in s
s=s.replace(needle,'''var attachments=ReviewUploads.Validate(uploads);var review=new DanhGiaRow{MaKH=customerId,MaSP=productId,SoSao=stars,NoiDung=text,NgayDanhGia=DateTime.Now,TrangThai=true};db.DanhGia.Add(review);db.SaveChanges();foreach(var media in attachments){media.ReviewId=review.MaDanhGia;db.ReviewMedia.Add(media);}return 0;''')
p.write_text(s,encoding='utf-8')
p=Path('Project_QLThoiTrang/Controllers/ShopController.cs')
s=p.read_text(encoding='utf-8')
start=s.index(' [Authorize(Roles="Customer"),HttpPost] public IActionResult Review(')
end=s.index(' [Authorize(Roles="Customer")] public IActionResult Checkout()',start)
s=s[:start]+''' [Authorize(Roles="Customer"),HttpPost,RequestSizeLimit(33554432),RequestFormLimits(MultipartBodyLengthLimit=33554432)]
 public async Task<IActionResult> Review(int id,int stars,string text,List<IFormFile>? media)
 {
  try
  {
   media??=[];
   if(media.Count>5||media.Sum(f=>f.Length)>ReviewUploads.TotalLimit||media.Any(f=>f.Length<=0||f.Length>ReviewUploads.VideoLimit))throw new InvalidOperationException("Tối đa 5 tệp, mỗi ảnh 5 MB, video 20 MB và tổng 30 MB.");
   var uploads=new List<ReviewUpload>();
   foreach(var file in media){using var buffer=new MemoryStream();await file.CopyToAsync(buffer,HttpContext.RequestAborted);uploads.Add(new(file.FileName,buffer.ToArray()));}
   store.Review(CustomerId,id,stars,text,uploads);TempData["Success"]="Đã lưu đánh giá.";
  }
  catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
  return RedirectToAction(nameof(Detail),new{id});
 }
 public IActionResult ReviewMedia(Guid id)
 {
  bool staff=User.IsInRole("Admin")||User.IsInRole("Support");
  var media=store.NativeRead(db=>(from m in db.ReviewMedia join r in db.DanhGia on m.ReviewId equals r.MaDanhGia where m.Id==id&&(r.TrangThai||staff) select m).FirstOrDefault());
  if(media==null)return NotFound();
  Response.Headers["X-Content-Type-Options"]="nosniff";Response.Headers.CacheControl="no-store";
  return File(media.Content,media.ContentType,enableRangeProcessing:true);
 }
'''+s[end:]
p.write_text(s,encoding='utf-8')
