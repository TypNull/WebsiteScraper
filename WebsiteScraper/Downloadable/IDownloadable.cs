using System.ComponentModel;
using WebsiteScraper.WebsiteUtilities;

namespace WebsiteScraper.Downloadable
{
    public interface IDownloadable<TClass> : INotifyPropertyChanged
    {
        public static abstract TClass Create(string url, Website website);
        public abstract Task UpdateAsync(CancellationToken? token);
    }

}
