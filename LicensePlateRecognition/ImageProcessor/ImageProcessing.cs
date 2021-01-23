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
        private readonly IImageConverter _imageConverter;
        private readonly IFileInputOutputHelper _fileInputOutputHelper;
        private readonly ILicensePlateAreaDetector _licensePlateAreaDetector;
        private readonly ILicensePlateAreaValidator _licensePlateAreaValidator;
        private readonly ILicensePlateReader _licensePlateReader;

        public ImageProcessing(
            IImageConverter imageConverter,
            IFileInputOutputHelper fileInputOutputHelper, 
            ILicensePlateAreaDetector licensePlateAreaDetector, 
            ILicensePlateAreaValidator licensePlateAreaValidator,
            ILicensePlateReader licensePlateReader)
        {
            _imageConverter = imageConverter;
            _fileInputOutputHelper = fileInputOutputHelper;
            _licensePlateAreaDetector = licensePlateAreaDetector;
            _licensePlateAreaValidator = licensePlateAreaValidator;
            _licensePlateReader = licensePlateReader;
        }

        public void Process(Settings settings)
        {
            var imagesPath = settings.ImagesPath;

            foreach (var image in _fileInputOutputHelper.ReadImages(imagesPath, FileType.jpg))
            {
                _imageConverter.ApplyFullCannyOperator(image, settings);
                _licensePlateAreaDetector.Detect(image);

                image.PotentialSecondLayerLicensePlates = _licensePlateAreaValidator.GetLicensePlateImages(image).ToList();

                _licensePlateReader.RecognizePlate(image, false);
                _fileInputOutputHelper.SaveImage(image, true);

                image.Dispose();
            }
        }
    }
}
