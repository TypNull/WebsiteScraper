﻿using AngleSharp.Dom;
using DownloadAssistant.Base;
using DownloadAssistant.Request;
using Requests;
using Requests.Options;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WebsiteScraper.WebsiteUtilities;


namespace WebsiteScraper.Downloadable.Books
{
    public class Comic : BookObject, ISearchable<Comic>
    {
        public DateTime LastUpdated { get; set; }
        public Status Status { get; set; } = Status.None;
        public string[] AlternativeTitles { get; set; } = Array.Empty<string>();
        public Chapter[] Chapter { get; set; } = Array.Empty<Chapter>();

        [JsonIgnore]
        public Website HoldingWebsite { get; private init; }
        public Comic(string url, string title, Website website) : base(url, title) { HoldingWebsite = website; }
        protected Dictionary<string, string> WebsiteKeys => HoldingWebsite.InputDictionary[nameof(Comic)];

        protected bool _isSearchObject;
        public string? CoverUrl { get; set; }


        public string GetFileName()
        {
            string fileName = Replace(Title);
            return (fileName.Length > 40 ? fileName[..40] : fileName).Trim();
        }

        private static string Replace(string filename)
        {
            return Regex.Replace(filename, $"[^a-zA-Z0-9_{Regex.Escape("@&%$§={}!;^°´+~#")}]+", " ", RegexOptions.Compiled);
        }

        public static Comic CreateSearch(IElement html, Website website)
        {
            Dictionary<string, string>? searchId = website.InputDictionary.GetValueOrDefault(nameof(Comic) + "Search");
            string dateSelector = string.Empty;
            string format = searchId?.GetValueOrDefault(nameof(LastUpdated))?.GetDateFormat(out dateSelector!) ?? string.Empty;
            Comic comic = new(GetParsed(html, searchId?.GetValueOrDefault(nameof(Url))),
                GetParsed(html, searchId?.GetValueOrDefault(nameof(Title))), website)
            {
                Description = GetParsed(html, searchId?.GetValueOrDefault(nameof(Description))),
                AlternativeTitles = GetParsedArray(html, searchId?.GetValueOrDefault(nameof(AlternativeTitles)) ?? string.Empty).Select((x) => x.UnicodeToText()).ToArray(),
                Genres = GetParsedArray(html, searchId?.GetValueOrDefault(nameof(Genres)) ?? string.Empty).Select((x) => x.UnicodeToText()).ToArray(),
                LastUpdated = StringToDateTime(GetParsed(html, dateSelector), format),
                Status = StringToStatus(GetParsed(html, searchId?.GetValueOrDefault(nameof(Status)))),
                CoverUrl = GetParsed(html, searchId?.GetValueOrDefault("Cover")),
                _isSearchObject = true,
                HoldingWebsite = website
            };
            comic.Chapter = new Chapter[] { new(comic) { Url = GetParsed(html, searchId?.GetValueOrDefault("LastChapter")) } };
            return comic;
        }

        private static string[] GetParsedArray(IElement html, string? pattern) => pattern?.ParseAll(html) ?? Array.Empty<string>();

        private static string GetParsed(IElement html, string? pattern) => pattern?.Parse(html) ?? string.Empty;

        private void Update(IElement? html)
        {
            if (html == null)
                return;
            IElement? container = WebsiteKeys.GetValueOrDefault("Container")?.GetElement(html);
            if (container == null)
                return;
            Title = GetParsed(container, WebsiteKeys.GetValueOrDefault(nameof(Title)));
            List<Chapter> chapters = new();

            IElement[] chapterContainer = WebsiteKeys.GetValueOrDefault("ChapterContainer")?.GetAllElements(container) ?? Array.Empty<IElement>();
            string? chapterDateSelector = WebsiteKeys.GetValueOrDefault("ChapterDate");
            string format = chapterDateSelector.GetDateFormat(out chapterDateSelector);
            foreach (IElement chapter in chapterContainer)
            {
                _ = float.TryParse(GetParsed(chapter, WebsiteKeys.GetValueOrDefault("ChapterNumber")).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out float num);
                Chapter foundChapter = new(this)
                {
                    Url = GetParsed(chapter, WebsiteKeys.GetValueOrDefault("ChapterUrl")),
                    Title = GetParsed(chapter, WebsiteKeys.GetValueOrDefault("ChapterTitle")),
                    UploadDateTime = StringToDateTime(GetParsed(chapter, chapterDateSelector), format),
                    DownloadURL = GetParsed(chapter, WebsiteKeys.GetValueOrDefault("ChapterDownloadUrl")),
                    Number = num
                };
                chapters.Add(foundChapter);
            }
            if (WebsiteKeys.GetValueOrDefault("OrderOfLinks") == "0")
                chapters.Sort(new ChapterComparer());
            else if (WebsiteKeys.GetValueOrDefault("OrderOfLinks") == "2")
                chapters.Reverse();
            for (int i = 0; i < chapters.Count; i++)
                chapters[i].SetOrder(1 + i);

            string? dateSelector = WebsiteKeys.GetValueOrDefault(nameof(LastUpdated));
            format = dateSelector.GetDateFormat(out dateSelector);
            Description = RemoveHTML(GetParsed(html, WebsiteKeys.GetValueOrDefault(nameof(Description))));
            Genres = GetParsedArray(html, WebsiteKeys[nameof(Genres)]).Select((x) => x.UnicodeToText()).ToArray();
            Author = GetParsed(container, WebsiteKeys.GetValueOrDefault(nameof(Author)));
            LastUpdated = StringToDateTime(GetParsed(html, dateSelector), format);
            Status = StringToStatus(GetParsed(html, WebsiteKeys.GetValueOrDefault(nameof(Status))));

            AlternativeTitles = GetParsedArray(html, WebsiteKeys.GetValueOrDefault(nameof(AlternativeTitles))).Select((x) => x.UnicodeToText()).ToArray();

            Chapter = chapters.ToArray();
            _isSearchObject = false;
            CoverUrl = GetParsed(html, WebsiteKeys.GetValueOrDefault("Cover"));

            OnPropertyChanged("Update");
        }



        public override async Task UpdateAsync(CancellationToken? token = default)
        {
            if (Uri.IsWellFormedUriString(Url, UriKind.Absolute))
                await new OwnRequest(async (DToken) =>
                {
                    using HttpRequestMessage? msg = new(HttpMethod.Get, Url);
                    using HttpResponseMessage res = await HttpGet.HttpClient.SendAsync(msg, HttpCompletionOption.ResponseContentRead, DToken);

                    if (!res.IsSuccessStatusCode)
                        return false;
                    Update(await (await res.Content.ReadAsStringAsync(DToken)).ToIElementAsync());
                    return true;
                }, new()
                {
                    CancellationToken = token,
                    Priority = RequestPriority.High
                }).Task;
        }

        public static new Comic Create(string url, Website website)
        {
            Comic newComic = new(url, url, website);
            return newComic;
        }


        public async Task DownloadCoverAsync(string savePath, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(CoverUrl))
                return;
            if (File.Exists(Path.Combine(savePath, GetFileName() + ".cover")))
            {
                CoverPath = Path.Combine(savePath, GetFileName() + ".cover");
                OnPropertyChanged(nameof(CoverPath));
                return;
            }
            else
                await new GetRequest(CoverUrl, new()
                {
                    DirectoryPath = savePath,
                    Filename = $"{GetFileName()}.cover",
                    RequestCompleated = (request, path) =>
                    {
                        CoverPath = path ?? string.Empty;
                        OnPropertyChanged(nameof(CoverPath));
                    },
                    CancellationToken = token
                }).Task;
        }
    }
}
