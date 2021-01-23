using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessor.Models.LicensePlate
{
    public class PotentialFirstLayerLicensePlate : BaseLicensePlate
    {
        public Image<Rgb, byte> Image { get; set; }

        public PotentialFirstLayerLicensePlate(Rectangle position, Image<Rgb, byte> image)
            : base(position)
        {
            Image = image;
        }
    }
}
