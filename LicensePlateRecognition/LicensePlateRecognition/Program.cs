using System;
using System.IO;
using ImageProcessor;
using ImageProcessor.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApplication
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("No input data. Provide directory path.");
            }

            if (!Directory.Exists(args[0]))
            {
                throw new ArgumentException("No such directory.");
            }

            // Dependency injection
            var serviceProvider = DependencyInjectionContainer.Build();
            var scope = serviceProvider.CreateScope();

            var settings = new Settings
            {
                ImagesPath = args[0]
            };

            scope.ServiceProvider.GetRequiredService<IImageProcessing>().Process(settings);
        }
    }
}
