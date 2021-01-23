using ImageProcessor;
using ImageProcessor.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using ImageProcessor.Helpers;

namespace ConsoleApplication
{
    public class DependencyInjectionContainer
    {
        public static IServiceProvider Build()
        {
            return new ServiceCollection()
                .AddSingleton<IImageProcessing, ImageProcessing>()
                .AddSingleton<IImageCropper, ImageCropper>()
                .AddSingleton<ILicensePlateReader, LicenseLicensePlateReader>()
                .AddScoped<ILicensePlateAreaDetector, LicensePlateAreaDetector>()
                .AddScoped<ILicensePlateAreaValidator, LicensePlateAreaValidator>()
                .AddScoped<IImagePathProvider, ImagePathProvider>()
                .AddScoped<IImageConverter, ImageConverter>()
                .AddScoped<IFileInputOutputHelper, FileInputOutputHelper>()
                .BuildServiceProvider();
        }
    }
}
