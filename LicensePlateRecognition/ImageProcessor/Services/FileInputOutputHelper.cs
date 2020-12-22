using ImageProcessor.Models;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ImageProcessor.Services
{
    public interface IFileInputOutputHelper
    {
        ImageContext ReadImage(string filePath);
        IEnumerable<ImageContext> ReadImages(string folderPath, FileType fileType, bool recursiveSearch = false);
        void SaveImage(ImageContext image, bool deleteIfExist = false);
    }

    public class FileInputOutputHelper : IFileInputOutputHelper
    {
        public ImageContext ReadImage(string filePath)
        {
            using var img = Image.FromFile(filePath);
            return new ImageContext(filePath, (Image) img.Clone());
        }

        public IEnumerable<ImageContext> ReadImages(string folderPath, FileType fileType, bool recursiveSearch = false)
        {
            var files = new DirectoryInfo(folderPath).GetFiles(
                $"*.{fileType}",
                recursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                yield return ReadImage(file.FullName);
            }
        }

        public void SaveImage(ImageContext image, bool deleteIfExist = false)
        {
            var path = image.GetProcessedFullPath();
            if (deleteIfExist && File.Exists(path))
            {
                File.Move(path, path + "_old");
                File.Delete(path + "_old");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (image.GenericImage != null)
            {
                image.GenericImage.Save(path);
            }
            else
            {
                image.ProcessedBitmap.Save(path);
            }
        }
    }
}
