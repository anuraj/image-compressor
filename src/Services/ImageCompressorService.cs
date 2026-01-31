using Actions.Core.Services;
using ImageCompressor.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace ImageCompressor.Services;

public class ImageCompressorService(ICoreService core)
{
    private readonly ICoreService _core = core;

    public async Task<CompressionResult> CompressImagesAsync(string imagesPath, int quality, int maxWidth)
    {
        if (!Directory.Exists(imagesPath))
        {
            _core.WriteWarning($"Images path '{imagesPath}' does not exist. Skipping compression.");
            return new CompressionResult();
        }

        // Get only changed/added images in the current commit/PR
        var changedImageFiles = GitHelper.GetChangedImageFiles();

        // Filter to only files in the specified images path
        var imageFiles = changedImageFiles
            .Where(f => f.StartsWith(imagesPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (imageFiles.Count == 0)
        {
            _core.WriteInfo("No changed images found to compress.");
            return new CompressionResult();
        }

        _core.WriteInfo($"Found {imageFiles.Count} images to process");

        var result = new CompressionResult
        {
            TotalImagesProcessed = imageFiles.Count
        };

        foreach (var imagePath in imageFiles)
        {
            await ProcessImageAsync(imagePath, quality, maxWidth, result);
        }

        _core.WriteInfo(@$"\n✅ Compressed {result.CompressedCount} images, 
            saved {FileHelper.FormatBytes(result.TotalBytesSaved)} total");

        return result;
    }

    private async Task ProcessImageAsync(string imagePath, int quality, int maxWidth, CompressionResult result)
    {
        var originalSize = new FileInfo(imagePath).Length;

        try
        {
            using var image = await Image.LoadAsync(imagePath);
            var extension = Path.GetExtension(imagePath).ToLower();

            // Resize if needed
            if (maxWidth > 0 && image.Width > maxWidth)
            {
                ResizeImage(image, maxWidth, imagePath);
            }

            // Compress based on format
            await CompressAndSaveImageAsync(image, imagePath, extension, quality);

            UpdateCompressionStats(imagePath, originalSize, result);
        }
        catch (Exception ex)
        {
            _core.WriteWarning($"Failed to compress {Path.GetFileName(imagePath)}: {ex.Message}");
        }
    }

    private void ResizeImage(Image image, int maxWidth, string imagePath)
    {
        var ratio = (double)maxWidth / image.Width;
        var newHeight = (int)(image.Height * ratio);

        image.Mutate(x => x.Resize(maxWidth, newHeight));
        _core.WriteDebug($"Resized {Path.GetFileName(imagePath)} to {maxWidth}x{newHeight}");
    }

    private static async Task CompressAndSaveImageAsync(Image image, string imagePath, 
        string extension, int quality)
    {
        if (extension == ".png")
        {
            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression
            };
            await image.SaveAsync(imagePath, encoder);
        }
        else // .jpg or .jpeg
        {
            var encoder = new JpegEncoder
            {
                Quality = quality
            };
            await image.SaveAsync(imagePath, encoder);
        }
    }

    private void UpdateCompressionStats(string imagePath, long originalSize, CompressionResult result)
    {
        var newSize = new FileInfo(imagePath).Length;
        var savedBytes = originalSize - newSize;

        if (savedBytes > 0)
        {
            result.TotalBytesSaved += savedBytes;
            result.CompressedCount++;
            var savedPercent = savedBytes * 100.0 / originalSize;
            _core.WriteInfo(@$"✓ {Path.GetFileName(imagePath)}: {FileHelper.FormatBytes(originalSize)} 
                → {FileHelper.FormatBytes(newSize)} (saved {savedPercent:F1}%)");
        }
        else
        {
            _core.WriteDebug($"○ {Path.GetFileName(imagePath)}: Already optimized");
        }
    }
}
