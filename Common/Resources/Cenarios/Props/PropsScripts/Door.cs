using Godot;
using System;

public partial class Door : Node
{
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region Variables
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    [Export] public Marker2D Spawn { get; set; }
    [Export] public string Destination_Scene_Tag { get; set; }
    [Export] public string Destination_Door_Tag { get; set; }
    [Export] public string Spawn_Direction { get; set; }

    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------


    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region Initialization
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        Reference_If_Null();
        
    }

    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------


    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region Signals Methods
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    public void On_Body_Entered(CharacterBody2D _body)
    {

        if (_body.IsInGroup("player"))
        {
            GD.Print("Changed Scene...");
            {
                Select_Scene(Destination_Scene_Tag);
            }
        }
    }

    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------


    //! --------------------------------------------------------------------------------------------------------------------------------------------------------
    #region Methods
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

    private void Reference_If_Null()
    {
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


    public void Select_Scene(string _scene_name)
    {
        if (_scene_name == "Forest")
        {
            Scene_Manager.Instance.Change_Scene(e_Game_Scenes.Forest);
            Scene_Manager.Instance.Update_Player_Position(Spawn.Position);
        }
        if (_scene_name == "Tunnel")
        {
            Scene_Manager.Instance.Change_Scene(e_Game_Scenes.Tunnel);
            Scene_Manager.Instance.Update_Player_Position(Spawn.Position);
        }
        if (_scene_name == "Clearing")
        {
            Scene_Manager.Instance.Change_Scene(e_Game_Scenes.Clearing);
            Scene_Manager.Instance.Update_Player_Position(Spawn.Position);
        }
    }

    #endregion
    //! --------------------------------------------------------------------------------------------------------------------------------------------------------

}
