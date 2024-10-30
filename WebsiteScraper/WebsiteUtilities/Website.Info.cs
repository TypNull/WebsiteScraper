using DownloadAssistant.Requests;
using Requests.Options;
using System.Diagnostics;
using System.Text.Json;

namespace WebsiteScraper.WebsiteUtilities
{
    public partial class Website
    {
        public string Name { get; init; } = string.Empty;
        public string Suffix { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string RedirectedUrl { get; private set; } = string.Empty;
        public string HexColor { get; init; } = "#6eaf28";
        public string SavedPath { get; private set; } = string.Empty;
        public bool IsOnline { get; private set; }

        private Task _statusTask = Task.CompletedTask;

        public Website? LoadWebsite() => SavedPath != null ? LoadWebsite(SavedPath) : null;

        public static Website LoadWebsite(string path)
        {
            using FileStream openStream = File.OpenRead(path);
            Website? loadedWebsite = JsonSerializer.DeserializeAsync<Website>(openStream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, WriteIndented = true }).Result;
            if (loadedWebsite == null)
                throw new JsonException($"Could not convert path: {path}");
            loadedWebsite.SavedPath = path;
            loadedWebsite.GetWebsiteStatus();
            openStream.Flush();
            return loadedWebsite;
        }

        public void WaitOnStatus() => _statusTask.Wait();
        public Task GetStatusTask() => _statusTask;

        private void GetWebsiteStatus()
        {
            _statusTask = new StatusRequest(Url.StartsWith("http://") || Url.StartsWith("https://") ? Url : $"https://{Url}/", new()
            {
                Priority = RequestPriority.High,
                RequestCompleated = (request, response) =>
                {
                    HttpResponseMessage responseMessage = response ?? new();
                    Debug.WriteLine($"The Website: \"{Url}\" answered with »{responseMessage.RequestMessage?.RequestUri}« and Status as {responseMessage.StatusCode}");
                    RedirectedUrl = responseMessage.RequestMessage?.RequestUri?.ToString() ?? "";
                    IsOnline = responseMessage.StatusCode == System.Net.HttpStatusCode.OK;
                },
                RequestFailed = (request, response) => Debug.WriteLine($"The request from website: \"{Url}\" didn't succeed")
            }).Task;
        }
    }
}

