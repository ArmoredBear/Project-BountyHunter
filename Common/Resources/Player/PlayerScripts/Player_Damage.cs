using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   PLAYER_DAMAGE
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Detects when the player's damage collider is hit by an enemy attack.
	**  2 - Triggers the red damage vignette flash if the player is still alive.
	**  3 - Marks the player as dead and loads the Game Over scene if health reaches zero.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Player_Damage : Area2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Signal Handlers
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Called when the player's damage collider overlaps with an enemy attack area.
    /// This is connected via the "area_entered" signal in the Player.tscn scene.
    /// </summary>
    public void On_Player_Damage_Collider_Area_Entered(Area2D _area)
    {
        // Only react to areas that belong to the "enemy_attack" group
        if(_area.IsInGroup("enemy_attack"))
        {
            GD.Print("SCRIPT - PLAYER DAMAGE: Enemy hit the player! Enemy name:" + _area.Name);
            Check_Damage_To_Player();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Checks the player's current health after taking damage.
    /// If alive → triggers the red screen flash.
    /// If dead → marks player as dead and transitions to Game Over.
    /// </summary>
    public void Check_Damage_To_Player()
    {
        // Player still has health remaining
        if (Player_Data_Autoload.Data.CURRENT_Health > 0)
        {
            GD.Print("SCRIPT - PLAYER DAMAGE: Damage to player was: " + Player_Data_Autoload.Instance.Current_Stored_Damage);
            GD.Print("SCRIPT - PLAYER DAMAGE: Player Current Health: " + Player_Data_Autoload.Data.CURRENT_Health);
        }

        // Player has no health left
        else if (Player_Data_Autoload.Data.CURRENT_Health <= 0)
        {
            Player_Data_Autoload.Data.Change_Alive();
            Player_Data_Autoload.Instance.Check_Alive_Caller();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
