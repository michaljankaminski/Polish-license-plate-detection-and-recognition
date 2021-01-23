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
        private readonly IBitmapConverter _bitmapConverter;
        private readonly IRectangleDetector _rectangleDetector;
        private readonly ILicensePlateDetector _licensePlateDetector;
        private readonly IImageCropper _imageCropper;

        public E2ETests()
        {
            _imagePathProvider = new ImagePathProvider();
            _fileInputOutputHelper = new FileInputOutputHelper(_imagePathProvider);
            _bitmapConverter = new BitmapConverter();
            _rectangleDetector = new RectangleDetector(_fileInputOutputHelper);
            _imageCropper = new ImageCropper();
            _licensePlateDetector = new LicensePlateDetector(_imageCropper);
        }

        [Fact]
        public void GetLicensesHistogramAverages()
        {
            var path = @"C:\dev\licenseplates";

            var images = _fileInputOutputHelper.ReadImages(path, FileType.png);

            var avgByImage = _licensePlateDetector.GetHistogramAverages(images).ToList();

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