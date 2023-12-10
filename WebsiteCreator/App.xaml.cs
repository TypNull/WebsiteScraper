using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WebsiteCreator.Core;
using WebsiteCreator.MVVM.ViewModel;
using WebsiteCreator.MVVM.ViewModel.Comic;

namespace WebsiteCreator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        public ServiceProvider ServiceProvider => _serviceProvider;
        internal static new App Current => (App)Application.Current;

        public App()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(provider => new MainWindow { DataContext = provider.GetRequiredService<MainWindowVM>() });

            AddViewModels(services);

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<Func<Type, ViewModelObject>>(provider => viewModelType => (ViewModelObject)provider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();

            _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<HomeVM>();

        }

        private static void AddViewModels(IServiceCollection services)
        {
            services.AddSingleton<MainWindowVM>();
            services.AddSingleton<HomeVM>();
            services.AddSingleton<InfoVM>();
            services.AddSingleton<SearchVM>();
            services.AddSingleton<ComicVM>();
            services.AddSingleton<ComicChapterVM>();
            services.AddSingleton<ComicHomeVM>();
            services.AddSingleton<ComicSearchVM>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _serviceProvider.GetRequiredService<MainWindow>().Show();
        }
    }
}
