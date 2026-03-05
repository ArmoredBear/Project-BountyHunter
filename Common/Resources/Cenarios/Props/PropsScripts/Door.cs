using Godot;

public partial class Door : Node2D
{
    [Export] public Marker2D Spawn;
    [Export] public Area2D Portal;
    [Export] public e_Game_Scenes Destination_Scene_Tag;
    [Export] public string Destination_Spawn_Tag = ""; // leave blank to auto‑fill

    public override void _Ready()
    {
        // Resolve Portal
        if (Portal == null)
        {
            Portal = GetNodeOrNull<Area2D>("%Portal");
            if (Portal == null)
            {
                Portal = GetNodeOrNull<Area2D>("Portal");
            }
        }
        if (Portal == null)
        {
            GD.PrintErr($"{Name}: Portal node not found.");
            return;
        }

        // Auto‑fill Destination_Spawn_Tag if left blank
        if (string.IsNullOrEmpty(Destination_Spawn_Tag))
        {
            Destination_Spawn_Tag = Name; // use this node’s name
            GD.Print($"{Name}: Auto‑filled Destination_Spawn_Tag = {Destination_Spawn_Tag}");
        }

        Portal.BodyEntered += On_Body_Entered;
    }

    private void On_Body_Entered(Node body)
    {
        if (!body.IsInGroup("player"))
            return;

        GD.Print($"{Name}: Triggered. Destination scene: {Destination_Scene_Tag}, spawn tag: {Destination_Spawn_Tag}");

        Scene_Manager.Instance.SetTransition(Destination_Spawn_Tag);
        Scene_Manager.Instance.CallDeferred("Change_Scene", (int)Destination_Scene_Tag);
    }

}
