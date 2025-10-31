using System;
using XUnity.AutoInstaller.Models;

namespace XUnity.AutoInstaller.Services;

/// <summary>
/// Singleton service for managing global game path state across the application
/// </summary>
public class GameStateService
{
    private static GameStateService? _instance;
    private static readonly object _lock = new object();

    private string? _currentGamePath;
    private readonly SettingsService _settingsService;

    /// <summary>
    /// Event fired when the game path changes
    /// </summary>
    public event EventHandler<string?>? GamePathChanged;

    /// <summary>
    /// Gets the singleton instance of GameStateService
    /// </summary>
    public static GameStateService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new GameStateService();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Gets the current game path
    /// </summary>
    public string? CurrentGamePath
    {
        get => _currentGamePath;
        private set
        {
            if (_currentGamePath != value)
            {
                _currentGamePath = value;
                GamePathChanged?.Invoke(this, _currentGamePath);
            }
        }
    }

    private GameStateService()
    {
        _settingsService = new SettingsService();
    }

    /// <summary>
    /// Initializes the service and loads the last used game path if enabled
    /// </summary>
    public void Initialize()
    {
        var settings = _settingsService.LoadSettings();

        if (settings.RememberLastGamePath && !string.IsNullOrEmpty(settings.LastGamePath))
        {
            // Validate the path exists before setting it
            if (System.IO.Directory.Exists(settings.LastGamePath))
            {
                // Use property assignment to trigger GamePathChanged event
                CurrentGamePath = settings.LastGamePath;
            }
        }
    }

    /// <summary>
    /// Sets the current game path and optionally saves it to settings
    /// </summary>
    /// <param name="gamePath">The game directory path</param>
    /// <param name="saveToSettings">Whether to save this path to persistent settings</param>
    public void SetGamePath(string? gamePath, bool saveToSettings = true)
    {
        CurrentGamePath = gamePath;

        if (saveToSettings && !string.IsNullOrEmpty(gamePath))
        {
            var settings = _settingsService.LoadSettings();
            settings.LastGamePath = gamePath;
            _settingsService.SaveSettings(settings);
        }
    }

    /// <summary>
    /// Clears the current game path
    /// </summary>
    public void ClearGamePath()
    {
        CurrentGamePath = null;
    }

    /// <summary>
    /// Checks if a valid game path is currently set
    /// </summary>
    public bool HasValidGamePath()
    {
        return !string.IsNullOrEmpty(_currentGamePath) &&
               System.IO.Directory.Exists(_currentGamePath);
    }
}
