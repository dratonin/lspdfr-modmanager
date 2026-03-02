using System.IO;
using System.IO.Compression;
using LSPDFRModManager.Helpers;

namespace LSPDFRModManager.Services;

/// <summary>
/// Handles file-system operations related to mod installation.
/// Extracted from the old MainViewModel so multiple pages can reuse it.
/// </summary>
public sealed class FileService
{
    /// <summary>
    /// Validates and installs a mod from a .zip archive into the GTA V folder.
    /// Returns <c>true</c> on success, <c>false</c> on failure.
    /// Progress and error messages are reported through <paramref name="progress"/>.
    /// </summary>
    public async Task<bool> InstallModAsync(
        string zipPath, string gtaFolderPath, IProgress<string> progress)
    {
        try
        {
            // Step 1 — Validate the archive
            progress.Report("Validating archive…");
            bool valid = await Task.Run(() => ValidateZip(zipPath));
            if (!valid)
            {
                progress.Report("Error: Invalid or empty ZIP archive.");
                return false;
            }

            // Step 2 — Extract and copy into GTA V folder
            await ExtractAndCopyModAsync(zipPath, gtaFolderPath, progress);
            return true;
        }
        catch (InvalidDataException)
        {
            progress.Report("Error: Not a valid ZIP file.");
            Logger.Log($"Invalid ZIP file: {zipPath}");
            return false;
        }
        catch (Exception ex)
        {
            progress.Report($"Error: {ex.Message}");
            Logger.Log($"Mod install failed for {zipPath}: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Checks that a ZIP file is readable and contains at least one entry.
    /// </summary>
    private static bool ValidateZip(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            if (archive.Entries.Count == 0)
            {
                Logger.Log($"ZIP validation failed: archive is empty — {zipPath}");
                return false;
            }
            return true;
        }
        catch (InvalidDataException)
        {
            Logger.Log($"ZIP validation failed: corrupt archive — {zipPath}");
            return false;
        }
    }

    /// <summary>
    /// Extracts a .zip mod archive to a temporary folder, then copies
    /// all files into the GTA V directory (preserving subdirectories).
    /// Reports progress messages through <paramref name="progress"/>.
    /// </summary>
    public async Task ExtractAndCopyModAsync(
        string zipPath, string gtaFolderPath, IProgress<string> progress)
    {
        string tempDir = Path.Combine(
            Path.GetTempPath(),
            "LSPDFRModManager_" + Guid.NewGuid().ToString("N"));

        try
        {
            progress.Report("Extracting mod archive…");
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, tempDir));

            progress.Report("Copying files to GTA V folder…");
            await Task.Run(() => CopyDirectory(tempDir, gtaFolderPath));

            progress.Report("Mod installed successfully! ✅");
            Logger.Log($"Mod installed from {zipPath}.");
        }
        finally
        {
            // Clean up the temp folder (best-effort).
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Not critical — the OS will clean temp eventually.
            }
        }
    }

    /// <summary>
    /// Recursively copies all files and subdirectories from
    /// <paramref name="sourceDir"/> into <paramref name="destDir"/>,
    /// overwriting any existing files.
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, file);
            string destFile = Path.Combine(destDir, relativePath);

            string? destSubDir = Path.GetDirectoryName(destFile);
            if (destSubDir is not null)
                Directory.CreateDirectory(destSubDir);

            File.Copy(file, destFile, overwrite: true);
        }
    }
}
