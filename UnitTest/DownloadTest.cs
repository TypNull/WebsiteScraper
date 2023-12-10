using DownloadAssistant.Request;
using Requests;
using System.Diagnostics;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace UnitTest
{
    [TestClass]
    public class DownloadTest
    {
        private string _pathToWebsite = @$"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName}\Websites\mangaread.org.wsf";

        private Website _mangaread = null!;

        [TestInitialize]
        public void Initialize()
        {
            _mangaread = Website.LoadWebsite(_pathToWebsite);
        }

        [TestMethod]
        public async Task SearchAsync()
        {
            _mangaread.ItemsPerSearch = 10;
            await _mangaread.GetStatusTask();
            SearchInfo searchinfo = new("isekai", _mangaread);
            searchinfo.EnableAbleTags["action"].State = EnableAbleState.Enabled;
            searchinfo.EnableAbleTags["comedy"].State = EnableAbleState.Enabled;
            searchinfo.RadioTags["genres condition"].EnabledKey = "and";
            List<Comic> searchComics = new();
            searchComics.AddRange(await _mangaread.SearchAsync<Comic>(searchinfo));
            searchComics.AddRange(await _mangaread.SearchNextAsync<Comic>());
            Console.WriteLine("Comics found: " + searchComics.Count);
            for (int i = 0; i < searchComics.Count; i++)
            {
                Console.WriteLine($"{i} | Title {searchComics[i].Title}");
                Console.WriteLine($"{i} | Genres {string.Join("; ", searchComics[i].Genres)}");
                Console.WriteLine($"{i} | Alternative titles{string.Join("; ", searchComics[i].AlternativeTitles)}");
                Console.WriteLine($"{i} | Description: {searchComics[i].Description}");
                Console.WriteLine($"{i} | Status: {searchComics[i].Status}");
                Console.WriteLine($"{i} | Author: {searchComics[i].Author}");
                Console.WriteLine($"{i} | Cover url: {searchComics[i].CoverUrl}");
                Console.WriteLine($"{i} | Last updated: {searchComics[i].LastUpdated}");
                Console.WriteLine($"{i} | Url: {searchComics[i].Url}");
                Console.WriteLine("");
            }
        }

        [TestMethod]
        public async Task UpdateTestAsync()
        {
            await _mangaread.GetStatusTask();
            Comic onePunch = new("https://www.mangaread.org/manga/one-punch-man-onepunchman/", "One Punch Man", _mangaread);
            await onePunch.UpdateAsync();
            Console.WriteLine($"Title {onePunch.Title}");
            Console.WriteLine($"Alternative titles: {string.Join("; ", onePunch.AlternativeTitles)}");
            Console.WriteLine($"Description: {string.Join("; ", onePunch.Genres)}");
            Console.WriteLine($"Status: {onePunch.Status}");
            Console.WriteLine($"Author: {onePunch.Author}");
            Console.WriteLine($"Chapter lenght: {onePunch.Chapter.Length}");
            Console.WriteLine($"Last updated: {onePunch.LastUpdated}");
            Console.WriteLine($"Cover url: {onePunch.CoverUrl}");
            Console.WriteLine($"Url: {onePunch.Url}");
        }

        [TestMethod]
        public async Task TestHomeAsync()
        {
            await _mangaread.GetStatusTask();
            Comic[] newComic = await _mangaread.LoadNewAsync<Comic>();
            Console.WriteLine($"Found {newComic.Length} new Comics");
            Comic[] rComic = await _mangaread.LoadExtraAsync<Comic>("Recommended");
            Console.WriteLine($"Found {rComic.Length} new Comics");
        }

        [TestMethod]
        public async Task DownloadTestAsync()
        {
            await _mangaread.GetStatusTask();
            Comic onePunch = new("https://www.mangaread.org/manga/one-punch-man-onepunchman/", "One Punch Man", _mangaread);
            await onePunch.UpdateAsync();
            ProgressableContainer<LoadRequest> container = await onePunch.Chapter.First().DownloadAsync(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\One Punch Man\1\");
            container.StateChanged += (s, e) => Console.WriteLine("State: " + e);
            await container.Task;
            Console.WriteLine("Downloaded");
        }
    }
}