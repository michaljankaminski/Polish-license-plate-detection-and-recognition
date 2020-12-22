using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ImageProcessor.Models;

namespace ImageProcessor.Services
{
    public interface IGaussianBlur
    {
        Bitmap Apply(Bitmap image, Settings settings);
    }

    public class GaussianBlur : IGaussianBlur
    {
        private static readonly ParallelOptions ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        public Bitmap Apply(Bitmap image, Settings settings)
        {
            var rectangle = new Rectangle(0, 0, image.Width, image.Height);
            var source = new int[rectangle.Width * rectangle.Height];
            var bits = image.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(bits.Scan0, source, 0, source.Length);
            image.UnlockBits(bits);

            var width = image.Width;
            var height = image.Height;

            var alpha = new int[width * height];
            var red = new int[width * height];
            var green = new int[width * height];
            var blue = new int[width * height];

            Parallel.For(0, source.Length, ParallelOptions, i =>
            {
                alpha[i] = (int)((source[i] & 0xff000000) >> 24);
                red[i] = (source[i] & 0xff0000) >> 16;
                green[i] = (source[i] & 0x00ff00) >> 8;
                blue[i] = source[i] & 0x0000ff;
            });

            var newAlpha = new int[width * height];
            var newRed = new int[width * height];
            var newGreen = new int[width * height];
            var newBlue = new int[width * height];
            var destination = new int[width * height];

            void GaussBlur(IList<int> src, IList<int> dest)
            {
                var bxs = BoxesForGauss(settings.KernelSize, (int)settings.Sigma);
                BoxBlur(src, dest, width, height, (bxs[0] - 1) / 2);
                BoxBlur(dest, src, width, height, (bxs[1] - 1) / 2);
                BoxBlur(src, dest, width, height, (bxs[2] - 1) / 2);
            }

            Parallel.Invoke(
                () => GaussBlur(alpha, newAlpha),
                () => GaussBlur(red, newRed),
                () => GaussBlur(green, newGreen),
                () => GaussBlur(blue, newBlue));

            Parallel.For(0, destination.Length, ParallelOptions, i =>
            {
                if (newAlpha[i] > 255) newAlpha[i] = 255;
                if (newRed[i] > 255) newRed[i] = 255;
                if (newGreen[i] > 255) newGreen[i] = 255;
                if (newBlue[i] > 255) newBlue[i] = 255;

                if (newAlpha[i] < 0) newAlpha[i] = 0;
                if (newRed[i] < 0) newRed[i] = 0;
                if (newGreen[i] < 0) newGreen[i] = 0;
                if (newBlue[i] < 0) newBlue[i] = 0;

                destination[i] = (int)((uint)(newAlpha[i] << 24) | (uint)(newRed[i] << 16) | (uint)(newGreen[i] << 8) | (uint)newBlue[i]);
            });

            var newImage = new Bitmap(width, height);
            var rct = new Rectangle(0, 0, newImage.Width, newImage.Height);
            var bits2 = newImage.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(destination, 0, bits2.Scan0, destination.Length);
            newImage.UnlockBits(bits2);
            return newImage;
        }

        private static IReadOnlyList<int> BoxesForGauss(int kernelSize, int sigma)
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / kernelSize) + 1);
            var wl = (int)Math.Floor(wIdeal);
            if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (double)(12 * sigma * sigma - kernelSize * wl * wl - 4 * kernelSize * wl - 3 * kernelSize) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);

            var sizes = new List<int>();
            for (var i = 0; i < kernelSize; i++) 
                sizes.Add(i < m ? wl : wu);
            return sizes;
        }

        private static void BoxBlur(IList<int> source, IList<int> destination, int w, int h, int r)
        {
            for (var i = 0; i < source.Count; i++) destination[i] = source[i];
            BoxBlurH(destination, source, w, h, r);
            BoxBlurT(source, destination, w, h, r);
        }

        private static void BoxBlurH(IList<int> source, IList<int> destination, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, h, ParallelOptions, i =>
            {
                var ti = i * w;
                var li = ti;
                var ri = ti + r;
                var fv = source[ti];
                var lv = source[ti + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri++] - fv;
                    destination[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = r + 1; j < w - r; j++)
                {
                    val += source[ri++] - destination[li++];
                    destination[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = w - r; j < w; j++)
                {
                    val += lv - source[li++];
                    destination[ti++] = (int)Math.Round(val * iar);
                }
            });
        }

        private static void BoxBlurT(IList<int> source, IList<int> destination, int width, int height, int r)
        {
            var iar = 1d / (r + r + 1);
            Parallel.For(0, width, ParallelOptions, i =>
            {
                var li = i;
                var ri = i + r * width;
                var fv = source[i];
                var lv = source[i + width * (height - 1)];
                var val = (r + 1) * fv;

                for (var j = 0; j < r; j++) 
                    val += source[i + j * width];

                for (var j = 0; j <= r; j++)
                {
                    val += source[ri] - fv;
                    destination[i] = (int)Math.Round(val * iar);
                    ri += width;
                    i += width;
                }

                for (var j = r + 1; j < height - r; j++)
                {
                    val += source[ri] - source[li];
                    destination[i] = (int)Math.Round(val * iar);
                    li += width;
                    ri += width;
                    i += width;
                }

                for (var j = height - r; j < height; j++)
                {
                    val += lv - source[li];
                    destination[i] = (int)Math.Round(val * iar);
                    li += width;
                    i += width;
                }
            });
        }
    }
}
