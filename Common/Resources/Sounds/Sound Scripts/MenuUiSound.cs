using Godot;
using System;

public partial class MenuUiSound : AudioStreamPlayer2D
{
    [Export] public Continue ContinueButton { get; set; }
    [Export] public LoadGame LoadGameButton { get; set; }
    [Export] public Button OptionsButton { get; set; }
    [Export] public Credits CreditsButton { get; set; }
    [Export] public Quit QuitButton { get; set; }

    public override void _Ready()
    {
        base._Ready();

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
    }

    private void PlaySound()
    {
        Play();
    }
}
