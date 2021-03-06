﻿using ImageProcessor.Helpers;
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
        private readonly IImagePathProvider _imagePathProvider;

        public FileInputOutputHelper(IImagePathProvider imagePathProvider)
        {
            _imagePathProvider = imagePathProvider;
        }

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
            var path = _imagePathProvider.GetProcessedFullPath(image);
            CreateDirectory(path);

            image.ProcessedImage?.Save(path);

            if (image.ContoursImage != null)
            {
                path = _imagePathProvider.GetContoursFullPath(image);
                CreateDirectory(path);
                image.ContoursImage.Save(path);
            }

            if (image.PotentialFirstLayerLicensePlates?.Count > 0)
            {
                CreateDirectory(_imagePathProvider.GetPotentialLicensePlateFullPath(image, -1));

                for (var i = 0; i < image.PotentialFirstLayerLicensePlates.Count; i ++ )
                {
                    path = _imagePathProvider.GetPotentialLicensePlateFullPath(image, i);
                    image.PotentialFirstLayerLicensePlates[i].Image.Save(path);
                }
            }

            if (image.PotentialSecondLayerLicensePlates?.Count > 0)
            {
                CreateDirectory(_imagePathProvider.GetPotentialLicensePlate2FullPath(image, -1));

                for (var i = 0; i < image.PotentialSecondLayerLicensePlates.Count; i++)
                {
                    path = _imagePathProvider.GetPotentialLicensePlate2FullPath(image, i);
                    image.PotentialSecondLayerLicensePlates[i].Image.Save(path);
                }
            }
            if (image.ActualLicensePlates?.Count > 0)
            {
                CreateDirectory(_imagePathProvider.GetActualLicensePlateFullPath(image, -1, ""));

                for (var i = 0; i < image.ActualLicensePlates.Count; i++)
                {
                    path = _imagePathProvider.GetActualLicensePlateFullPath(image, i, image.ActualLicensePlates[i].PlateNumber);
                    image.ActualLicensePlates[i].Image.Save(path);
                }
            }

            CreateDirectory(_imagePathProvider.GetFinalImageFullPath(image));
            path = _imagePathProvider.GetFinalImageFullPath(image);
            image.ImageWithLicenses?.Save(path);
        }

        private static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        }
    }
}
