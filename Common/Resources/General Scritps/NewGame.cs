using Godot;
using System;

public partial class NewGame : Button
{
    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    private void OnButtonPressed()
    {
        // Full state reset for a fresh new game
        Player_Data_Autoload.Instance.Reset();

        Player.Instance.Visible = true;
        Player.Instance.GetNode<CanvasLayer>("%Player_UI").Visible = true;
        Scene_Manager.Instance.Change_Scene(e_Game_Scenes.Forest);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
