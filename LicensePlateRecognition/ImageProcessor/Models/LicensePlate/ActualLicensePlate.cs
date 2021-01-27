using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessor.Models.LicensePlate
{
    public class ActualLicensePlate : BaseLicensePlate
    {
        public string PlateNumber { get; set; }
        public Image<Hsv, byte> Image { get; set; }

        public ActualLicensePlate(PotentialSecondLayerLicensePlate potentialLicensePlate, string plateNumber, Image<Hsv, byte> cleanedLicensePlate) : base(potentialLicensePlate.Position)
        {
            PlateNumber = plateNumber;
            Image = cleanedLicensePlate;
        }
    }
}
