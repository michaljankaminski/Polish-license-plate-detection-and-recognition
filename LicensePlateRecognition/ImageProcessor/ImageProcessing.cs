using System.Linq;
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
        private readonly ILicensePlateDetector _licensePlateDetector;
        private readonly IPlateRecognizer _plateRecognizer;

        public ImageProcessing(
            IBitmapConverter bitmapConverter,
            IFileInputOutputHelper fileInputOutputHelper, 
            IRectangleDetector rectangleDetector, 
            ILicensePlateDetector licensePlateDetector,
            IPlateRecognizer plateRecognizer)
        {
            _bitmapConverter = bitmapConverter;
            _fileInputOutputHelper = fileInputOutputHelper;
            _rectangleDetector = rectangleDetector;
            _licensePlateDetector = licensePlateDetector;
            _plateRecognizer = plateRecognizer;
        }

        public void Process(Settings settings)
        {
            var imagesPath = settings.ImagesPath;

            foreach (var image in _fileInputOutputHelper.ReadImages(imagesPath, FileType.jpg))
            {
                _bitmapConverter.ApplyFullCannyOperator(image, settings);
                _rectangleDetector.DetectPlayGround(image);

                image.ActualLicensePlates = _licensePlateDetector.GetLicensePlateImages(image).ToList();

                _plateRecognizer.RecognizePlate(image, false);
                _fileInputOutputHelper.SaveImage(image, true);

                image.Dispose();
            }
        }
    }
}
