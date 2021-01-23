using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessor.Models.LicensePlate
{
    public class PotentialSecondLayerLicensePlate : BaseLicensePlate
    {
        public Image<Gray, byte> Image { get; set; }

        public PotentialSecondLayerLicensePlate(PotentialFirstLayerLicensePlate potentialLicensePlateFirstLayer, Image<Gray, byte> image) : base(potentialLicensePlateFirstLayer.Position)
        {
            Image = image;
        }
    }
}
