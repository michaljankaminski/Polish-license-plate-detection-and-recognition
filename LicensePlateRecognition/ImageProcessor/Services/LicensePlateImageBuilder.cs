using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageProcessor.Models;

namespace ImageProcessor.Services
{
    public interface ILicensePlateImageBuilder
    {
        /// <summary>
        /// Creates an image with all license plates found on the photo.
        /// </summary>
        /// <param name="imageContext"> ImageContext after final processing </param>
        void Build(ImageContext imageContext);
    }

    public class LicensePlateImageBuilder : ILicensePlateImageBuilder
    {
        public void Build(ImageContext imageContext)
        {
            var image = imageContext.OriginalBitmap.ToImage<Rgb, byte>();

            var color = new MCvScalar(0, 255, 0);

            foreach (var actualLicensePlate in imageContext.ActualLicensePlates)
            {
                var position = actualLicensePlate.GetFullyScaledRectangle(imageContext);

                CvInvoke.Rectangle(image, position, color, 2);

                position.Y -= 5;
                CvInvoke.PutText(image, actualLicensePlate.PlateNumber, position.Location, FontFace.HersheyTriplex, 1, color,2);
            }

            imageContext.ImageWithLicenses = image.ToBitmap();
        }
    }
}
