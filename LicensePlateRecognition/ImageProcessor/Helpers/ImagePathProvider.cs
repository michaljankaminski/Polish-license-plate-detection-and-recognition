using ImageProcessor.Models;

namespace ImageProcessor.Helpers
{
    public interface IImagePathProvider
    {
        string GetProcessedFullPath(ImageContext imageContext);
        string GetContoursFullPath(ImageContext imageContext);
        string GetPotentialLicensePlateFullPath(ImageContext imageContext, int number);
        string GetActualLicensePlateFullPath(ImageContext imageContext, int number);
    }

    public class ImagePathProvider : IImagePathProvider
    {
        public string GetProcessedFullPath(ImageContext imageContext) => @$"{imageContext.FolderPath}\Processed\{imageContext.FileName}_afterCanny.png";
        public string GetContoursFullPath(ImageContext imageContext) => @$"{imageContext.FolderPath}\Contours\{imageContext.FileName}_contours.png";

        public string GetPotentialLicensePlateFullPath(ImageContext imageContext, int number) => @$"{imageContext.FolderPath}\Potential\{imageContext.FileName}\{number}.png";
        public string GetActualLicensePlateFullPath(ImageContext imageContext, int number) => @$"{imageContext.FolderPath}\Actual\{imageContext.FileName}\{number}.png";
    }
}
