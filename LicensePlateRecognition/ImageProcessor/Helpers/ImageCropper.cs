using System.Drawing;

namespace ImageProcessor.Helpers
{
    public interface IImageCropper
    {
        Bitmap CropImage(Bitmap image, Rectangle rectangle);
    }

    public class ImageCropper : IImageCropper
    {
        public Bitmap CropImage(Bitmap image, Rectangle rectangle)
        {
            var bitmap = new Bitmap(rectangle.Width, rectangle.Height);

            bitmap.SetResolution(image.VerticalResolution, image.HorizontalResolution);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(image, 0, 0, rectangle, GraphicsUnit.Pixel);
            }

            return bitmap;
        }
    }
}
