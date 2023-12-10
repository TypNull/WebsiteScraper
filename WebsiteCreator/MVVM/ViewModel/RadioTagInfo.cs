using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace WebsiteCreator.MVVM.ViewModel
{
    public partial class RadioTagInfo : ObservableObject
    {
        public string? Key { get; private set; }
        public string? Title
        {
            get => _title;
            set { _title = value?.Trim(); Key = _title?.ToLower(); }
        }
        private string? _title;

        [ObservableProperty]
        private TagInfo? _default;

        [ObservableProperty]
        private TagInfo _newTag = new();
        [ObservableProperty]
        private ObservableCollection<TagInfo> _tagList = new();

        [RelayCommand]
        private void SetDefault(TagInfo taginfo) => Default = taginfo;


        [RelayCommand]
        private void Delete(TagInfo tagInfo)
        {
            if (tagInfo == Default)
                Default = TagList.FirstOrDefault();
            TagList.Remove(tagInfo);
        }
        public void Add()
        {
            if (TagList.Any(x => x?.Key == NewTag?.Key))
                return;
            TagList.Add(NewTag);
            if (Default == null)
                Default = NewTag;
            NewTag = new();

        }
    }

}
