using System;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessor.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageProcessor.Helpers;
using ImageProcessor.Models.LicensePlate;

namespace ImageProcessor.Services
{
    public interface ILicensePlateAreaValidator
    {
        /// <summary>
        /// For each first layer potential license plate we are valtidating the histogram of the image. Each accepted image is also applying binary thresholding.
        /// </summary>
        /// <param name="imageContext"> ImageContext </param>
        void SetPotentialSecondLayerLicensePlates(ImageContext imageContext);

        IEnumerable<PotentialSecondLayerLicensePlate> GetPotentialSecondLayerLicensePlates(ImageContext imageContext);

        /// <summary>
        /// Get average of saturation [0] and value [1] channel averages.
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        IEnumerable<(ImageContext Image, float[] Averages)> GetHistogramAverages(IEnumerable<ImageContext> images);
    }

    public class LicensePlateAreaValidator : ILicensePlateAreaValidator
    {
        private readonly IImageCropper _imageCropper;

        public LicensePlateAreaValidator(IImageCropper imageCropper)
        {
            _imageCropper = imageCropper;
        }

        public void SetPotentialSecondLayerLicensePlates(ImageContext imageContext)
        {
            imageContext.PotentialSecondLayerLicensePlates = GetPotentialSecondLayerLicensePlates(imageContext).ToList();
        }

        public IEnumerable<PotentialSecondLayerLicensePlate> GetPotentialSecondLayerLicensePlates(ImageContext imageContext)
        {
            return imageContext.PotentialFirstLayerLicensePlates.Where(IsLicensePlate).Select(GetConvertedImage);
        }

        public IEnumerable<(ImageContext Image, float[] Averages)> GetHistogramAverages(IEnumerable<ImageContext> images)
        {
            foreach (var image in images)
            {
                var croppedImage = GetCroppedImage(image.OriginalBitmap);
                var avgs = GetHistogramAverages(croppedImage);

                image.ProcessedImage = croppedImage.Convert<Gray, byte>();

                yield return (image, new[] { avgs.SatAverage, avgs.ValAverage });
            }
        }

        private Image<Hsv, byte> GetCroppedImage(Bitmap image)
        {
            var rectangle = new Rectangle(
                Convert.ToInt32(0.4 * image.Width),
                Convert.ToInt32(0.4 * image.Height),
                Convert.ToInt32(0.3 * image.Width),
                Convert.ToInt32(0.3 * image.Height));

            return _imageCropper.CropImage(image, rectangle).ToImage<Hsv, byte>();
        }

        
        private (float SatAverage, float ValAverage) GetHistogramAverages(Image<Hsv, byte> image)
        {
            image._EqualizeHist();

            var hsvPlanes = new VectorOfMat();
            CvInvoke.Split(image, hsvPlanes);

            var histSize = new[] { 256 };
            var range = new[] { 0f, 255f };
            var accumulate = false;

            var sHist = new Mat();
            var vHist = new Mat();

            CvInvoke.CalcHist(hsvPlanes, new[] { 1 }, new Mat(), sHist, histSize, range, accumulate);
            CvInvoke.CalcHist(hsvPlanes, new[] { 2 }, new Mat(), vHist, histSize, range, accumulate);

            var sAvg = GetHistogramAverage(sHist);
            var vAvg = GetHistogramAverage(vHist);

            return (sAvg, vAvg);
        }

        private bool IsLicensePlate(PotentialFirstLayerLicensePlate licensePlate)
        {
            var croppedImage = GetCroppedImage(licensePlate.Image.ToBitmap());

            var avgs = GetHistogramAverages(croppedImage);

            return IsSaturationInRange(avgs.SatAverage) && IsValueInRange(avgs.ValAverage);
        }

        private static bool IsSaturationInRange(float saturationAverage)
        {
            // Accepted staturation average range
            return saturationAverage > 120 && saturationAverage < 145;
        }

        private static bool IsValueInRange(float valueAverage)
        {
            // Accepted value average range
            return valueAverage > 100 && valueAverage < 200;
        }

        // Histogram average calculation per single channel
        private static float GetHistogramAverage(Mat mat)
        {
            var sumWeight = 0f;
            var sum = 0f;
            var values = mat.GetData();

            for (int i = 0; i < values.Length; i++)
            {
                var value = (float)mat.GetData().GetValue(i, 0);

                if (!float.IsNaN(value))
                {
                    sumWeight += value;
                    sum += i * value;
                }
            }

            return sumWeight == 0 ? 0 : sum / sumWeight;
        }

        private PotentialSecondLayerLicensePlate GetConvertedImage(PotentialFirstLayerLicensePlate licensePlate)
        {
            using var bitMap = licensePlate.Image.ToBitmap();

            ImageConverter.SetContrast(bitMap, 15);

            var newImg = bitMap
                .ToImage<Hsv, byte>()
                .ThresholdBinary(new Hsv(360, 0, 100), new Hsv(0, 0, 255));

            return new PotentialSecondLayerLicensePlate(licensePlate, newImg);
        }
    }
}