using ImageProcessor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using System.Drawing;
using Emgu.CV.OCR;
using Emgu.CV;
using System.Text.RegularExpressions;

namespace ImageProcessor.Services
{
    public interface IPlateRecognizer
    {
        /// <summary>
        /// From the cropped /segmented image it reads the numeber plates
        /// information and returns it as a string.
        /// </summary>
        /// <param name="imageContext">Segmented image</param>
        void RecognizePlate(ImageContext imageContext, bool useTesseract = true);
    }
    public class PlateRecognizer : IPlateRecognizer
    {
        private readonly IDictionary<string, string> _ocrParams;
        private readonly IBitmapConverter _bitmapConverter;
        public PlateRecognizer(IBitmapConverter bitmapConverter)
        {
            _bitmapConverter = bitmapConverter;
            _ocrParams = new Dictionary<string, string>
            {
                { "TEST_DATA_PATH", "D:\\OCR\\"},
                { "TEST_DATA_LANG","License_plate"},
                { "WHITE_LIST", "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890" }
            };
        }
        public void RecognizePlate(ImageContext imageContext, bool useTesseract = true)
        {
            // RecognizePlateWithSplit(imageContext);
            RecognizePlate(imageContext);
        }
        private void RecognizePlate(ImageContext imageContext)
        {
            _ocrParams["TEST_DATA_LANG"] = "lplate+eng2";
            foreach (var image in imageContext.ActualLicensePlates)
            {
                var platesArea = FindPlateContours(image, false);
                if (platesArea != null)
                {
                    string potentialNumber = RecognizeNumber(platesArea.First().Item1, PageSegMode.RawLine);
                    if (ValidateCharactersSet(potentialNumber))
                        Console.WriteLine($"{imageContext.FileName}: {potentialNumber}");
                }
            }
        }
        private void RecognizePlateWithSplit(ImageContext imageContext)
        {
            _ocrParams["TEST_DATA_LANG"] = "mf+lplate";

            List<string> foundPlates = new List<string>();
            foreach (var image in imageContext.ActualLicensePlates)
            {
                List<string> plateNumber = new List<string>();
                var characterAreas = FindPlateContours(image, true);
                if (characterAreas != null)
                    foreach (var character in characterAreas)
                        plateNumber.Add(RecognizeNumber(character.Item1, PageSegMode.SingleChar));

                if (plateNumber.Count > 5)
                    Console.WriteLine($"{imageContext.FileName}: {String.Join("", plateNumber)}");
            }
            imageContext.FoundLicensePlates = foundPlates;
        }
        private List<(UMat, int)> FindPlateContours(Image<Hsv, byte> croppedImage, bool split = false)
        {
            int charactersCnt = 0;

            List<(UMat Mat, int Order)> mats = new List<(UMat, int)>();
            using (var bilateralMat = new Mat())
            using (var cannyMat = new Mat())
            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.BilateralFilter(
                    croppedImage, 
                    bilateralMat, 
                    20, 20, 10);

                var treshold = BitmapConverter.GetAutomatedTreshold(bilateralMat.ToImage<Gray, byte>());

                CvInvoke.Canny(
                    croppedImage, 
                    cannyMat, 
                    treshold.Lower, 
                    treshold.Upper);

                CvInvoke.FindContours(
                    cannyMat.ToImage<Gray, byte>(), 
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
                        0.01 * CvInvoke.ArcLength(contour, true), true);

                    if (smoothContour.Size >= 4)
                    {
                        var rect = CvInvoke.BoundingRectangle(smoothContour);
                        double ratio = (double)rect.Width / rect.Height;
                        double area = rect.Width * (double)rect.Height;

                        if (ratio <= 1.5 &&
                            ratio >= 0.01 &&
                            area >= 400)
                        {
                            if (split)
                            {
                                UMat potentialCharArea = new UMat(croppedImage.Convert<Gray, byte>().ToUMat(), rect);
                                mats.Add((potentialCharArea, rect.X));
                            }
                            else
                                CvInvoke.Rectangle(croppedImage, rect, new MCvScalar(0, 250, 0));

                            charactersCnt++;
                        }
                    }
                }


                if (!split)
                    mats.Add((croppedImage.ToUMat(), 0));
                if (charactersCnt > 4)
                    return mats.OrderBy(a => a.Order).ToList();
            }
            return default;
        }

        private string RecognizeNumber(UMat imgWithNumber, PageSegMode pageMode = PageSegMode.SingleChar)
        {
            Emgu.CV.OCR.Tesseract.Character[] characters;

            StringBuilder licensePlateNumber = new StringBuilder();
            using (var ocr = new Emgu.CV.OCR.Tesseract())
            {
                ocr.Init(
                    _ocrParams["TEST_DATA_PATH"],
                    _ocrParams["TEST_DATA_LANG"],
                    OcrEngineMode.LstmOnly);

                ocr.SetVariable(
                    "tessedit_char_whitelist",
                    _ocrParams["WHITE_LIST"]);

                ocr.PageSegMode = pageMode;

                using (UMat tmp = imgWithNumber.Clone())
                {
                    ocr.SetImage(tmp);
                    ocr.Recognize();
                    characters = ocr.GetCharacters();

                    for (int i = 0; i < characters.Length; i++)
                        licensePlateNumber.Append(characters[i].Text);

                }

                return licensePlateNumber.ToString();
            }
        }
        /// <summary>
        /// Validate the character set with potential 
        /// license plate number in it.
        /// </summary>
        /// <param name="characters">Set of characters</param>
        /// <returns>Validation status</returns>
        private static bool ValidateCharactersSet(string characters)
        {
            characters = characters.Replace(" ", "");
            if (characters.Length == 0)
                return false;
            else if (characters.Length >= 6 &&
                characters.Length < 9)
                return true;
            else
                return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="licensePlate"></param>
        private static void ParseLicensePlate(string licensePlate)
        {
            Regex numberMatch = new Regex("[0-9]");
            if (licensePlate.Length >= 7 &&
                numberMatch.IsMatch(licensePlate[0].ToString()))
                licensePlate = licensePlate.Remove(0);
        }
    }
}
