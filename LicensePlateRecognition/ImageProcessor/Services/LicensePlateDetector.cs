using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessor.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageProcessor.Services
{
    public interface ILicensePlateDetector
    {
        IEnumerable<Image<Hsv, byte>> GetLicensePlateImages(ImageContext imageContext);
    }

    public class LicensePlateDetector : ILicensePlateDetector
    {
        public IEnumerable<Image<Hsv, byte>> GetLicensePlateImages(ImageContext imageContext)
        {
            return imageContext.PotentialLicensePlates.Where(IsLicensePlate).Select(GetConvertedImage);
        }

        private bool IsLicensePlate(Bitmap image)
        {
            var newImg = image.ToImage<Rgb, byte>();
            VectorOfMat bgrPlanes = new VectorOfMat();
            CvInvoke.Split(newImg, bgrPlanes);

            var dim = new[] { 1 };
            var histSize = new[] { 256 };
            var range = new[] { 0f, 255f};
            var accumulate = false;

            var bHist = new Mat();
            var gHist = new Mat();
            var rHist = new Mat();

            CvInvoke.CalcHist(bgrPlanes, new[] { 2 }, new Mat(), bHist, histSize, range, accumulate);
            CvInvoke.CalcHist(bgrPlanes, new[] { 1 }, new Mat(), gHist, histSize, range, accumulate);
            CvInvoke.CalcHist(bgrPlanes, new[] { 0 }, new Mat(), rHist, histSize, range, accumulate);

            var bAvg = GetAverage(bHist);
            var gAvg = GetAverage(gHist);
            var rAvg = GetAverage(rHist);

            return IsMeanWhiteLike(bAvg) && IsMeanWhiteLike(gAvg) && IsMeanWhiteLike(rAvg);
        }

        private bool IsMeanWhiteLike(float avg)
        {
            if (avg > 95 && avg < 150)
            {
                return true;
            }

            return false;
        }

        private float GetAverage(Mat mat)
        {
            var sumWeight = 0f;
            var sum = 0f;
            var values = mat.GetData();

            for (int i = 0; i < values.Length; i++)
            {
                var value = (float) mat.GetData().GetValue(i, 0);
                sumWeight += value;
                sum += i * value;
            }

            return sum / sumWeight;
        }

        private Image<Hsv, byte> GetConvertedImage(Bitmap image)
        {
            //Readable for ocr - newImg.ThresholdBinary(new Hsv(255, 0, 100), new Hsv(0, 15, 255))
            var newImg = image.ToImage<Hsv, byte>();
            //return newImg;
            return newImg.ThresholdBinary(new Hsv(360, 0, 100), new Hsv(0, 15, 255));
        }
    }
}