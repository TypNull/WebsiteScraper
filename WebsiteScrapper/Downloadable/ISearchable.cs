using AngleSharp.Dom;
using WebsiteScraper.WebsiteUtilities;

namespace WebsiteScraper.Downloadable
{
    public interface ISearchable<TClass> : IDownloadable<TClass>
    {
        public static abstract TClass CreateSearch(IElement containierElement, Website website);
    }
}
