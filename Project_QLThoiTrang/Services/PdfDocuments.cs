// VAI TRÒ FILE: Sinh PDF hỗ trợ tiếng Việt, xuống dòng và nhiều trang. Quyền tải và dữ liệu chứng
// từ do
// DocumentsController chuẩn bị.
// TRA CỨU: HUONG_DAN_CODE.md ở thư mục gốc có đường dẫn và bảng chức năng.

using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Project_QLThoiTrang.Services;

// THAM SỐ KHỞI TẠO directory (string): thư mục chứa font .ttf cho chứng từ PDF; do DI hoặc nơi gọi
// constructor truyền vào.
public sealed class DocumentFontResolver(string directory) : IFontResolver
{
    // HÀM ResolveTypeface: Chọn Arial thường hoặc đậm cho PDF theo yêu cầu bold; mã font được
    // GetFont dùng để đọc tệp thực tế.
    // THAM SỐ familyName (string): tên họ font yêu cầu; resolver hiện trả bộ Arial được cấu hình
    // cho PDF tiếng Việt.
    // THAM SỐ bold (bool): true yêu cầu kiểu chữ đậm, false dùng chữ thường.
    // THAM SỐ italic (bool): cờ yêu cầu chữ nghiêng; resolver hiện tại chỉ phân biệt theo bold,
    // không chọn tệp font nghiêng riêng.
    // TRẢ VỀ (FontResolverInfo): FontResolverInfo; ý nghĩa được xác định bởi biểu thức return bên
    // dưới.
    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(bold ? "qoanh-bold" : "qoanh-regular");

    // HÀM GetFont: Đọc arial.ttf hoặc arialbd.ttf từ thư mục font đã cấu hình để PDF hỗ trợ tiếng
    // Việt.
    // THAM SỐ faceName (string): mã font nội bộ do ResolveTypeface trả; GetFont đổi mã này thành
    // tệp .ttf cần đọc.
    // TRẢ VỀ (byte[]): mảng byte dữ liệu nhị phân; ý nghĩa được xác định bởi biểu thức return bên
    // dưới.
    public byte[] GetFont(string faceName) =>
        File.ReadAllBytes(
            Path.Combine(directory, faceName == "qoanh-bold" ? "arialbd.ttf" : "arial.ttf")
        );
}

