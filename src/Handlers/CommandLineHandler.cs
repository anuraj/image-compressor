using System.CommandLine;
using Actions.Core.Services;
using ImageCompressor.Services;

namespace ImageCompressor.Handlers;

public class CommandLineHandler(ICoreService core, ImageCompressorService compressorService)
{
    private readonly ICoreService _core = core;
    private readonly ImageCompressorService _compressorService = compressorService;

    public RootCommand CreateRootCommand()
    {
        var pathOption = new Option<string>("--path")
        {
            Description = "The path to the directory containing images to compress. Default is 'assets/images'.",
            DefaultValueFactory = (arg) =>
            {
                return string.IsNullOrEmpty(arg.GetValueOrDefault<string>()) ? "assets/images" 
                    : arg.GetValueOrDefault<string>();
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
                var compressionResult = await _compressorService.CompressImagesAsync(imagesPath, quality, maxWidth);
                await _core.SetOutputAsync("compressed-count", compressionResult.CompressedCount.ToString());
                await _core.SetOutputAsync("saved-bytes", compressionResult.TotalBytesSaved.ToString());
            }
            catch (Exception ex)
            {
                _core.SetFailed($"Image compression failed: {ex.Message}");
                Environment.ExitCode = 1;
            }
        });

        return rootCommand;
    }
}
