using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.Model;
using WebsiteCreator.MVVM.ViewModel.Comic;

namespace WebsiteCreator.MVVM.ViewModel
{
    internal partial class HomeVM : ViewModelObject
    {
        [ObservableProperty]
        private string _infoText = "Not finished";
        [ObservableProperty]
        private string _searchText = "Not created";
        [ObservableProperty]
        private string _comicItemText = "Not created";
        [ObservableProperty]
        private string _comicSearchText = "Not created";
        [ObservableProperty]
        private string _comicHomeText = "Not created";
        [ObservableProperty]
        private string _comicChapterText = "Not created";

        [ObservableProperty]
        private bool _canExport;

        private InfoVM _infoVM;
        private ComicVM _comicVM;
        private ComicChapterVM _comicChapterVM;
        private ComicHomeVM _comicHomeVM;
        private ComicSearchVM _comicSearchVM;
        private SearchVM _searchVM;
        public event EventHandler<string>? OpenLoadDialog;
        public HomeVM(INavigationService navigation) : base(navigation)
        {
            _infoVM = GetService<InfoVM>();
            _comicVM = GetService<ComicVM>();
            _comicChapterVM = GetService<ComicChapterVM>();
            _comicHomeVM = GetService<ComicHomeVM>();
            _searchVM = GetService<SearchVM>();
            _comicSearchVM = GetService<ComicSearchVM>();
            Navigation.PropertyChanged += Navigation_PropertyChanged;
        }

        private void Navigation_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            List<UsingState> states = new();
            states.Add(_infoVM.GetUsingState());
            states.Add(_searchVM.GetUsingState());

            states.Add(_comicVM.GetUsingState());
            states.Add(_comicChapterVM.GetUsingState());
            states.Add(_comicHomeVM.GetUsingState());
            states.Add(_comicSearchVM.GetUsingState());
            var state =
            InfoText = states[0] == UsingState.Ready ? "Ready" : "Not finished";
            SearchText = GetUsingText(states[1]);
            ComicItemText = GetUsingText(states[2]);
            ComicChapterText = GetUsingText(states[3]);
            ComicHomeText = GetUsingText(states[4]);
            ComicSearchText = GetUsingText(states[5]);
            CanExport = !states.Any(x => x == UsingState.NotFinished);
        }

        private string GetUsingText(UsingState usingState) => usingState == UsingState.Ready ? "Ready" : usingState == UsingState.NotUsed ? "Not created" : "Not finished";

        [RelayCommand]
        private void ChangeView(string name)
        {
            if (Type.GetType($"WebsiteCreator.MVVM.ViewModel.{name}VM") is Type viewModel)
                Navigation.NavigateTo(viewModel);
        }

        [RelayCommand]
        private void Load() => OpenLoadDialog?.Invoke(this, "Load");

        [RelayCommand]
        private void Save() => OpenLoadDialog?.Invoke(this, "Save");

        [RelayCommand]
        private void Export() => OpenLoadDialog?.Invoke(this, "Export");

        [RelayCommand]
        private void SaveWebsite(string destination)
        {
            if (!destination.EndsWith(".wsfs"))
                destination += ".wsfs";
            NewWebsite newWebsite = new();
            _infoVM.AddToWebsite(newWebsite);
            _searchVM.AddToWebsite(newWebsite);
            _comicVM.AddToWebsite(newWebsite);
            _comicChapterVM.AddToWebsite(newWebsite);
            _comicHomeVM.AddToWebsite(newWebsite);
            _comicSearchVM.AddToWebsite(newWebsite);
            Debug.WriteLine("Excecute Save");
            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };
            using FileStream createStream = File.Create(destination);
            JsonSerializer.Serialize(createStream, newWebsite, options);

        }

        [RelayCommand]
        private void ExportWebsite(string destination)
        {
            if (!destination.EndsWith(".wsf"))
                destination += ".wsf";
            NewWebsite newWebsite = new();
            _infoVM.AddToWebsite(newWebsite);
            _searchVM.AddToWebsite(newWebsite);
            _comicVM.AddToWebsite(newWebsite);
            _comicChapterVM.AddToWebsite(newWebsite);
            _comicHomeVM.AddToWebsite(newWebsite);
            _comicSearchVM.AddToWebsite(newWebsite);
            Debug.WriteLine("Excecute Export");
            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };
            File.Delete(destination);
            var json = JsonSerializer.Serialize(newWebsite, options);
            json = new Regex("\": \"\\.\",").Replace(json, "\": null,");
            File.WriteAllText(destination, json);

        }


        [RelayCommand]
        private void ImportWebsite(string destinaion)
        {
            try
            {
                NewWebsite? imported = JsonSerializer.Deserialize<NewWebsite>(File.ReadAllText(destinaion));
                if (imported == null)
                    return;
                _infoVM.GetFromWebsite(imported);
                _searchVM.GetFromWebsite(imported);
                _comicVM.GetFromWebsite(imported);
                _comicChapterVM.GetFromWebsite(imported);
                _comicHomeVM.GetFromWebsite(imported);
                _comicSearchVM.GetFromWebsite(imported);
                Navigation_PropertyChanged(null, null!);
            }
            catch (Exception)
            {
            }

        }

    }

}

