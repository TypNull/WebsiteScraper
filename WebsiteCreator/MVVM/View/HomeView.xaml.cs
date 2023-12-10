using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows.Controls;
using WebsiteCreator.MVVM.ViewModel;

namespace WebsiteCreator.MVVM.View
{
    /// <summary>
    /// Interaktionslogik für HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        private HomeVM _vm;
        private static HomeView? _instance;
        public HomeView()
        {
            InitializeComponent();
            _vm = App.Current.ServiceProvider.GetRequiredService<HomeVM>();
            if (_instance != null)
                _vm.OpenLoadDialog -= _instance.OpenLoadDialog;
            _instance = this;
            _vm.OpenLoadDialog += OpenLoadDialog;
        }

        private void OpenLoadDialog(object? sender, string parameter)
        {
            try
            {
                switch (parameter)
                {
                    case "Load":
                        OpenFileDialog openFileDialog = new()
                        {
                            Title = "Open saved the Website data",
                            Filter = "Website|*.wsfs"
                        };

                        if (openFileDialog.ShowDialog() == true)
                            _vm.ImportWebsiteCommand.Execute(openFileDialog.FileName);
                        break;
                    case "Save":
                        SaveFileDialog saveFileDialog = new()
                        {
                            Title = "Save the Website",
                            Filter = "Website|*.wsfs"
                        };
                        if (saveFileDialog.ShowDialog() == true)
                            _vm.SaveWebsiteCommand.Execute(saveFileDialog.FileName);
                        break;
                    case "Export":
                        SaveFileDialog exportFileDialog = new()
                        {
                            Title = "Export the Website",
                            Filter = "Website|*.wsf"
                        };
                        if (exportFileDialog.ShowDialog() == true)
                            _vm.ExportWebsiteCommand.Execute(exportFileDialog.FileName);
                        break;
                }
            }
            catch
            {

            }
        }
    }
}
