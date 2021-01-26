using ImageProcessor.Models;
using ImageProcessor.Services;
using System.Drawing;
using System.Threading.Tasks;

namespace ImageProcessor
{
    public interface IImageProcessing
    {
        /// <summary>
        /// Process full license plate detection on images which are set in settings.ImagesPath. Images with license plate filled will be located in source destination.
        /// </summary>
        /// <param name="settings"> Settings for license plate detection. </param>
        void Process(Settings settings);
        /// <summary>
        /// Process full license plate detection
        /// </summary>
        /// <param name="filePath"> Full image path. File can be of type .jpg, .png or .bmp</param>
        /// <param name="settings"> Settings for license plate detection. </param>
        /// <returns> Bitmap with license plates </returns>
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
            var images = _fileInputOutputHelper.ReadImages(imagesPath, FileType.jpg);

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 16
            };

            Parallel.ForEach(images, options,(imageContext) =>
            {
                ProcessSingleImage(imageContext, settings);
                _fileInputOutputHelper.SaveImage(imageContext);
                imageContext.Dispose();
            });
            //    foreach (var imageContext in _fileInputOutputHelper.ReadImages(imagesPath, FileType.jpg))
            //{

            //}
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
