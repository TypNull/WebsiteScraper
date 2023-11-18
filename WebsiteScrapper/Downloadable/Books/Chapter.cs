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
        public void SetOrder(int i)
        {
            if (Order == 0)
                Order = i;
        }

        public Chapter(Comic holdingComic) => HoldingComic = holdingComic;

        public ProgressableContainer<LoadRequest>? Download(string destination, string? tempDestination = null, CancellationToken? token = null, Action? finished = null)
        {
            if (!string.IsNullOrWhiteSpace(DownloadURL))
                return DownloadImageFromFileAsync(token);
            if (HoldingComic?.HoldingWebsite?.InputDictionary["Chapter"].GetValueOrDefault("ListExtension") != null)
                return DownloadImageList(destination, tempDestination, token, finished);
            else if (HoldingComic?.HoldingWebsite?.InputDictionary["Chapter"].GetValueOrDefault("PageExtension") != null)
                return DownloadImagePage(destination, tempDestination, token, finished);
            else return null;
        }

        private ProgressableContainer<LoadRequest> DownloadImageList(string destination, string? tempDestination, CancellationToken? token, Action? finished = null)
        {
            ProgressableContainer<LoadRequest> container = new();
            _ = new OwnRequest(async DToken =>
            {
                string? selector = HoldingComic?.HoldingWebsite.InputDictionary.GetValueOrDefault("Chapter")?.GetValueOrDefault("ImageList");
                if (selector == null)
                    return false;
                using HttpRequestMessage? msg = new(HttpMethod.Get, Url + (HoldingComic?.HoldingWebsite.InputDictionary.GetValueOrDefault("Chapter")?.GetValueOrDefault("ListExtension") ?? string.Empty));
                using HttpResponseMessage res = await HttpGet.HttpClient.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, DToken);
                if (!res.IsSuccessStatusCode)
                    return false;

                IElement? html = await (await res.Content.ReadAsStringAsync(DToken)).ToIElementAsync();
                if (html == null)
                    return false;

                string[] imageLinks = selector.ParseAll(html);

                for (int i = 0; i < imageLinks.Length; i++)
                    container.Add(new LoadRequest(imageLinks[i], new()
                    {
                        IsDownload = true,
                        DestinationPath = destination,
                        TempDestination = tempDestination ?? string.Empty,
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
            });

            return container;
        }

        private ProgressableContainer<LoadRequest> DownloadImagePage(string destination, string? tempDestination, CancellationToken? token, Action? finished = null)
        {
            ProgressableContainer<LoadRequest> container = new();
            _ = new OwnRequest(async DToken =>
             {
                 bool stop = false;
                 string? selector = HoldingComic?.HoldingWebsite.InputDictionary?.GetValueOrDefault("Chapter")?.GetValueOrDefault("ImagePage");
                 if (selector == null)
                     stop = true;
                 for (int i = 1; !stop; i++)
                 {
                     using HttpRequestMessage? msg = new(System.Net.Http.HttpMethod.Get, Url + HoldingComic?.HoldingWebsite?.InputDictionary["Chapter"].GetValueOrDefault("PageExtension")?.Replace("[page]", i.ToString()));
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
                     container.Add(new LoadRequest(imageLink, new()
                     {
                         IsDownload = true,
                         Priority = RequestPriority.High,
                         DestinationPath = destination,
                         TempDestination = tempDestination ?? string.Empty,
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
             });

            return container;
        }

        private ProgressableContainer<LoadRequest> DownloadImageFromFileAsync(CancellationToken? token)
        {
            throw new NotImplementedException();
        }
    }
}