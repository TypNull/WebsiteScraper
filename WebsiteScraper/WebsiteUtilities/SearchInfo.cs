using WebsiteScraper.Downloadable;

namespace WebsiteScraper.WebsiteUtilities
{
    public enum DisableTagState
    {
        None = 0,
        Enabeld = 1,
        Disabled = 2,
    }
    public enum TagState
    {
        None = 0,
        Enabeld = 1,
    }

    public class SearchInfo
    {
        public SearchInfo(string search) => Search = search;
        public string Search { get; set; }
        public bool IsDirect => (Search.StartsWith('\'') && Search.EndsWith('\'')) || (Search.StartsWith('\"') && Search.EndsWith('\"'));
        public string? Author { get; set; }
        public Status Status { get; set; }
        public Dictionary<string, TagState> Tags { get; set; } = new();
        public Dictionary<string, DisableTagState> DisableTags { get; set; } = new();

    }
}
