using Godot;
using System;

public partial class Credits : Button
{
    [Export] public TextureRect Credits_Panel;
    public void OnButtonPressed()
    {
        if (!Credits_Panel.Visible)
        {
            Credits_Panel.Visible = true;
        }

        else
        {
            Credits_Panel.Visible = false;
        }
    }
}
