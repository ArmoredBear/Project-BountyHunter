using Godot;
using System;

public partial class Continue : Button
{

    /**-----------------------------------------------------------------------------------------------------------------------
     *!                                                   WARNING!!! THIS IS TEMPORARY!!!
     *-----------------------------------------------------------------------------------------------------------------------**/


    private void OnButtonPressed()
    {
        Player.Instance.Visible = true;
        Player.Instance.GetNode<CanvasLayer>("%Player_UI").Visible = true;
        Scene_Manager.Instance.Change_Scene(e_Game_Scenes.Forest);
    }
}
