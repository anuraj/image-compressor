# Jekyll Image Compressor 🖼️✨

A high-performance GitHub Action that automatically compresses and optimizes images in your Jekyll blog, reducing file sizes and improving page load times without sacrificing visual quality.

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## 🎯 Why Use This Action?

### Speed Up Your Blog
- **Faster Page Loads**: Compressed images mean quicker loading times for your readers
- **Better SEO**: Google favors fast-loading websites in search rankings
- **Reduced Bandwidth**: Save on hosting costs with smaller file sizes

### Automated Optimization
- **Zero Manual Work**: Automatically compresses images in every commit or pull request
- **Smart Compression**: Uses industry-leading ImageSharp library for optimal results
- **Format-Specific**: Tailored compression strategies for JPEG and PNG files

### Flexible & Powerful
- **Configurable Quality**: Choose between maximum compression or highest visual quality
- **Responsive Images**: Optionally resize images to a maximum width for responsive design
- **Non-Destructive**: Only compresses images that can be optimized, skips already-optimized files

## 🚀 Quick Start

### Basic Usage

Add this to your workflow file (e.g., `.github/workflows/compress-images.yml`):

```yaml
name: Compress Images

on:
  push:
    paths:
      - 'assets/images/**'
  pull_request:
    paths:
      - 'assets/images/**'

jobs:
  compress:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Compress Images
        uses: anuraj/image-compressor@v1
        with:
          images-path: 'assets/images'
          quality: 85
          max-width: 1920
```

### Configuration Options

| Input | Description | Default | Required |
|-------|-------------|---------|----------|
| `images-path` | Path to the directory containing images | `assets/images` | No |
| `quality` | JPEG compression quality (1-100, higher = better quality) | `85` | No |
| `max-width` | Maximum width in pixels (0 to skip resizing) | `1920` | No |

### Example: High-Quality Compression

```yaml
- name: Compress Images (High Quality)
  uses: anuraj/image-compressor@v1
  with:
    images-path: 'assets/images'
    quality: 90        # Higher quality
    max-width: 2560    # Support 2K displays
```

### Example: Aggressive Compression

```yaml
- name: Compress Images (Maximum Compression)
  uses: anuraj/image-compressor@v1
  with:
    images-path: 'assets/images'
    quality: 75        # More compression
    max-width: 1280    # Standard HD width
```

### Example: Compression Without Resizing

```yaml
- name: Compress Images (No Resize)
  uses: anuraj/image-compressor@v1
  with:
    images-path: 'assets/images'
    quality: 85
    max-width: 0       # Disable resizing
```

## 📊 Action Outputs

The action provides useful metrics you can use in subsequent steps:

```yaml
- name: Compress Images
  id: compress
  uses: anuraj/image-compressor@v1

- name: Show Compression Stats
  run: |
    echo "Compressed ${{ steps.compress.outputs.compressed-count }} images"
    echo "Saved ${{ steps.compress.outputs.saved-bytes }} bytes"
```

| Output | Description |
|--------|-------------|
| `compressed-count` | Number of images successfully compressed |
| `saved-bytes` | Total bytes saved across all compressed images |

## 🔧 Complete Workflow Example

Here's a complete example that compresses images and commits them back to the repository:

```yaml
name: Optimize Images

on:
  push:
    branches: [main]
    paths:
      - 'assets/images/**'
  workflow_dispatch:

jobs:
  optimize:
    runs-on: ubuntu-latest
    permissions:
      contents: write
    
    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
      
      - name: Compress Images
        id: compress
        uses: anuraj/image-compressor@v1
        with:
          images-path: 'assets/images'
          quality: 85
          max-width: 1920
      
      - name: Commit Optimized Images
        if: steps.compress.outputs.compressed-count > 0
        run: |
          git config --local user.email "github-actions[bot]@users.noreply.github.com"
          git config --local user.name "github-actions[bot]"
          git add assets/images/
          git commit -m "🖼️ Optimize images: compressed ${{ steps.compress.outputs.compressed-count }} files, saved ${{ steps.compress.outputs.saved-bytes }} bytes"
          git push
```

## 🏗️ Architecture & Implementation

### Technology Stack

- **.NET 10.0**: Modern, high-performance runtime
- **ImageSharp**: Industry-leading cross-platform image processing library
- **System.CommandLine**: Type-safe command-line parsing
- **GitHub Actions Core**: Native GitHub Actions integration

### Project Structure

```
src/
├── Program.cs                          # Application entry point
├── Models/
│   └── CompressionResult.cs           # Data model for compression statistics
├── Services/
│   ├── ImageCompressorService.cs      # Core compression logic
│   └── FileHelper.cs                  # File utility methods
└── Handlers/
    └── CommandLineHandler.cs          # CLI configuration and setup
```

### Design Principles

#### Separation of Concerns
Each component has a single, well-defined responsibility:
- **CommandLineHandler**: Manages CLI interface and option parsing
- **ImageCompressorService**: Handles image processing and compression
- **FileHelper**: Provides utility functions for file operations
- **CompressionResult**: Encapsulates compression statistics

#### Key Components

**1. ImageCompressorService**
```csharp
public class ImageCompressorService
{
    // Main orchestration method
    public async Task<CompressionResult> CompressImagesAsync(...)
    
    // Handles individual image processing
    private async Task ProcessImageAsync(...)
    
    // Smart resizing with aspect ratio preservation
    private void ResizeImage(...)
    
    // Format-specific compression
    private async Task CompressAndSaveImageAsync(...)
}
```

**2. Compression Strategy**

- **PNG Files**: Uses `PngCompressionLevel.BestCompression` for lossless optimization
- **JPEG Files**: Configurable quality setting (default: 85) for lossy compression
- **Resizing**: Maintains aspect ratio when resizing to maximum width

**3. Processing Pipeline**

1. **Discovery**: Recursively scans directory for `.jpg`, `.jpeg`, and `.png` files
2. **Analysis**: Records original file size for comparison
3. **Transformation**: Resizes image if it exceeds maximum width
4. **Compression**: Applies format-specific compression algorithms
5. **Validation**: Compares file sizes and reports savings
6. **Reporting**: Aggregates statistics and outputs results

### Performance Characteristics

- **Memory Efficient**: Processes images one at a time to minimize memory usage
- **Error Resilient**: Continues processing remaining images if one fails
- **Detailed Logging**: Provides per-image compression statistics
- **Smart Optimization**: Skips images that are already optimized

### Extensibility

The modular architecture makes it easy to extend:

- Add new image formats by extending `GetImageFiles()` and `CompressAndSaveImageAsync()`
- Implement custom compression strategies by modifying `ImageCompressorService`
- Add new CLI options through `CommandLineHandler`
- Integrate additional image processing operations (watermarks, filters, etc.)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [ImageSharp](https://github.com/SixLabors/ImageSharp) by Six Labors
- Powered by [GitHub Actions](https://github.com/features/actions)