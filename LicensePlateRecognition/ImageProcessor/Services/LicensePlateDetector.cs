using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageProcessor.Models;

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
            return imageContext.PotentialLicensePlates.Select(GetConvertedImage);
        }

        private bool IsLicensePlate(Bitmap image)
        {
            var newImg = image.ToImage<Rgb, byte>();


            return true;
        }

        private Image<Hsv, byte> GetConvertedImage(Bitmap image)
        {
            //Readable for ocr - newImg.ThresholdBinary(new Hsv(255, 0, 100), new Hsv(0, 15, 255))

            var newImg = image.ToImage<Hsv, byte>();
            var mask = newImg.InRange(new Hsv(0, 0, 0), new Hsv(15, 15, 15));


            return newImg.ThresholdBinary(new Hsv(360, 0, 100), new Hsv(0, 15, 255));
        }
    }
}
