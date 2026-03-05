using Godot;
using System;

public partial class PlayerMenu : CanvasLayer
{
    public override void _Ready()
    {
        base._Ready();
        Visible = false;
    }

    
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Input.IsActionJustPressed("Menu"))
        {
            Visible = !Visible;
        }
    }


    public void On_Save_And_Quit_Pressed()
    {
        Scene_Manager.Instance.Change_Scene(e_Game_Scenes.MainMenu);
    }
}
