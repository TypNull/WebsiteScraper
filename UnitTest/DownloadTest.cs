using DownloadAssistant.Request;
using Requests;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace UnitTest
{
    [TestClass]
    public class DownloadTest
    {
        private const string _pathToWebsite = @"D:\Bibliothek\Visual Studio\Librarys\WebsiteScraper\mangaread.org.wsf";

        private Website _mangaread = null!;

        [TestInitialize]
        public void Initialize()
        {
            _mangaread = Website.LoadWebsite(_pathToWebsite);
        }

        [TestMethod]
        public async Task SearchAsync()
        {
            _mangaread.ItemsPerSearch = 20;
            await _mangaread.GetStatusTask();
            Comic[] searchComics = await _mangaread.SearchAsync<Comic>("isekai");
            Console.WriteLine("Comics found: " + searchComics.Length);
            for (int i = 0; i < searchComics.Length; i++)
                Console.WriteLine($"{i} | {searchComics[i].Title}");
        }

        [TestMethod]
        public async Task UpdateTestAsync()
        {
            await _mangaread.GetStatusTask();
            Comic onePunch = new("https://www.mangaread.org/manga/one-punch-man-onepunchman/", "One Punch Man", _mangaread);
            await onePunch.UpdateAsync();
            Console.WriteLine(onePunch.Title);
            Console.WriteLine(onePunch.Chapter.Length + " Chapters");
            Console.WriteLine("Description: " + onePunch.Description);
        }

        [TestMethod]
        public async Task DownloadTestAsync()
        {
            await _mangaread.GetStatusTask();
            Comic onePunch = new("https://www.mangaread.org/manga/one-punch-man-onepunchman/", "One Punch Man", _mangaread);
            await onePunch.UpdateAsync();
            ProgressableContainer<LoadRequest> container = await onePunch.Chapter.First().DownloadAsync(@"D:\Bibliothek\Downloads\One Punch Man\1\");
            container.StateChanged += (s, e) => Console.WriteLine("State: " + e);
            await container.Task;
            Console.WriteLine("Downloaded");
        }
    }
}