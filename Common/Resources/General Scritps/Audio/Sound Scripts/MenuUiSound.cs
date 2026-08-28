using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   MENU UI SOUND
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Plays the menu UI hover/click sound, routed through the "SFX" bus.
	**  2 - Connects the top-level menu buttons and every Button found inside
	**	the HoverSources containers (e.g. the Options panel controls).
	**
	**  NOTE: The recursive container scan runs deferred so buttons that are
	**	created at runtime (ControlsPage binding rows) already exist first.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class MenuUiSound : AudioStreamPlayer
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    [Export] public Button ContinueButton { get; set; }
    [Export] public LoadGame LoadGameButton { get; set; }
    [Export] public Button OptionsButton { get; set; }
    [Export] public Credits CreditsButton { get; set; }
    [Export] public Quit QuitButton { get; set; }

    // Containers whose Button descendants all get the hover/click sound
    // (e.g. the Options panel, including buttons created at runtime).
    [Export] public Node[] HoverSources { get; set; }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        // Route menu sounds through the "SFX" bus so they can be volume
        // controlled independently of music.
        Bus = "SFX";

        // Connect the buttons' signals to play the sound
        if (ContinueButton != null)
        {
            ContinueButton.Pressed += PlaySound;
            ContinueButton.MouseEntered += PlaySound;
        }
        if (LoadGameButton != null)
        {
            LoadGameButton.Pressed += PlaySound;
            LoadGameButton.MouseEntered += PlaySound;
        }
        if (OptionsButton != null)
        {
            OptionsButton.Pressed += PlaySound;
            OptionsButton.MouseEntered += PlaySound;
        }
        if (CreditsButton != null)
        {
            CreditsButton.Pressed += PlaySound;
            CreditsButton.MouseEntered += PlaySound;
        }
        if (QuitButton != null)
        {
            QuitButton.Pressed += PlaySound;
            QuitButton.MouseEntered += PlaySound;
        }

        // Deferred so runtime-built rows (e.g. ControlsPage binding buttons)
        // already exist by the time we scan the containers.
        CallDeferred(nameof(ConnectHoverSources));
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Connects every Button found under each HoverSources container so the
    /// Options panel controls play the same hover/click sound as the menu.
    /// </summary>
    private void ConnectHoverSources()
    {
        if (HoverSources == null) return;

        foreach (Node source in HoverSources)
        {
            if (source != null)
                ConnectButtonsRecursive(source);
        }
    }

    /// <summary>
    /// Walks the node tree under a container, wiring up any Button found.
    /// </summary>
    private void ConnectButtonsRecursive(Node node)
    {
        if (node is Button button)
        {
            button.Pressed += PlaySound;
            button.MouseEntered += PlaySound;
        }

        foreach (Node child in node.GetChildren())
            ConnectButtonsRecursive(child);
    }

    /// <summary>
    /// Plays the hover/click stream if it is not already playing.
    /// </summary>
    private void PlaySound()
    {
        if (IsInsideTree() && Playing == false)
        {
            Play();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
