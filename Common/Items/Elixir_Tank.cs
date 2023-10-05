using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                  ELIXIR TANK CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Hold the elixir tank information and methods that will influence player.
	*
	*  
*-----------------------------------------------------------------------------------------------------------------------**/

[Tool]
public partial class Elixir_Tank : Item
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------
    private Player_Data_Autoload _player_data_reference;
    private int _elixir_amount;
    private int _amount_to_heal;
    private bool _damaged;
    

    

    #endregion

    //!---------------------------------------------------------------------------------------------------------
    #region Properties
    //!---------------------------------------------------------------------------------------------------------

    [Export] public Player_Data_Autoload Player_Data_Reference
	{
		get
		{
			return _player_data_reference;
		}

		set
		{
			_player_data_reference = value;
		}
	}

    [Export] public int Elixir_Amount
    {
        get
        {
            return _elixir_amount;
        }

        set
        {
            _elixir_amount = value;
        }
    }
    
    [Export] public int Amount_to_Heal
    {
        get
        {
            return _amount_to_heal;
        }

        set
        {
            _amount_to_heal = value;
        }
    }

    [Export] public bool Damaged
    {
        get
        {
            return _damaged;
        }

        set
        {
            _damaged = value;
        }
    }
    
    
    
    #endregion

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {   
        Item_Name = "Elixir Tank";
        Elixir_Amount = 100;
        Damaged = false;
        Equipable = true;
        Amount_to_Heal = 25;

        Player_Data_Reference = GetNode<Player_Data_Autoload>("/root/PlayerDataAutoload");
    }

    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    public void Refill_Tank(int _amount_to_refill)
	{
		Elixir_Amount += _amount_to_refill;
        GD.Print("Elixir Tank amount: " + Elixir_Amount);
	}

    public void Heal()
    {
        Player_Data_Reference.Data.Restore_Health(Amount_to_Heal);
    }

    public void Upgrade_Tank_Healing()
    {
        
    }

    public void Upgrade_Tank_Capacity()
    {

    }

    #endregion

    #endregion
    


}