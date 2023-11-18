using AngleSharp.Dom;
using DownloadAssistant.Base;
using Requests;
using Requests.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using WebsiteScraper.Downloadable;

namespace WebsiteScraper.WebsiteUtilities
{

    public partial class Website
    {
        public string SearchString
        {
            get => _searchString; init
            {
                _searchString = value;
                ImplementsSites = value.Contains("[page]") || value.Contains("[page as 0]");
            }
        }
        private string _searchString = string.Empty;
        public bool ImplementsSites { get; private set; }
        public bool ImplementsExcludeGenres { get; private set; }
        public string DirectSearchString { get; init; } = string.Empty;
        public char Seperator { get; init; }
        public string[] StatusSearch { get; init; } = Array.Empty<string>();
        public string[] TypeSearch { get; init; } = Array.Empty<string>();
        public string[] AuthorSearch { get; init; } = Array.Empty<string>();
        public bool CanSearchNext { get; private set; }
        public int ItemsPerSearch { get; set; } = 20;
        public Dictionary<string, (string notSet, string enabled)> TagSearch { get; init; } = new();
        public Dictionary<string, TagInfo> DisableTagSearch { get; init; } = new();
        private CancellationTokenSource? CTS { get; set; }

        private SearchInfo _lastSearch = new(string.Empty);
        private int _lastSearchSite = 1;
        private IEnumerable<object>? _searchRest;

        public async Task<TResult[]> SearchNextAsync<TResult>() where TResult : ISearchable<TResult>
        {
            List<TResult> result = new();
            if (string.IsNullOrWhiteSpace(_lastSearch.Search))
                return result.ToArray();
            result.AddRange(_searchRest?.Cast<TResult>() ?? Array.Empty<TResult>());
            _searchRest = null;

            if (result.Count < ItemsPerSearch && ImplementsSites)
                foreach (TResult? item in await SearchAsync<TResult>())
                    result.Add(item);
            CanSearchNext = true;
            if (result.Count <= ItemsPerSearch)
            {
                if (result.Count < ItemsPerSearch)
                    CanSearchNext = false;
                _searchRest = null;
                return result.ToArray();
            }
            else
            {
                _searchRest = (IEnumerable<object>?)result.GetRange(ItemsPerSearch, result.Count - ItemsPerSearch);
                return result.GetRange(0, ItemsPerSearch).ToArray();
            }
        }

        /// <summary>
        /// Search a website with a searchrequest
        /// </summary>
        /// <param name="search">search input</param>
        /// <param name="site">number of serched site</param>
        /// <returns>A array with all SearchMangas that found for this search Max Legth 40</returns>
        public async Task<TResult[]> SearchAsync<TResult>(SearchInfo search, int site = 1) where TResult : ISearchable<TResult>
        {
            _lastSearch = search;
            _lastSearchSite = site;
            CTS?.Cancel();
            CTS = new CancellationTokenSource();
            return await SearchAsync<TResult>();
        }

        public async Task<TResult[]> SearchAsync<TResult>(string search, int site = 1) where TResult : ISearchable<TResult> => await SearchAsync<TResult>(new SearchInfo(search), site);

