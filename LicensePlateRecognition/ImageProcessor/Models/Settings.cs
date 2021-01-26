namespace ImageProcessor.Models
{
    public class Settings
    {
        public string ImagesPath { get; set; }

        public int KernelSize { get; set; } = 5;//5
        public double Sigma { get; set; } = 1.4;//1.5
        public bool UseAutoThreshold { get; set; } = true;
        public double LowThreshold { get; set; } = 50;
        public double HighThreshold { get; set; } = 150;//200

        public static int ResizeWidth { get; } = 800;
        public static int ResizeHeight { get; } = 600;
    
    }
}
