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
        public string FolderPath { get; set; }
        public string FileName { get; set; }
        public FileType FileType { get; set; }
        public Image OriginalImage { get; set; }

        public double WidthResizeRatio { get; set; }
        public double HeightResizeRatio { get; set; }

        public Image<Gray, byte> GenericImage { get; set; }
        public Bitmap ProcessedBitmap { get; set; }
        public Image<Rgb, byte> ContoursImage { get; set; }

        public IReadOnlyList<Bitmap> PotentialLicensePlates { get; set; }

        public string GetProcessedFullPath() => $"{FolderPath}/Processed/{FileName}_afterCanny.{FileType}";
        public string GetContoursFullPath() => $"{FolderPath}/Contours/{FileName}_contours.{FileType}";

        public string GetPotentialLicensePlateFullPath(int number) => $"{FolderPath}/Potential/{FileName}/{number}.{FileType}";

        public ImageContext(string filePath, Image image)
        {
            FolderPath = Path.GetDirectoryName(filePath);
            FileName = Path.GetFileNameWithoutExtension(filePath);
            FileType = Enum.Parse<FileType>(Path.GetExtension(filePath).Substring(1),true);
            OriginalImage = image;
            ProcessedBitmap = new Bitmap(image);
        }

        public void Dispose()
        {
            OriginalImage?.Dispose();
            GenericImage?.Dispose();
            ProcessedBitmap?.Dispose();
            ContoursImage?.Dispose();

            foreach (var potentialLicensePlate in PotentialLicensePlates)
            {
                potentialLicensePlate.Dispose();
            }
        }
    }
}
