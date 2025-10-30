using Godot;
using System;

public partial class Player_Damage : Node
{
    public void Damage_To_Player(int _value)
    {
        if (Player_Data_Autoload.Data.CURRENT_Health > 0)
        {
            Player_Data_Autoload.Data.TakeDamage(_value);
            GD.Print("SCRIPT - PLAYER DAMAGE - : Player attacked! Damage is: " + _value);
            GD.Print("SCRIPT - PLAYER DAMAGE - : Player CURRENT HEALTH: " + Player_Data_Autoload.Data.CURRENT_Health);
        }

        else if (Player_Data_Autoload.Data.CURRENT_Health <= 0)
        {
            GD.Print("SCRIPT - PLAYER DAMAGE - : Player is Dead!");
        }
    }

    public void On_Player_Damage_Collider_Area_Entered(Node2D _area)
    {
        if(_area.IsInGroup("enemy_attack"))
        {
            Damage_To_Player(20);
        }
    }
}
