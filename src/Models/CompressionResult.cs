namespace ImageCompressor.Models;

public class CompressionResult
{
    public int CompressedCount { get; set; }
    public long TotalBytesSaved { get; set; }
    public int TotalImagesProcessed { get; set; }
}
