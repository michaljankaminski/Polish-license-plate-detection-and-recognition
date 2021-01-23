using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessor.Helpers;
using ImageProcessor.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageProcessor.Services
{
    public interface IRectangleDetector
    {
        /// <summary>
        /// Detects the rectangles from given image context
        /// </summary>
        /// <remarks>
        /// Processes the image after initial preparation, such as 
        /// greyscaling, tresholding, etc. 
        /// </remarks>
        /// <param name="imageContext">Context of the processing image</param>
        /// <param name="useOpenCV">Use OpenCV library algorithms</param>
        ImageContext Detect(ImageContext imageContext);
    }
    /// <inheritdoc cref="IRectangleDetector"/>
    public class RectangleDetector : IRectangleDetector
    {
        public ImageContext Detect(ImageContext imageContext)
        {
            var contours = new VectorOfVectorOfPoint();
            var contoursImage = imageContext.GenericImage.Convert<Rgb, byte>();
            var potentialLicensePlates = new List<Bitmap>();

            CvInvoke.FindContours(
                imageContext.GenericImage,
                contours,
                null,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            for (var i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];
                var smoothContour = new VectorOfPoint(contour.Size);

                CvInvoke.ApproxPolyDP(
                    contour,
                    smoothContour,
                    0.01 * CvInvoke.ArcLength(contour, true),
                    true);

                if (smoothContour.Size >= 4)
                {
                    var rect = CvInvoke.BoundingRectangle(smoothContour);
                    var ratio = (double)rect.Width / rect.Height;

                    //Standard polish license plate has dimensions //520x114 so the ratio is around 4.56
                    if (ratio > 2 &&
                        ratio < 5)
                    {
                        var croppedImage = RotateContour(
                           imageContext.ProcessedBitmap.ToImage<Rgb, byte>(),
                           ScaleContour(smoothContour, imageContext.WidthResizeRatio, imageContext.HeightResizeRatio));

                        potentialLicensePlates.Add(croppedImage.ToBitmap());
                        CvInvoke.Rectangle(contoursImage, rect, new MCvScalar(0, 250, 0));
                    }
                }
            }
            imageContext.PotentialLicensePlates = potentialLicensePlates;
            imageContext.ContoursImage = contoursImage;

            return imageContext;
        }

        #region HELPERS
        private Rectangle ExpandRectangleArea(Image<Gray, byte> frm, Rectangle boundingBox, int padding)
        {
            Rectangle returnRect = new Rectangle(boundingBox.X - padding, boundingBox.Y - padding, boundingBox.Width + (padding * 2), boundingBox.Height + (padding * 2));
            if (returnRect.X < 0) returnRect.X = 0;
            if (returnRect.Y < 0) returnRect.Y = 0;
            if (returnRect.X + returnRect.Width >= frm.Cols) returnRect.Width = frm.Cols - returnRect.X;
            if (returnRect.Y + returnRect.Height >= frm.Rows) returnRect.Height = frm.Rows - returnRect.Y;
            return returnRect;
        }
        private VectorOfPoint ScaleContour(VectorOfPoint contour, double widthRatio, double heightRatio)
        {
            Point[] pointsArr = new Point[contour.Size];

            for (int i = 0; i < contour.Size; i++)
            {
                pointsArr[i] = new Point(
                    (int)Math.Ceiling(contour[i].X * widthRatio),
                    (int)Math.Ceiling(contour[i].Y * heightRatio));
            }

            return new VectorOfPoint(pointsArr);
        }
        private Image<Rgb, byte> RotateContour(Image<Rgb, byte> image, VectorOfPoint contour)
        {
            int edgePixelSize = 2;

            RotatedRect box = CvInvoke.MinAreaRect(contour);
            if (box.Angle < -45.0)
            {
                float tmp = box.Size.Width;
                box.Size.Width = box.Size.Height;
                box.Size.Height = tmp;
                box.Angle += 90.0f;
            }
            else if (box.Angle > 45.0)
            {
                float tmp = box.Size.Width;
                box.Size.Width = box.Size.Height;
                box.Size.Height = tmp;
                box.Angle -= 90.0f;
            }
            using (UMat rotatedMat = new UMat())
            using (UMat resizedMat = new UMat())
            {
                PointF[] srcCorners = box.GetVertices();
                PointF[] destCorners =
                    new PointF[]
                    {
                        new PointF(0, box.Size.Height - 1),
                        new PointF(0, 0),
                        new PointF(box.Size.Width - 1, 0),
                        new PointF(box.Size.Width - 1, box.Size.Height - 1)
                    };

                using (Mat rot = CvInvoke.GetAffineTransform(srcCorners, destCorners))
                    CvInvoke.WarpAffine(image, rotatedMat, rot, Size.Round(box.Size));

                Size approxSize =
                    new Size(
                        Settings.ResizeWidth,
                        Settings.ResizeHeight);

                double scale = Math.Min(approxSize.Width / box.Size.Width, approxSize.Height / box.Size.Height);

                Size newSize = new Size((int)Math.Round(box.Size.Width * scale), (int)Math.Round(box.Size.Height * scale));
                CvInvoke.Resize(rotatedMat, resizedMat, newSize, 0, 0, Inter.Cubic);

                Rectangle newRoi = new Rectangle(new Point(edgePixelSize, edgePixelSize),
                   resizedMat.Size - new Size(2 * edgePixelSize, 2 * edgePixelSize));

                UMat plate = new UMat(resizedMat, newRoi);

                return plate.ToImage<Rgb, byte>();
            }
        }
        #endregion
    }
}
