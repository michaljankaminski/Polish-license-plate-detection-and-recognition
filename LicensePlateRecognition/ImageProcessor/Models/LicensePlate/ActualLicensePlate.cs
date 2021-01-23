using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessor.Models.LicensePlate
{
    public class ActualLicensePlate : BaseLicensePlate
    {
        public string PlateNumber { get; set; }
        public Image<Gray, byte> Image { get; set; }

        public ActualLicensePlate(PotentialSecondLayerLicensePlate potentialLicensePlate, string plateNumber) : base(potentialLicensePlate.Position)
        {
            PlateNumber = plateNumber;
            Image = potentialLicensePlate.Image;
        }
    }
}
