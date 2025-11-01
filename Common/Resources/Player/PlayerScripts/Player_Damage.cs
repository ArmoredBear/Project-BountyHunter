using Godot;
using System;

public partial class Player_Damage : Area2D
{
    public void Check_Damage_To_Player()
    {
        if (Player_Data_Autoload.Data.CURRENT_Health > 0)
        {
            GD.Print("SCRIPT - PLAYER DAMAGE: Damage to player was: " + Player_Data_Autoload.Instance.Current_Stored_Damage);
            GD.Print("SCRIPT - PLAYER DAMAGE: Player Current Health: " + Player_Data_Autoload.Data.CURRENT_Health);
        }

        else if (Player_Data_Autoload.Data.CURRENT_Health <= 0)
        {
            Player_Data_Autoload.Data.Change_Alive();
            Player_Data_Autoload.Instance.Check_Alive_Caller();
        }
    }

    public void On_Player_Damage_Collider_Area_Entered(Area2D _area)
    {
        if(_area.IsInGroup("enemy_attack"))
        {
            GD.Print("SCRIPT - PLAYER DAMAGE: Enemy hit the player! Enemy name:" + _area.Name);
            Check_Damage_To_Player();
        }
    }
}
