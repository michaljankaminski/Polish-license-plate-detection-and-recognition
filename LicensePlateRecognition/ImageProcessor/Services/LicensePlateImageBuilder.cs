using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageProcessor.Models;

namespace ImageProcessor.Services
{
    public interface ILicensePlateImageBuilder
    {
        void Build(ImageContext imageContext);
    }

    public class LicensePlateImageBuilder : ILicensePlateImageBuilder
    {
        public void Build(ImageContext imageContext)
        {
            var image = imageContext.OriginalBitmap.ToImage<Rgb, byte>();

            var color = new MCvScalar(0, 250, 0);

            foreach (var actualLicensePlate in imageContext.ActualLicensePlates)
            {
                CvInvoke.Rectangle(image, actualLicensePlate.Position, color);
                CvInvoke.PutText(image, actualLicensePlate.PlateNumber, actualLicensePlate.Position.Location, FontFace.HersheySimplex, 3, color);
            }

            imageContext.ImageWithLicenses = image.ToBitmap();
        }
    }
}
