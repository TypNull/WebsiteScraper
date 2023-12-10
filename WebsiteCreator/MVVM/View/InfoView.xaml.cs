using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using WebsiteCreator.MVVM.Model;
using WebsiteCreator.MVVM.ViewModel;

namespace WebsiteCreator.MVVM.View
{
    /// <summary>
    /// Interaktionslogik für InfoView.xaml
    /// </summary>
    public partial class InfoView : UserControl
    {
        public InfoView()
        {
            InitializeComponent();
            App.Current.ServiceProvider.GetRequiredService<InfoVM>().OpenFileDialog += OpenFile;
        }

        private void OpenFile(object? sender, System.EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Image files (*.jpg)(*.png)|*.jpg;*.jpeg;*.png;*.gif;";
            if (openFileDialog.ShowDialog() == true)
                ((InfoVM)DataContext).Logo = IOManager.LoadImageFromFile(openFileDialog.FileName);

        }

        private void Border_Drop(object sender, DragEventArgs e) => ((InfoVM)DataContext).ImageDrop(e);
    }
}
