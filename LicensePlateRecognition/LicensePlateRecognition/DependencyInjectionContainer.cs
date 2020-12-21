﻿using System;
using ImageProcessor;
using ImageProcessor.Services;
using ImageProcessor.Services.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApplication
{
    public class DependencyInjectionContainer
    {
        public static IServiceProvider Build()
        {
            return new ServiceCollection()
                .AddSingleton<IImageProcessing, ImageProcessing>()
                .AddScoped<IBitmapConverter, BitmapConverter>()
                .AddScoped<IFileInputOutputHelper, FileInputOutputHelper>()
                .AddScoped<IGaussianBlur, GaussianBlur>()
                .AddScoped<ISobelFilter, SobelFilter>()
                .BuildServiceProvider();
        }
    }
}
