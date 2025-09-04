using Godot;
using System;

public partial class Continue : Node
{

    /**-----------------------------------------------------------------------------------------------------------------------
     *!                                                   WARNING!!! THIS IS TEMPORARY!!!
     *-----------------------------------------------------------------------------------------------------------------------**/


    private void OnButtonPressed()
    {
        Player.Instance.Visible = true;
        Player.Instance.GetNode<CanvasLayer>("%Player_UI").Visible = true;
        GetTree().ChangeSceneToFile("res://Common/MainScenes/Main.tscn");
    }
}
