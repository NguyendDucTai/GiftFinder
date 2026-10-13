using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GiftFinder.Application.Common.Helpers;

public static class SlugHelper
{
    public static string ToUrlFriendlySlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "tag-" + Guid.NewGuid().ToString("N")[..6];

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var clean = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        clean = clean.Replace("đ", "d").Replace("Đ", "d");
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
        clean = Regex.Replace(clean, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(clean) ? "tag-" + Guid.NewGuid().ToString("N")[..6] : clean;
    }
}
