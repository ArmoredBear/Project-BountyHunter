using Godot;
using System;

public partial class Door : Node
{
    [Export] public Marker2D Spawn { get; set; }
    [Export] public string Destination_Scene_Tag { get; set; }
    [Export] public string Destination_Door_Tag { get; set; }
    [Export] public string Spawn_Direction { get; set; }

    public override void _Ready()
    {
        base._Ready();

        if (Spawn == null)
        {
            GD.Print("Spawn is null!!");
        }

        if (Destination_Scene_Tag == null)
        {
            GD.Print("Destination Scene Tag is null!!");
        }

        if (Destination_Door_Tag == null)
        {
            GD.Print("Destination Door Tag is null!!");
        }

        if (Spawn_Direction == null)
        {
            GD.Print("Spawn Direction is null!!");
        }
    }


    public void On_Body_Entered(CharacterBody2D _body)
    {

        if (_body.IsInGroup("player"))
        {
            GD.Print("Changed Scene...");
            {
                Verify_Scene(Destination_Scene_Tag);
            }
        }
    }

    public void Verify_Scene(string _scene_name)
    {
        string _scene_path = null;
        
        if(Destination_Scene_Tag == "Forest")
        {
            _scene_path = "res://Common/Resources/Cenarios/Prefabs/Forest.tscn";
            Change_Scene(_scene_path);
        }
        if(Destination_Scene_Tag == "Tunnel")
        {
            _scene_path = "res://Common/Resources/Cenarios/Prefabs/Tunnel.tscn";
            Change_Scene(_scene_path);
        }
        if(Destination_Scene_Tag == "Clearing")
        {
            _scene_path = "res://Common/Resources/Cenarios/Prefabs/Clearing.tscn";
            Change_Scene(_scene_path);
        }
    }

    public void Change_Scene(string _scene_path)
    {
        GetTree().ChangeSceneToFile(_scene_path);
    }
    
    public void Set_Player_Spawn_Position()
    {
        Player.Instance.Position = Spawn.Position;
    }

}
