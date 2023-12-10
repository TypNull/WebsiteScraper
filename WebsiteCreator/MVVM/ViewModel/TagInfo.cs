using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace WebsiteCreator.MVVM.ViewModel
{

    public partial class TagInfo : ObservableObject
    {
        [JsonIgnore]
        public string?[] Info { get => new string?[] { Title, First, Second, Third }; set { Title = value[0]; First = value[1]; Second = value[2]; Third = value[3]; } }
        public string? Title
        {
            get => _title;
            set { SetProperty(ref _title, value?.Trim()); Key = _title?.ToLower(); }
        }
        private string? _title;
        [ObservableProperty]
        private string? _key;
        [ObservableProperty]
        public string? _first;
        [ObservableProperty]
        public string? _second;
        [ObservableProperty]
        public string? _third;
    }

    enum DisableAbleState
    {
        NotSet,
        Enabled
    }
    enum EnableAbleState
    {
        NotSet,
        Enabled
    }


    public record EnableAbleTag
    {
        public EnableAbleTag(TagInfo tagInfo)
        {
            Title = tagInfo.Title;
            NotSet = tagInfo.First;
            Enabled = tagInfo.Second;
        }
        public EnableAbleTag() { }

        public string? Title { get; set; }
        public string? NotSet { get; set; }
        public string? Enabled { get; set; }

        public virtual TagInfo ToTagInfo()
        {
            return new()
            {
                Title = Title,
                First = NotSet,
                Second = Enabled,
            };
        }

    }

    public record DisableAbleTag : EnableAbleTag
    {
        public DisableAbleTag(TagInfo tagInfo) : base(tagInfo)
        {
            Disabled = tagInfo.Third;
        }
        public DisableAbleTag() { }

        public string? Disabled { get; set; }

        public override TagInfo ToTagInfo()
        {
            TagInfo taginfo = base.ToTagInfo();
            taginfo.Third = Disabled;
            return taginfo;
        }
    }

    public record TextTag
    {
        public TextTag(TagInfo tagInfo)
        {
            Title = tagInfo.Title;
            NotSet = tagInfo.First;
            InputPattern = tagInfo.Second;
            Seperator = tagInfo.Third;
        }
        public TextTag() { }

        public string? Title { get; set; }
        public string? NotSet { get; set; }
        public string? InputPattern { get; set; }
        public string? Seperator { get; set; }

        public TagInfo ToTagInfo()
        {
            return new()
            {
                Title = Title,
                First = NotSet,
                Second = InputPattern,
                Third = Seperator
            };
        }
    }

    public record RadioTag
    {
        public RadioTag(RadioTagInfo tagInfo)
        {
            Title = tagInfo.Title;
            DefaultKey = tagInfo.Default?.Key;
            Tags = tagInfo.TagList.Select(x => new EnableAbleTag(x)).ToArray();

        }
        public string? Title { get; set; }
        public string? DefaultKey { get; set; }
        public EnableAbleTag[]? Tags { get; set; }
        public RadioTag() { }

        public RadioTagInfo ToRadioTagInfo()
        {
            RadioTagInfo radioTagInfo = new()
            {
                Title = Title,
                TagList = new(Tags!.Select(x => x.ToTagInfo())),
                NewTag = new()
            };
            radioTagInfo.Default = radioTagInfo.TagList.Where(x => x.Key == DefaultKey).FirstOrDefault();
            return radioTagInfo;
        }
    }
}
