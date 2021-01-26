using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ImageProcessor;
using ImageProcessor.Services;
using ImageProcessor.Helpers;

namespace LicensePlatesRecognizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;
        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }
        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<IImageProcessing, ImageProcessing>();
            services.AddSingleton<IImageCropper, ImageCropper>();
            services.AddSingleton<ILicensePlateReader, LicensePlateReader>();
            services.AddScoped<ILicensePlateAreaDetector, LicensePlateAreaDetector>();
            services.AddScoped<ILicensePlateAreaValidator, LicensePlateAreaValidator>();
            services.AddScoped<IImagePathProvider, ImagePathProvider>();
            services.AddScoped<IImageConverter, ImageConverter>();
            services.AddScoped<ILicensePlateImageBuilder, LicensePlateImageBuilder>();
            services.AddScoped<IFileInputOutputHelper, FileInputOutputHelper>();
            services.AddSingleton<MainWindow>();
        }
        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.Show();
        }
    }
}
