using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessor.Models;
using ImageProcessor.Models.LicensePlate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageProcessor.Services
{
    public interface ILicensePlateReader
    {
        /// <summary>
        /// From the cropped /segmented image it reads the license plate number
        /// and returns the context of the image with filled array of found
        /// plates numbers and rectangle areas on which they were found. 
        /// </summary>
        /// <param name="imageContext">Segmented image</param>
        void RecognizePlate(ImageContext imageContext);
    }
    /// <inheritdoc cref="ILicensePlateReader" />
    public class LicensePlateReader : ILicensePlateReader
    {
        private readonly IDictionary<string, string> _ocrParams;

        public LicensePlateReader()
        {
            _ocrParams = new Dictionary<string, string>
            {
                { "TEST_DATA_PATH", "D:\\OCR\\"},
                { "TEST_DATA_LANG", ""},
                { "WHITE_LIST", "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890 " }
            };
        }

        /// <summary>
        /// Performs potential number recognition by splitting 
        /// the number into single characters
        /// </summary>
        /// <param name="imageContext">Context of the image</param>
        public void RecognizePlate(ImageContext imageContext)
        {
            var actualLicensePlates = new List<ActualLicensePlate>();

            _ocrParams["TEST_DATA_LANG"] = "lplate+eng2+pol";

            foreach (var potentialLicensePlate in imageContext.PotentialSecondLayerLicensePlates)
            {
                var platesArea = FindPlateContours(potentialLicensePlate.Image, false);
                if (platesArea != null)
                {
                    string potentialNumber = RecognizeNumber(platesArea, PageSegMode.SingleBlock);
                    if (ValidateCharactersSet(ref potentialNumber))
                    {
                        var similarPlateNumber = actualLicensePlates
                            .FirstOrDefault(a => potentialNumber.Contains(a.PlateNumber));

                        if (actualLicensePlates.FirstOrDefault(a => a.PlateNumber.Contains(potentialNumber)) == null)
                        {
                            if (similarPlateNumber != null)
                                actualLicensePlates.Remove(similarPlateNumber);

                            actualLicensePlates.Add(new ActualLicensePlate(potentialLicensePlate, potentialNumber, platesArea.ToImage<Hsv, byte>()));
                        }
                    }
                }
            }

            DisplayFoundPlates(actualLicensePlates, imageContext.FileName);
            imageContext.ActualLicensePlates = actualLicensePlates;
        }
        /// <summary>
        /// From a given cropped image looks for single characters contours. 
        /// </summary>
        /// <remarks>
        /// After applying basic bilateral and canny filter it searches the contours of single characters.
        /// Each contour is being validated whether it is a contour of a possible character or not. It uses some 
        /// basic total area and ratio (width/height) comparison. If contour met the requiremenents, 
        /// it is then marked with a bounding rectangular to simplify the further recognition. 
        /// </remarks>
        /// <param name="croppedImage">Cropped image on which we want to search characters' contours</param>
        /// <param name="split">Whether the plate should be splitted into single characters array</param>
        /// <returns></returns>
        private Mat FindPlateContours(Image<Hsv, byte> croppedImage, bool split = false)
        {
            using (var bilateralMat = new Mat())
            using (var cannyMat = new Mat())
            using (var contours = new VectorOfVectorOfPoint())
            {
                var treshold = ImageConverter.GetAutomatedTreshold(bilateralMat.ToImage<Gray, byte>());
                var cleanMat = new Mat(croppedImage.Rows, croppedImage.Cols, DepthType.Cv8U, 1);
                cleanMat.SetTo(new MCvScalar(255));

                CvInvoke.BilateralFilter(
                  croppedImage,
                  bilateralMat,
                  20, 20, 10);

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
                            ratio >= 0.1 &&
                            area >= 400)
                        {
                            Mat ROI = new Mat(cleanMat, rect);
                            Mat potentialCharArea = new Mat(croppedImage.Convert<Gray, byte>().Mat, rect);
                            potentialCharArea.CopyTo(ROI);
                            // w celach analizy dokładamy wartości ratio
                            CvInvoke.Rectangle(croppedImage, rect, new MCvScalar(0, 250, 0));
                            CvInvoke.PutText(croppedImage, Math.Round(ratio, 3).ToString(), rect.Location, FontFace.HersheyDuplex, fontScale: 0.5d, new MCvScalar(0, 250, 0));
                        }
                    }
                }

                return cleanMat;
            }
        }
        /// <summary>
        /// Recognize the license plate number from a given image
        /// </summary>
        /// <remarks>
        /// Uses a Tesseract OCR library to recognize the set of characters. 
        /// When you would like to distinguish single character PageSegMode.SingleChar should be used,
        /// otherwise PageSegMode.SingleBlock will be applied. 
        /// Tesseract uses already prepared training data which consists of built-in data set and 
        /// special training set for polish license plates.
        /// </remarks>
        /// <param name="imgWithNumber">Mat containing the image of possible license plate area</param>
        /// <param name="pageMode">PageSegMode which should be used when recognizing the character </param>
        /// <returns>Recognized plate number</returns>
        private string RecognizeNumber(Mat imgWithNumber, PageSegMode pageMode = PageSegMode.SingleChar)
        {
            Tesseract.Character[] characters;

            StringBuilder licensePlateNumber = new StringBuilder();
            using (var ocr = new Tesseract())
            {
                ocr.Init(
                    _ocrParams["TEST_DATA_PATH"],
                    _ocrParams["TEST_DATA_LANG"],
                    OcrEngineMode.LstmOnly);

                ocr.SetVariable(
                    "tessedit_char_whitelist",
                    _ocrParams["WHITE_LIST"]);

                ocr.SetVariable
                    ("user_defined_dpi", "70");

                ocr.PageSegMode = pageMode;

                using (Mat tmp = imgWithNumber.Clone())
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
        /// <remarks>
        /// Validates the given string whether it can be a license plate number or not.
        /// Rules which are being applied were generated with the help of the definition 
        /// of license plate in Polish Law. That is why for instance potential number 
        /// which exceeds the length of 8 is being rejected.
        /// Other validation rules:
        /// - total number of characters
        /// - total number of letters
        /// - total number of numbers
        /// - the first and the last character
        /// - two first characters (whether there is a single space) 
        /// </remarks>
        /// <param name="characters">Set of characters</param>
        /// <returns>Validation status</returns>
        private static bool ValidateCharactersSet(ref string characters)
        {
            char[] deniedFirstCharacters =
                new char[] { 'A', 'H', 'I', 'J', 'M', 'U', 'V', 'Y' };

            string firstLetterSpaceMatch = @"^[A-z]{1} [A-z]{1}[A-z0-9 ]*$";
            string lastLetterSpaceMatch = @"^[A-z0-9 ]* [A-z]$";

            characters = characters.Trim();
            if (!String.IsNullOrEmpty(characters))
            {
                if (Char.IsNumber(characters[0]) ||
                    Regex.IsMatch(characters, firstLetterSpaceMatch))
                    characters = characters[1..].Trim();

                if (!String.IsNullOrEmpty(characters) &&
                    deniedFirstCharacters.Contains(characters[0]))
                    characters = characters[1..].Trim();

                if (characters.Replace(" ", "").Length == 9 ||
                    Regex.IsMatch(characters, lastLetterSpaceMatch))
                    characters = characters[0..^1].Trim();

                if (
                    characters.Count(Char.IsLetter) > 5 ||
                    characters.Count(Char.IsNumber) > 5 ||
                    characters.Count(Char.IsNumber) < 1 ||
                    characters.Count(Char.IsLetter) < 2 ||
                    characters.Count(Char.IsWhiteSpace) >= 2)
                    return false;

                if (characters.Replace(" ", "").Length < 9 &&
                    characters.Replace(" ", "").Length > 5)
                    return true;
            }

            return false;
        }
        private static void DisplayFoundPlates(IEnumerable<ActualLicensePlate> plates, string imageName)
        {
            foreach (var foundPlate in plates)
                Console.WriteLine($"Image: {imageName}, Number: {foundPlate.PlateNumber} ");
        }
    }
}

