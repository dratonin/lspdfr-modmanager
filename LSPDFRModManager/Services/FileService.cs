using System.IO;
using System.IO.Compression;
using LSPDFRModManager.Helpers;
using SharpCompress.Archives;

namespace LSPDFRModManager.Services;

/// <summary>
/// Handles file-system operations related to mod installation.
/// Supports both ZIP and RAR archive formats.
/// </summary>
public sealed class FileService
{
    private static readonly string[] ZipExtensions = [".zip"];
    private static readonly string[] RarExtensions = [".rar"];

    /// <summary>
    /// Validates and installs a mod from a .zip or .rar archive into the GTA V folder.
    /// Returns <c>true</c> on success, <c>false</c> on failure.
    /// Progress and error messages are reported through <paramref name="progress"/>.
    /// </summary>
    public async Task<bool> InstallModAsync(
        string archivePath, string gtaFolderPath, IProgress<string> progress)
    {
        try
        {
            string ext = Path.GetExtension(archivePath).ToLowerInvariant();

            // Step 1 — Validate the archive
            progress.Report("Validating archive\u2026");

            if (ZipExtensions.Contains(ext))
            {
                bool valid = await Task.Run(() => ValidateZip(archivePath));
                if (!valid)
                {
                    progress.Report("Error: Invalid or empty ZIP archive.");
                    return false;
                }
            }
            else if (RarExtensions.Contains(ext))
            {
                bool valid = await Task.Run(() => ValidateRar(archivePath));
                if (!valid)
                {
                    progress.Report("Error: Invalid or empty RAR archive.");
                    return false;
                }
            }
            else
            {
                progress.Report("Error: Unsupported archive format. Use .zip or .rar.");
                return false;
            }

            // Step 2 — Extract and copy into GTA V folder
            await ExtractAndCopyModAsync(archivePath, gtaFolderPath, progress);
            return true;
        }
        catch (InvalidDataException)
        {
            progress.Report("Error: Not a valid archive file.");
            Logger.Log($"Invalid archive file: {archivePath}");
            return false;
        }
        catch (Exception ex)
        {
            progress.Report($"Error: {ex.Message}");
            Logger.Log($"Mod install failed for {archivePath}: {ex}");
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
                Logger.Log($"ZIP validation failed: archive is empty \u2014 {zipPath}");
                return false;
            }
            return true;
        }
        catch (InvalidDataException)
        {
            Logger.Log($"ZIP validation failed: corrupt archive \u2014 {zipPath}");
            return false;
        }
    }

    /// <summary>
    /// Checks that a RAR file is readable and contains at least one entry.
    /// </summary>
    private static bool ValidateRar(string rarPath)
    {
        try
        {
            using var archive = ArchiveFactory.OpenArchive(rarPath);
            if (!archive.Entries.Any(e => !e.IsDirectory))
            {
                Logger.Log($"RAR validation failed: archive is empty \u2014 {rarPath}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"RAR validation failed: {ex.Message} \u2014 {rarPath}");
            return false;
        }
    }

    /// <summary>
    /// Extracts a mod archive (.zip or .rar) to a temporary folder, then copies
    /// all files into the GTA V directory (preserving subdirectories).
    /// </summary>
    public async Task ExtractAndCopyModAsync(
        string archivePath, string gtaFolderPath, IProgress<string> progress)
    {
        string tempDir = Path.Combine(
            Path.GetTempPath(),
            "LSPDFRModManager_" + Guid.NewGuid().ToString("N"));

        try
        {
            progress.Report("Extracting mod archive\u2026");

            string ext = Path.GetExtension(archivePath).ToLowerInvariant();
            if (ZipExtensions.Contains(ext))
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, tempDir));
            }
            else
            {
                await Task.Run(() => ExtractWithSharpCompress(archivePath, tempDir));
            }

            progress.Report("Copying files to GTA V folder\u2026");
            await Task.Run(() => CopyDirectory(tempDir, gtaFolderPath));

            progress.Report("Mod installed successfully! \u2705");
            Logger.Log($"Mod installed from {archivePath}.");
        }
        finally
        {
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
    /// Extracts an archive using SharpCompress (supports RAR, 7z, tar, etc.).
    /// </summary>
    private static void ExtractWithSharpCompress(string archivePath, string destDir)
    {
        using var archive = ArchiveFactory.OpenArchive(archivePath);
        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
        {
            entry.WriteToDirectory(destDir);
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
