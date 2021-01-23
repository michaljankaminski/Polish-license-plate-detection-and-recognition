using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessor.Models.LicensePlate
{
    public class PotentialSecondLayerLicensePlate : BaseLicensePlate
    {
        public Image<Hsv, byte> Image { get; set; }

        public PotentialSecondLayerLicensePlate(PotentialFirstLayerLicensePlate potentialLicensePlateFirstLayer, Image<Hsv, byte> image) : base(potentialLicensePlateFirstLayer.Position)
        {
            Image = image;
        }
    }
}