public sealed class PdfDocuments
{
    // HÀM Create: Tạo PDF từ tiêu đề, thông tin, bảng và tổng tiền; các hàm nội bộ đo chiều rộng,
    // xuống dòng và sang trang khi thiếu chỗ. Hàm chỉ sinh byte[], controller kiểm tra quyền rồi
    // trả File cho người dùng.
    // THAM SỐ title (string): tiêu đề chứng từ PDF như ĐƠN HÀNG hoặc PHIẾU NHẬP KHO.
    // THAM SỐ code (string): mã chứng từ in ở đầu PDF.
    // THAM SỐ info (IEnumerable<string>): các dòng thông tin đầu chứng từ PDF như ngày, người nhận,
    // điện thoại và địa chỉ.
    // THAM SỐ headers (string[]): nhãn các cột bảng PDF; số cột ảnh hưởng độ rộng và cách vẽ bảng.
    // THAM SỐ rows (List<string[]>): các dòng bảng PDF, mỗi dòng là mảng chuỗi theo thứ tự headers.
    // THAM SỐ totals (IEnumerable<string>): các dòng tổng tiền/ghi chú cuối chứng từ PDF.
    // TRẢ VỀ (byte[]): byte[] của tệp PDF hoàn chỉnh; DocumentsController dùng mảng này trả File
    // cho trình duyệt.
    // BIẾN LAMBDA cell (string): chuỗi ký tự; tên này chỉ có nghĩa trong lambda tương ứng, không
    // phải biến toàn cục.
    // BIẾN LAMBDA i (int): số nguyên 32 bit; tên này chỉ có nghĩa trong lambda tương ứng, không
    // phải biến toàn cục.
    // BIẾN LAMBDA c (List<string>): danh sách/truy vấn các phần tử: string; tên này chỉ có nghĩa
    // trong lambda tương ứng, không phải biến toàn cục.
    public byte[] Create(
        string title,
        string code,
        IEnumerable<string> info,
        string[] headers,
        List<string[]> rows,
        IEnumerable<string> totals
    )
    {
        // BIẾN doc (PdfDocument): tài liệu PDF đang được dựng trong RAM, chứa danh sách trang và
        // các thành phần vẽ.
        using var doc = new PdfDocument();
        doc.Info.Title = title + " " + code;
        doc.Info.Author = "QOANH";
        // BIẾN gfx (XGraphics?): ngữ cảnh vẽ của trang PDF hiện tại; được thay khi sang trang và
        // dispose trước khi kết thúc.
        XGraphics? gfx = null;
        // BIẾN y (double): tọa độ dọc hiện tại trên trang PDF, đơn vị point; tăng sau mỗi đoạn/bảng
        // để tránh chồng nội dung.
        double y = 0;
        // BIẾN pageNumber (int): bộ đếm trang PDF đã tạo, phục vụ phần đánh số trang/chứng từ.
        int pageNumber = 0;
        // BIẾN font (XFont): font Arial thường dùng cho nội dung PDF.
        var font = new XFont("Arial", 10);
        // BIẾN bold (XFont): font Arial đậm dùng cho tiêu đề cột hoặc đoạn cần nhấn mạnh.
        var bold = new XFont("Arial", 10, XFontStyleEx.Bold);
        // BIẾN large (XFont): font cỡ lớn dùng cho tiêu đề chứng từ PDF.
        var large = new XFont("Arial", 20, XFontStyleEx.Bold);
        // BIẾN green (XSolidBrush): bút tô màu nhấn của chứng từ PDF, không liên quan trạng thái
        // nghiệp vụ.
        var green = new XSolidBrush(XColor.FromArgb(37, 56, 45));
        // BIẾN widths (double[]): độ rộng các cột bảng PDF theo point, chọn theo số cột headers.
        double[] widths = headers.Length == 4 ? [260, 55, 100, 100] : [515.0 / headers.Length];
        // HÀM Wrap: Hàm nội bộ của Create: đo chữ theo font và chia thành các dòng vừa chiều rộng;
        // từ quá dài được cắt thành nhiều phần.
        // THAM SỐ text (string): chuỗi đầu vào cần chuẩn hóa, so khớp hoặc chia dòng theo chức năng
        // của hàm.
        // THAM SỐ width (double): chiều rộng tối đa của dòng chữ PDF theo point, không phải số ký
        // tự.
        // THAM SỐ f (XFont): font dùng đo/vẽ đoạn chữ; cùng nội dung nhưng font khác có thể cần số
        // dòng khác nhau.
        // TRẢ VỀ (List<string>): danh sách/truy vấn các phần tử: string; có thể rỗng khi không có
        // dữ liệu khớp.
        List<string> Wrap(string text, double width, XFont f)
        {
            // BIẾN result (List<string>): các dòng chữ sau khi Wrap đo và chia theo chiều rộng PDF,
            // không phải phản hồi HTTP.
            var result = new List<string>();
            // BIẾN LẶP paragraph (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in,
            // chỉ dùng trong vòng foreach này.
            foreach (var paragraph in (text ?? "").Replace("\r", "").Split('\n'))
            {
                // BIẾN line (string): dòng chữ PDF đang tích lũy; thêm từ tới khi đo vượt độ rộng
                // cho phép.
                string line = "";
                // BIẾN LẶP raw (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in,
                // chỉ dùng trong vòng foreach này.
                foreach (var raw in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    // BIẾN word (string): từ đang thử xếp vào dòng PDF, có thể bị cắt khi bản thân
                    // từ quá dài.
                    string word = raw;
                    // BIẾN next (string): dòng thử sau khi nối thêm word; chỉ nhận khi
                    // MeasureString cho biết còn vừa chiều rộng.
                    string next = line.Length == 0 ? word : line + " " + word;
                    if (gfx!.MeasureString(next, f).Width <= width)
                    {
                        line = next;
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        result.Add(line);
                        line = "";
                    }

                    while (gfx.MeasureString(word, f).Width > width)
                    {
                        // BIẾN cut (int): vị trí cắt từ dài sao cho phần đầu vừa chiều rộng PDF.
                        int cut = word.Length - 1;
                        while (cut > 1 && gfx.MeasureString(word[..cut], f).Width > width)
                            cut--;
                        result.Add(word[..cut]);
                        word = word[cut..];
                    }

                    line = word;
                }

                result.Add(line);
            }

            return result;
        }

        // HÀM NewPage: Hàm nội bộ của Create: giải phóng ngữ cảnh vẽ trang cũ, thêm trang A4 và đặt
        // lại tọa độ/đánh số trang.
        // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân
        // hàm sử dụng.
        // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ
        // báo lỗi cho caller.
        void NewPage()
        {
            gfx?.Dispose();
            // BIẾN page (PdfPage): trang PDF mới tạo và sắp được chọn làm trang vẽ hiện tại.
            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            pageNumber++;
            y = 40;
            gfx.DrawString("QOANH", large, green, 40, y);
            y += 28;
            gfx.DrawString(title, bold, XBrushes.Black, 40, y);
            y += 19;
            gfx.DrawString(code, font, XBrushes.DimGray, 40, y);
            y += 25;
        }

        // HÀM Ensure: Hàm nội bộ của Create: kiểm tra phần chiều cao còn lại trước khi vẽ; vượt mốc
        // nội dung thì gọi NewPage.
        // THAM SỐ height (double): chiều cao cần dùng tiếp trên trang PDF theo point; thiếu chỗ thì
        // tạo trang mới.
        // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ
        // báo lỗi cho caller.
        void Ensure(double height)
        {
            if (y + height > 775)
                NewPage();
        }

        // HÀM Text: Hàm nội bộ của Create: Wrap nội dung, kiểm tra chỗ trống rồi vẽ từng dòng và
        // tăng tọa độ y.
        // THAM SỐ value (string): nội dung cần vẽ thành đoạn chữ PDF.
        // THAM SỐ f (XFont): font dùng đo/vẽ đoạn chữ; cùng nội dung nhưng font khác có thể cần số
        // dòng khác nhau.
        // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ
        // báo lỗi cho caller.
        void Text(string value, XFont f)
        {
            // BIẾN lines (List<string>): danh sách/truy vấn các phần tử: string.
            var lines = Wrap(value, 515, f);
            // BIẾN LẶP line (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in, chỉ
            // dùng trong vòng foreach này.
            foreach (var line in lines)
            {
                Ensure(16);
                gfx!.DrawString(
                    line,
                    f,
                    XBrushes.Black,
                    new XRect(40, y, 515, 16),
                    XStringFormats.TopLeft
                );
                y += 16;
            }

            y += 5;
        }

        // HÀM Header: Hàm nội bộ của Create: vẽ hàng tiêu đề các cột bảng trên trang PDF hiện tại.
        // ĐẦU VÀO: không có tham số trực tiếp; đọc trạng thái/service trong phạm vi lớp nếu thân
        // hàm sử dụng.
        // TRẢ VỀ (void): không trả dữ liệu; hoàn tất tác động được mô tả ở nhiệm vụ hàm. Ngoại lệ
        // báo lỗi cho caller.
        void Header()
        {
            Ensure(32);
            gfx!.DrawRectangle(new XSolidBrush(XColor.FromArgb(237, 241, 233)), 40, y, 515, 28);
            // BIẾN x (double): tọa độ ngang hiện tại trên trang PDF, đơn vị point; tăng theo độ
            // rộng cột khi vẽ ô.
            double x = 40;
            // BIẾN i (int): chỉ số cột/trang tùy vòng for ngay bên dưới; bắt đầu 0 và dùng truy cập
            // mảng cột hoặc Pages.
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawString(
                    headers[i],
                    bold,
                    green,
                    new XRect(x + 6, y + 6, widths[i] - 12, 20),
                    XStringFormats.TopLeft
                );
                x += widths[i];
            }

            y += 30;
        }

