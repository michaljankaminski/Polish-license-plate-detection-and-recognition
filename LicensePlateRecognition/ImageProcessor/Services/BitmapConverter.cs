using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageProcessor.Models;
using System;
using System.Drawing;

namespace ImageProcessor.Services
{
    public interface IBitmapConverter
    {
        void ApplyFullCannyOperator(ImageContext imageContext, Settings settings);
    }

    public class BitmapConverter : IBitmapConverter
    {
        public void ApplyFullCannyOperator(ImageContext imageContext, Settings settings)
        {
            //Grayscale
            var processedImage = imageContext.ProcessedBitmap.ToImage<Gray, byte>();
            //Resize
            ResizeImage(imageContext, processedImage);
            var thresholds = GetThreshHold(processedImage, settings);
            //Gaussian
            processedImage = processedImage.SmoothGaussian(settings.KernelSize, settings.KernelSize, settings.Sigma, settings.Sigma);
            //Canny
            processedImage = processedImage.Canny(thresholds.Lower, thresholds.Upper);

            imageContext.GenericImage = processedImage;
        }

        private static void ResizeImage(ImageContext imageContext, Image<Gray, byte> image)
        {
            SetResizeRatio(imageContext, image);

            CvInvoke.Resize(image, image, new Size(Settings.ResizeWidth, Settings.ResizeHeight), interpolation: Inter.Linear);
        }

        private static void SetResizeRatio(ImageContext imageContext, Image<Gray,byte> image)
        {
            var originalWidth = image.Size.Width;
            var originalHeight = image.Size.Height;

            imageContext.WidthResizeRatio = (double)originalWidth / Settings.ResizeWidth;
            imageContext.HeightResizeRatio = (double)originalHeight / Settings.ResizeHeight;
        }

        private static (double Lower, double Upper) GetThreshHold(Image<Gray, byte> image, Settings settings, double sigma = 0.33)
        {
            return
                settings.UseAutoThreshold
                ? GetAutomatedTreshold(image, sigma)
                : (settings.LowThreshold, settings.HighThreshold);
        }
        public static (double Lower, double Upper) GetAutomatedTreshold(Image<Gray, byte> image, double sigma = 0.33)
        {
            var median = image.GetAverage().Intensity;

            var lower = Math.Max(0, (1 - sigma) * median);
            var upper = Math.Min(255, (1 + sigma) * median);

            return (lower, upper);
        }
    }
}
