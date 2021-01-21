namespace ImageProcessor.Models
{
    public class Settings
    {
        public string ImagesPath { get; set; }

        public int KernelSize { get; set; } = 5;//5
        public double Sigma { get; set; } = 1.5;//2
        public double LowThreshold { get; set; } = 80;
        public double HighThreshold { get; set; } = 240;//200
    }
}
