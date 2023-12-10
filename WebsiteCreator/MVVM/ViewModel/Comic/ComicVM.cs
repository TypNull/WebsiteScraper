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
using System.Windows.Media.Imaging;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.Model;

namespace WebsiteCreator.MVVM.ViewModel.Comic
{
    internal partial class ComicVM : ViewModelObject, IProofable, IWebsiteParser
    {
        private readonly HtmlParser _parser = new();
        private IElement? _website;
        private IElement? _containerElement;
        private IElement? _chapterContainerElement;
        private InfoVM _infoVM;

        [ObservableProperty]
        private bool _showTxtSearchOutput;
        [ObservableProperty]
        private string? _directSearchPattern;
        [ObservableProperty]
        private string? _seperator;
        [ObservableProperty]
        private string? _example;
        [ObservableProperty]
        private string? sampleSearch;
        [ObservableProperty]
        private bool _showTxtDirectOutput;
        [ObservableProperty]
        private string? _containerQuery, _container;
        [ObservableProperty]
        private bool _comicIsAvailable, _isChapterVisible;
        [ObservableProperty]
        private string? _titleQuery, _title;
        [ObservableProperty]
        private string? _descriptionQuery, _description;
        [ObservableProperty]
        private string? _coverQuery, _cover;
        [ObservableProperty]
        public BitmapSource? _coverSource;
        [ObservableProperty]
        private string? _statusQuery, _status;
        [ObservableProperty]
        private string? _authorQuery, _author;
        [ObservableProperty]
        private string? _genresQuery, _genres;
        [ObservableProperty]
        private string? _alternativeTitlesQuery, _alternativeTitles;
        [ObservableProperty]
        private string? _dateQuery, _date;
        [ObservableProperty]
        private string? _chapterContainerQuery, _chapterContainer;
        [ObservableProperty]
        private string? _chapterLinkQuery, _chapterLink;
        [ObservableProperty]
        private string? _chapterDownloadLinkQuery, _chapterDownloadLink;
        [ObservableProperty]
        private string? _chapterTitleQuery, _chapterTitle;
        [ObservableProperty]
        private string? _chapterDateQuery, _chapterDate;
        [ObservableProperty]
        private string? _chapterNumberQuery, _chapterNumber;
        [ObservableProperty]
        private ObservableCollection<bool> _orderOfLinks = new(new bool[3]);

        public ComicVM(INavigationService navigation) : base(navigation)
        {
            PropertyChanged += ComicVM_PropertyChanged;
            _infoVM = GetService<InfoVM>();
            _infoVM.PropertyChanged += (s, e) => { if (e.PropertyName == "RedirectedUrl") OnPropertyChanged(nameof(DirectSearchPattern)); };
        }

