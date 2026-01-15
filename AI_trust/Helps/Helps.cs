using System.Text.RegularExpressions;

namespace AI_trust.Helps
{
    public static class HtmlHelper
    {
        public static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return Regex.Replace(html, "<.*?>", string.Empty)
                        .Replace("&nbsp;", " ")
                        .Trim();
        }
    }
}