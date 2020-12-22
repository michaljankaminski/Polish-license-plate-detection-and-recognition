namespace ImageProcessor.Models
{
    public class Settings
    {
        public string ImagesPath { get; set; }

        public int KernelSize { get; set; } = 9;//5
        public double Sigma { get; set; } = 4;//2
        public double LowThreshold { get; set; } = 0;
        public double HighThreshold { get; set; } = 160;//200
        public int WeakPixel { get; set; } = 100;
    }
}
