using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SPAWN ROUTER
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Places the player at the correct door spawn point when entering a new scene.
	**  2 - Reads the spawn tag set by the last door from Player_Data_Autoload.
	**  3 - Falls back to debug prints when the spawn point or player is unavailable.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class SpawnRouter : Node
{
    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        // Scene is fully instanced here — safe to place the player
        var currentScene = GetTree().CurrentScene;
        GD.Print("SpawnRouter active in scene: ", currentScene.Name);

        var doors = currentScene.GetNodeOrNull<Node>("Doors");
        if (doors == null)
        {
            GD.Print("No Doors node found in this scene.");
            return;
        }

        string tag = null;
        if (Player_Data_Autoload.Instance != null)
        {
            tag = Player_Data_Autoload.Instance.NextSpawnName;
        }
        if (string.IsNullOrEmpty(tag))
        {
            GD.PrintErr("NextSpawnName is empty — did a Door set it?");
            return;
        }

        var spawn = doors.GetNodeOrNull<Node2D>(tag);
        if (spawn == null)
        {
            GD.PrintErr($"Spawn '{tag}' not found under Doors. Listing children:");
            foreach (Node child in doors.GetChildren())
                GD.Print("Doors child: ", child.Name);
            return;
        }

        if (Player.Instance == null)
        {
            GD.PrintErr("Player.Instance is null — check autoload setup.");
            return;
        }

        Player.Instance.GlobalPosition = spawn.GlobalPosition;
        GD.Print($"Player placed at {spawn.GlobalPosition} via Doors/{tag}");
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
