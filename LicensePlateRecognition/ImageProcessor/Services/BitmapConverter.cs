using Emgu.CV;
using Emgu.CV.Structure;
using ImageProcessor.Models;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageProcessor.Services
{
    public interface IBitmapConverter
    {
        ImageContext ApplyFullCannyOperator(ImageContext imageContext, Settings settings);
    }

    public class BitmapConverter : IBitmapConverter
    {
        public ImageContext ApplyFullCannyOperator(ImageContext imageContext, Settings settings)
        {
            imageContext.ProcessedBitmap = new Bitmap(imageContext.OriginalImage);

            //Grayscale
            imageContext.GenericImage = imageContext.ProcessedBitmap.ToImage<Gray, byte>();

            //Resize
            CvInvoke.Resize(imageContext.GenericImage, imageContext.GenericImage, new Size(600, 450));

            var thresholds = GetThreshHold(imageContext);

            //Gaussian
            imageContext.GenericImage = imageContext.GenericImage.SmoothGaussian(settings.KernelSize, settings.KernelSize, settings.Sigma, settings.Sigma);
            //imageContext.GenericImage = imageContext.GenericImage.SmoothBilateral(settings.KernelSize, 15, 15);

            //Canny
            imageContext.GenericImage = imageContext.GenericImage.Canny(thresholds.Lower, thresholds.Upper);

            return imageContext;
        }

        private (double Lower, double Upper) GetThreshHold(ImageContext imageContext, double sigma = 0.33)
        {
            var median = imageContext.GenericImage.GetAverage().Intensity;

            var lower = Math.Max(0, (1 - sigma) * median);
            var upper = Math.Min(255, (1 + sigma) * median);

            return (lower, upper);
        }

        private Bitmap ResizeImage(Bitmap image, int width = 600, int height = 450)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.Default;
                graphics.InterpolationMode = InterpolationMode.Default;
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
