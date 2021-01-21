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
        private const int Width = 600;
        private const int Height = 450;

        public ImageContext ApplyFullCannyOperator(ImageContext imageContext, Settings settings)
        {
            //Grayscale
            imageContext.GenericImage = imageContext.ProcessedBitmap.ToImage<Gray, byte>();

            //Resize
            ResizeImage(imageContext);

            var thresholds = GetThreshHold(imageContext);

            //Gaussian
            imageContext.GenericImage = imageContext.GenericImage.SmoothGaussian(settings.KernelSize, settings.KernelSize, settings.Sigma, settings.Sigma);

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

        private void ResizeImage(ImageContext imageContext)
        {
            SetResizeRatio(imageContext);

            CvInvoke.Resize(imageContext.GenericImage, imageContext.GenericImage, new Size(Width, Height));
        }

        private void SetResizeRatio(ImageContext imageContext)
        {
            var originalWidth = imageContext.GenericImage.Size.Width;
            var originalHeight = imageContext.GenericImage.Size.Height;

            imageContext.WidthResizeRatio = (double)originalWidth / Width;
            imageContext.HeightResizeRatio =(double)originalHeight / Height;
        }

        private Bitmap ResizeImage(Bitmap image)
        {
            var destRect = new Rectangle(0, 0, Width, Height);
            var destImage = new Bitmap(Width, Height);

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
