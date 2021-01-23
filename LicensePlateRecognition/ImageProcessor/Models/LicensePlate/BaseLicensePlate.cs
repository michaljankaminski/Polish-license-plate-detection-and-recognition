using System.Drawing;

namespace ImageProcessor.Models.LicensePlate
{
    public abstract class BaseLicensePlate
    {
        public Rectangle Position { get; set; }

        protected BaseLicensePlate(Rectangle position)
        {
            Position = position;
        }
    }
}
