using ImageProcessor.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor.Services.Filters
{
    public interface INonMaximumSuppresion
    {
        Bitmap Apply(Bitmap image, Settings settings);
    }

    public class NonMaximumSuppresion : INonMaximumSuppresion
    {
        public Bitmap Apply(Bitmap image, Settings settings)
        {
            var newBitmap = new Bitmap(image.Width, image.Height);

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, image.PixelFormat);
            var values = new float[image.Width, image.Height];
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytes = Math.Abs(bmpData.Stride) * image.Height;

                for (int y = 0; y < image.Height; y++)
                {
                    var row = ptr + (y * bmpData.Stride);
                    for (int x = 0; x < image.Width; x++)
                    {
                        var q = 255;
                        var r = 255;
                        var pixel = row + x * 3;

                        throw new NotImplementedException();
                    }
                }
            }

            image.UnlockBits(bmpData);

            return image;
        }
    }
}
