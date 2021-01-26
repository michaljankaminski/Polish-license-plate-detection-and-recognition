using System;
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
            //path = @"C:\dev\notlicense";

            var images = _fileInputOutputHelper.ReadImages(path, FileType.png);

            var avgByImage = _licensePlateAreaValidator.GetHistogramAverages(images).ToList();

            var avgs = avgByImage.Select(x => x.Averages).Where(x=> x[0] > 0 && x[1] > 0).ToList();

            var lowestSatAvg = avgs.Min(x => x[0]);
            var meanSatAvg = avgs.Average(x => x[0]);
            var highestSatAvg = avgs.Max(x => x[0]);

            var lowestValueAvg = avgs.Min(x => x[1]);
            var meanValueAvg = avgs.Average(x => x[1]);
            var highestValueAvg = avgs.Max(x => x[1]);

            int w = 1;

            foreach (var image in avgByImage)
            {
                _fileInputOutputHelper.SaveImage(image.Image);
            }
        }
    }
}