        private void ComicVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.PropertyName);
            if (e.PropertyName is nameof(Seperator) or nameof(SampleSearch) or nameof(DirectSearchPattern))
                DownloadAndSaveHtmlString("Direct");
            else if (e.PropertyName?.EndsWith("Query") == true)
            {
                if (e.PropertyName == nameof(ContainerQuery))
                {
                    ParseMessage message = ElementParser.Parse(new(ContainerQuery, _website) { BaseAttribute = "html" });
                    Container = message.Message;
                    ComicIsAvailable = message.IsSuccess;
                    _containerElement = message.FoundElement;
                    ContainerChanged();
                }
                else if (e.PropertyName is (nameof(TitleQuery)) or (nameof(DescriptionQuery)) or nameof(StatusQuery) or nameof(AuthorQuery)
                    or nameof(ChapterLinkQuery) or nameof(ChapterDownloadLinkQuery) or nameof(ChapterTitleQuery))
                {
                    PropertyInfo? prop = typeof(ComicVM).GetProperty(e.PropertyName);
                    PropertyInfo? prop1 = typeof(ComicVM).GetProperty(e.PropertyName.Replace("Query", ""));
                    ParseMessage message = ElementParser.Parse(new(prop!.GetValue(this) as string, (prop.Name.Contains("Chapter") ? _chapterContainerElement : _containerElement)!) { Tag = prop1?.Name.ToLower() ?? string.Empty });
                    prop1?.SetValue(this, message.Message);
                }
                else if (e.PropertyName is (nameof(AlternativeTitlesQuery)) or (nameof(GenresQuery)))
                {
                    PropertyInfo? prop = typeof(ComicVM).GetProperty(e.PropertyName);
                    PropertyInfo? prop1 = typeof(ComicVM).GetProperty(e.PropertyName.Replace("Query", ""));
                    ParseMessage message = ElementParser.Parse(new(prop!.GetValue(this) as string, _containerElement!) { Tag = prop1?.Name.ToLower() ?? string.Empty, Seperator = ';' });
                    prop1?.SetValue(this, message.Message);
                }
                else if (e.PropertyName == nameof(CoverQuery))
                {
                    CoverSource = null;
                    Cover = ElementParser.Parse(new(CoverQuery, _containerElement!) { Raw = true }).Message;
                    GetCoverSource(Cover);
                }
                else if (e.PropertyName == nameof(ChapterContainerQuery))
                {
                    ParseMessage message = ElementParser.Parse(new(ChapterContainerQuery, _containerElement!) { BaseAttribute = "html" });
                    ChapterContainer = message.Message;
                    IsChapterVisible = message.IsSuccess;
                    _chapterContainerElement = message.FoundElement;
                    ChapterContainerChanged();
                }
                else if (e.PropertyName == nameof(DateQuery))
                    Date = ElementParser.ParseDate(new(DateQuery, _containerElement)).Message;
                else if (e.PropertyName == nameof(ChapterDateQuery))
                    ChapterDate = ElementParser.ParseDate(new(ChapterDateQuery, _chapterContainerElement)).Message;
                else if (e.PropertyName == nameof(ChapterNumberQuery))
                    ChapterNumber = ElementParser.ParseFloat(new(ChapterNumberQuery, _chapterContainerElement)).Message;

            }
        }

        private void ContainerChanged()
        {
            OnPropertyChanged(nameof(TitleQuery));
            OnPropertyChanged(nameof(DescriptionQuery));
            OnPropertyChanged(nameof(CoverQuery));
            OnPropertyChanged(nameof(StatusQuery));
            OnPropertyChanged(nameof(AuthorQuery));
            OnPropertyChanged(nameof(DateQuery));
            OnPropertyChanged(nameof(AlternativeTitlesQuery));
            OnPropertyChanged(nameof(GenresQuery));
            OnPropertyChanged(nameof(ChapterContainerQuery));
            ChapterContainerChanged();
        }
        private void ChapterContainerChanged()
        {
            OnPropertyChanged(nameof(ChapterLinkQuery));
            OnPropertyChanged(nameof(ChapterTitleQuery));
            OnPropertyChanged(nameof(ChapterDownloadLinkQuery));
            OnPropertyChanged(nameof(ChapterDateQuery));
            OnPropertyChanged(nameof(ChapterNumberQuery));
        }

        [RelayCommand]
        private void Back() => Navigation.NavigateTo<HomeVM>();

        [RelayCommand]
        private void SetChapterOrder(string input)
        {
            int j = int.Parse(input);
            for (int i = 0; i < OrderOfLinks.Count; i++)
                OrderOfLinks[i] = i == j ? true : false;
        }

        [RelayCommand]
        private void OpenHtml(string parameter) => Process.Start("notepad.exe", Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt");

        public async void DownloadAndSaveHtmlString(string parameter)
        {
            Example = null;
            ShowTxtSearchOutput = false;
            if (string.IsNullOrWhiteSpace(Seperator) || string.IsNullOrWhiteSpace(SampleSearch) || string.IsNullOrWhiteSpace(DirectSearchPattern))
                return;
            string url = DirectSearchPattern?
                        .Replace("[name]", SampleSearch?.Replace(" ", Seperator)).Replace("[id]", SampleSearch?.Replace(" ", Seperator))
                        .Replace("[url]", _infoVM.RedirectedUrl ?? "[Not set]") ?? string.Empty;
            if (parameter == "Direct")
                Example = url;

            string? html = await IOManager.LoadHtmlFromUrlAsync(url);
            if (html == null)
            {
                ShowTxtDirectOutput = false;
                return;
            }

            Directory.CreateDirectory(Path.GetTempPath() + @$"WebsiteCreator\");
            if (File.Exists(Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt"))
                File.Delete(Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt");
            await File.WriteAllTextAsync(Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt", html);
            if (parameter == "Direct")
                ShowTxtDirectOutput = true;
            _website = _parser.ParseDocument(html).DocumentElement;
            if (!string.IsNullOrWhiteSpace(ContainerQuery))
                OnPropertyChanged(nameof(ContainerQuery));
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
            if (string.IsNullOrWhiteSpace(DirectSearchPattern))
                return UsingState.NotUsed;
            List<string?> proofingStrong = new()
            {
                Seperator,
                Title,
                ChapterLink,
                ChapterNumber
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

        public void AddToWebsite(NewWebsite website)
        {
            Dictionary<string, string?>? comicDictionary = new()
            {
                { nameof(Seperator), Seperator },
                { nameof(DirectSearchPattern), DirectSearchPattern },
                { nameof(ContainerQuery), ContainerQuery },
                { nameof(TitleQuery), TitleQuery },
                { nameof(DescriptionQuery), DescriptionQuery },
                { nameof(CoverQuery), CoverQuery },
                { nameof(StatusQuery), StatusQuery },
                { nameof(AuthorQuery), AuthorQuery },
                { nameof(AlternativeTitlesQuery), AlternativeTitlesQuery },
                { nameof(GenresQuery), GenresQuery },
                { nameof(DateQuery), DateQuery },
                { nameof(OrderOfLinks), OrderOfLinks.IndexOf(true).ToString() },

                { nameof(ChapterContainerQuery), ChapterContainerQuery },
                { nameof(ChapterLinkQuery), ChapterLinkQuery },
                { nameof(ChapterTitleQuery), ChapterTitleQuery },
                { nameof(ChapterDownloadLinkQuery), ChapterDownloadLinkQuery },
                { nameof(ChapterNumberQuery), ChapterNumberQuery },
                { nameof(ChapterDateQuery), ChapterDateQuery }
            };
            website.SetValue("Comic", comicDictionary);
        }

        public void GetFromWebsite(NewWebsite website)
        {
            Dictionary<string, string>? comicDictionary = website.GetValue<Dictionary<string, string>>("Comic");
            if (comicDictionary == null)
                return;
            Seperator = comicDictionary.GetValueOrDefault(nameof(Seperator));
            DirectSearchPattern = comicDictionary.GetValueOrDefault(nameof(DirectSearchPattern));

            ContainerQuery = comicDictionary.GetValueOrDefault(nameof(ContainerQuery));
            TitleQuery = comicDictionary.GetValueOrDefault(nameof(TitleQuery));
            DescriptionQuery = comicDictionary.GetValueOrDefault(nameof(DescriptionQuery));
            CoverQuery = comicDictionary.GetValueOrDefault(nameof(CoverQuery));
            StatusQuery = comicDictionary.GetValueOrDefault(nameof(StatusQuery));
            AuthorQuery = comicDictionary.GetValueOrDefault(nameof(AuthorQuery));
            AlternativeTitlesQuery = comicDictionary.GetValueOrDefault(nameof(AlternativeTitlesQuery));
            GenresQuery = comicDictionary.GetValueOrDefault(nameof(GenresQuery));
            DateQuery = comicDictionary.GetValueOrDefault(nameof(DateQuery));
            bool result = int.TryParse(comicDictionary.GetValueOrDefault(nameof(OrderOfLinks)), out int i);
            OrderOfLinks[result ? i >= 0 ? i : 0 : 0] = true;

            ChapterContainerQuery = comicDictionary.GetValueOrDefault(nameof(ChapterContainerQuery));
            ChapterLinkQuery = comicDictionary.GetValueOrDefault(nameof(ChapterLinkQuery));
            ChapterTitleQuery = comicDictionary.GetValueOrDefault(nameof(ChapterTitleQuery));
            ChapterDownloadLinkQuery = comicDictionary.GetValueOrDefault(nameof(ChapterDownloadLinkQuery));
            ChapterNumberQuery = comicDictionary.GetValueOrDefault(nameof(ChapterNumberQuery));
            ChapterDateQuery = comicDictionary.GetValueOrDefault(nameof(ChapterDateQuery));
        }
    }
}
