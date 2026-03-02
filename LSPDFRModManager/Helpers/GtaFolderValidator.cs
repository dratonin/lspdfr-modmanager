using System.IO;

namespace LSPDFRModManager.Helpers;

/// <summary>
/// Validates that a folder is a genuine GTA V installation
/// by checking for the presence of GTA5.exe.
/// </summary>
public static class GtaFolderValidator
{
    /// <summary>
    /// Returns <c>true</c> if the directory exists and contains GTA5.exe.
    /// </summary>
    public static bool IsValid(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return false;

        string exePath = Path.Combine(folderPath, "GTA5.exe");
        return File.Exists(exePath);
    }
}
