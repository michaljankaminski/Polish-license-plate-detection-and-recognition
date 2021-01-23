using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ImageProcessor.Models.LicensePlate;

namespace ImageProcessor.Models
{
    public class ImageContext : IDisposable
    {
        public string FolderPath { get; }
        public string FileName { get; }
        public FileType FileType { get; }

        public double WidthResizeRatio { get; set; }
        public double HeightResizeRatio { get; set; }

        public Bitmap OriginalBitmap { get; set; }
        public Image<Gray, byte> ProcessedImage { get; set; }
        public Image<Rgb, byte> ContoursImage { get; set; }

        public IReadOnlyList<PotentialFirstLayerLicensePlate> PotentialFirstLayerLicensePlates { get; set; }
        public IReadOnlyList<PotentialSecondLayerLicensePlate> PotentialSecondLayerLicensePlates { get; set; }
        public IReadOnlyList<ActualLicensePlate> ActualLicensePlates { get; set; }

        public ImageContext(string filePath, Image image)
        {
            FolderPath = Path.GetDirectoryName(filePath);
            FileName = Path.GetFileNameWithoutExtension(filePath);
            FileType = Enum.Parse<FileType>(Path.GetExtension(filePath).Substring(1),true);
            OriginalBitmap = new Bitmap(image);
        }

        public void Dispose()
        {
            ProcessedImage?.Dispose();
            OriginalBitmap?.Dispose();
            ContoursImage?.Dispose();

            foreach (var potentialLicensePlate in PotentialFirstLayerLicensePlates)
            {
                potentialLicensePlate.Image.Dispose();
            }

            foreach (var actualLicensePlates in PotentialSecondLayerLicensePlates)
            {
                actualLicensePlates.Image.Dispose();
            }
        }
    }
}
