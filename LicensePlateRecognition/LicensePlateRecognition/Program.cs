using ImageProcessor;
using ImageProcessor.Models;
using Microsoft.Extensions.DependencyInjection;
using Utils;

namespace ConsoleApplication
{
    internal class Program
    {
        private static void Main()
        {
            Logger.Log("Start");

            var serviceProvider = DependencyInjectionContainer.Build();
            var scope = serviceProvider.CreateScope();

            var settings = new Settings
            {
                ImagesPath = "D:/lp"
            };
            scope.ServiceProvider.GetRequiredService<IImageProcessing>().Process(settings);

            Logger.Log("Finished");
        }
    }
}
