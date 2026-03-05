using Godot;
using System;

public partial class LoadGame : Button
{
    private void OnButtonPressed()
    {
        Save_Load_Control.Instance.Load_Game();
    }
}
