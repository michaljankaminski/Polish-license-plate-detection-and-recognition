using ImageProcessor.Helpers;
using ImageProcessor.Models;
using ImageProcessor.Services;
using System.Linq;
using Xunit;

namespace ImageProcessorTests
{
    public class E2ETests
    {
        private readonly IFileInputOutputHelper _fileInputOutputHelper;
        private readonly IImagePathProvider _imagePathProvider;
        private readonly IImageConverter _imageConverter;
        private readonly ILicensePlateAreaDetector _licensePlateAreaDetector;
        private readonly ILicensePlateAreaValidator _licensePlateAreaValidator;
        private readonly IImageCropper _imageCropper;

        public E2ETests()
        {
            _imagePathProvider = new ImagePathProvider();
            _fileInputOutputHelper = new FileInputOutputHelper(_imagePathProvider);
            _imageConverter = new ImageConverter();
            _imageCropper = new ImageCropper();
            _licensePlateAreaDetector = new LicensePlateAreaDetector();
            _licensePlateAreaValidator = new LicensePlateAreaValidator(_imageCropper);
        }

        [Fact]
        public void GetLicensesHistogramAverages()
        {
            var path = @"C:\dev\licenseplates";

            var images = _fileInputOutputHelper.ReadImages(path, FileType.png);

            var avgByImage = _licensePlateAreaValidator.GetHistogramAverages(images).ToList();

            var lowestRedAvg = avgByImage.Min(x => x.Averages[0]);
            var lowestGreenAvg = avgByImage.Min(x => x.Averages[1]);
            var lowestBlueAvg = avgByImage.Min(x => x.Averages[2]);

            var highestRedAvg = avgByImage.Max(x => x.Averages[0]);
            var highestGreenAvg = avgByImage.Max(x => x.Averages[1]);
            var highestBlueAvg = avgByImage.Max(x => x.Averages[2]);

            int w = 1;

            foreach (var image in avgByImage)
            {
                _fileInputOutputHelper.SaveImage(image.Image);
            }
        }
    }
}