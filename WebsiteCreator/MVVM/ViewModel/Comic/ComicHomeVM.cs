using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Creator.MVVM.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.Model;

namespace WebsiteCreator.MVVM.ViewModel.Comic
{
    partial class ComicHomeVM : ViewModelObject, IWebsiteParser, IProofable
    {
        private readonly HtmlParser _parser = new();
        [ObservableProperty]
        private IElement? _newElement;
        [ObservableProperty]
        private IElement? _recomElement;
        private InfoVM _infoVM;
        [ObservableProperty]
        private string? _newPattern, _newExample;
        [ObservableProperty]
        private string? _recomPattern, _recomExample;

        [ObservableProperty]
        private string? _newComicQuery, _newComic;
        [ObservableProperty]
        private string? _recomComicQuery, _recomComic;


        public ComicHomeVM(INavigationService navigation) : base(navigation)
        {
            _infoVM = GetService<InfoVM>();
            _infoVM.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(RecomPattern));
                OnPropertyChanged(nameof(NewPattern));
            };
            PropertyChanged += ComicHomeVM_PropertyChanged;
        }

        private void ComicHomeVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.PropertyName);
            if (e.PropertyName == nameof(RecomPattern))
            {
                if (RecomPattern == ".")
                    RecomPattern = "[Not Set]";
                else
                {

                    RecomExample = null;
                    DownloadAndSaveHtmlString("Recom");
                }
            }
            else if (e.PropertyName == nameof(NewPattern))
            {

                if (NewPattern == ".")
                    NewPattern = "[Not Set]";
                else
                {
                    NewExample = null;
                    DownloadAndSaveHtmlString("New");
                }
            }
            else if (e.PropertyName == nameof(RecomComicQuery))
            {
                if (RecomElement == null)
                    return;
                RecomComic = ElementParser.Parse(new(RecomComicQuery, RecomElement) { Tag = "Recommended Comics", Seperator = ';' }).Message;
            }
            else if (e.PropertyName == nameof(NewComicQuery))
            {
                if (NewElement == null)
                    return;
                NewComic = ElementParser.Parse(new(NewComicQuery, NewElement) { Tag = "Recommended Comics", Seperator = ';' }).Message;
            }
            else if (e.PropertyName == nameof(RecomElement))
                OnPropertyChanged(nameof(RecomComicQuery));
            else if (e.PropertyName == nameof(NewElement))
                OnPropertyChanged(nameof(NewComicQuery));
        }

        [RelayCommand]
        private void Back() => Navigation.NavigateTo<HomeVM>();

        [RelayCommand]
        private void OpenHtml(string parameter) => Process.Start("notepad.exe", Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt");

        private async void DownloadAndSaveHtmlString(string parameter)
        {
            if (string.IsNullOrWhiteSpace(_infoVM.RedirectedUrl))
                return;
            string? search = (parameter == "New" ? NewPattern : RecomPattern);
            if (string.IsNullOrWhiteSpace(search))
                return;
            search = search.Replace("[url]", _infoVM.RedirectedUrl);
            if (parameter == "New")
                NewExample = string.IsNullOrWhiteSpace(search) ? null : search;
            else
                RecomExample = string.IsNullOrWhiteSpace(search) ? null : search;
            search = await IOManager.LoadHtmlFromUrlAsync(search);
            if (string.IsNullOrWhiteSpace(search))
                return;
            try
            {
                Directory.CreateDirectory(Path.GetTempPath() + @$"NewWebsite\");
                if (File.Exists(Path.GetTempPath() + @$"NewWebsite\html{parameter}.txt"))
                    File.Delete(Path.GetTempPath() + @$"NewWebsite\html{parameter}.txt");

                await File.WriteAllTextAsync(Path.GetTempPath() + @$"NewWebsite\html{parameter}.txt", search);
            }
            catch (System.Exception)
            {

            }

            if (parameter == "New")
                NewElement = _parser.ParseDocument(search).DocumentElement;
            else
                RecomElement = _parser.ParseDocument(search).DocumentElement;
        }

        public UsingState GetUsingState()
        {
            if (string.IsNullOrWhiteSpace(GetService<ComicVM>().DirectSearchPattern))
                return UsingState.NotUsed;

            if ((NewPattern == "." || NewComic?.StartsWith("Found ") == true) && (RecomPattern == "." || RecomComic?.StartsWith("Found ") == true))
                return UsingState.Ready;

            return UsingState.NotFinished;
        }

        public void AddToWebsite(NewWebsite website)
        {
            Dictionary<string, string?> comicDictionary = new()
            {
                { "NewComicPattern", NewPattern },
                { "RecommendedComicPattern", RecomPattern },
                { nameof(NewComicQuery), NewComicQuery },
                { "RecommendedComicQuery", RecomComicQuery }
            };
            website.SetValue("Extra", comicDictionary);
        }

        public void GetFromWebsite(NewWebsite website)
        {
            Dictionary<string, string>? comicDictionary = website.GetValue<Dictionary<string, string>>("Extra");
            if (comicDictionary == null)
                return;

            NewPattern = comicDictionary.GetValueOrDefault("NewComicPattern");
            RecomPattern = comicDictionary.GetValueOrDefault("RecommendedComicPattern");
            NewComicQuery = comicDictionary.GetValueOrDefault(nameof(NewComicQuery));
            RecomComicQuery = comicDictionary.GetValueOrDefault("RecommendedComicQuery");

        }
    }
}
