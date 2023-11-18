using System.Runtime.InteropServices;

namespace WebsiteScraper.Downloadable.Books
{
    internal class ChapterComparer : IComparer<Chapter>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string? x, string? y);
        public int Compare(Chapter? x, Chapter? y) => StrCmpLogicalW(x?.Url, y?.Url);

    }
}
