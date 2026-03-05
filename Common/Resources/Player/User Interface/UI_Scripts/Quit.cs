using Godot;
using System;

public partial class Quit : Button
{
    public void On_Quit_Pressed()
    {
        
        GetTree().Quit();
    }
}
