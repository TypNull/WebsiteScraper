namespace WebsiteScraper.WebsiteUtilities
{
    public class SearchInfo
    {
        public SearchInfo(string search, Website website)
        {
            Search = search;
            EnableAbleTags = website.EnableTags?.Select(x => x with { }).ToDictionary(x => x.Key) ?? new Dictionary<string, EnableAbleTag>();
            DisableAbleTags = website.DisableTags?.Select(x => x with { }).ToDictionary(x => x.Key) ?? new Dictionary<string, DisableAbleTag>();
            TextTags = website.TextTags?.Select(x => x with { }).ToDictionary(x => x.Key) ?? new Dictionary<string, TextTag>();
            RadioTags = website.RadioTags?.Select(x => x with { }).ToDictionary(x => x.Key) ?? new Dictionary<string, RadioTag>();
        }
        public string Search { get; set; }
        public bool IsDirect => (Search.StartsWith('\'') && Search.EndsWith('\'')) || (Search.StartsWith('\"') && Search.EndsWith('\"'));
        public IReadOnlyDictionary<string, TextTag> TextTags { get; }
        public IReadOnlyDictionary<string, EnableAbleTag> EnableAbleTags { get; }
        public IReadOnlyDictionary<string, DisableAbleTag> DisableAbleTags { get; }
        public IReadOnlyDictionary<string, RadioTag> RadioTags { get; }
    }
}
