using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ImageProcessor.Models
{
    public class ImageContext : IDisposable
    {
        public string FolderPath { get; }
        public string FileName { get; }
        public FileType FileType { get; }

        public double WidthResizeRatio { get; set; }
        public double HeightResizeRatio { get; set; }

        public Bitmap ProcessedBitmap { get; set; }
        public Image<Gray, byte> GenericImage { get; set; }
        public Image<Rgb, byte> ContoursImage { get; set; }

        public IReadOnlyList<Bitmap> PotentialLicensePlates { get; set; }
        public IReadOnlyList<Image<Hsv, byte>> ActualLicensePlates { get; set; }

        public ImageContext(string filePath, Image image)
        {
            FolderPath = Path.GetDirectoryName(filePath);
            FileName = Path.GetFileNameWithoutExtension(filePath);
            FileType = Enum.Parse<FileType>(Path.GetExtension(filePath).Substring(1),true);
            ProcessedBitmap = new Bitmap(image);
        }

        public void Dispose()
        {
            GenericImage?.Dispose();
            ProcessedBitmap?.Dispose();
            ContoursImage?.Dispose();

            foreach (var potentialLicensePlate in PotentialLicensePlates)
            {
                potentialLicensePlate.Dispose();
            }

            foreach (var actualLicensePlates in ActualLicensePlates)
            {
                actualLicensePlates.Dispose();
            }
        }
    }
}
