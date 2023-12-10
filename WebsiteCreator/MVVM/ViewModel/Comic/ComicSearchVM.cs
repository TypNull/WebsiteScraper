using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Creator.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.Model;

namespace WebsiteCreator.MVVM.ViewModel.Comic
{
    partial class ComicSearchVM : ViewModelObject, IWebsiteParser, IProofable
    {
        InfoVM _infoVM;
        SearchVM _searchVM;
        private readonly HtmlParser _parser = new();
        private IElement? _website;
        private IElement? _comicContainerElement;

        [ObservableProperty]
        private bool _showInput;
        [ObservableProperty]
        private bool _showTxtOutput;
        [ObservableProperty]
        private string? _sampleSearch, _example;
        [ObservableProperty]
        private string? _containerQuery, _container;
        [ObservableProperty]
        private string? _titleQuery, _title;
        [ObservableProperty]
        private string? _linkQuery, _link;
        [ObservableProperty]
        private string? _descriptionQuery, _description;
        [ObservableProperty]
        private string? _genresQuery, _genres;
        [ObservableProperty]
        private string? _alternativeTitlesQuery, _alternativeTitles;
        [ObservableProperty]
        private string? _lastUpdatedQuery, _lastUpdated;
        [ObservableProperty]
        private string? _statusQuery, _status;
        [ObservableProperty]
        private string? _lastChapterLinkQuery, _lastChapterLink;
        [ObservableProperty]
        private string? _coverQuery, _cover;
        [ObservableProperty]
        public BitmapSource? _coverSource;
        public ComicSearchVM(INavigationService navigation) : base(navigation)
        {
            PropertyChanged += ComicSearchVM_PropertyChanged; ;
            _infoVM = GetService<InfoVM>();
            _infoVM.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(_infoVM.RedirectedUrl)) OnPropertyChanged(nameof(SampleSearch)); };
            _searchVM = GetService<SearchVM>();
            _searchVM.PropertyChanged += (s, e) => { if (e.PropertyName is nameof(_searchVM.Seperator) or nameof(_searchVM.SearchPattern)) OnPropertyChanged(nameof(SampleSearch)); };
        }

        private void ComicSearchVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.PropertyName);
            if (e.PropertyName is nameof(SampleSearch))
                DownloadAndSaveHtmlString("Comic");
            else if (e.PropertyName?.EndsWith("Query") == true)
            {
                if (e.PropertyName == nameof(ContainerQuery))
                {
                    ParseMessage message = ElementParser.Parse(new(ContainerQuery, _website) { BaseAttribute = "html" });
                    Container = message.Message;
                    ShowInput = message.IsSuccess;
                    _comicContainerElement = message.FoundElement;
                    ContainerChanged();
                }
                else if (e.PropertyName is nameof(TitleQuery) or nameof(DescriptionQuery) or nameof(StatusQuery))
                {
                    PropertyInfo? prop = typeof(ComicSearchVM).GetProperty(e.PropertyName);
                    PropertyInfo? prop1 = typeof(ComicSearchVM).GetProperty(e.PropertyName.Replace("Query", ""));
                    ParseMessage message = ElementParser.Parse(new(prop!.GetValue(this) as string, _comicContainerElement) { Tag = prop1?.Name.ToLower() ?? string.Empty });
                    prop1?.SetValue(this, message.Message);
                }
                else if (e.PropertyName is nameof(AlternativeTitlesQuery) or nameof(GenresQuery))
                {
                    PropertyInfo? prop = typeof(ComicSearchVM).GetProperty(e.PropertyName);
                    PropertyInfo? prop1 = typeof(ComicSearchVM).GetProperty(e.PropertyName.Replace("Query", ""));
                    ParseMessage message = ElementParser.Parse(new(prop!.GetValue(this) as string, _comicContainerElement) { Tag = prop1?.Name.ToLower() ?? string.Empty, Seperator = ';' });
                    prop1?.SetValue(this, message.Message);
                }
                else if (e.PropertyName is nameof(LastChapterLinkQuery) or nameof(LinkQuery))
                {
                    PropertyInfo? prop = typeof(ComicSearchVM).GetProperty(e.PropertyName);
                    PropertyInfo? prop1 = typeof(ComicSearchVM).GetProperty(e.PropertyName.Replace("Query", ""));
                    ParseMessage message = ElementParser.ParseLink(new(prop!.GetValue(this) as string, _comicContainerElement!) { Raw = true }, _infoVM.RedirectedUrl);
                    prop1?.SetValue(this, message.Message);
                }
                else if (e.PropertyName == nameof(CoverQuery))
                {
                    CoverSource = null;
                    Cover = ElementParser.Parse(new(CoverQuery, _comicContainerElement!) { Raw = true }).Message;
                    GetCoverSource(Cover);
                }
                else if (e.PropertyName == nameof(LastUpdatedQuery))
                    LastUpdated = ElementParser.ParseDate(new(LastUpdatedQuery, _comicContainerElement)).Message;
            }
        }

        public async void GetCoverSource(string url)
        {
            if (url == null)
                return;
            if (url.StartsWith('/'))
                url = _infoVM.RedirectedUrl + url.Remove(0, 1);
            else if (url.StartsWith("../"))
                url = _infoVM.RedirectedUrl + url.Remove(0, 3);
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
            {
                if (CoverSource != null)
                    CoverSource = null;
                return;
            }
            CoverSource = await IOManager.LoadImage(url);
        }

        public UsingState GetUsingState()
        {
            if (string.IsNullOrWhiteSpace(SampleSearch))
                return UsingState.NotUsed;
            List<string?> proofingStrong = new()
            {
                Title,
                Link,
                ContainerQuery
            };
            bool isFinished = false;
            isFinished = !proofingStrong.Any(x => !x?.Equals("[Not Set]") == false);
            PropertyInfo[] informations = GetType().GetProperties().Where(x => x.PropertyType == typeof(string)).ToArray();
            isFinished = isFinished && !informations.Any(x =>
            {
                if (x.GetValue(this) is string value)
                {
                    return value == string.Empty || value.EndsWith('!');
                }
                return false;
            });
            if (isFinished == false)
                return UsingState.NotFinished;
            return UsingState.Ready;
        }


        private void ContainerChanged()
        {
            OnPropertyChanged(nameof(TitleQuery));
            OnPropertyChanged(nameof(StatusQuery));
            OnPropertyChanged(nameof(CoverQuery));
            OnPropertyChanged(nameof(GenresQuery));
            OnPropertyChanged(nameof(AlternativeTitlesQuery));
            OnPropertyChanged(nameof(DescriptionQuery));
            OnPropertyChanged(nameof(LastChapterLinkQuery));
            OnPropertyChanged(nameof(LastUpdatedQuery));
            OnPropertyChanged(nameof(LinkQuery));
        }

        private void UpdateExample()
        {
            if (string.IsNullOrWhiteSpace(SampleSearch))
                return;
            if (string.IsNullOrWhiteSpace(_searchVM.SearchPattern))
                return;
            StringBuilder example = new(_searchVM.SearchPattern);
            example = example.Replace("[search]", SampleSearch.Replace(" ", _searchVM.Seperator));
            example = example.Replace("[url]", _infoVM.RedirectedUrl ?? "[Not set]");
            if (new Regex("[[]page as ([0-9]+)[]]") is Regex reg && reg.Match(_searchVM.SearchPattern) is Match match && match.Success)
            {
                example = example.Replace($"[page as {match.Groups[1].Value}]", match.Groups[1].Value);
            }
            example = example.Replace("[page]", "1");
            int i = 0;
            foreach (ObservableCollection<TagInfo> collection in new ObservableCollection<TagInfo>[] { _searchVM.DisableTagList, _searchVM.EnableTagList, _searchVM.TextTagList })
            {
                StringBuilder tags = new();
                for (int j = 0; j < collection.Count; j++)
                {
                    if (_searchVM.SearchPattern.Contains($"[{collection[j].Key}]"))
                        example.Replace($"[{collection[j].Key}]", collection[j].First);
                    else
                        tags.Append(collection[j].First);
                }
                if (i == 0)
                {
                    if (_searchVM.SearchPattern.Contains("[disable_tags]"))
                        example.Replace("[disable_tags]", tags.ToString());
                    else if (tags.Length > 0) example.Append("\nCould not find [disable_tags] tag in search pattern");
                }
                else if (i == 1)
                {
                    if (_searchVM.SearchPattern.Contains("[tags]"))
                        example.Replace("[tags]", tags.ToString());
                    else if (tags.Length > 0) example.Append("\nCould not find [tags] tag in search pattern");
                }
                else if (i == 2 && tags.Length > 0)
                {
                    foreach (TagInfo? item in collection.Where(x => !_searchVM.SearchPattern.Contains($"[{x.Key}]")))
                        example.Append($"\nCould not find [{item.Key}] tag in search pattern");
                }

                i++;
            }
            foreach (RadioTagInfo radioTag in _searchVM.RadioTagList)
            {
                StringBuilder radiotags = new();
                foreach (TagInfo tag in radioTag.TagList)
                {
                    if (_searchVM.SearchPattern.Contains($"[{tag.Key}]"))
                        example.Replace($"[{tag.Key}]", tag.First);
                    radiotags.Append(tag.First);
                }
                if (!string.IsNullOrEmpty(radiotags.ToString()) && !_searchVM.SearchPattern.Contains($"[{radioTag.Key}]"))
                {
                    foreach (TagInfo item in radioTag.TagList.Where(x => !_searchVM.SearchPattern.Contains($"[{x.Key}]")))
                        example.Append($"\nCould not find [{item.Key}] tag in search pattern");
                    example.Append($"\n Or could not find [{radioTag.Key}] tag in search pattern");
                }
                example.Replace($"[{radioTag.Key}]", radiotags.ToString());
            }

            Example = example.ToString();
        }

        public async void DownloadAndSaveHtmlString(string parameter)
        {
            if (string.IsNullOrWhiteSpace(SampleSearch))
                return;
            if (string.IsNullOrWhiteSpace(_searchVM.SearchPattern))
                return;
            UpdateExample();
            ShowTxtOutput = false;
            if (string.IsNullOrWhiteSpace(Example))
                return;

            string? html = await IOManager.LoadHtmlFromUrlAsync(Example);
            if (html == null)
            {
                ShowTxtOutput = false;
                return;
            }

            Directory.CreateDirectory(Path.GetTempPath() + @$"WebsiteCreator\");
            if (File.Exists(Path.GetTempPath() + @$"WebsiteCreator\search{parameter}html.txt"))
                File.Delete(Path.GetTempPath() + @$"WebsiteCreator\search{parameter}html.txt");
            await File.WriteAllTextAsync(Path.GetTempPath() + @$"WebsiteCreator\search{parameter}html.txt", html);
            ShowTxtOutput = true;
            _website = _parser.ParseDocument(html).DocumentElement;
            if (!string.IsNullOrWhiteSpace(ContainerQuery))
                OnPropertyChanged(nameof(ContainerQuery));
        }

        [RelayCommand]
        private void Back() => Navigation.NavigateTo<HomeVM>();

        [RelayCommand]
        private void OpenHtml(string parameter) => Process.Start("notepad.exe", Path.GetTempPath() + @$"WebsiteCreator\search{parameter}html.txt");

        public void AddToWebsite(NewWebsite website)
        {
            Dictionary<string, string?> comicDictionary = new()
            {
                { nameof(ContainerQuery), ContainerQuery },
                { nameof(LinkQuery), LinkQuery },
                { nameof(TitleQuery), TitleQuery },
                { nameof(DescriptionQuery), DescriptionQuery },
                { nameof(AlternativeTitlesQuery), AlternativeTitlesQuery },
                { nameof(GenresQuery), GenresQuery },
                { nameof(LastUpdatedQuery), LastUpdatedQuery },
                { nameof(CoverQuery), CoverQuery },
                { nameof(LastChapterLinkQuery), LastChapterLinkQuery },
                { nameof(StatusQuery), StatusQuery }
            };
            website.SetValue("ComicSearch", comicDictionary);
        }

        public void GetFromWebsite(NewWebsite website)
        {
            Dictionary<string, string>? comicDictionary = website.GetValue<Dictionary<string, string>>("ComicSearch");
            if (comicDictionary == null)
                return;
            ContainerQuery = comicDictionary.GetValueOrDefault(nameof(ContainerQuery));
            LinkQuery = comicDictionary.GetValueOrDefault(nameof(LinkQuery));
            TitleQuery = comicDictionary.GetValueOrDefault(nameof(TitleQuery));
            DescriptionQuery = comicDictionary.GetValueOrDefault(nameof(DescriptionQuery));
            AlternativeTitlesQuery = comicDictionary.GetValueOrDefault(nameof(AlternativeTitlesQuery));
            GenresQuery = comicDictionary.GetValueOrDefault(nameof(GenresQuery));
            LastUpdatedQuery = comicDictionary.GetValueOrDefault(nameof(LastUpdatedQuery));
            CoverQuery = comicDictionary.GetValueOrDefault(nameof(CoverQuery));
            LastChapterLinkQuery = comicDictionary.GetValueOrDefault(nameof(LastChapterLinkQuery));
            StatusQuery = comicDictionary.GetValueOrDefault(nameof(StatusQuery));
        }
    }
}
