using ImageProcessor.Models;
using ImageProcessor.Services;
using System.Drawing;

namespace ImageProcessor
{
    public interface IImageProcessing
    {
        void Process(Settings settings);
        Bitmap Process(string filePath, Settings settings = null);
    }

    public class ImageProcessing : IImageProcessing
    {
        private readonly IImageConverter _imageConverter;
        private readonly IFileInputOutputHelper _fileInputOutputHelper;
        private readonly ILicensePlateAreaDetector _licensePlateAreaDetector;
        private readonly ILicensePlateAreaValidator _licensePlateAreaValidator;
        private readonly ILicensePlateReader _licensePlateReader;
        private readonly ILicensePlateImageBuilder _licensePlateImageBuilder;

        public ImageProcessing(
            IImageConverter imageConverter,
            IFileInputOutputHelper fileInputOutputHelper, 
            ILicensePlateAreaDetector licensePlateAreaDetector, 
            ILicensePlateAreaValidator licensePlateAreaValidator,
            ILicensePlateReader licensePlateReader, 
            ILicensePlateImageBuilder licensePlateImageBuilder)
        {
            _imageConverter = imageConverter;
            _fileInputOutputHelper = fileInputOutputHelper;
            _licensePlateAreaDetector = licensePlateAreaDetector;
            _licensePlateAreaValidator = licensePlateAreaValidator;
            _licensePlateReader = licensePlateReader;
            _licensePlateImageBuilder = licensePlateImageBuilder;
        }

        public void Process(Settings settings)
        {
            var imagesPath = settings.ImagesPath;

            foreach (var imageContext in _fileInputOutputHelper.ReadImages(imagesPath, FileType.jpg))
            {
                ProcessSingleImage(imageContext, settings);
                _fileInputOutputHelper.SaveImage(imageContext);
                imageContext.Dispose();
            }
        }

        public Bitmap Process(string filePath, Settings settings = null)
        {
            if (settings == null)
            {
                settings = new Settings();
            }

            var imageContext = _fileInputOutputHelper.ReadImage(filePath);

            return ProcessSingleImage(imageContext, settings);
        }

        private Bitmap ProcessSingleImage(ImageContext imageContext, Settings settings)
        {
            _imageConverter.ApplyFullCannyOperator(imageContext, settings);
            _licensePlateAreaDetector.Detect(imageContext);
            _licensePlateAreaValidator.SetPotentialSecondLayerLicensePlates(imageContext);
            _licensePlateReader.RecognizePlate(imageContext, false);
            _licensePlateImageBuilder.Build(imageContext);

            return imageContext.ImageWithLicenses;
        }
    }
}
