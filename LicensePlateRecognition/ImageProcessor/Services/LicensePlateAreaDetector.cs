using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessor.Models;
using ImageProcessor.Models.LicensePlate;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ImageProcessor.Services
{
    public interface ILicensePlateAreaDetector
    {
        /// <summary>
        /// Detects the rectangles (contours) from given image context
        /// </summary>
        /// <remarks>
        /// Processes the image after initial preparation, such as 
        /// greyscaling, tresholding, etc. 
        /// </remarks>
        /// <param name="imageContext">Context of the processing image</param>
        /// <param name="useOpenCV">Use OpenCV library algorithms</param>
        ImageContext Detect(ImageContext imageContext);
    }
    /// <inheritdoc cref="ILicensePlateAreaDetector"/>
    public class LicensePlateAreaDetector : ILicensePlateAreaDetector
    {
        public ImageContext Detect(ImageContext imageContext)
        {
            var contours = new VectorOfVectorOfPoint();
            var contoursImage = imageContext.ProcessedImage.Convert<Rgb, byte>();
            var potentialLicensePlates = new List<PotentialFirstLayerLicensePlate>();

            // Finding contours on canny processed image
            CvInvoke.FindContours(
                imageContext.ProcessedImage,
                contours,
                null,
                RetrType.Tree,
                ChainApproxMethod.ChainApproxSimple);

            for (var i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];
                var smoothContour = new VectorOfPoint(contour.Size);

                // Smoothing the contours
                CvInvoke.ApproxPolyDP(
                    contour,
                    smoothContour,
                    0.01 * CvInvoke.ArcLength(contour, true),
                    true);

                if (smoothContour.Size >= 4)
                {
                    // Bulding rectangle out of the contour
                    var rect = CvInvoke.BoundingRectangle(smoothContour);

                    var ratio = (double)rect.Width / rect.Height;

                    //Standard polish license plate has dimensions //520x114 so the ratio is around 4.56. Due to possible angle the range is much larger
                    if (ratio > 2 &&
                        ratio < 5)
                    {
                        // Scalling the image
                        var croppedImage = RotateContour(
                           imageContext.OriginalBitmap.ToImage<Rgb, byte>(),
                           ScaleContour(smoothContour, imageContext.WidthResizeRatio, imageContext.HeightResizeRatio));

                        potentialLicensePlates.Add(new PotentialFirstLayerLicensePlate(rect, croppedImage));
                        CvInvoke.Rectangle(contoursImage, rect, new MCvScalar(0, 250, 0));
                    }
                }
            }
            imageContext.PotentialFirstLayerLicensePlates = potentialLicensePlates;
            imageContext.ContoursImage = contoursImage;

            return imageContext;
        }

        #region HELPERS

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
