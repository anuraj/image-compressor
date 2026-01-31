using System.CommandLine;
using Actions.Core.Extensions;
using Actions.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

using var services = new ServiceCollection()
    .AddGitHubActionsCore()
    .BuildServiceProvider();

var core = services.GetRequiredService<ICoreService>();

var pathOption = new Option<string>("--path")
{
    Description = "The path to the directory containing images to compress. Default is 'assets/images'.",
    DefaultValueFactory = (arg) =>
    {
        return string.IsNullOrEmpty(arg.GetValueOrDefault<string>()) ? "assets/images" : arg.GetValueOrDefault<string>();
    }
};

var qualityOption = new Option<int>("--quality")
{
    Description = "The quality to compress images to (1-100). Higher is better quality. Default is 75.",
    DefaultValueFactory = (arg) =>
    {
        return arg.GetValueOrDefault<int>() == 0 ? 75 : arg.GetValueOrDefault<int>();
    }
};

var maxWidthOption = new Option<int>("--max-width")
{
    Description = "The maximum width to resize images to. Set to 0 to disable resizing. Default is 0.",
    DefaultValueFactory = (arg) =>
    {
        return arg.GetValueOrDefault<int>() == 0 ? 0 : arg.GetValueOrDefault<int>();
    }
};

var rootCommand = new RootCommand("Compress images for Jekyll blog")
{
    pathOption,
    qualityOption,
    maxWidthOption
};

rootCommand.Description = "Compress images in the specified directory using ImageSharp.";

rootCommand.SetAction(async (result) =>
{
    var imagesPath = result.GetValue(pathOption)!;
    var quality = result.GetValue(qualityOption);
    var maxWidth = result.GetValue(maxWidthOption);
    try
    {
        await CompressImages(imagesPath, quality, maxWidth);
    }
    catch (Exception ex)
    {
        core.SetFailed($"Image compression failed: {ex.Message}");
        Environment.ExitCode = 1;
    }
    
});

return rootCommand.Parse(args).Invoke();

async Task CompressImages(string imagesPath, int quality, int maxWidth)
{
    if (!Directory.Exists(imagesPath))
    {
        core.WriteWarning($"Images path '{imagesPath}' does not exist. Skipping compression.");
        await core.SetOutputAsync("compressed-count", "0");
        await core.SetOutputAsync("saved-bytes", "0");
        return;
    }

    var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
    var imageFiles = Directory.GetFiles(imagesPath, "*.*", SearchOption.AllDirectories)
        .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
        .ToList();

    if (imageFiles.Count == 0)
    {
        core.WriteInfo("No images found to compress.");
        await core.SetOutputAsync("compressed-count", "0");
        await core.SetOutputAsync("saved-bytes", "0");
        return;
    }

    core.WriteInfo($"Found {imageFiles.Count} images to process");

    int compressedCount = 0;
    long totalBytesSaved = 0;

    // using var group = await core.GroupAsync("Compressing Images");

    foreach (var imagePath in imageFiles)
    {
        var originalSize = new FileInfo(imagePath).Length;

        try
        {
            using var image = await Image.LoadAsync(imagePath);
            var extension = Path.GetExtension(imagePath).ToLower();

            // Resize if needed
            if (maxWidth > 0 && image.Width > maxWidth)
            {
                var ratio = (double)maxWidth / image.Width;
                var newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(maxWidth, newHeight));
                core.WriteDebug($"Resized {Path.GetFileName(imagePath)} to {maxWidth}x{newHeight}");
            }

            // Compress based on format
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

            var newSize = new FileInfo(imagePath).Length;
            var savedBytes = originalSize - newSize;

            if (savedBytes > 0)
            {
                totalBytesSaved += savedBytes;
                compressedCount++;
                var savedPercent = savedBytes * 100.0 / originalSize;
                core.WriteInfo($"✓ {Path.GetFileName(imagePath)}: {FormatBytes(originalSize)} → {FormatBytes(newSize)} (saved {savedPercent:F1}%)");
            }
            else
            {
                core.WriteDebug($"○ {Path.GetFileName(imagePath)}: Already optimized");
            }
        }
        catch (Exception ex)
        {
            core.WriteWarning($"Failed to compress {Path.GetFileName(imagePath)}: {ex.Message}");
        }
    }

    core.WriteInfo($"\n✅ Compressed {compressedCount} images, saved {FormatBytes(totalBytesSaved)} total");

    await core.SetOutputAsync("compressed-count", compressedCount.ToString());
    await core.SetOutputAsync("saved-bytes", totalBytesSaved.ToString());
}

static string FormatBytes(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB" };
    double len = bytes;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len /= 1024;
    }
    return $"{len:0.##} {sizes[order]}";
}