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
        ImageContext Detect(ImageContext imageContext, bool useOpenCV = false);
        ImageContext DetectPlayGround(ImageContext imageContext);
    }
    /// <inheritdoc cref="IRectangleDetector"/>
    public class RectangleDetector : IRectangleDetector
    {
        private readonly IImageCropper _imageCropper;
        int[,] outputMatrix = null;

        public RectangleDetector(IImageCropper imageCropper)
        {
            _imageCropper = imageCropper;
        }

        public ImageContext Detect(ImageContext imageContext, bool useOpenCV = false)
        {
            if (imageContext.ProcessedBitmap.Width > 0
                && imageContext.ProcessedBitmap.Height > 0)
            {
                if (useOpenCV)
                    ProcessOpenCv(imageContext.ProcessedBitmap);
                else
                    ProcessCCL(imageContext.ProcessedBitmap);
            }
            return imageContext;
        }

        public ImageContext DetectPlayGround(ImageContext imageContext)
        {
            var newImage = imageContext.GenericImage.Convert<Rgb, byte>();
            var contours = new VectorOfVectorOfPoint();
            var potentialLicensePlates = new List<Bitmap>();

            CvInvoke.FindContours(imageContext.GenericImage, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            for (var i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];
                var newContour = new VectorOfPoint(contour.Size);

                CvInvoke.ApproxPolyDP(contour, newContour, 0.01 * CvInvoke.ArcLength(contour, true), true);

                if (newContour.Size >= 4)
                {
                    var rect = CvInvoke.BoundingRectangle(newContour);
                    var ratio = (double)rect.Width / rect.Height;

                    //Standard polish license plate has dimensions //520x114 so the ratio is around 4.56
                    if (ratio > 2 && ratio < 5)
                    {
                        var croppedImage = RotateContour(
                           imageContext.ProcessedBitmap.ToImage<Rgb, byte>(),
                           ScaleContour(newContour, imageContext.WidthResizeRatio, imageContext.HeightResizeRatio));

                        potentialLicensePlates.Add(croppedImage.ToBitmap());
                        CvInvoke.Rectangle(newImage, rect, new MCvScalar(0, 250, 0));
                        //CvInvoke.PutText(newImage, ratio.ToString(), newContour[0], FontFace.HersheyComplex, 0.3, new MCvScalar(0, 250, 250));
                    }
                }
            }
            
            imageContext.PotentialLicensePlates = potentialLicensePlates;
            imageContext.ContoursImage = newImage;

            return imageContext;
        }

        public Bitmap CropImage(ImageContext imageContext, Rectangle section)
        {
            var newWidth = (int)Math.Ceiling(section.Width * imageContext.WidthResizeRatio);
            var newHeight = (int)Math.Ceiling(section.Height * imageContext.HeightResizeRatio);

            section.Size = new Size(newWidth, newHeight);
            section.X = (int) Math.Ceiling(section.X * imageContext.HeightResizeRatio);
            section.Y = (int) Math.Ceiling(section.Y * imageContext.HeightResizeRatio);

            return _imageCropper.CropImage(imageContext.ProcessedBitmap, section);
        }

        public Bitmap CropImageWithoutResize(ImageContext imageContext, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(imageContext.ProcessedBitmap, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }


        #region Procesowanie własne CCL
        private void FindCCL(int[,] boolMatrix)
        {
            List<int[]> linked = new List<int[]>();
            var rows = boolMatrix.GetLength(0);
            var cols = boolMatrix.GetLength(1);

            outputMatrix = new int[rows, cols];
            int nextLabel = 1;
            // żeby nie bawić się w sprawdzanie warunków
            // granicznych możemy od razu pominąć pierwszy
            // wiersz i kolumnę
            for (int row = 1; row < rows-1; row++)
            {
                for (int col = 1; col < cols-2; col++)
                {   
                    int currentPixel = boolMatrix[row, col];
                    // Sprawdzamy czy dany piksel jest tłem
                    // czy może właściwym pikselem który nas 
                    // interesuje
                    if (IsWhite(currentPixel) 
                        && outputMatrix[row,col] == 0)
                    {
                        nextLabel++;
                        DiscoverNeighbors(row, col, boolMatrix, nextLabel);
                    }
                }
            }

            for(int i = 0; i < rows-1; i++)
            {
                int currentPixel = boolMatrix[i, 0];
                if (IsWhite(currentPixel)
                    && outputMatrix[i, 0] == 0)
                {
                    nextLabel++;
                    outputMatrix[i, 0] = nextLabel;
                }
                else
                {
                    if (boolMatrix[i, (cols - 1)] == 1
                        && outputMatrix[i, (cols - 1)] == 0)
                    {
                        nextLabel++;
                        outputMatrix[i, (cols - 1)] = nextLabel;
                    }
                }
            }

            for(int j = 0; j < cols-1; j++)
            {
                int currentPixel = boolMatrix[0, j];
                if (IsWhite(currentPixel)
                    && outputMatrix[0, j] == 0)
                {
                    nextLabel++;
                    outputMatrix[0, j] = nextLabel;
                }
                else
                {
                   if(boolMatrix[rows-1,j] == 1 && outputMatrix[rows-1,j] ==0)
                    {
                        nextLabel++;
                        outputMatrix[rows - 1, j] = nextLabel;
                    }
                }
            }


            SaveMatToFile(outputMatrix, @"D:\\testMat3.txt");
            void SaveMatToFile(int[,] boolMatrix, string filePath)
            {
                using (var sw = new StreamWriter(filePath))
                {
                    for (int i = 0; i < boolMatrix.GetLength(0); i++)
                    {
                        for (int j = 0; j < boolMatrix.GetLength(1); j++)
                        {
                            sw.Write(boolMatrix[i, j] + "\t");
                        }
                        sw.Write(";\n");
                    }
                    sw.Flush();
                    sw.Close();
                }
            }
            bool IsWhite(int pixel)
            {
                if (pixel == 1)
                    return true;
                return false;
            }
        }
        private void DiscoverNeighbors(int row, int col, int[,] matrix, int nextLabel)
        {
            outputMatrix[row, col] = nextLabel;

            int[] rowDirection = new int[] { 1, -1, 0, 0, 1, -1, 1, -1 };
            int[] colDirection = new int[] { 0, 0, 1, -1, 1, 1, -1, -1 };

            if(row > 0 && col > 0 && row < matrix.GetLength(0)-1 && col < matrix.GetLength(1)-1)
            {
                for(int i = 0; i < 8; i++)
                {
                    int nRow = row + rowDirection[i];
                    int nCol = col + colDirection[i];
                    if (matrix[nRow, nCol] == 1 && outputMatrix[nRow, nCol] == 0)
                        DiscoverNeighbors(nRow, nCol, matrix, nextLabel);
                }
            }
        }
        private Bitmap ProcessCCL(System.Drawing.Bitmap image)
        {
            var binaryMatrix = ConvertBitmapTo2d(image);
            if (binaryMatrix.Length > 0)
            {
                // generalnie musimy coś tutaj rozsądnego
                // zwracać z tej metody
                //DetectAreas(binaryMatrix);
                FindCCL(binaryMatrix);
            }

            return image;
        }
        /// <summary>
        /// Converts an image bitmap to 2d-array (matrix)
        /// </summary>
        /// <remarks>
        /// The matrix is beign filled with 0/1 values,
        /// where 0 can be read as black pixel, and 1 as a white one.
        /// </remarks>
        /// <param name="image">Bitmap image</param>
        /// <returns>Matrix of an image</returns>
        private static int[,] ConvertBitmapTo2d(System.Drawing.Bitmap image)
        {
            // teraz tak, ważne żeby ta bitmapa była w określonym formacie BGR
            // musimy rozważyć ew. jeszce alphe, tylko wtedy bedą nam przypadać
            // 4 bity na kazdy piksel 
            var test = image.PixelFormat;
            if (image.PixelFormat != PixelFormat.Format24bppRgb
                && image.PixelFormat != PixelFormat.Format32bppRgb
                && image.PixelFormat != PixelFormat.Format32bppArgb)
                return default;
            // można ew. rozważyć użycie innych formatów, ale nie wiem
            // czy jest sens bawić się w kanał alfa póki co 
            int[,] boolMatrix = new int[image.Height, image.Width];
            // póki co pomysł jest taki żeby macierz T/F uzupełniać
            // wartościami białych pikseli, przez co potem będziemy mogli
            // w miarę łatwiej wyznaczyć potencjalne regiony
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData =
                image.LockBits(rect, ImageLockMode.ReadWrite,
                image.PixelFormat);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytes = Math.Abs(bmpData.Stride) * image.Height;

                for (int y = 0; y < image.Height; y++)
                {
                    var row = ptr + (y * bmpData.Stride);
                    for (int x = 0; x < image.Width; x++)
                    {
                        // Zgodnie z początkowym założeniem akceptujemy
                        // kanał RGB, więc przypada nam 3 bity na każdy 
                        // piksel |B|G|R|
                        var pixel = row + x * 3;
                        // proste sprawdzenie czy dany piksel jest biały 
                        // dla czarnego oczywiście sprawdzalibyśmy 0 
                        // w tym przypadku interesują nas jednak tylko białe
                        // a kolory pozostałych są nieistotne
                        bool isWhite = (pixel[0] == 255 &&
                                        pixel[1] == 255 &&
                                        pixel[2] == 255);
                        boolMatrix[y, x] = isWhite == true ? 1 : 0;
                    }
                }
            }

            SaveMatToFile(boolMatrix, @"D:\\mat_out.txt");
            return boolMatrix;

            // poniżej generalnie do zaorania 
            // na potrzeby testów tylko sobie odkładamy
            void SaveMatToFile(int[,] boolMatrix, string filePath)
            {
                using (var sw = new StreamWriter(filePath))
                {
                    for (int i = 0; i < boolMatrix.GetLength(0); i++)
                    {
                        for (int j = 0; j < boolMatrix.GetLength(1); j++)
                        {
                            sw.Write(boolMatrix[i, j] + " ");
                        }
                        sw.Write("\n");
                    }
                    sw.Flush();
                    sw.Close();
                }
            }
        }
       
        #endregion
        #region Procesowanie OpenCV
        private static Bitmap ProcessOpenCv(System.Drawing.Bitmap image)
        {
            var matImage = ConvertBitmapToMat(image);
            Mat proceMat = new Mat();
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                // Na podstawie obrazka, który już mamy odpowiednie przeprocesowany
                // możemy wykryć wszystkie kontury jakie się w nim znajdują
                // to nic innego jak 0 bity oddzielony 1 bitami 
                // realizujemy podobne podejście jak w metodzie własnej
                CvInvoke.CvtColor(matImage, proceMat, ColorConversion.Bgr2Gray);
                CvInvoke.FindContours(proceMat, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                if (contours.Size > 0)
                {
                    double maxArea = 0;
                    int chosen = 0;
                    for (int i = 0; i < contours.Size; i++)
                    {
                        // Każdy kontur następnie zaznaczamy na docelowym 
                        // obrazku 
                        VectorOfPoint contour = contours[i];
                        double area = CvInvoke.ContourArea(contour);
                        if (area > maxArea)
                        {
                            maxArea = area;
                            chosen = i;
                            MarkDetectedObject(matImage, contours[chosen], maxArea);
                        }
                    }
                }
                Image<Bgr, Byte> imageBgr = matImage.ToImage<Bgr, Byte>();
                return imageBgr.ToBitmap();
            }
        }
        private static void MarkDetectedObject(Mat frame, VectorOfPoint contour, double area)
        {
            // Getting minimal rectangle which contains the contour
            Rectangle box = CvInvoke.BoundingRectangle(contour);

            // Drawing contour and box around it
            CvInvoke.Polylines(frame, contour, true, new MCvScalar(0, 255, 0), 2, LineType.Filled);
            //CvInvoke.Rectangle(frame, box, new MCvScalar(0,255,0), 2, LineType.Filled);

            // Write information next to marked object
            Point center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
            //WriteMultilineText(frame, info, new Point(box.Right + 5, center.Y));
        }
        /// <summary>
        /// Konwertuje obraz bitmapy do odpowiedniej macierzy
        /// </summary>
        /// <param name="image">Bitmapa z obrazem</param>
        /// <returns>Macierz zgodna z modelem OpenCV</returns>
        private static Mat ConvertBitmapToMat(Bitmap image)
        {
            int stride = 0;
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);

            PixelFormat pf = image.PixelFormat;
            if (pf == PixelFormat.Format32bppArgb)
                stride = image.Width * 4;
            else
                stride = image.Width * 3;

            Image<Bgra, byte> cvImage = new Image<Bgra, byte>(image.Width, image.Height, stride, (IntPtr)bmpData.Scan0);
            image.UnlockBits(bmpData);

            return cvImage.Mat;
        }
        #endregion
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

                Size approxSize = new Size(600, 450);
                double scale = Math.Min(approxSize.Width / box.Size.Width, approxSize.Height / box.Size.Height);
                Size newSize = new Size((int)Math.Round(box.Size.Width * scale), (int)Math.Round(box.Size.Height * scale));
                CvInvoke.Resize(rotatedMat, resizedMat, newSize, 0, 0, Inter.Cubic);

                int edgePixelSize = 2;
                Rectangle newRoi = new Rectangle(new Point(edgePixelSize, edgePixelSize),
                   resizedMat.Size - new Size(2 * edgePixelSize, 2 * edgePixelSize));

                UMat plate = new UMat(resizedMat, newRoi);

                return plate.ToImage<Rgb, byte>();
            }
        }
        #endregion
    }
}
