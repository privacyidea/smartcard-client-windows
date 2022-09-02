using System;
using System.Windows;
using PISmartcardClient.Model;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider Services { get; }

        public new static App Current => (App)Application.Current;
        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IPrivacyIDEAService, PrivacyIDEAService>();
            services.AddSingleton<IPersistenceService, PersistenceService>();
            services.AddSingleton<IDeviceService, DeviceService>();
            services.AddSingleton<IUIDispatcher, UIDispatcher>();

            // Singleton VMs
            services.AddSingleton<MainVM>();
            services.AddSingleton<SettingsVM>();

            // Transient VMs
            services.AddTransient<YKMgmtKeyVM>();
            services.AddTransient<AuthVM>();
            services.AddTransient<PromptVM>();
            services.AddTransient<EnrollmentFormVM>();
            services.AddTransient<ConfirmationPromptVM>();
            services.AddTransient<LoadingVM>();
            services.AddTransient<PinVM>();

            return services.BuildServiceProvider();
        }
    }
}
