using AngleSharp.Html.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Creator.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.Model;

namespace WebsiteCreator.MVVM.ViewModel.Comic
{
    internal partial class ComicChapterVM : ViewModelObject, IWebsiteParser, IProofable
    {
        [ObservableProperty]
        private string? _chapterUrl;
        [ObservableProperty]
        private bool _showTxtOutputPaged;
        [ObservableProperty]
        private bool _showTxtOutputListed;
        [ObservableProperty]
        private string? _addToPagedUrl;
        [ObservableProperty]
        private string? _addToListUrl;
        [ObservableProperty]
        private string? _addToPagedUrlOutput;
        [ObservableProperty]
        private string? _addToListUrlOutput;
        [ObservableProperty]
        private string? _listImageQuery, _listImage;
        [ObservableProperty]
        private string? _pageImageQuery, _pageImage;

        private ComicVM _comicVM;

        public ComicChapterVM(INavigationService navigation) : base(navigation)
        {
            PropertyChanged += ComicChapterVM_PropertyChanged;
            _comicVM = GetService<ComicVM>();
            _comicVM.PropertyChanged += ComicVM_PropertyChanged;
        }

        private void ComicVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChapterLink" && _comicVM.ChapterLink != null)
            {
                string found = _comicVM.ChapterLink[(_comicVM.ChapterLink.IndexOf('"') + 1)..^1];
                bool result = Uri.TryCreate(found, UriKind.Absolute, out Uri? uriResult)
              && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                ChapterUrl = result ? found : null;
            }

        }

        private void ComicChapterVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChapterUrl))
            {
                OnPropertyChanged(nameof(AddToListUrl));
                OnPropertyChanged(nameof(AddToPagedUrl));
            }
            if (e.PropertyName == nameof(AddToListUrl))
            {
                if (AddToListUrl == ".")
                    AddToListUrlOutput = "[Not Set]";
                else
                    AddToListUrlOutput = ChapterUrl + AddToListUrl;
                DownloadAndSaveHtmlString("ImageListed");
            }
            else if (e.PropertyName == nameof(AddToPagedUrl))
            {
                if (AddToPagedUrl == ".")
                    AddToPagedUrlOutput = "[Not Set]";
                else
                    AddToPagedUrlOutput = (AddToPagedUrl?.Contains("[page]") == true ? "" : "Format not right! Have to contain [page]:\n") + ChapterUrl + AddToPagedUrl;
                DownloadAndSaveHtmlString("ImagePaged");
            }
            try
            {
                if (e.PropertyName == nameof(PageImageQuery))
                {
                    if (ChapterUrl == null)
                        return;
                    AngleSharp.Html.Dom.IHtmlDocument doc = new HtmlParser().ParseDocument(File.ReadAllText(Path.GetTempPath() + @$"WebsiteCreator\itemImagePagedhtml.txt"));
                    PageImage = ElementParser.Parse(new(PageImageQuery, doc.DocumentElement)
                    {
                        Raw = true
                    }).Message;

                    _ = ProofImageAsync(PageImage);
                }
                else if (e.PropertyName == nameof(ListImageQuery))
                {
                    if (ChapterUrl == null)
                        return;
                    AngleSharp.Html.Dom.IHtmlDocument doc = new HtmlParser().ParseDocument(File.ReadAllText(Path.GetTempPath() + @$"WebsiteCreator\itemImageListedhtml.txt"));
                    ListImage = ElementParser.Parse(new(ListImageQuery, doc.DocumentElement)
                    {
                        Seperator = ';',
                        Raw = true

                    }).Message;

                    string[] images = ListImage.Split(";\n");
                    images[^1] = images.Last().Remove(images.Last().Length - 1);
                    _ = ProofImageAsync(images);
                }
            }
            catch (Exception)
            {
            }

        }

        [RelayCommand]
        private void Back() => Navigation.NavigateTo<HomeVM>();

        [RelayCommand]
        private void OpenHtml(string parameter) => Process.Start("notepad.exe", Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt");

        private async Task ProofImageAsync(string url)
        {
            if (await IOManager.LoadImage(url) == null)
                PageImage = "No Image Found!: \n" + PageImage;
            else if (PageImage != "Regular Expression pattern is not valid!" && PageImage != "Nothing found!" && PageImage != "File error please try again to download the HTML")
                PageImage = "Image Found!: \n" + PageImage;
        }

        private async Task ProofImageAsync(string[] urls)
        {
            if (await IOManager.LoadImage(urls[0]) == null)
                ListImage = "No Images Found!: \n" + ListImage;
            else if (await IOManager.LoadImage(urls.Last()) == null)
                ListImage = "No Images Found!: \n" + ListImage;
            else if (await IOManager.LoadImage(urls[urls.Length / 2]) == null)
                ListImage = "No Images Found!: \n" + ListImage;
            else if (ListImage != "Regular Expression pattern is not valid!" && ListImage != "Nothing found!" && ListImage != "File error please try again to download the HTML")
                ListImage = "Images Found!: \n" + ListImage;
        }
        public async void DownloadAndSaveHtmlString(string parameter)
        {
            string? search = await IOManager.LoadHtmlFromUrlAsync((parameter == "ImagePaged" ? AddToPagedUrlOutput?.Replace("[page]", "1") : AddToListUrlOutput) ?? string.Empty);
            if (search == null)
                return;
            if (search.Length < 200)
            {
                if (parameter == "ImagePaged")
                    ShowTxtOutputPaged = false;
                else if (parameter == "ImageListed")
                    ShowTxtOutputListed = false;
                return;
            }
            Directory.CreateDirectory(Path.GetTempPath() + @$"WebsiteCreator\");
            if (File.Exists(Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt"))
                File.Delete(Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt");
            await File.WriteAllTextAsync(Path.GetTempPath() + @$"WebsiteCreator\item{parameter}html.txt", search);
            if (parameter == "ImagePaged")
            {
                ShowTxtOutputPaged = true;
                OnPropertyChanged(nameof(PageImageQuery));
            }

            else if (parameter == "ImageListed")
            {
                ShowTxtOutputListed = true;
                OnPropertyChanged(nameof(ListImageQuery));
            }
        }
        public UsingState GetUsingState()
        {
            if (string.IsNullOrWhiteSpace(_comicVM.DirectSearchPattern))
                return UsingState.NotUsed;
            if ((ListImage?.StartsWith("Images Found!: \n") == true || ListImage == ".") && (PageImage?.StartsWith("Image Found!: \n") == true || PageImage == "."))
                return UsingState.Ready;
            return UsingState.NotFinished;
        }

        public void AddToWebsite(NewWebsite website)
        {
            Dictionary<string, string?> comicDictionary = new()
            {
                { nameof(AddToPagedUrl), AddToPagedUrl },
                { nameof(AddToListUrl), AddToListUrl },
                { nameof(ListImageQuery), ListImageQuery },
                { nameof(PageImageQuery), PageImageQuery }
            };
            website.SetValue("ComicChapter", comicDictionary);
        }

        public void GetFromWebsite(NewWebsite website)
        {
            Dictionary<string, string>? comicDictionary = website.GetValue<Dictionary<string, string>>("ComicChapter");
            if (comicDictionary == null)
                return;

            AddToPagedUrl = comicDictionary.GetValueOrDefault(nameof(AddToPagedUrl));
            AddToListUrl = comicDictionary.GetValueOrDefault(nameof(AddToListUrl));
            ListImageQuery = comicDictionary.GetValueOrDefault(nameof(ListImageQuery));
            PageImageQuery = comicDictionary.GetValueOrDefault(nameof(PageImageQuery));

        }
    }
}
