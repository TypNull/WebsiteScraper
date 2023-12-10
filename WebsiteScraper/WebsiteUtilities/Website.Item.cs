using AngleSharp.Dom;
using DownloadAssistant.Base;
using Requests;
using WebsiteScraper.Downloadable;

namespace WebsiteScraper.WebsiteUtilities
{
    public partial class Website
    {
        public TResult Create<TResult>(string url) where TResult : IDownloadable<TResult>
        {
            CTS?.Cancel();
            CTS = new CancellationTokenSource();
            return TResult.Create(url, this);
        }

        public async Task<TResult> LoadAsync<TResult>(string url) where TResult : IDownloadable<TResult>
        {
            CTS?.Cancel();
            CTS = new CancellationTokenSource();
            TResult createdObject = TResult.Create(url, this);
            await createdObject.UpdateAsync(CTS?.Token);
            return createdObject;
        }

        public async Task<TResult[]> LoadNewAsync<TResult>() where TResult : IDownloadable<TResult> => await LoadExtraAsync<TResult>("New");

        public async Task<TResult[]> LoadExtraAsync<TResult>(string name) where TResult : IDownloadable<TResult>
        {
            string? selector = GetValue<Dictionary<string, string>>("Extra")?.GetValueOrDefault(name + typeof(TResult).Name + "Query");
            if (string.IsNullOrWhiteSpace(selector))
                return Array.Empty<TResult>();
            await GetStatusTask();
            TResult[] result = Array.Empty<TResult>();
            if (IsOnline)
                await new OwnRequest(async DToken =>
                {
                    HttpRequestMessage? msg = new(HttpMethod.Get, GetValue<Dictionary<string, string>>("Extra")?.GetValueOrDefault(name + typeof(TResult).Name + "Pattern")?.Replace("[url]", RedirectedUrl) ?? RedirectedUrl);
                    HttpResponseMessage res = await HttpGet.HttpClient.SendAsync(msg, HttpCompletionOption.ResponseContentRead, DToken);
                    if (!res.IsSuccessStatusCode)
                        return false;
                    string htmls = await res.Content.ReadAsStringAsync(DToken);
                    IElement? html = await htmls.ToIElementAsync();
                    if (html == null)
                        return false;
                    string[] links = selector.ParseAll(html);
                    res.Dispose();
                    result = new TResult[links.Length];
                    for (int i = 0; i < result.Length; i++)
                        result[i] = TResult.Create(links[i], this);
                    return true;
                }).Task;
            return result;
        }
    }
}
