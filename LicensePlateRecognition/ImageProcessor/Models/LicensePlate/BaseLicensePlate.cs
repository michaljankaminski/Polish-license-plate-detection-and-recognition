using System;
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

        public Rectangle GetFullyScaledRectangle(ImageContext imageContext)
        {
            return new(
                (int) Math.Ceiling(Position.X * imageContext.WidthResizeRatio),
                (int) Math.Ceiling(Position.Y * imageContext.HeightResizeRatio),
                (int) Math.Ceiling(Position.Width * imageContext.WidthResizeRatio),
                (int) Math.Ceiling(Position.Height * imageContext.HeightResizeRatio));
        }
    }
}
