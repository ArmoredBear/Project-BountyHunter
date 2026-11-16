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
    #region Properties
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>Spawn marker used when no valid destination tag is stored in Player_Data_Autoload.</summary>
    [ExportGroup("Spawning")]
    [Export] public string Default_Spawn_Tag = "";

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        // Scene is fully instanced here — safe to place the player
        var currentScene = GetTree().CurrentScene;
        GD.Print("SpawnRouter active in scene: ", currentScene.Name);

        if (Player_Data_Autoload.Instance != null && Player_Data_Autoload.Instance.Try_Apply_Loaded_Position())
        {
            GD.Print("SpawnRouter: using loaded position instead of a spawn marker.");
            return;
        }

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

        Node2D spawn = Resolve_Spawn(doors, tag);
        if (spawn == null)
        {
            GD.PushError("SpawnRouter: no spawn markers found under Doors.");
            return;
        }

        if (Player.Instance == null)
        {
            GD.PrintErr("Player.Instance is null — check autoload setup.");
            return;
        }

        Player.Instance.Teleport_To(spawn.GlobalPosition);
        GD.Print($"Player placed at {spawn.GlobalPosition} via Doors/{spawn.Name}");
    }

    private Node2D Resolve_Spawn(Node doors, string requestedTag)
    {
        if (!string.IsNullOrEmpty(requestedTag))
        {
            Node2D requestedSpawn = doors.GetNodeOrNull<Node2D>(requestedTag);
            if (requestedSpawn != null)
            {
                return requestedSpawn;
            }

            if (requestedTag != "Spawn_Default")
            {
                GD.PushWarning($"SpawnRouter: spawn '{requestedTag}' not found under Doors. Using the default spawn.");
                foreach (Node child in doors.GetChildren())
                {
                    GD.Print("Doors child: ", child.Name);
                }
            }
        }

        if (!string.IsNullOrEmpty(Default_Spawn_Tag))
        {
            Node2D defaultSpawn = doors.GetNodeOrNull<Node2D>(Default_Spawn_Tag);
            if (defaultSpawn != null)
            {
                return defaultSpawn;
            }

            GD.PushWarning($"SpawnRouter: default spawn '{Default_Spawn_Tag}' not found under Doors. Using the first Marker2D.");
        }

        foreach (Node child in doors.GetChildren())
        {
            if (child is Marker2D marker)
            {
                return marker;
            }
        }

        return null;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
