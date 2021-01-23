using System;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessor.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageProcessor.Helpers;

namespace ImageProcessor.Services
{
    public interface ILicensePlateDetector
    {
        IEnumerable<Image<Hsv, byte>> GetLicensePlateImages(ImageContext imageContext);

        IEnumerable<(ImageContext Image, float[] Averages)> GetHistogramAverages(IEnumerable<ImageContext> images);
    }

    public class LicensePlateDetector : ILicensePlateDetector
    {
        private readonly IImageCropper _imageCropper;

        public LicensePlateDetector(IImageCropper imageCropper)
        {
            _imageCropper = imageCropper;
        }

        public IEnumerable<Image<Hsv, byte>> GetLicensePlateImages(ImageContext imageContext)
        {
            return imageContext.PotentialLicensePlates.Where(IsLicensePlate).Select(GetConvertedImage);
        }

        public IEnumerable<(ImageContext Image, float[] Averages)> GetHistogramAverages(IEnumerable<ImageContext> images)
        {
            foreach (var image in images)
            {
                var section = new Rectangle(
                    Convert.ToInt32(0.3 * image.ProcessedBitmap.Width),
                    Convert.ToInt32(0.3 * image.ProcessedBitmap.Height),
                    Convert.ToInt32(0.4 * image.ProcessedBitmap.Width),
                    Convert.ToInt32(0.4 * image.ProcessedBitmap.Height));

                var rgbImage = _imageCropper.CropImage(image.ProcessedBitmap, section).ToImage<Rgb, byte>();

                var bgrPlanes = new VectorOfMat();
                CvInvoke.Split(rgbImage, bgrPlanes);

                var histSize = new[] { 256 };
                var range = new[] { 0f, 255f };
                var accumulate = false;

                var bHist = new Mat();
                var gHist = new Mat();
                var rHist = new Mat();

                CvInvoke.CalcHist(bgrPlanes, new[] { 0 }, new Mat(), rHist, histSize, range, accumulate);
                CvInvoke.CalcHist(bgrPlanes, new[] { 1 }, new Mat(), gHist, histSize, range, accumulate);
                CvInvoke.CalcHist(bgrPlanes, new[] { 2 }, new Mat(), bHist, histSize, range, accumulate);

                var rAvg = GetHistogramAttributes(rHist);
                var gAvg = GetHistogramAttributes(gHist);
                var bAvg = GetHistogramAttributes(bHist);

                image.GenericImage = rgbImage.Convert<Gray, byte>();

                yield return (image, new[] {bAvg.Average, gAvg.Average, rAvg.Average });
            }
        }

        private bool IsLicensePlate(Bitmap image)
        {
            var rectangle = new Rectangle(
                Convert.ToInt32(0.3 * image.Width),
                Convert.ToInt32(0.3 * image.Height),
                Convert.ToInt32(0.4 * image.Width),
                Convert.ToInt32(0.4 * image.Height));

            var cutImage = _imageCropper.CropImage(image, rectangle).ToImage<Rgb, byte>();
            var bgrPlanes = new VectorOfMat();
            CvInvoke.Split(cutImage, bgrPlanes);

            var histSize = new[] { 256 };
            var range = new[] { 0f, 255f};
            var accumulate = false;

            var bHist = new Mat();
            var gHist = new Mat();
            var rHist = new Mat();

            CvInvoke.CalcHist(bgrPlanes, new[] { 2 }, new Mat(), bHist, histSize, range, accumulate);
            CvInvoke.CalcHist(bgrPlanes, new[] { 1 }, new Mat(), gHist, histSize, range, accumulate);
            CvInvoke.CalcHist(bgrPlanes, new[] { 0 }, new Mat(), rHist, histSize, range, accumulate);

            var bAvg = GetHistogramAttributes(bHist);
            var gAvg = GetHistogramAttributes(gHist);
            var rAvg = GetHistogramAttributes(rHist);

            return IsMeanWhiteLike(bAvg.Average, bAvg.IsMostlyWhite) && IsMeanWhiteLike(gAvg.Average, gAvg.IsMostlyWhite) && IsMeanWhiteLike(rAvg.Average, rAvg.IsMostlyWhite);
        }

        private static bool IsMeanWhiteLike(float average, bool isMostlyWhite)
        {
            return (average > 110 && average < 205) && isMostlyWhite;
        }

        private static (float Average, bool IsMostlyWhite) GetHistogramAttributes(Mat mat)
        {
            const int lowerBoundy = 150;
            var sumOverLowerBoundry = 0;
            var sumWeight = 0f;
            var sum = 0f;
            var values = mat.GetData();

            for (int i = 0; i < values.Length; i++)
            {
                var value = (float) mat.GetData().GetValue(i, 0);
                sumWeight += value;
                sum += i * value;

                if (i >= lowerBoundy)
                {
                    sumOverLowerBoundry += Convert.ToInt32(value);
                }
            }

            return (sum / sumWeight, (sumOverLowerBoundry / sumWeight > 0.5f));
        }

        private Image<Hsv, byte> GetConvertedImage(Bitmap image)
        {
            //Readable for ocr - newImg.ThresholdBinary(new Hsv(255, 0, 100), new Hsv(0, 15, 255))
            var newImg = image.ToImage<Hsv, byte>();

            return newImg.ThresholdBinary(new Hsv(360, 0, 100), new Hsv(0, 15, 255));
        }
    }
}