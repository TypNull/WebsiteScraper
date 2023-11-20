using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WebsiteScraper
{
    internal static class ElementPaser
    {
        /// <summary>
        /// Formats the Text from Unicode to standard
        /// </summary>
        /// <param name="text">Text that will be searched</param>
        /// <returns>string string with with formated version of first text</returns>
        public static string UnicodeToText(this string text) => (text == null) ? string.Empty : Regex.Replace(text, @"&#(\d+);", (Match m) => char.ToString((char)int.Parse(m.Groups[1].Value)));

        public static string GetDateFormat(this string? chapterDateSelector, out string? chapterDateSelectorOut)
        {
            string format = string.Empty;
            if (new Regex("[[]format[]]{(.*?)}") is Regex reg && reg.Match(chapterDateSelector ?? string.Empty) is Match match && match.Success)
            {
                format = match.Groups[1].Value.Trim();
                if (chapterDateSelector != null)
                    chapterDateSelector = reg.Replace(chapterDateSelector, string.Empty);
            }
            chapterDateSelectorOut = chapterDateSelector;
            return format;
        }


        public static string[] ParseAll(this string? selector, IElement element)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return Array.Empty<string>();
            string regexPattern = string.Empty;
            string atribute = "raw-content";

            if (new Regex("[[]regex[]]{(.*?)}") is Regex reg && reg.Match(selector) is Match match && match.Success)
            {
                regexPattern = match.Groups[1].Value.Trim();
                selector = reg.Replace(selector, string.Empty);
            }

            reg = new Regex("[[]attribute[]]{(.*?)}");
            match = reg.Match(selector);
            if (match.Success)
            {
                atribute = match.Groups[1].Value.Trim();
                selector = reg.Replace(selector, string.Empty);
            }

            List<string> found = CssQueryAll(element, selector, atribute);
            if (regexPattern != string.Empty)
            {
                List<string> cssFound = found;
                found = new();
                for (int i = 0; i < cssFound.Count; i++)
                    found.AddRange(RegexMatches(cssFound[i], regexPattern));

            }
            return found.ToArray();
        }

        private static List<string> CssQueryAll(IElement element, string selector, string atribute)
        {
            List<string> found = new();
            try
            {
                IHtmlCollection<IElement> elements = element.QuerySelectorAll(selector);

                if (elements.Length == 0)
                    return found;
                foreach (IElement foundItem in elements)
                {
                    if (atribute == "html")
                        found.Add(foundItem.Html().Trim());
                    else if (atribute == "raw-content")
                        found.Add(foundItem.TextContent.Trim());
                    else if (foundItem.GetAttribute(atribute) is string tag)
                        found.Add(tag.Trim());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return found;
        }

        private static string[] RegexMatches(string found, string regexPattern)
        {
            string[] s = Array.Empty<string>();
            if (found == null || regexPattern == null)
                return s;
            return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(2)).Matches(found).Select((x) => x.Value).ToArray();
        }

        private static string CssQuery(IElement element, string selector, string atribute)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return string.Empty;
            string found = string.Empty;
            try
            {
                IElement? foundelement = element.QuerySelector(selector);

                if (foundelement == null)
                    return string.Empty;

                if (atribute == "html")
                    found = foundelement.Html().Trim();
                else if (atribute == "raw-content")
                    found = foundelement.TextContent.Trim();
                else if (foundelement.GetAttribute(atribute) is string tag)
                    found = tag.Trim();


            }
            catch (Exception)
            {
                Debug.WriteLine(selector);
            }
            return found;
        }

        public static string Parse(this string selector, IElement element)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return string.Empty;
            string regexPattern = string.Empty;
            string atribute = "raw-content";

            if (new Regex("[[]regex[]]{(.*?)}") is Regex reg && reg.Match(selector) is Match match && match.Success)
            {
                regexPattern = match.Groups[1].Value.Trim();
                selector = reg.Replace(selector, string.Empty);
            }

            reg = new Regex("[[]attribute[]]{(.*?)}");
            match = reg.Match(selector);
            if (match.Success)
            {
                atribute = match.Groups[1].Value.Trim();
                selector = reg.Replace(selector, string.Empty);
            }

            string found = CssQuery(element, selector, atribute);
            if (regexPattern != string.Empty)
            {
                found = RegexMatch(found, regexPattern);
            }
            return found;
        }

        private static string RegexMatch(string found, string regexPattern) => new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline).Match(found).Value;

        public static IElement? GetElement(this string selector, IElement element)
        {
            try
            {
                return element.QuerySelector(selector);
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static async Task<IElement?> ToIElementAsync(this string html)
        {
            HtmlParser parser = new();
            return (await parser.ParseDocumentAsync(html)).DocumentElement;
        }

        public static IElement[] GetAllElements(this string selector, IElement element)
        {
            try
            {
                return element.QuerySelectorAll(selector).ToArray();
            }
            catch (Exception)
            {

            }
            return Array.Empty<IElement>();
        }
    }
}
