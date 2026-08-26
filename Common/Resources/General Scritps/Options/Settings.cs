using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SETTINGS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Autoloaded singleton that persists the player's settings to a ConfigFile in user://settings.cfg and applies them to the engine.
	**  2 - Manages audio volumes (Master / Music / SFX), video options (resolution, window mode, VSync) and the game language (pt/en).
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Settings : Node
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    public static Settings Instance;

    // Emitted whenever a setting changes so UI can refresh itself.
    [Signal]
    public delegate void SettingsChangedEventHandler();

    private const string SettingsPath = "user://settings.cfg";
    private const string SectionAudio = "audio";
    private const string SectionVideo = "video";
    private const string SectionControls = "controls";
    private const string SectionLanguage = "language";

    private ConfigFile _config;

    // Audio volumes, normalized to 0.0 (silent) .. 1.0 (full).
    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;

    // Video settings.
    private Vector2I _resolution;
    // 0 = Windowed, 1 = Fullscreen, 2 = Borderless.
    private int _windowMode = 0;
    private bool _vsyncEnabled = true;

    // Game language locale code ("pt" or "en").
    private string _language = "pt";

    // Project-default bindings, cached before any saved rebinds override them,
    // so controls can be reset to their original values.
    private readonly System.Collections.Generic.Dictionary<string, Godot.Collections.Array<InputEvent>>
        _defaultBindings = new();

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    public float MasterVolume => _masterVolume;
    public float MusicVolume => _musicVolume;
    public float SfxVolume => _sfxVolume;

    public Vector2I Resolution => _resolution;
    public int WindowModeIndex => _windowMode;
    public bool VsyncEnabled => _vsyncEnabled;
    public string Language => _language;

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        // Singleton guard, following the AudioManager pattern.
        if (Instance == null || !GodotObject.IsInstanceValid(Instance))
        {
            Instance = this;
        }
        else
        {
            GD.PrintErr("Multiple Settings instances detected!");
            QueueFree();
            return;
        }

        LoadSettings();
    }

    /// <summary>
    /// Loads the ConfigFile from disk (if present) and applies the stored values.
    /// </summary>
    private void LoadSettings()
    {
        _config = new ConfigFile();
        Error err = _config.Load(SettingsPath);
        if (err == Error.Ok)
        {
            _masterVolume = (float)_config.GetValue(SectionAudio, "master_volume", 1f);
            _musicVolume = (float)_config.GetValue(SectionAudio, "music_volume", 1f);
            _sfxVolume = (float)_config.GetValue(SectionAudio, "sfx_volume", 1f);

            _resolution = (Vector2I)_config.GetValue(SectionVideo, "resolution", DisplayServer.WindowGetSize());
            _windowMode = (int)_config.GetValue(SectionVideo, "window_mode", 0);
            _vsyncEnabled = (bool)_config.GetValue(SectionVideo, "vsync", true);
            _language = (string)_config.GetValue(SectionLanguage, "language", "pt");
        }
        else
        {
            _resolution = DisplayServer.WindowGetSize();
        }

        ApplyAudioVolumes();
        ApplyVideoSettings();
        ApplyControlsBindings();
        ApplyLanguage();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Audio Settings
    //!---------------------------------------------------------------------------------------------------------

    public void SetMasterVolume(float value)
    {
        _masterVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudioVolumes();
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    public void SetMusicVolume(float value)
    {
        _musicVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudioVolumes();
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    public void SetSfxVolume(float value)
    {
        _sfxVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudioVolumes();
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    /// <summary>
    /// Applies the current volume levels to the audio buses.
    /// Buses that don't exist (index -1) are silently skipped.
    /// </summary>
    private void ApplyAudioVolumes()
    {
        SetBusVolume("Master", _masterVolume);
        SetBusVolume("Music", _musicVolume);
        SetBusVolume("SFX", _sfxVolume);
    }

    private static void SetBusVolume(string busName, float volume)
    {
        int idx = AudioServer.GetBusIndex(busName);
        if (idx == -1) return;

        // Clamp near-zero so LinearToDb never returns -inf.
        float clamped = Mathf.Clamp(volume, 0.001f, 1f);
        AudioServer.SetBusVolumeDb(idx, Mathf.LinearToDb(clamped));
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Video Settings
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Changes the window resolution to the given size and centers it.
    /// </summary>
    public void SetResolution(Vector2I size)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        _resolution = size;
        ApplyVideoSettings();
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    /// <summary>
    /// Sets the window mode: 0 = Windowed, 1 = Fullscreen, 2 = Borderless.
    /// </summary>
    public void SetWindowMode(int mode)
    {
        _windowMode = Mathf.Clamp(mode, 0, 2);
        ApplyVideoSettings();
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    /// <summary>
    /// Enables or disables vertical synchronization.
    /// </summary>
    public void SetVsync(bool enabled)
    {
        _vsyncEnabled = enabled;
        ApplyVideoSettings();
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    /// <summary>
    /// Applies the current video settings to the display server.
    /// </summary>
    private void ApplyVideoSettings()
    {
        DisplayServer.WindowSetSize(_resolution);
        DisplayServer.WindowSetVsyncMode(_vsyncEnabled
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);

        switch (_windowMode)
        {
            case 0:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                break;
            case 1:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                break;
            case 2:
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                break;
        }

        // Center the window on the current screen.
        int screen = DisplayServer.WindowGetCurrentScreen();
        Vector2I screenSize = DisplayServer.ScreenGetSize(screen);
        Vector2I windowSize = DisplayServer.WindowGetSize();
        DisplayServer.WindowSetPosition(
            DisplayServer.ScreenGetPosition(screen) + (screenSize - windowSize) / 2);
    }

    /// <summary>
    /// Returns the preset window resolutions, keeping only those that fit on the
    /// current monitor (Godot never changes the monitor's resolution itself).
    /// </summary>
    public Vector2I[] GetSupportedResolutions()
    {
        Vector2I screenSize = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());

        Vector2I[] presets =
        {
            new(2560, 1080),
            new(2560, 1440),
            new(1920, 1080),
            new(1680, 1050),
            new(1600, 900),
            new(1440, 900),
            new(1366, 768),
            new(1280, 800),
            new(1280, 720),
        };

        var usable = new System.Collections.Generic.List<Vector2I>();
        foreach (var preset in presets)
        {
            if (preset.X <= screenSize.X && preset.Y <= screenSize.Y && !usable.Contains(preset))
                usable.Add(preset);
        }

        // Always include the monitor's native resolution at the top.
        if (!usable.Contains(screenSize))
            usable.Insert(0, screenSize);

        return usable.ToArray();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Language Settings
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Sets the game language ("pt" or "en"), applies it to the engine and
    /// persists it. Since it emits SettingsChanged, open UI refreshes itself.
    /// </summary>
    public void SetLanguage(string lang)
    {
        if (lang != "pt" && lang != "en") return;
        if (_language == lang) return;
        _language = lang;
        ApplyLanguage();
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    /// <summary>
    /// Applies the current language to the translation server, which makes
    /// every tr() call and every auto-translating Control follow it.
    /// </summary>
    private void ApplyLanguage()
    {
        TranslationServer.SetLocale(_language);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Controls Settings
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Replaces the given action's InputMap events with the provided one and
    /// persists it. The encoded binding is stored per action in the controls
    /// section of the settings file.
    /// </summary>
    public void SetBinding(string action, InputEvent ev)
    {
        if (!InputMap.HasAction(action) || ev == null) return;

        InputMap.ActionEraseEvents(action);
        InputMap.ActionAddEvent(action, ev);
        _config.SetValue(SectionControls, action, EncodeBinding(ev));
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    /// <summary>
    /// Restores the project-default binding for an action, erasing any stored
    /// override for it.
    /// </summary>
    public void ResetBinding(string action)
    {
        if (!InputMap.HasAction(action)) return;

        if (_defaultBindings.TryGetValue(action, out Godot.Collections.Array<InputEvent> defaults))
        {
            InputMap.ActionEraseEvents(action);
            foreach (var ev in defaults)
                InputMap.ActionAddEvent(action, ev);
        }
        if (_config.HasSectionKey(SectionControls, action))
            _config.EraseSectionKey(SectionControls, action);
        SaveSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    /// <summary>
    /// Resets every persisted control binding back to its project default.
    /// </summary>
    public void ResetAllBindings()
    {
        foreach (string action in _defaultBindings.Keys)
            ResetBinding(action);
    }

    /// <summary>
    /// Caches the project-default bindings (so they can be restored later) and
    /// then applies any saved rebinds from the settings file.
    /// </summary>
    private void ApplyControlsBindings()
    {
        // Snapshot the defaults before any stored override is applied.
        foreach (var actionName in InputMap.GetActions())
        {
            string action = actionName;
            var defaults = new Godot.Collections.Array<InputEvent>();
            foreach (var ev in InputMap.ActionGetEvents(action))
                defaults.Add(ev);
            _defaultBindings[action] = defaults;
        }

        if (!_config.HasSection(SectionControls)) return;
        foreach (string action in _config.GetSectionKeys(SectionControls))
        {
            if (!InputMap.HasAction(action)) continue;
            string encoded = (string)_config.GetValue(SectionControls, action, "");
            InputEvent ev = DecodeBinding(encoded);
            if (ev == null) continue;
            InputMap.ActionEraseEvents(action);
            InputMap.ActionAddEvent(action, ev);
        }
    }

    /// <summary>
    /// Serializes an input event into a short string so it can be stored in the
    /// ConfigFile: "key:87", "joy:1", "axis:0:-1".
    /// </summary>
    private static string EncodeBinding(InputEvent ev)
    {
        if (ev is InputEventKey key)
            return "key:" + (int)key.PhysicalKeycode;
        if (ev is InputEventJoypadButton btn)
            return "joy:" + (int)btn.ButtonIndex;
        if (ev is InputEventJoypadMotion motion)
        {
            string axisSign;
            if (motion.AxisValue >= 0f)
            {
                axisSign = "1";
            }
            else
            {
                axisSign = "-1";
            }
            return "axis:" + (int)motion.Axis + ":" + axisSign;
        }
        return "";
    }

    /// <summary>
    /// Parses an encoded binding string back into an input event, or null if it
    /// is empty / unrecognized.
    /// </summary>
    private static InputEvent DecodeBinding(string encoded)
    {
        if (string.IsNullOrEmpty(encoded)) return null;
        string[] parts = encoded.Split(":");
        if (parts.Length == 0) return null;

        switch (parts[0])
        {
            case "key" when parts.Length == 2 && int.TryParse(parts[1], out int keyCode):
                return new InputEventKey { PhysicalKeycode = (Key)keyCode };

            case "joy" when parts.Length == 2 && int.TryParse(parts[1], out int buttonIndex):
                return new InputEventJoypadButton { ButtonIndex = (JoyButton)buttonIndex };

            case "axis" when parts.Length == 3
                && int.TryParse(parts[1], out int axis)
                && int.TryParse(parts[2], out int sign):
                    float axisValue;
                    if (sign >= 0)
                    {
                        axisValue = 1f;
                    }
                    else
                    {
                        axisValue = -1f;
                    }
                    return new InputEventJoypadMotion
                    {
                        Axis = (JoyAxis)axis,
                        AxisValue = axisValue
                    };
        }
        return null;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Persistence
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Writes the current settings back to the ConfigFile on disk.
    /// </summary>
    private void SaveSettings()
    {
        _config.SetValue(SectionAudio, "master_volume", _masterVolume);
        _config.SetValue(SectionAudio, "music_volume", _musicVolume);
        _config.SetValue(SectionAudio, "sfx_volume", _sfxVolume);

        _config.SetValue(SectionVideo, "resolution", _resolution);
        _config.SetValue(SectionVideo, "window_mode", _windowMode);
        _config.SetValue(SectionVideo, "vsync", _vsyncEnabled);

        _config.SetValue(SectionLanguage, "language", _language);

        Error err = _config.Save(SettingsPath);
        if (err != Error.Ok)
            GD.PrintErr("Settings: failed to save settings to " + SettingsPath + " (" + err + ")");
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
