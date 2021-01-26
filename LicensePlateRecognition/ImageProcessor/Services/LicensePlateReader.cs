﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ImageProcessor.Models;
using ImageProcessor.Models.LicensePlate;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        void RecognizePlate(ImageContext imageContext, bool useTesseract = true);
    }
    /// <inheritdoc cref="ILicensePlateReader" />
    public class LicensePlateReader : ILicensePlateReader
    {
        private readonly IDictionary<string, string> _ocrParams;
        private readonly IImageConverter _imageConverter;
        public LicensePlateReader(IImageConverter imageConverter)
        {
            _imageConverter = imageConverter;
            _ocrParams = new Dictionary<string, string>
            {
                { "TEST_DATA_PATH", "D:\\OCR\\"},
                { "TEST_DATA_LANG", ""},
                { "WHITE_LIST", "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890 " }
            };
        }
        public void RecognizePlate(ImageContext imageContext, bool useTesseract = true)
        {
            //RecognizePlateWithSplit(imageContext);
            RecognizePlate(imageContext);
        }
        /// <summary>
        /// Performs potential number recognition by splitting 
        /// the number into single characters
        /// </summary>
        /// <param name="imageContext">Context of the image</param>
        private void RecognizePlate(ImageContext imageContext)
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
                        Console.WriteLine($"{imageContext.FileName}: {potentialNumber}");

                        actualLicensePlates.Add(new ActualLicensePlate(potentialLicensePlate, potentialNumber, platesArea.ToImage<Hsv,byte>()));
                    }
                    //string potentialNumber = RecognizeNumber(platesArea.First().Item1, PageSegMode.SingleBlock);
                    //if (ValidateCharactersSet(ref potentialNumber))
                    //{
                    //    Console.WriteLine($"{imageContext.FileName}: {potentialNumber}");
                    //    actualLicensePlates.Add(new ActualLicensePlate(potentialLicensePlate, potentialNumber));
                    //}
                }
            }

            imageContext.ActualLicensePlates = actualLicensePlates;
        }
        /// <summary>
        /// Performs potential number recognition without 
        /// splitting it into single characters array. Only
        /// character segmentation is being applied.
        /// </summary>
        /// <param name="imageContext">Context of the image</param>
        private void RecognizePlateWithSplit(ImageContext imageContext)
        {
            _ocrParams["TEST_DATA_LANG"] = "lplate+mf";

            List<ActualLicensePlate> foundPlates = new List<ActualLicensePlate>();

            foreach (var image in imageContext.PotentialSecondLayerLicensePlates)
            {
                List<string> plateNumber = new List<string>();
                var characterAreas = FindPlateContours(image.Image, true);
                //if (characterAreas != null)
                //    foreach (var character in characterAreas)
                //        plateNumber.Add(RecognizeNumber(character.Item1, PageSegMode.SingleChar));

                if (plateNumber.Count > 5)
                    Console.WriteLine($"{imageContext.FileName}: {String.Join("", plateNumber)}");
            }

            imageContext.ActualLicensePlates = foundPlates;
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

                var treshold = ImageConverter.GetAutomatedTreshold(bilateralMat.ToImage<Gray, byte>());

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

                var cleanMat = new Mat(cannyMat.Rows, cannyMat.Cols, DepthType.Cv8U, 1);
                cleanMat.SetTo(new MCvScalar(255));


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
                            ratio >= 0.06 &&
                            area >= 400)
                        {
                            Mat ROI = new Mat(cleanMat, rect);
                            Mat potentialCharArea = new Mat(croppedImage.Convert<Gray, byte>().Mat, rect);

                            potentialCharArea.CopyTo(ROI);

                            //if (split)
                            //{
                            //   

                            //    //UMat potentialCharArea = new UMat(croppedImage.Convert<Gray, byte>().ToUMat(), rect);
                            //    //mats.Add((potentialCharArea, rect.X));
                            //}
                            //else
                            //    CvInvoke.Rectangle(croppedImage, rect, new MCvScalar(0, 250, 0));

                            charactersCnt++;
                        }
                    }
                }

                return cleanMat;

            }
            return default;
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
            using (var ocr = new Emgu.CV.OCR.Tesseract())
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

                if (characters.Length == 9 ||
                    Regex.IsMatch(characters, lastLetterSpaceMatch))
                    characters = characters[0..^1].Trim();

                if (
                    characters.Where(a => Char.IsLetter(a)).Count() > 5 ||
                    characters.Where(a => Char.IsNumber(a)).Count() > 5 ||
                    characters.Where(a => Char.IsNumber(a)).Count() < 1 ||
                    characters.Where(a => Char.IsLetter(a)).Count() < 2)
                    return false;

                if (characters.Replace(" ", "").Length < 9 &&
                    characters.Replace(" ", "").Length > 5)
                    return true;
            }

            return false;
        }
    }
}

