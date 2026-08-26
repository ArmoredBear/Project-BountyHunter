using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   OPTIONSMENU
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Drives the Options panel using a fully custom tab system; a vertical list of Buttons acts as the tab labels.
	**  2 - Pressing a tab shows the matching page Control under Content, keeping the buttons anywhere in the tree.
	**  3 - Binds the AudioPage sliders and video options to the Settings autoload and keeps them in sync.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class OptionsMenu : Panel
{
    // Vertical list of tab Buttons (matched by order to the pages under Content).
    [Export] public BoxContainer TabBar;

    // Container that holds one Control (page) per tab, in the same order.
    [Export] public Control Content;

    [Export] public HSlider MasterSlider;
    [Export] public HSlider MusicSlider;
    [Export] public HSlider SfxSlider;

    [Export] public OptionButton ResolutionOption;
    [Export] public OptionButton WindowModeOption;
    [Export] public CheckBox VsyncCheck;

    private Vector2I[] _resolutions;

    // Tracks the language the dropdowns were last populated with, so they
    // can be rebuilt only when it actually changes.
    private string _lastLanguage;

    // Guards against feedback loops when programmatically setting slider values,
    // since setting HSlider.Value also emits ValueChanged.
    private bool _syncing;

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        if (Settings.Instance == null)
        {
            GD.PrintErr("OptionsMenu: Settings autoload not found!");
            return;
        }

        ConnectTabs();

        MasterSlider.ValueChanged += OnMasterChanged;
        MusicSlider.ValueChanged += OnMusicChanged;
        SfxSlider.ValueChanged += OnSfxChanged;

        ResolutionOption.ItemSelected += OnResolutionSelected;
        WindowModeOption.ItemSelected += OnWindowModeSelected;
        VsyncCheck.Toggled += OnVsyncToggled;

        Settings.Instance.SettingsChanged += OnSettingsChanged;

        PopulateResolutionOptions();
        _lastLanguage = Settings.Instance.Language;
        RefreshFromSettings();
    }

    /// <summary>
    /// Unsubscribes from the Settings signal when the node is removed so a
    /// freed OptionsMenu is never called after the scene changes.
    /// </summary>
    public override void _ExitTree()
    {
        base._ExitTree();
        if (Settings.Instance != null)
            Settings.Instance.SettingsChanged -= OnSettingsChanged;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Tabs
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Binds every Button under TabBar to its matching page under Content
    /// (matched by child order) and shows the first page.
    /// </summary>
    private void ConnectTabs()
    {
        for (int i = 0; i < TabBar.GetChildCount(); i++)
        {
            if (TabBar.GetChild(i) is Button tabButton)
            {
                int index = i;
                tabButton.Pressed += () => ShowPage(index);
            }
        }
        ShowPage(0);
    }

    /// <summary>
    /// Shows the page at the given index and hides all others.
    /// </summary>
    private void ShowPage(int index)
    {
        for (int i = 0; i < Content.GetChildCount(); i++)
        {
            if (Content.GetChild(i) is Control page)
                page.Visible = i == index;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Slider Handlers
    //!---------------------------------------------------------------------------------------------------------

    private void OnMasterChanged(double value)
    {
        if (_syncing) return;
        Settings.Instance.SetMasterVolume(NormalizeSliderValue(MasterSlider, value));
    }

    private void OnMusicChanged(double value)
    {
        if (_syncing) return;
        Settings.Instance.SetMusicVolume(NormalizeSliderValue(MusicSlider, value));
    }

    private void OnSfxChanged(double value)
    {
        if (_syncing) return;
        Settings.Instance.SetSfxVolume(NormalizeSliderValue(SfxSlider, value));
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Video Handlers
    //!---------------------------------------------------------------------------------------------------------

    private void OnResolutionSelected(long index)
    {
        if (_syncing) return;
        if (index >= 0 && index < _resolutions.Length)
            Settings.Instance.SetResolution(_resolutions[index]);
    }

    private void OnWindowModeSelected(long index)
    {
        if (_syncing) return;
        Settings.Instance.SetWindowMode((int)index);
    }

    private void OnVsyncToggled(bool enabled)
    {
        if (_syncing) return;
        Settings.Instance.SetVsync(enabled);
    }

    /// <summary>
    /// Fills the Resolution dropdown with the monitor's supported resolutions
    /// and the Window Mode dropdown with the three window modes.
    /// </summary>
    private void PopulateResolutionOptions()
    {
        _resolutions = Settings.Instance.GetSupportedResolutions();

        ResolutionOption.Clear();
        for (int i = 0; i < _resolutions.Length; i++)
        {
            ResolutionOption.AddItem(_resolutions[i].X + " x " + _resolutions[i].Y);
        }

        WindowModeOption.Clear();
        WindowModeOption.AddItem("Janela");
        WindowModeOption.AddItem("Tela Cheia");
        WindowModeOption.AddItem("Sem Bordas");
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Sync
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads the current volumes from the Settings autoload into the sliders.
    /// </summary>
    private void RefreshFromSettings()
    {
        _syncing = true;
        MasterSlider.Value = DenormalizeSliderValue(MasterSlider, Settings.Instance.MasterVolume);
        MusicSlider.Value = DenormalizeSliderValue(MusicSlider, Settings.Instance.MusicVolume);
        SfxSlider.Value = DenormalizeSliderValue(SfxSlider, Settings.Instance.SfxVolume);

        int resIndex = System.Array.IndexOf(_resolutions, Settings.Instance.Resolution);
        if (resIndex >= 0)
        {
            ResolutionOption.Selected = resIndex;
        }
        else
        {
            ResolutionOption.Selected = 0;
        }
        WindowModeOption.Selected = Settings.Instance.WindowModeIndex;
        VsyncCheck.SetPressedNoSignal(Settings.Instance.VsyncEnabled);
        _syncing = false;
    }

    /// <summary>
    /// Called when the Settings autoload emits SettingsChanged, so any external
    /// change to a setting is reflected in the sliders.
    /// </summary>
    private void OnSettingsChanged()
    {
        if (Settings.Instance.Language != _lastLanguage)
        {
            _lastLanguage = Settings.Instance.Language;
            // Rebuild the dropdowns so the window-mode items ("Janela",
            // "Tela Cheia", "Sem Bordas") pick up the new language.
            PopulateResolutionOptions();
        }
        RefreshFromSettings();
    }

    /// <summary>
    /// Converts a slider value into a normalized 0..1 volume, regardless of the
    /// range configured on the slider in the editor.
    /// </summary>
    private static float NormalizeSliderValue(HSlider slider, double value)
    {
        double range = slider.MaxValue - slider.MinValue;
        if (range <= 0f)
        {
            return 0f;
        }
        return (float)((value - slider.MinValue) / range);
    }

    /// <summary>
    /// Converts a normalized 0..1 volume into a value within the slider's range.
    /// </summary>
    private static double DenormalizeSliderValue(HSlider slider, float volume)
    {
        return slider.MinValue + volume * (slider.MaxValue - slider.MinValue);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
