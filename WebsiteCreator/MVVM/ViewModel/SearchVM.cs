using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.Model;

namespace WebsiteCreator.MVVM.ViewModel
{
    internal partial class SearchVM : ViewModelObject, IProofable, IWebsiteParser
    {
        [ObservableProperty]
        private ObservableCollection<TagInfo> _disableTagList = new();
        [ObservableProperty]
        private ObservableCollection<TagInfo> _enableTagList = new();
        [ObservableProperty]
        private ObservableCollection<TagInfo> _textTagList = new();
        [ObservableProperty]
        private ObservableCollection<RadioTagInfo> _radioTagList = new();

        [ObservableProperty]
        private TagInfo _newDisableTag = new();
        [ObservableProperty]
        private TagInfo _newEnableTag = new();
        [ObservableProperty]
        private TagInfo _newTextTag = new();
        [ObservableProperty]
        private RadioTagInfo _newRadioTag = new();

        [ObservableProperty]
        private string? _searchPattern, _seperator, example;
        InfoVM _infoVM;
        public SearchVM(INavigationService navigation) : base(navigation)
        {
            PropertyChanged += SearchVM_PropertyChanged;
            _infoVM = GetService<InfoVM>();
            _infoVM.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_infoVM.RedirectedUrl)) UpdateExample(); };
        }

        private void SearchVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SearchPattern) or nameof(Seperator))
                UpdateExample();
        }

        private void UpdateExample()
        {
            if (string.IsNullOrWhiteSpace(SearchPattern))
                return;
            StringBuilder example = new(SearchPattern);
            example = example.Replace("[search]", "example input".Replace(" ", Seperator));
            example = example.Replace("[url]", _infoVM.RedirectedUrl ?? "[Not set]");
            if (new Regex("[[]page as ([0-9]+)[]]") is Regex reg && reg.Match(SearchPattern) is Match match && match.Success)
            {
                example = example.Replace($"[page as {match.Groups[1].Value}]", match.Groups[1].Value);
            }
            example = example.Replace("[page]", "1");
            int i = 0;
            //[url]/Search=[search]/p[page as 0]/dTags:[disable_tags]/tags:[tags]/Author=[author][gender bender]
            foreach (ObservableCollection<TagInfo> collection in new ObservableCollection<TagInfo>[] { DisableTagList, EnableTagList, TextTagList })
            {
                StringBuilder tags = new();
                for (int j = 0; j < collection.Count; j++)
                {
                    if (SearchPattern.Contains($"[{collection[j].Key}]"))
                        example.Replace($"[{collection[j].Key}]", collection[j].Second);
                    else if ((j % 2) == 0)
                        tags.Append(collection[j].Second);
                    else
                        tags.Append(collection[j].First);
                }
                if (i == 0)
                {
                    if (SearchPattern.Contains("[disable_tags]"))
                        example.Replace("[disable_tags]", tags.ToString());
                    else if (tags.Length > 0) example.Append("\nCould not find [disable_tags] tag in search pattern");
                }
                else if (i == 1)
                {
                    if (SearchPattern.Contains("[tags]"))
                        example.Replace("[tags]", tags.ToString());
                    else if (tags.Length > 0) example.Append("\nCould not find [tags] tag in search pattern");
                }
                else if (i == 2 && tags.Length > 0)
                {
                    foreach (TagInfo? item in collection.Where(x => !SearchPattern.Contains($"[{x.Key}]")))
                        example.Append($"\nCould not find [{item.Key}] tag in search pattern");
                }

                i++;
            }
            foreach (RadioTagInfo radioTag in RadioTagList)
            {
                StringBuilder radiotags = new();
                foreach (TagInfo tag in radioTag.TagList)
                {
                    if (tag == radioTag.Default)
                    {
                        if (SearchPattern.Contains($"[{tag.Key}]"))
                            example.Replace($"[{tag.Key}]", tag.Second);
                        else
                            radiotags.Append(tag.Second);
                    }
                    else
                    {
                        if (SearchPattern.Contains($"[{tag.Key}]"))
                            example.Replace($"[{tag.Key}]", tag.First);
                        else
                            radiotags.Append(tag.First);
                    }

                }
                if (radiotags.Length > 0 && !SearchPattern.Contains($"[{radioTag.Key}]"))
                {
                    foreach (TagInfo? item in radioTag.TagList.Where(x => !SearchPattern.Contains($"[{x.Key}]")))
                        example.Append($"\nCould not find [{item.Key}] tag in search pattern");
                    example.Append($"\n Or could not find [{radioTag.Key}] tag in search pattern");
                }
                example.Replace($"[{radioTag.Key}]", radiotags.ToString());
            }

            Example = example.ToString();
        }

        [RelayCommand]
        private void Back() => Navigation.NavigateTo<HomeVM>();

        [RelayCommand]
        private void EnableDelete(TagInfo tagInfo) => Delete(EnableTagList, tagInfo);
        [RelayCommand]
        private void DisableDelete(TagInfo tagInfo) => Delete(DisableTagList, tagInfo);
        [RelayCommand]
        private void TextDelete(TagInfo tagInfo) => Delete(TextTagList, tagInfo);

        [RelayCommand]
        private void RadioDelete(RadioTagInfo tagInfo)
        {
            RadioTagList.Remove(tagInfo);
            UpdateExample();
        }

        [RelayCommand]
        private void EnableAdd(TagInfo tagInfo) { if (AddTag(tagInfo, EnableTagList)) NewEnableTag = new(); else OnPropertyChanged(nameof(NewEnableTag)); }
        [RelayCommand]
        private void DisableAdd(TagInfo tagInfo) { if (AddTag(tagInfo, DisableTagList)) NewDisableTag = new(); else OnPropertyChanged(nameof(NewDisableTag)); }
        [RelayCommand]
        private void TextAdd(TagInfo tagInfo) { if (AddTag(tagInfo, TextTagList)) NewTextTag = new(); else OnPropertyChanged(nameof(NewTextTag)); }

        [RelayCommand]
        private void RadioAdd(RadioTagInfo tagInfo)
        {
            if (RadioTagList.Any(x => x?.Key == tagInfo?.Key))
                return;
            RadioTagList.Add(tagInfo);
            UpdateExample();
            NewRadioTag = new();
        }

        private void Delete(ObservableCollection<TagInfo> collection, TagInfo tagInfo)
        {
            collection.Remove(tagInfo);
            UpdateExample();
        }

        private bool AddTag(TagInfo tagInfo, ObservableCollection<TagInfo> collection)
        {
            if (collection.Any(x => x?.Key == tagInfo?.Key))
                return false;
            collection.Add(tagInfo);
            UpdateExample();
            return true;
        }

        public virtual UsingState GetUsingState()
        {
            if (string.IsNullOrWhiteSpace(Example) || Example.Contains('\n'))
                return UsingState.NotFinished;
            if (DisableTagList.Any(x => string.IsNullOrWhiteSpace(x.Title) || string.IsNullOrWhiteSpace(x.Second) || string.IsNullOrWhiteSpace(x.Third)))
                return UsingState.NotFinished;
            if (EnableTagList.Any(x => string.IsNullOrWhiteSpace(x.Title) || string.IsNullOrWhiteSpace(x.Second)))
                return UsingState.NotFinished;
            if (TextTagList.Any(x => string.IsNullOrWhiteSpace(x.Title) || string.IsNullOrWhiteSpace(x.Second) || string.IsNullOrEmpty(x.Third)))
                return UsingState.NotFinished;
            if (RadioTagList.Any(x => string.IsNullOrWhiteSpace(x.Title) || x.TagList.Any(y => string.IsNullOrWhiteSpace(y.Title) || string.IsNullOrWhiteSpace(y.Second))))
                return UsingState.NotFinished;
            return UsingState.Ready;
        }

        public void AddToWebsite(NewWebsite website)
        {
            website.SearchPattern = SearchPattern;
            website.Seperator = Seperator;
            website.EnableTags = EnableTagList.Select(x => new EnableAbleTag(x)).ToArray();
            website.DisableTags = DisableTagList.Select(x => new DisableAbleTag(x)).ToArray();
            website.TextTags = TextTagList.Select(x => new TextTag(x)).ToArray();
            website.RadioTags = RadioTagList.Select(x => new RadioTag(x)).ToArray();
        }

        public void GetFromWebsite(NewWebsite website)
        {
            SearchPattern = website.SearchPattern;
            Seperator = website.Seperator;

            if (website.EnableTags != null)
                EnableTagList = new(website.EnableTags.Select(x => x.ToTagInfo()));
            if (website.DisableTags != null)
                DisableTagList = new(website.DisableTags.Select(x => x.ToTagInfo()));
            if (website.TextTags != null)
                TextTagList = new(website.TextTags.Select(x => x.ToTagInfo()));
            if (website.RadioTags != null)
                RadioTagList = new(website.RadioTags.Select(x => x.ToRadioTagInfo()));
        }
    }

}
