namespace WebsiteScraper.WebsiteUtilities
{
    public enum EnableAbleState
    {
        NotSet,
        Enabled
    }

    public enum DisableAbleState
    {
        NotSet,
        Enabled,
        Disabled,
    }

    public record EnableAbleTag
    {
        public string Title
        {
            get => _title;
            set { _title = value.Trim(); Key = _title.ToLower(); }
        }
        private string _title = string.Empty;
        public string Key { get; private set; } = string.Empty;
        public string NotSet { get; init; } = string.Empty;
        public string Enabled { get; init; } = string.Empty;
        public EnableAbleState State { get; set; }
    }

    public record DisableAbleTag : EnableAbleTag
    {
        public string Disabled { get; init; } = string.Empty;
        public new DisableAbleState State { get; set; }
    }

    public record TextTag
    {
        public string Title
        {
            get => _title;
            set { _title = value.Trim(); Key = _title.ToLower(); }
        }
        private string _title = string.Empty;
        public string Key { get; private set; } = string.Empty;
        public string NotSet { get; init; } = string.Empty;
        public string InputPattern { get; init; } = string.Empty;
        public string Seperator { get; init; } = string.Empty;
        public string Input { get; set; } = string.Empty;
        public bool IsSet => !string.IsNullOrEmpty(Input);
    }

    public record RadioTag
    {
        public string Title
        {
            get => _title;
            init { _title = value.Trim(); Key = _title.ToLower(); }
        }
        private string _title = string.Empty;
        public string Key { get; private set; } = string.Empty;
        public string DefaultKey { get => _defaultKey; init { _defaultKey = value; EnabledKey = value; } }
        private string _defaultKey = string.Empty;
        public string EnabledKey
        {
            get => EnabledTag?.Key ?? "None"; set
            {
                if (EnabledTag != null)
                    EnabledTag.State = EnableAbleState.NotSet;
                EnabledTag = Tags.Where(x => x.Key == value).FirstOrDefault() ?? EnabledTag;
                if (EnabledTag != null)
                    EnabledTag.State = EnableAbleState.Enabled;
            }
        }
        public EnableAbleTag[] Tags
        {
            get => _tags; init
            {
                _tags = value;
                EnabledTag = Tags.Where(x => x.Key == EnabledKey).FirstOrDefault() ?? (Tags.FirstOrDefault() ?? EnabledTag);
                if (EnabledTag != null)
                    EnabledTag.State = EnableAbleState.Enabled;
            }
        }
        private EnableAbleTag[] _tags = Array.Empty<EnableAbleTag>();
        public EnableAbleTag? EnabledTag { get; set; }
    }
}
