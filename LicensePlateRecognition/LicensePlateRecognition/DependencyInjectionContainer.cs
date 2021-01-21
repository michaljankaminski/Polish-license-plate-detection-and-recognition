using ImageProcessor;
using ImageProcessor.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ConsoleApplication
{
    public class DependencyInjectionContainer
    {
        public static IServiceProvider Build()
        {
            return new ServiceCollection()
                .AddSingleton<IRectangleDetector, RectangleDetector>()
                .AddSingleton<IImageProcessing, ImageProcessing>()
                .AddScoped<IBitmapConverter, BitmapConverter>()
                .AddScoped<IFileInputOutputHelper, FileInputOutputHelper>()
                .BuildServiceProvider();
        }
    }
}
