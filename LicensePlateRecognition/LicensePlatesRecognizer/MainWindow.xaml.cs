using ImageProcessor;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LicensePlatesRecognizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string FileFilter = "Image files (*.jpg)|*.jpg|*.png|*.jpeg";
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
            dlg.Filter = FileFilter;
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
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Process confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    outPhotoContainer.Source = null;
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

            outImage.Dispose();

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
