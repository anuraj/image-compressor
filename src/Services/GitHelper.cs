namespace ImageCompressor.Services;

public static class GitHelper
{
    /// <summary>
    /// Gets a list of changed image files (added or modified) in the current commit/PR.
    /// </summary>
    /// <remarks>
    /// For Pull Requests: Uses git diff against the base branch (typically main/master)
    /// For Push events: Uses git diff against the previous commit
    /// </remarks>
    public static List<string> GetChangedImageFiles()
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png" };
        
        try
        {
            // Try to get changed files (works for both PR and push events)
            var changedFiles = GetGitDiff();
            
            if (changedFiles.Count == 0)
            {
                return [];
            }

            // Filter to only image files
            return changedFiles
                .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Where(f => File.Exists(f)) // Ensure file still exists (wasn't deleted)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not detect changed files via git: {ex.Message}");
            return [];
        }
    }

    private static List<string> GetGitDiff()
    {
        try
        {
            // For pull requests, get diff against the merge base
            // For push events, this will show changed files in the commit
            var result = RunGitCommand("diff --name-only --diff-filter=AM HEAD~1 HEAD");
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .ToList();
            }

            return [];
        }
        catch
        {
            // Fallback: Try getting diff for merge commits or first commits
            try
            {
                var result = RunGitCommand("diff --name-only --diff-filter=AM HEAD^ HEAD");
                return result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .ToList();
            }
            catch
            {
                return [];
            }
        }
    }

    private static string RunGitCommand(string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}
