using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ImageProcessor;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace LicensePlatesRecognizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _fileFilter = "Image files (*.jpg)|*.jpg|*.png|*.jpeg";
        private readonly IImageProcessing _imageProcessing;
        public MainWindow(IImageProcessing imageProcessing)
        {
            _imageProcessing = imageProcessing;
            InitializeComponent();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.Filter = "";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
                var filePath = dlg.FileName;
                var initialBmp = new BitmapImage(new Uri(filePath));
                imgFormat.Text = $"{initialBmp.Format.BitsPerPixel} bpp";
                imgHeight.Text = initialBmp.Height.ToString();
                imgWidth.Text = initialBmp.Width.ToString();

                orgPhotoContainer.Source = initialBmp;

                Task.Run(() =>
                {
                    DetectPlate(filePath);
                });
            }
        }
        private void DetectPlate(string filePath)
        {
            var outImage = _imageProcessing.Process(filePath);
            var outBitmapImage = ToBitmapImage(outImage);

            this.Dispatcher.Invoke(() =>
            {
                outPhotoContainer.Source = outBitmapImage;
            });
        }

        #region HELPER
        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
        #endregion
    }
}
