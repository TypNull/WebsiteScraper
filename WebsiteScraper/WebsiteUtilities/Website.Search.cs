using AngleSharp.Dom;
using DownloadAssistant.Base;
using Requests;
using Requests.Options;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WebsiteScraper.Downloadable;

namespace WebsiteScraper.WebsiteUtilities
{

    public partial class Website
    {
        public string SearchPattern
        {
            get => _searchPattern; init
            {
                _searchPattern = value;
                ImplementsSites = value.Contains("[page");
            }
        }
        private string _searchPattern = string.Empty;
        public bool ImplementsSites { get; private set; }
        public string Seperator { get; init; } = string.Empty;
        public bool CanSearchNext { get; private set; }
        public int ItemsPerSearch { get; set; } = 20;
        public EnableAbleTag[]? EnableTags { get; set; }
        public DisableAbleTag[]? DisableTags { get; set; }
        public TextTag[]? TextTags { get; set; }
        public RadioTag[]? RadioTags { get; set; }
        private CancellationTokenSource? CTS { get; set; }

        private SearchInfo? _lastSearch;
        private int _lastSearchSite = 1;
        private IEnumerable<object>? _searchRest;

        public async Task<TResult[]> SearchNextAsync<TResult>() where TResult : ISearchable<TResult>
        {
            List<TResult> result = new();
            if (string.IsNullOrWhiteSpace(_lastSearch?.Search))
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

        public async Task<TResult[]> SearchAsync<TResult>(string search, int site = 1) where TResult : ISearchable<TResult> => await SearchAsync<TResult>(new SearchInfo(search, this), site);

        private async Task<TResult[]> SearchAsync<TResult>() where TResult : ISearchable<TResult>
        {
            await GetStatusTask();
            if (!IsOnline)
                return Array.Empty<TResult>();
            List<TResult> foundList = new();
            if (_lastSearch!.IsDirect)
            {
                TResult result = TResult.Create(CreateDirectURL(typeof(TResult).Name).ToString(), this);
                await result.UpdateAsync(CTS?.Token);
                _lastSearch = new(string.Empty, this);
                return new TResult[] { result };
            }
            await new OwnRequest(async (token) =>
            {
                StringBuilder genertedUrl = CreateSearchURL();
                do
                {
                    StringBuilder searchUrl = new(genertedUrl.ToString());
                    string genrated = genertedUrl.ToString();
                    if (ImplementsSites)
                    {
                        if (genrated.Contains("[page]"))
                            searchUrl.Replace("[page]", _lastSearchSite++.ToString());
                        else
                        {
                            if (new Regex("[[]page as ([0-9]+)[]]") is Regex reg && reg.Match(genrated) is Match match && match.Success)
                            {
                                if (int.TryParse(match.Groups[1].Value, out int pagestart))
                                    searchUrl.Replace($"[page as {pagestart}]", (pagestart + _lastSearchSite++).ToString());
                                else
                                    searchUrl.Replace($"[page as {match.Groups[1].Value}]", _lastSearchSite++.ToString());
                            }
                        }
                    }

                    List<TResult> found = await DownloadSiteAndGetItemsAsync<TResult>(searchUrl.ToString(), token);
                    if (found.Count == 0)
                        break;
                    foundList.AddRange(found);
                    if (foundList.Count < 5)
                        break;
                } while (ImplementsSites && foundList.Count <= ItemsPerSearch);
                if (foundList.Count < ItemsPerSearch)
                    _lastSearch = new(string.Empty, this);
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
            Debug.WriteLine($"The website {searchUrl} is searched for {typeof(TResult).Name}");
            using HttpRequestMessage msg = new(HttpMethod.Get, searchUrl);
            using HttpResponseMessage res = await HttpGet.HttpClient.SendAsync(msg, token);

            if (!res.IsSuccessStatusCode)
                return new();
            IElement? html = await (await res.Content.ReadAsStringAsync(token)).ToIElementAsync();
            if (html == null)
                return new();
            List<TResult> found = new();
            string? containerSelector = GetValue<Dictionary<string, string>>(typeof(TResult).Name + "Search")?.GetValueOrDefault("ContainerQuery");
            foreach (IElement item in containerSelector?.GetAllElements(html) ?? Array.Empty<IElement>())
                found.Add(TResult.CreateSearch(item, this));
            Debug.WriteLine($"Search found {found.Count} items in website");
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
            if (_lastSearch == null)
                return new();

            if (_lastSearch.Search.Contains(RedirectedUrl))
                return new(_lastSearch.Search);
            else
                stringBuilder = new(SearchPattern);

            stringBuilder.Replace("[url]", RedirectedUrl);
            StringBuilder tags = new();
            foreach (EnableAbleTag tag in _lastSearch.EnableAbleTags.Values)
            {
                if (SearchPattern.Contains($"[{tag.Key}]"))
                {
                    if (tag.State == EnableAbleState.Enabled)
                        stringBuilder.Replace($"[{tag.Key}]", tag.Enabled);
                    else
                        stringBuilder.Replace($"[{tag.Key}]", tag.NotSet);
                }
                else tags.Append(tag.State == EnableAbleState.Enabled ? tag.Enabled : tag.NotSet);
            }
            stringBuilder.Replace("[tags]", tags.ToString());
            tags = new();
            StringBuilder en_tags = new();
            bool isEn_Tags = SearchPattern.Contains($"[enabled_tags]");
            StringBuilder ex_tags = new();
            bool isEx_Tags = SearchPattern.Contains($"[disabled_tags]");

            foreach (DisableAbleTag tag in _lastSearch.DisableAbleTags.Values)
            {
                if (SearchPattern.Contains($"[{tag.Key}]"))
                {
                    if (tag.State == DisableAbleState.Enabled)
                        stringBuilder.Replace($"[{tag.Key}]", tag.Enabled);
                    else if (tag.State == DisableAbleState.Disabled)
                        stringBuilder.Replace($"[{tag.Key}]", tag.Disabled);
                    else
                        stringBuilder.Replace($"[{tag.Key}]", tag.NotSet);
                }
                else if (tag.State == DisableAbleState.Enabled)
                {
                    if (isEn_Tags)
                        en_tags.Append(tag.Enabled);
                    else tags.Append(tag.Enabled);
                }
                else if (tag.State == DisableAbleState.Disabled)
                {
                    if (isEx_Tags)
                        ex_tags.Append(tag.Disabled);
                    else tags.Append(tag.Disabled);
                }
                else
                    tags.Append(tag.NotSet);
            }
            stringBuilder.Replace("[disable_tags]", tags.ToString());
            stringBuilder.Replace("[disabled_tags]", ex_tags.ToString());
            stringBuilder.Replace("[enabled_tags]", en_tags.ToString());

            foreach (TextTag tag in _lastSearch.TextTags.Values)
            {
                if (tag.IsSet)
                    stringBuilder.Replace($"[{tag.Key}]", tag.InputPattern.Replace("[input]", tag.Input.Replace(" ", tag.Seperator)));
                else
                    stringBuilder.Replace($"[{tag.Key}]", tag.NotSet);
            }
            foreach (RadioTag radioTag in _lastSearch.RadioTags.Values)
            {
                tags = new();
                foreach (EnableAbleTag tag in radioTag.Tags)
                {
                    if (SearchPattern.Contains($"[{tag.Key}]"))
                    {
                        if (radioTag.EnabledTag == tag)
                            stringBuilder.Replace($"[{tag.Key}]", tag.Enabled);
                        else
                            stringBuilder.Replace($"[{tag.Key}]", tag.NotSet);
                    }
                    else tags.Append(radioTag.EnabledTag == tag ? tag.Enabled : tag.NotSet);
                }
                stringBuilder.Replace($"[{radioTag.Key}]", tags.ToString());
            }

            stringBuilder.Replace("[search]", _lastSearch?.Search.Replace(" ", Seperator));
            return stringBuilder;
        }

        public static bool ProofArray(string[] strings) => strings != null && strings.Length > 0 && !strings[1..].Any(x => string.IsNullOrWhiteSpace(x));


        private StringBuilder CreateDirectURL(string type)
        {
            string name = _lastSearch?.Search.Length > 1 ? _lastSearch.Search[1..][..(-2 + _lastSearch.Search.Length)] : string.Empty;
            if (name.Contains(RedirectedUrl))
                return new(name);
            Dictionary<string, string>? dict = GetValue<Dictionary<string, string>>(type);
            if (dict == null)
                return new(null);
            StringBuilder stringBuilder = new(dict.GetValueOrDefault("DirectSearchPattern"));
            stringBuilder.Replace("[url]", RedirectedUrl);
            stringBuilder.Replace("[id]", name);
            stringBuilder.Replace("[name]", name);
            stringBuilder.Replace("[page]", "0");
            stringBuilder.Replace(" ", dict.GetValueOrDefault("Seperator"));
            return stringBuilder;
        }
    }
}

