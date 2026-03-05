using Godot;
using System;

public partial class GameOverScreen : CanvasLayer
{
    public override void _Ready()
    {
        // Reset player health to full
        Player_Data_Autoload.Data.CURRENT_Health = 100;
        Player_Data_Autoload.Data.Alive = true;
        // Reset position to initial forest position
        Player_Data_Autoload.Instance.LoadedPosition = new Vector2(21579, -3);
        Player_Data_Autoload.Instance.ShouldSetLoadedPosition = true;
        // Update UI (if needed, but since scene changed, maybe not)
        if (Player_Data_Autoload.Instance.Player_Healthbar != null)
        {
            Player_Data_Autoload.Instance.Player_Healthbar.Health_Monitor.Value = Player_Data_Autoload.Data.CURRENT_Health;
            Player_Data_Autoload.Instance.Player_Healthbar.Lines.Value = Player_Data_Autoload.Data.CURRENT_Health;
        }

        // Wait 5 seconds, then change to main menu using SceneManager
        GetTree().CreateTimer(5.0f).Timeout += () =>
        {
            Scene_Manager.Instance.Change_Scene(e_Game_Scenes.MainMenu);
        };
    }
}
