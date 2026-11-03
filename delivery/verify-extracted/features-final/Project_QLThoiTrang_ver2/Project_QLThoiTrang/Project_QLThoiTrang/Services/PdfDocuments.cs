using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
namespace Project_QLThoiTrang.Services;
public sealed class DocumentFontResolver(string directory):IFontResolver
{
 public FontResolverInfo ResolveTypeface(string familyName,bool bold,bool italic)=>new(bold?"qoanh-bold":"qoanh-regular");
 public byte[] GetFont(string faceName)=>File.ReadAllBytes(Path.Combine(directory,faceName=="qoanh-bold"?"arialbd.ttf":"arial.ttf"));
}
public sealed class PdfDocuments
{
 public byte[] Create(string title,string code,IEnumerable<string> info,string[] headers,List<string[]> rows,IEnumerable<string> totals)
 {
  using var doc=new PdfDocument();doc.Info.Title=title+" "+code;doc.Info.Author="QOANH";XGraphics? gfx=null;double y=0;int pageNumber=0;var font=new XFont("Arial",10);var bold=new XFont("Arial",10,XFontStyleEx.Bold);var large=new XFont("Arial",20,XFontStyleEx.Bold);var green=new XSolidBrush(XColor.FromArgb(37,56,45));double[] widths=headers.Length==4?[260,55,100,100]:[515.0/headers.Length];
  List<string> Wrap(string text,double width,XFont f){var result=new List<string>();foreach(var paragraph in (text??"").Replace("\r","").Split('\n')){string line="";foreach(var raw in paragraph.Split(' ',StringSplitOptions.RemoveEmptyEntries)){string word=raw;string next=line.Length==0?word:line+" "+word;if(gfx!.MeasureString(next,f).Width<=width){line=next;continue;}if(line.Length>0){result.Add(line);line="";}while(gfx.MeasureString(word,f).Width>width){int cut=word.Length-1;while(cut>1&&gfx.MeasureString(word[..cut],f).Width>width)cut--;result.Add(word[..cut]);word=word[cut..];}line=word;}result.Add(line);}return result;}
  void NewPage(){gfx?.Dispose();var page=doc.AddPage();page.Size=PdfSharp.PageSize.A4;gfx=XGraphics.FromPdfPage(page);pageNumber++;y=40;gfx.DrawString("QOANH",large,green,40,y);y+=28;gfx.DrawString(title,bold,XBrushes.Black,40,y);y+=19;gfx.DrawString(code,font,XBrushes.DimGray,40,y);y+=25;}
  void Ensure(double height){if(y+height>775)NewPage();}
  void Text(string value,XFont f){var lines=Wrap(value,515,f);foreach(var line in lines){Ensure(16);gfx!.DrawString(line,f,XBrushes.Black,new XRect(40,y,515,16),XStringFormats.TopLeft);y+=16;}y+=5;}
  void Header(){Ensure(32);gfx!.DrawRectangle(new XSolidBrush(XColor.FromArgb(237,241,233)),40,y,515,28);double x=40;for(int i=0;i<headers.Length;i++){gfx.DrawString(headers[i],bold,green,new XRect(x+6,y+6,widths[i]-12,20),XStringFormats.TopLeft);x+=widths[i];}y+=30;}
  NewPage();foreach(var line in info)Text(line,font);Header();
  foreach(var row in rows){var cells=row.Select((cell,i)=>Wrap(cell,widths[i]-12,font)).ToList();double height=cells.Max(c=>c.Count)*15+12;if(y+height>775){NewPage();Header();}double x=40;for(int i=0;i<row.Length;i++){for(int j=0;j<cells[i].Count;j++)gfx!.DrawString(cells[i][j],font,XBrushes.Black,new XRect(x+6,y+6+j*15,widths[i]-12,15),XStringFormats.TopLeft);x+=widths[i];}y+=height;gfx!.DrawLine(new XPen(XColor.FromArgb(225,228,220)),40,y,555,y);}
  y+=16;foreach(var total in totals)Text(total,bold);Text("Chứng từ nội bộ - không thay thế hóa đơn thuế.",font);gfx?.Dispose();
  for(int i=0;i<doc.PageCount;i++){using var footer=XGraphics.FromPdfPage(doc.Pages[i],XGraphicsPdfPageOptions.Append);footer.DrawString($"QOANH  |  {code}  |  Trang {i+1}/{doc.PageCount}",new XFont("Arial",8),XBrushes.DimGray,new XRect(40,806,515,14),XStringFormats.TopLeft);}
  using var stream=new MemoryStream();doc.Save(stream,false);return stream.ToArray();
 }
}
