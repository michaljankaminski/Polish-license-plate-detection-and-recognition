using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;

namespace ImageProcessor.Models
{
    public class ImageContext
    {
        public string FolderPath { get; set; }
        public string FileName { get; set; }
        public FileType FileType { get; set; }
        public Image OriginalImage { get; set; }
        public Image<Gray,byte> GenericImage { get; set; }
        public Bitmap ProcessedBitmap { get; set; }

        public string GetProcessedFullPath() => $"{FolderPath}/Processed/{FileName}_processed.{FileType}";

        public ImageContext(string filePath, Image image)
        {
            FolderPath = Path.GetDirectoryName(filePath);
            FileName = Path.GetFileNameWithoutExtension(filePath);
            FileType = Enum.Parse<FileType>(Path.GetExtension(filePath).Substring(1),true);
            OriginalImage = image;
            ProcessedBitmap = new Bitmap(image);
        }
    }
}
