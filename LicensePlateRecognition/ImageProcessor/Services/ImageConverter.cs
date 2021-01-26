using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageProcessor.Models;
using System;
using System.Drawing;

namespace ImageProcessor.Services
{
    public interface IImageConverter
    {
        void ApplyFullCannyOperator(ImageContext imageContext, Settings settings);
    }

    public class ImageConverter : IImageConverter
    {
        public void ApplyFullCannyOperator(ImageContext imageContext, Settings settings)
        {
            var rgbImage = imageContext.OriginalBitmap.ToImage<Rgb, byte>();

            rgbImage._EqualizeHist();

            //Grayscale
            var processedImage = rgbImage.Convert<Gray, byte>();

            //Resize
            ResizeImage(imageContext, processedImage);

            //Gaussian
            processedImage = processedImage.SmoothGaussian(settings.KernelSize, settings.KernelSize, settings.Sigma, settings.Sigma);

            var (lower, upper) = GetThreshHold(processedImage, settings);

            //Canny
            processedImage = processedImage.Canny(lower, upper);

            imageContext.ProcessedImage = processedImage;
        }

        public static (double Lower, double Upper) GetAutomatedTreshold(Image<Gray, byte> image, double sigma = 0.33)
        {
            var median = image.GetAverage().Intensity;

            var lower = Math.Max(0, (1 - sigma) * median);
            var upper = Math.Min(255, (1 + sigma) * median);

            return (lower, upper);
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

        public static void SetContrast(Bitmap bmp, int threshold)
        {
            var lockedBitmap = new LockBitmap(bmp);
            lockedBitmap.LockBits();

            var contrast = Math.Pow((100.0 + threshold) / 100.0, 2);

            for (int y = 0; y < lockedBitmap.Height; y++)
            {
                for (int x = 0; x < lockedBitmap.Width; x++)
                {
                    var oldColor = lockedBitmap.GetPixel(x, y);
                    var red = ((((oldColor.R / 255.0) - 0.5) * contrast) + 0.5) * 255.0;
                    var green = ((((oldColor.G / 255.0) - 0.5) * contrast) + 0.5) * 255.0;
                    var blue = ((((oldColor.B / 255.0) - 0.5) * contrast) + 0.5) * 255.0;
                    if (red > 255) red = 255;
                    if (red < 0) red = 0;
                    if (green > 255) green = 255;
                    if (green < 0) green = 0;
                    if (blue > 255) blue = 255;
                    if (blue < 0) blue = 0;

                    var newColor = Color.FromArgb(oldColor.A, (int)red, (int)green, (int)blue);
                    lockedBitmap.SetPixel(x, y, newColor);
                }
            }
            lockedBitmap.UnlockBits();
        }
    }
}