        NewPage();
        // BIẾN LẶP line (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng
        // trong vòng foreach này.
        foreach (var line in info)
            Text(line, font);
        Header();
        // BIẾN LẶP row (string[]): string[]; mỗi lượt nhận một phần tử của nguồn sau in, chỉ dùng
        // trong vòng foreach này.
        foreach (var row in rows)
        {
            // BIẾN cells (List<List<string>>): các ô của một dòng bảng PDF, mỗi ô đã được tách
            // thành danh sách dòng chữ vừa chiều rộng cột.
            var cells = row.Select((cell, i) => Wrap(cell, widths[i] - 12, font)).ToList();
            // BIẾN height (double): chiều cao dòng bảng PDF suy ra từ ô có nhiều dòng nhất, cộng
            // phần đệm.
            double height = cells.Max(c => c.Count) * 15 + 12;
            if (y + height > 775)
            {
                NewPage();
                Header();
            }

            // BIẾN x (double): tọa độ ngang hiện tại trên trang PDF, đơn vị point; tăng theo độ
            // rộng cột khi vẽ ô.
            double x = 40;
            // BIẾN i (int): chỉ số cột/trang tùy vòng for ngay bên dưới; bắt đầu 0 và dùng truy cập
            // mảng cột hoặc Pages.
            for (int i = 0; i < row.Length; i++)
            {
                // BIẾN j (int): chỉ số dòng chữ bên trong một ô bảng PDF, dùng đặt tọa độ từng dòng
                // khi vẽ.
                for (int j = 0; j < cells[i].Count; j++)
                    gfx!.DrawString(
                        cells[i][j],
                        font,
                        XBrushes.Black,
                        new XRect(x + 6, y + 6 + j * 15, widths[i] - 12, 15),
                        XStringFormats.TopLeft
                    );
                x += widths[i];
            }

            y += height;
            gfx!.DrawLine(new XPen(XColor.FromArgb(225, 228, 220)), 40, y, 555, y);
        }

