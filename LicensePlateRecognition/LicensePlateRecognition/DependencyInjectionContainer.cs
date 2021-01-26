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
                .AddScoped<IImageCropper, ImageCropper>()
                .AddScoped<ILicensePlateReader, LicensePlateReader>()
                .AddScoped<ILicensePlateAreaDetector, LicensePlateAreaDetector>()
                .AddScoped<ILicensePlateAreaValidator, LicensePlateAreaValidator>()
                .AddScoped<ILicensePlateImageBuilder, LicensePlateImageBuilder>()
                .AddScoped<IImagePathProvider, ImagePathProvider>()
                .AddScoped<IImageConverter, ImageConverter>()
                .AddScoped<IFileInputOutputHelper, FileInputOutputHelper>()
                .BuildServiceProvider();
        }
    }
}
