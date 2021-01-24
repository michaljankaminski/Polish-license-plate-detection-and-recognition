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
        private string _filePath = "";
        public MainWindow(IImageProcessing imageProcessing)
        {
            _imageProcessing = imageProcessing;
            InitializeComponent();
            loaderImg.Visibility = Visibility.Hidden;
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
                imgPalette.Text = initialBmp.PixelWidth.ToString();
                orgPhotoContainer.Source = initialBmp;
                _filePath = filePath;
                
            }
        }
        private void StartProcessing_Click(object sender, RoutedEventArgs e)
        {
            if(!String.IsNullOrEmpty(_filePath))
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Process confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    loaderImg.Visibility = Visibility.Visible;
                    Task.Run(() =>
                    {
                        DetectPlate(_filePath);
                    });
                }
                    
            }
            
        }
        private void DetectPlate(string filePath)
        {
            var outImage = _imageProcessing.Process(filePath);
            var outBitmapImage = ToBitmapImage(outImage);

            this.Dispatcher.Invoke(() =>
            {
                outPhotoContainer.Source = outBitmapImage;
                loaderImg.Visibility = Visibility.Hidden;
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
