using System.Net.Http;
using LSPDFRModManager.Helpers;

namespace LSPDFRModManager.Services;

/// <summary>
/// Fetches mod version information from the LCPDFR public API.
/// Uses a single shared HttpClient instance with a custom User-Agent header.
/// </summary>
public sealed class LcpdfrApiService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string ApiBaseUrl =
        "https://www.lcpdfr.com/applications/downloadsng/interface/api.php";

    static LcpdfrApiService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LSPDFRModManager/1.0");
    }

    /// <summary>
    /// Queries the LCPDFR API for the latest version of a file.
    /// Returns the version string, or a fallback message on failure.
    /// </summary>
    /// <param name="fileId">The LCPDFR file ID to query.</param>
    public async Task<string> GetVersionAsync(int fileId)
    {
        try
        {
            string url = $"{ApiBaseUrl}?do=checkForUpdates&fileId={fileId}&textOnly=1";
            string version = await HttpClient.GetStringAsync(url);

            version = version.Trim();
            if (string.IsNullOrEmpty(version))
                return "Unknown";

            Logger.Log($"LCPDFR API: fileId={fileId} → v{version}");
            return version;
        }
        catch (HttpRequestException ex)
        {
            Logger.Log($"LCPDFR API error for fileId={fileId}: {ex.Message}");
            return "Unavailable";
        }
        catch (TaskCanceledException)
        {
            Logger.Log($"LCPDFR API timeout for fileId={fileId}");
            return "Timeout";
        }
    }
}
