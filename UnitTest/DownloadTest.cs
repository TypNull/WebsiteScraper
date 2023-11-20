using DownloadAssistant.Request;
using Requests;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace UnitTest
{
    [TestClass]
    public class DownloadTest
    {
        [TestMethod]
        public async Task Search()
        {
            Website mangaread = Website.LoadWebsite(@"C:\Bibliothek\Visual Studio\WebsiteScraper\mangaread.org.wsf");
            mangaread.ItemsPerSearch = 20;
            await mangaread.GetStatusTask();
            Comic[] searchComics = await mangaread.SearchAsync<Comic>("isekai");
            Console.WriteLine("Comics found: " + searchComics.Length);
            for (int i = 0; i < searchComics.Length; i++)
            {
                Console.WriteLine($"{i} | {searchComics[i].Title}");
            }
        }

        [TestMethod]
        public async Task Update()
        {
            Website mangaread = Website.LoadWebsite(@"C:\Bibliothek\Visual Studio\WebsiteScraper\mangaread.org.wsf");
            Comic onePunch = new("https://www.mangaread.org/manga/one-punch-man-onepunchman/", "One Punch Man", mangaread);
            await onePunch.UpdateAsync();
            Console.WriteLine(onePunch.Title);
            Console.WriteLine(onePunch.Chapter.Length + " Chapters");
            Console.WriteLine("Description: " + onePunch.Description);
        }

        [TestMethod]
        public async Task Download()
        {
            Website mangaread = Website.LoadWebsite(@"C:\Bibliothek\Visual Studio\WebsiteScraper\mangaread.org.wsf");
            await mangaread.GetStatusTask();
            Comic onePunch = new("https://www.mangaread.org/manga/one-punch-man-onepunchman/", "One Punch Man", mangaread);
            await onePunch.UpdateAsync();
            ProgressableContainer<LoadRequest>? container = await onePunch.Chapter.First().DownloadAsync(@"C:\Bibliothek\Download\One Punch Man\1\");
            container.StateChanged += Container_StateChanged;
            if (container != null)
                await container.Task;
            else throw new ArgumentNullException();
        }

        private void Container_StateChanged(object? sender, Requests.Options.RequestState e)
        {
            Console.WriteLine(e);
        }
    }
}