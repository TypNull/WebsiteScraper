using AngleSharp.Dom;
using DownloadAssistant.Base;
using DownloadAssistant.Request;
using Requests;
using Requests.Options;
using System.Diagnostics;

namespace WebsiteScraper.Downloadable.Books
{
    public class Chapter
    {
        public string Title { get; init; } = string.Empty;
        public float Number { get; init; } = 0;
        public string Url { get; init; } = string.Empty;
        public string DownloadURL { get; init; } = string.Empty;
        public int Order { get; private set; }
        public DateTime UploadDateTime { get; init; }
        public Comic HoldingComic { get; init; }
        private Dictionary<string, string>? _dictionary;
        public void SetOrder(int i)
        {
            if (Order == 0)
                Order = i;
        }

        private string? GetValue(string key)
        {
            if (_dictionary == null)
                _dictionary = HoldingComic?.HoldingWebsite?.GetValue<Dictionary<string, string>>("ComicChapter");
            return _dictionary?.GetValueOrDefault(key);
        }

        public Chapter(Comic holdingComic) => HoldingComic = holdingComic;

        public Task<ProgressableContainer<GetRequest>> DownloadAsync(string destination, CancellationToken? token = null)
        {
            if (!string.IsNullOrWhiteSpace(DownloadURL))
                return DownloadImageFromFileAsync(token);
            if (GetValue("AddToListUrl") != null)
                return DownloadImageListAsync(destination, token);
            else if (GetValue("AddToPagedUrl") != null)
                return DownloadImagePageAsync(destination, token);
            else throw new Exception("Can not donwload this object");
        }

        private async Task<ProgressableContainer<GetRequest>> DownloadImageListAsync(string destination, CancellationToken? token)
        {
            ProgressableContainer<GetRequest> container = new();
            await new OwnRequest(async DToken =>
            {
                string? selector = GetValue("ListImageQuery");
                if (selector == null)
                    return false;
                using HttpRequestMessage? msg = new(HttpMethod.Get, Url + (GetValue("AddToListUrl") ?? string.Empty));
                using HttpResponseMessage res = await HttpGet.HttpClient.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, DToken);
                if (!res.IsSuccessStatusCode)
                    return false;

                IElement? html = await (await res.Content.ReadAsStringAsync(DToken)).ToIElementAsync();
                if (html == null)
                    return false;

                string[] imageLinks = selector.ParseAll(html);

                for (int i = 0; i < imageLinks.Length; i++)
                    container.Add(new GetRequest(imageLinks[i], new()
                    {
                        IsDownload = true,
                        DirectoryPath = destination,
                        CancellationToken = token,
                        Filename = $"{Order}_{i}.*",
                        Priority = RequestPriority.High,
                        RequestFailed = (request, path) => { Debug.WriteLine("Failed: " + path); }
                    }));
                return true;
            }, new()
            {
                Handler = RequestHandler.MainRequestHandlers[1],
                CancellationToken = token
            }).Task;

            return container;
        }

        private async Task<ProgressableContainer<GetRequest>> DownloadImagePageAsync(string destination, CancellationToken? token)
        {
            ProgressableContainer<GetRequest> container = new();
            await new OwnRequest(async DToken =>
             {
                 bool stop = false;
                 string? selector = GetValue("PageImageQuery");
                 if (selector == null)
                     stop = true;
                 for (int i = 1; !stop; i++)
                 {
                     using HttpRequestMessage? msg = new(HttpMethod.Get, Url + GetValue("AddToPagedUrl")?.Replace("[page]", i.ToString()));
                     using HttpResponseMessage res = await HttpGet.HttpClient.SendAsync(msg, HttpCompletionOption.ResponseContentRead, DToken);
                     if (!res.IsSuccessStatusCode)
                         break;

                     IElement? html = await (await res.Content.ReadAsStringAsync(DToken)).ToIElementAsync();
                     if (html == null)
                         break;
                     string imageLink = selector?.Parse(html) ?? string.Empty;
                     res.Dispose();
                     if (string.IsNullOrWhiteSpace(imageLink))
                         break;
                     container.Add(new GetRequest(imageLink, new()
                     {
                         IsDownload = true,
                         Priority = RequestPriority.High,
                         DirectoryPath = destination,
                         CancellationToken = token,
                         Filename = $"{Order}_{i}.*",
                         RequestFailed = (request, path) => { Debug.WriteLine("Failed: " + path); }
                     }));
                 }
                 stop = true;
                 return true;
             }, new()
             {
                 Handler = RequestHandler.MainRequestHandlers[1],
                 Priority = RequestPriority.Normal,
                 CancellationToken = token
             }).Task;

            return container;
        }

        private Task<ProgressableContainer<GetRequest>> DownloadImageFromFileAsync(CancellationToken? token)
        {
            throw new NotImplementedException();
        }
    }
}