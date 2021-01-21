using ImageProcessor.Models;
using ImageProcessor.Services;

namespace ImageProcessor
{
    public interface IImageProcessing
    {
        void Process(Settings settings);
    }

    public class ImageProcessing : IImageProcessing
    {
        private readonly IBitmapConverter _bitmapConverter;
        private readonly IFileInputOutputHelper _fileInputOutputHelper;
        private readonly IRectangleDetector _rectangleDetector;

        public ImageProcessing(
            IBitmapConverter bitmapConverter,
            IFileInputOutputHelper fileInputOutputHelper, 
            IRectangleDetector rectangleDetector)
        {
            _bitmapConverter = bitmapConverter;
            _fileInputOutputHelper = fileInputOutputHelper;
            _rectangleDetector = rectangleDetector;
        }

        public void Process(Settings settings)
        {
            var imagesPath = settings.ImagesPath;

            var context = new ImageProcessingContext();

            var images = _fileInputOutputHelper.ReadImages(imagesPath, FileType.jpg);

            foreach (var image in images)
            {
                _bitmapConverter.ApplyFullCannyOperator(image, settings);
                _rectangleDetector.DetectPlayGround(image);
                _fileInputOutputHelper.SaveImage(image, true);
            }
        }
    }
}
