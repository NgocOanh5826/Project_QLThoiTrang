using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Project_QLThoiTrang.Models;
namespace Project_QLThoiTrang.Services;
public sealed partial class StoreService
{
 public BankTransferOptions BankOptions=>configuration?.GetSection("BankTransfer").Get<BankTransferOptions>()??new();
 static void ChatAccess(Data.StoreDbContext db,int actor,int customer)
 {Require(db.KhachHang.Any(c=>c.MaKH==customer&&c.TrangThai),"Khách hàng không hoạt động.");if(actor==customer&&actor>0)return;NativePermission(db,actor,"Support");}
 public List<ChatMessage> Messages(int customer,int actor,long before=0,long after=0)=>NativeRead(db=>{ChatAccess(db,actor,customer);var query=db.ChatMessages.Where(m=>m.CustomerId==customer&&(before==0||m.Id<before)&&m.Id>after);return (after>0?query.OrderBy(m=>m.Id):query.OrderByDescending(m=>m.Id)).Take(100).ToList().OrderBy(m=>m.Id).ToList();});
 public ChatMessage SendMessage(int customer,int actor,string text,Guid clientToken)=>NativeWrite(db=>
 {ChatAccess(db,actor,customer);text=Text(text,1,2000,"Tin nhắn");Require(clientToken!=Guid.Empty,"Mã tin nhắn không hợp lệ.");var old=db.ChatMessages.SingleOrDefault(m=>m.CustomerId==customer&&m.ClientToken==clientToken);if(old!=null){Require(old.SenderId==actor&&old.Text==text,"Mã tin nhắn đã sử dụng.");return old;}var since=DateTime.UtcNow.AddMinutes(-1);Require(db.ChatMessages.Count(m=>m.SenderId==actor&&m.CreatedUtc>since)<20,"Bạn gửi quá nhanh. Vui lòng chờ một phút.");var thread=db.ChatThreads.SingleOrDefault(t=>t.CustomerId==customer);if(thread==null){thread=new(){CustomerId=customer};db.ChatThreads.Add(thread);}thread.UpdatedUtc=DateTime.UtcNow;var message=new ChatMessage{CustomerId=customer,SenderId=actor,Text=text,ClientToken=clientToken,CreatedUtc=DateTime.UtcNow};db.ChatMessages.Add(message);return message;});
 public void MarkChatRead(int customer,int actor,long last)=>NativeWrite(db=>{ChatAccess(db,actor,customer);var t=db.ChatThreads.SingleOrDefault(t=>t.CustomerId==customer);if(t==null)return 0;long max=db.ChatMessages.Where(m=>m.CustomerId==customer).Select(m=>(long?)m.Id).Max()??0;last=Math.Min(Math.Max(0,last),max);if(actor>0)t.CustomerReadId=Math.Max(t.CustomerReadId,last);else t.StaffReadId=Math.Max(t.StaffReadId,last);return 0;});
 public List<(int Id,string Name,DateTime Updated,int Unread)> ChatInbox(int actor)=>NativeRead(db=>{NativePermission(db,actor,"Support");return (from t in db.ChatThreads join c in db.KhachHang on t.CustomerId equals c.MaKH orderby t.UpdatedUtc descending select new{c.MaKH,c.HoTen,t.UpdatedUtc,Unread=db.ChatMessages.Count(m=>m.CustomerId==t.CustomerId&&m.SenderId>0&&m.Id>t.StaffReadId)}).Take(200).ToList().Select(t=>(t.MaKH,t.HoTen,t.UpdatedUtc,t.Unread)).ToList();});
 internal (Guid Id,string Email,string Code)? IssueReset(string email,string secret)=>NativeWrite(db=>
 {email=email.Trim().ToLowerInvariant();var c=db.KhachHang.SingleOrDefault(c=>c.Email==email&&c.TrangThai);if(c==null)return null;var now=DateTime.UtcNow;var hour=now.AddHours(-1);var minute=now.AddMinutes(-1);if(db.ResetCodes.Any(r=>r.CustomerId==c.MaKH&&r.CreatedUtc>minute)||db.ResetCodes.Count(r=>r.CustomerId==c.MaKH&&r.CreatedUtc>hour)>=5)return null;foreach(var old in db.ResetCodes.Where(r=>r.CustomerId==c.MaKH&&r.UsedUtc==null))old.UsedUtc=now;string code=RandomNumberGenerator.GetInt32(1000000).ToString("D6");var row=new ResetCode{Id=Guid.NewGuid(),CustomerId=c.MaKH,Stamp=PasswordStamp(c.MatKhau),CreatedUtc=now,ExpiresUtc=now.AddMinutes(10)};row.Hash=OtpHash(row.Id,code,secret);db.ResetCodes.Add(row);return ((Guid Id,string Email,string Code)?)(row.Id,c.Email,code);});
 internal void ResetDelivered(Guid id)=>NativeWrite(db=>{var code=db.ResetCodes.Single(r=>r.Id==id);code.Sent=true;return 0;});
 public bool ResetPassword(ResetInput input,string secret)
 {Validator.ValidateObject(input,new ValidationContext(input),true);Require(secret.Length>=32,"Chức năng khôi phục chưa cấu hình.");return NativeWrite(db=>{var email=input.Email.Trim().ToLowerInvariant();var c=db.KhachHang.SingleOrDefault(c=>c.Email==email&&c.TrangThai);if(c==null)return false;var r=db.ResetCodes.Where(r=>r.CustomerId==c.MaKH&&r.Sent&&r.UsedUtc==null).OrderByDescending(r=>r.CreatedUtc).FirstOrDefault();var now=DateTime.UtcNow;if(r==null||r.ExpiresUtc<=now||r.Attempts>=5||r.Stamp!=PasswordStamp(c.MatKhau))return false;r.Attempts++;if(!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(r.Hash),Convert.FromHexString(OtpHash(r.Id,input.Code,secret))))return false;c.MatKhau=hasher.HashPassword(new Customer(),input.Password);r.UsedUtc=now;foreach(var other in db.ResetCodes.Where(x=>x.CustomerId==c.MaKH&&x.UsedUtc==null))other.UsedUtc=now;return true;});}
 static string PasswordStamp(string hash)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
 static string OtpHash(Guid id,string code,string secret)=>Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes(id.ToString("N")+":"+code)));
}
public interface IMailSender { Task SendCode(string email,string code,CancellationToken cancellationToken); }
public sealed class SmtpMailSender(IConfiguration config):IMailSender
{
 public async Task SendCode(string email,string code,CancellationToken cancellationToken)
 {var o=config.GetSection("Smtp").Get<SmtpOptions>()??new();if(!o.Ready||!o.EnableSsl)throw new InvalidOperationException("SMTP chưa cấu hình TLS.");using var client=new System.Net.Mail.SmtpClient(o.Host,o.Port){EnableSsl=true,UseDefaultCredentials=false,Credentials=new System.Net.NetworkCredential(o.User,o.Password),Timeout=15000};using var message=new System.Net.Mail.MailMessage(o.From,email,"Mã khôi phục tài khoản QOANH",$"Mã OTP: {code}\nMã có hiệu lực 10 phút, tối đa 5 lần thử. Không cung cấp mã cho người khác.");await client.SendMailAsync(message,cancellationToken);}
}
public sealed class PasswordRecovery(StoreService store,IMailSender mail,IConfiguration config,ILogger<PasswordRecovery> logger)
{
 public bool Ready=>(config.GetSection("Smtp").Get<SmtpOptions>()??new()).Ready&&(config["PasswordReset:HashKey"]?.Length??0)>=32;
 public async Task Request(string email,CancellationToken cancellationToken)
 {if(!Ready)return;var challenge=store.IssueReset(email,config["PasswordReset:HashKey"]!);if(challenge==null)return;try{await mail.SendCode(challenge.Value.Email,challenge.Value.Code,cancellationToken);store.ResetDelivered(challenge.Value.Id);}catch(Exception ex) when(ex is System.Net.Mail.SmtpException or InvalidOperationException or OperationCanceledException or FormatException or ArgumentException){logger.LogWarning("Password recovery delivery failed ({ErrorType}); no OTP or recipient logged.",ex.GetType().Name);}}
 public bool Complete(ResetInput input)=>Ready&&store.ResetPassword(input,config["PasswordReset:HashKey"]!);
}
