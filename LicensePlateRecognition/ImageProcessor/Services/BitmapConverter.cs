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
        ImageContext ApplyFullCannyOperator(ImageContext imageContext, Settings settings);
    }

    public class BitmapConverter : IBitmapConverter
    {
        private const int Width = 600;
        private const int Height = 450;

        public ImageContext ApplyFullCannyOperator(ImageContext imageContext, Settings settings)
        {
            //Grayscale
            imageContext.GenericImage = imageContext.ProcessedBitmap.ToImage<Gray, byte>();

            //Resize
            ResizeImage(imageContext);

            var thresholds = GetThreshHold(imageContext, settings);

            //Gaussian
            imageContext.GenericImage = imageContext.GenericImage.SmoothGaussian(settings.KernelSize, settings.KernelSize, settings.Sigma, settings.Sigma);

            //Canny
            imageContext.GenericImage = imageContext.GenericImage.Canny(thresholds.Lower, thresholds.Upper);

            return imageContext;
        }

        private static void ResizeImage(ImageContext imageContext)
        {
            SetResizeRatio(imageContext);

            CvInvoke.Resize(imageContext.GenericImage, imageContext.GenericImage, new Size(Width, Height), interpolation: Inter.Linear);
        }

        private static void SetResizeRatio(ImageContext imageContext)
        {
            var originalWidth = imageContext.GenericImage.Size.Width;
            var originalHeight = imageContext.GenericImage.Size.Height;

            imageContext.WidthResizeRatio = (double)originalWidth / Width;
            imageContext.HeightResizeRatio = (double)originalHeight / Height;
        }

        private static (double Lower, double Upper) GetThreshHold(ImageContext imageContext, Settings settings, double sigma = 0.33)
        {
            if (settings.UseAutoThreshold)
            {
                var median = imageContext.GenericImage.GetAverage().Intensity;

                var lower = Math.Max(0, (1 - sigma) * median);
                var upper = Math.Min(255, (1 + sigma) * median);

                return (lower, upper);
            }

            return (settings.LowThreshold, settings.HighThreshold);
        }
    }
}