        y += 16;
        // BIẾN LẶP total (string): chuỗi ký tự; mỗi lượt nhận một phần tử của nguồn sau in, chỉ
        // dùng trong vòng foreach này.
        foreach (var total in totals)
            Text(total, bold);
        Text("Chứng từ nội bộ - không thay thế hóa đơn thuế.", font);
        gfx?.Dispose();
        // BIẾN i (int): chỉ số cột/trang tùy vòng for ngay bên dưới; bắt đầu 0 và dùng truy cập
        // mảng cột hoặc Pages.
        for (int i = 0; i < doc.PageCount; i++)
        {
            // BIẾN footer (XGraphics): ngữ cảnh vẽ bổ sung vào trang PDF đã có để ghi phần
            // chân/đánh số trang.
            using var footer = XGraphics.FromPdfPage(doc.Pages[i], XGraphicsPdfPageOptions.Append);
            footer.DrawString(
                $"QOANH  |  {code}  |  Trang {i + 1}/{doc.PageCount}",
                new XFont("Arial", 8),
                XBrushes.DimGray,
                new XRect(40, 806, 515, 14),
                XStringFormats.TopLeft
            );
        }

        // BIẾN stream (MemoryStream): MemoryStream nhận nội dung PDF để chuyển thành byte[] trả về
        // controller.
        using var stream = new MemoryStream();
        doc.Save(stream, false);
        return stream.ToArray();
    }
}
