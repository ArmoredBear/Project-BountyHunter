using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   LANGUAGEPAGE
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Language tab of the Options panel; lets the player pick between Portuguese and English.
	**  2 - The choice is persisted through the Settings autoload and applied game-wide via
	**	TranslationServer.SetLocale().
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class LanguagePage : Control
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // Dropdown that offers "Português" and "English".
    [Export] public OptionButton LanguageOption;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        if (Settings.Instance == null)
        {
            GD.PrintErr("LanguagePage: Settings autoload not found!");
            return;
        }

        if (LanguageOption != null)
        {
            LanguageOption.Clear();
            // Language names stay in their own language regardless of the
            // current locale ("Português" / "English"), so they are not
            // part of the translation table.
            LanguageOption.AddItem("Português");
            LanguageOption.AddItem("English");
            LanguageOption.ItemSelected += OnLanguageSelected;
        }

        RefreshFromSettings();

        Settings.Instance.SettingsChanged += RefreshFromSettings;
    }

    /// <summary>
    /// Unsubscribes from the Settings signal when the node is removed so a
    /// freed LanguagePage is never called after the scene changes.
    /// </summary>
    public override void _ExitTree()
    {
        base._ExitTree();
        if (Settings.Instance != null)
            Settings.Instance.SettingsChanged -= RefreshFromSettings;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    private void OnLanguageSelected(long index)
    {
        if (index == 1)
        {
            Settings.Instance.SetLanguage("en");
        }
        else
        {
            Settings.Instance.SetLanguage("pt");
        }
    }

    private void RefreshFromSettings()
    {
        if (LanguageOption == null) return;

        if (Settings.Instance.Language == "en")
        {
            LanguageOption.Selected = 1;
        }
        else
        {
            LanguageOption.Selected = 0;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
