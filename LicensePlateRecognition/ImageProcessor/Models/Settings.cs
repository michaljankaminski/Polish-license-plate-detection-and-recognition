namespace ImageProcessor.Models
{
    public class Settings
    {
        public string ImagesPath { get; set; }
        public static string TrainedDataPath { get; set; } = "D:\\OCR\\";

        public int KernelSize { get; set; } = 7;//5
        public double Sigma { get; set; } = 1.2;//1.5
        public bool UseAutoThreshold { get; set; } = true;
        public double LowThreshold { get; set; } = 50;
        public double HighThreshold { get; set; } = 150;//200

        public static int ResizeWidth { get; } = 1000;
        public static int ResizeHeight { get; } = 750;
    }
}