        private async Task<TResult[]> SearchAsync<TResult>() where TResult : ISearchable<TResult>
        {
            await GetStatusTask();
            if (!IsOnline)
                return Array.Empty<TResult>();
            List<TResult> foundList = new();
            if (_lastSearch.IsDirect)
            {
                TResult result = TResult.Create(CreateDirectURL().ToString(), this);
                await result.UpdateAsync(CTS?.Token);
                _lastSearch = new(string.Empty);
                return new TResult[] { result };
            }
            await new OwnRequest(async (token) =>
            {
                StringBuilder genertedUrl = CreateSearchURL();
                do
                {
                    StringBuilder searchUrl = new(genertedUrl.ToString());
                    if (ImplementsSites)
                        searchUrl.Replace("[page]", _lastSearchSite++.ToString());
                    List<TResult> found = await DownloadSiteAndGetItemsAsync<TResult>(searchUrl.ToString(), token);
                    if (found.Count == 0)
                        break;
                    foundList.AddRange(found);
                    if (foundList.Count < 5)
                        break;
                } while (ImplementsSites && foundList.Count <= ItemsPerSearch);
                if (foundList.Count < ItemsPerSearch)
                    _lastSearch = new(string.Empty);
                return true;
            }, new()
            {
                CancellationToken = CTS?.Token,
                Priority = RequestPriority.High
            }).Task;
            if (foundList.Count == 0)
                return Array.Empty<TResult>();
            CanSearchNext = true;
            if (foundList.Count <= ItemsPerSearch)
            {
                if (foundList.Count < ItemsPerSearch)
                    CanSearchNext = false;
                _searchRest = null;
                return foundList.ToArray();
            }
            else
            {
                _searchRest = (IEnumerable<object>?)foundList.GetRange(ItemsPerSearch, foundList.Count - ItemsPerSearch);
                return foundList.GetRange(0, ItemsPerSearch).ToArray();
            }
        }
        /// <summary>
        /// Gets a List of all SearchMangas in website
        /// </summary>
        /// <param name="searchUrl">website url</param>
        /// <param name="t">CancelationToken to cancel download</param>
        /// <returns>A List with all SearchMangas in Website</returns>
        private async Task<List<TResult>> DownloadSiteAndGetItemsAsync<TResult>(string searchUrl, CancellationToken token) where TResult : ISearchable<TResult>
        {
            Debug.WriteLine(searchUrl);
            using HttpRequestMessage msg = new(HttpMethod.Get, searchUrl);
            using HttpResponseMessage res = await HttpGet.HttpClient.SendAsync(msg, token);

            if (!res.IsSuccessStatusCode)
                return new();
            IElement? html = await (await res.Content.ReadAsStringAsync(token)).ToIElementAsync();
            if (html == null)
                return new();
            List<TResult> found = new();
            string? containerSelector = InputDictionary[typeof(TResult).Name + "Search"].GetValueOrDefault("Items");
            foreach (IElement item in containerSelector?.GetAllElements(html) ?? Array.Empty<IElement>())
                found.Add(TResult.CreateSearch(item, this));
            return found;
        }


        /// <summary>
        /// Creates the Search string to search
        /// </summary>
        /// <param name="search">search inpur</param>
        /// <param name="url">website url</param>
        /// <returns>A StringBuilder object that contains the search URL</returns>
        private StringBuilder CreateSearchURL()
        {
            StringBuilder stringBuilder;

            if (_lastSearch.Search.Contains(RedirectedUrl))
                return new(_lastSearch.Search);
            else
                stringBuilder = new(SearchString);

            stringBuilder.Replace("[url]", RedirectedUrl);
            stringBuilder.Replace("[status]", StatusSearch[(int)_lastSearch.Status]);
            stringBuilder.Replace("[type]", TypeSearch[0]);
            if (_lastSearch.Author != null)
                stringBuilder.Replace("[author]", AuthorSearch?[0]);

            StringBuilder tags = new();
            foreach (KeyValuePair<string, DisableTagState> tag in _lastSearch.DisableTags)
            {
                if (DisableTagSearch.TryGetValue(tag.Key, out TagInfo value))
                {
                    tags.Append(tag.Value == DisableTagState.None ? value.Normal : (tag.Value == DisableTagState.Enabeld ? value.Include : value.Exclude));

                }
            }

            foreach (KeyValuePair<string, TagState> tag in _lastSearch.Tags)
            {
                if (TagSearch.TryGetValue(tag.Key, out (string notSet, string enabled) value))
                    tags.Append(tag.Value == TagState.None ? value.notSet : value.enabled);
            }
            stringBuilder.Replace("[tag]", tags.ToString());

            stringBuilder.Replace("[search]", _lastSearch.Search);
            stringBuilder.Replace(" ", "%20");
            return stringBuilder;
        }

        public static bool ProofArray(string[] strings) => strings != null && strings.Length > 0 && !strings[1..].Any(x => string.IsNullOrWhiteSpace(x));


        private StringBuilder CreateDirectURL()
        {
            string name = _lastSearch.Search.Length > 1 ? _lastSearch.Search[1..][..(-2 + _lastSearch.Search.Length)] : string.Empty;
            if (name.Contains(RedirectedUrl))
                return new(name);
            StringBuilder stringBuilder = new(DirectSearchString);
            stringBuilder.Replace("[url]", RedirectedUrl);
            stringBuilder.Replace("[id]", name);
            stringBuilder.Replace("[name]", name);
            stringBuilder.Replace("[page]", "0");
            stringBuilder.Replace(' ', Seperator);
            return stringBuilder;
        }
    }
    public class TagInfo
    {
        [JsonIgnore]
        public string[] Info { get => new string[] { Normal, Include, Exclude }; internal set { Include = value[0]; Exclude = value[1]; Normal = value[2]; } }
        public string Include { get; set; } = string.Empty;
        public string Exclude { get; set; } = string.Empty;
        public string Normal { get; set; } = string.Empty;
    }
}

