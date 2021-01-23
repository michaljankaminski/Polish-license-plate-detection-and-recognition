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
                .AddSingleton<IPlateRecognizer, PlateRecognizer>()
                .AddScoped<IRectangleDetector, RectangleDetector>()
                .AddScoped<ILicensePlateDetector, LicensePlateDetector>()
                .AddScoped<IImagePathProvider, ImagePathProvider>()
                .AddScoped<IBitmapConverter, BitmapConverter>()
                .AddScoped<IFileInputOutputHelper, FileInputOutputHelper>()
                .BuildServiceProvider();
        }
    }
}
