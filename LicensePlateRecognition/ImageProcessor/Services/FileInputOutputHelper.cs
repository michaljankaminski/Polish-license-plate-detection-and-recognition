using ImageProcessor.Models;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

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
            DeleteFileAndCreateDirectory(path);

            image.GenericImage?.Save(path);

            if (image.ContoursImage != null)
            {
                path = image.GetContoursFullPath();
                DeleteFileAndCreateDirectory(path);
                image.ContoursImage.Save(path);
            }

            if (image.PotentialLicensePlates?.Count > 0)
            {
                DeleteFileAndCreateDirectory(image.GetPotentialLicensePlateFullPath(-1));

                for (var i = 0; i < image.PotentialLicensePlates.Count; i ++ )
                { 
                    path = image.GetPotentialLicensePlateFullPath(i);
                    image.PotentialLicensePlates[i].Save(path);
                }
            }
        }

        private void DeleteFileAndCreateDirectory(string path)
        {
            if (File.Exists(path))
            {
                File.Move(path, path + "_old");
                File.Delete(path + "_old");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
    }
}
