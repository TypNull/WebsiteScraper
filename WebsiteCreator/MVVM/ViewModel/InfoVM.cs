using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadAssistant.Request;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.Model;

namespace WebsiteCreator.MVVM.ViewModel
{
    internal partial class InfoVM : ViewModelObject, IProofable, IWebsiteParser
    {
        [ObservableProperty]
        private BitmapSource? _logo;
        public event EventHandler? OpenFileDialog;
        [ObservableProperty]
        private string _hexColor = "#FF90EED6";
        [ObservableProperty]
        private string _foregroundColor = "#FF808080";
        [ObservableProperty]
        private string? _colorInput;
        [ObservableProperty]
        private string? _description;
        [ObservableProperty]
        private string? _name;
        [ObservableProperty]
        private string? _url;
        [ObservableProperty]
        private string? _redirectedUrl;
        [ObservableProperty]
        private bool _urlFormat;
        [ObservableProperty]
        private string? _websiteInformation;
        public InfoVM(INavigationService navigation) : base(navigation)
        {
            PropertyChanged += InfoVM_PropertyChanged;
        }

        private void InfoVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ColorInput))
            {
                if (Regex.Match(ColorInput!, "^#(?:[0-9a-fA-F]{3}){1,2}$").Success)
                {
                    HexColor = ColorInput!;
                    if (System.Drawing.ColorTranslator.FromHtml(HexColor).GetBrightness() > 0.4)
                        ForegroundColor = "#FF808080";
                    else ForegroundColor = "#FFD3D3D3";
                }
            }
            if (e.PropertyName == nameof(Url))
                ProofWebsite();

        }

        private void ProofWebsite()
        {
            UrlFormat = false;
            WebsiteInformation = null;
            RedirectedUrl = null;
            if (string.IsNullOrWhiteSpace(Url))
                return;
            Debug.WriteLine("Proof Website: " + Url);
            string url = string.Empty;
            if (Url.StartsWith("http://") || Url.StartsWith("https://"))
                url = Url;
            else url = "https://" + Url;
            StatusRequest req = new(url, new()
            {
                RequestCompleated = (request, response) =>
                {
                    string info = string.Empty;
                    response!.EnsureSuccessStatusCode();
                    RedirectedUrl = response.RequestMessage!.RequestUri!.ToString();
                    Debug.WriteLine($"The Website: {RedirectedUrl} is {response.StatusCode}");
                    info += $"Responsed Url: {RedirectedUrl}\n";
                    info += (response.StatusCode == System.Net.HttpStatusCode.OK ? "Is Online" : "Is Offline") + "\n";
                    info += $"Server: {(response.Headers.Server == null ? "[Not found]" : response.Headers.Server)}\n";
                    info += $"Location: {(response.Headers.Location == null ? "[Not found]" : response.Headers.Location)}";
                    WebsiteInformation = info;
                    UrlFormat = true;
                },
            });
        }

        [RelayCommand]
        private void LoadLogo() => OpenFileDialog?.Invoke(this, EventArgs.Empty);
        [RelayCommand]
        private void Back() => Navigation.NavigateTo<HomeVM>();

        public void ImageDrop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] ImageExtensions = { ".JPG", ".JPEG", ".BMP", ".GIF", ".PNG" };
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 1)
                    return;
                if (Array.Exists(ImageExtensions, x => x.Contains(Path.GetExtension(files[0]).ToUpperInvariant())))
                    Logo = IOManager.LoadImageFromFile(files[0]);
            }
        }

        public virtual UsingState GetUsingState()
        {
            if (SEmty(ForegroundColor) || SEmty(Description) || SEmty(Name) || SEmty(RedirectedUrl) || Logo == null)
                return UsingState.NotFinished;
            return UsingState.Ready;
        }

        private bool SEmty(string? s) => string.IsNullOrWhiteSpace(s);

        public void AddToWebsite(NewWebsite website)
        {
            website.Name = Name;
            website.Description = Description;
            website.Url = Url;
            website.Hexcolor = ColorInput;
            string? url = Url?.Split('?').FirstOrDefault();
            url = url?.Split('/')?.Last();
            website.Suffix = url?.Contains('.') == true ? url[url.LastIndexOf('.')..] : "" ?? string.Empty;
            website.Logo = IOManager.BitmapSourceToByte(Logo) ?? Array.Empty<byte>();
        }

        public void GetFromWebsite(NewWebsite website)
        {
            Name = website.Name;
            Description = website.Description;
            Url = website.Url;
            ColorInput = website.Hexcolor;
            Logo = BitmapFrame.Create(new MemoryStream(website.Logo ?? Array.Empty<byte>()));
        }
    }
}
