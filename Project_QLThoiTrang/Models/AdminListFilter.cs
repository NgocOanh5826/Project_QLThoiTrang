namespace Project_QLThoiTrang.Models;
public sealed record AdminFilterOption(string Value,string Text);
public sealed record AdminListFilter(string Key,string Label,IReadOnlyList<AdminFilterOption>? Options=null)
{
 public static AdminListFilter Choice(string key,string label,params (string Value,string Text)[] choices)=>new(key,label,choices.Select(c=>new AdminFilterOption(c.Value,c.Text)).ToArray());
 public static bool Matches(string text,string? query)
 {static string Fold(string value)=>new string(value.Normalize(System.Text.NormalizationForm.FormD).Where(c=>System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).Replace('đ','d').Replace('Đ','D').ToUpperInvariant();return string.IsNullOrWhiteSpace(query)||Fold(text).Contains(Fold(query.Trim()),StringComparison.Ordinal);}
}
