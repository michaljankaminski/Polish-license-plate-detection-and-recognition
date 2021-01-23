using ImageProcessor;
using ImageProcessor.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApplication
{
    internal class Program
    {
        private static void Main()
        {

            var serviceProvider = DependencyInjectionContainer.Build();
            var scope = serviceProvider.CreateScope();

            var settings = new Settings
            {
                ImagesPath = @"c:\dev\small"
            };
            scope.ServiceProvider.GetRequiredService<IImageProcessing>().Process(settings);

        }
    }
}
