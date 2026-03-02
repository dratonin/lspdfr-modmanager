using LSPDFRModManager.ViewModels;

namespace LSPDFRModManager.Models;

/// <summary>
/// Represents a mod available for download and installation.
/// Extends ViewModelBase so property changes (version, install state)
/// automatically notify the UI.
/// </summary>
public sealed class ModEntry : ViewModelBase
{
    // ──────────────────────────────────────────────
    //  Static properties (set once at construction)
    // ──────────────────────────────────────────────

    /// <summary>Display name of the mod.</summary>
    public string Name { get; }

    /// <summary>Short description shown on the card.</summary>
    public string Description { get; }

    /// <summary>Category used for grouping in the library.</summary>
    public string Category { get; }

    /// <summary>LCPDFR file ID used for API version lookups.</summary>
    public int FileId { get; }

    /// <summary>URL to the mod's download page on lcpdfr.com.</summary>
    public string DownloadPageUrl { get; }

    /// <summary>Optional thumbnail URL (placeholder for now).</summary>
    public string ThumbnailUrl { get; }

    // ──────────────────────────────────────────────
    //  Detail page content
    // ──────────────────────────────────────────────

    /// <summary>Full description for the mod detail page.</summary>
    public string DetailDescription { get; }

    /// <summary>Important notes shown on the detail page.</summary>
    public string Notes { get; }

    /// <summary>Installation instructions shown on the detail page.</summary>
    public string InstallationGuide { get; }

    /// <summary>Credits / attribution text.</summary>
    public string Credits { get; }

    // ──────────────────────────────────────────────
    //  Observable runtime state
    // ──────────────────────────────────────────────

    private string _version = "Fetching…";
    /// <summary>Current version fetched from the LCPDFR API.</summary>
    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    private bool _isInstalled;
    /// <summary>True after the mod has been successfully installed.</summary>
    public bool IsInstalled
    {
        get => _isInstalled;
        set => SetProperty(ref _isInstalled, value);
    }

    private bool _isInstalling;
    /// <summary>True while an installation is in progress.</summary>
    public bool IsInstalling
    {
        get => _isInstalling;
        set => SetProperty(ref _isInstalling, value);
    }

    private string _statusMessage = string.Empty;
    /// <summary>Per-card status text (progress, errors, etc.).</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ──────────────────────────────────────────────
    //  Constructor
    // ──────────────────────────────────────────────

    public ModEntry(
        string name,
        string description,
        string category,
        int fileId,
        string downloadPageUrl,
        string thumbnailUrl = "",
        string detailDescription = "",
        string notes = "",
        string installationGuide = "",
        string credits = "")
    {
        Name = name;
        Description = description;
        Category = category;
        FileId = fileId;
        DownloadPageUrl = downloadPageUrl;
        ThumbnailUrl = thumbnailUrl;
        DetailDescription = detailDescription;
        Notes = notes;
        InstallationGuide = installationGuide;
        Credits = credits;
    }
}
