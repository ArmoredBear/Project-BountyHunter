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

public partial class Elixir_Tank : Item
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------
    private Player_Data_Autoload _player_data_reference;
    private int _max_elixir_amount;
    private int _current_elixir_amount;
    private int _amount_to_heal;
    private int _tank_upgrade;
    private int _heal_upgrade;
    private bool _equiped;
    private bool _damaged;
    
    private const int _max_heal_upgrade = 3;
    private const int _max_tank_upgrade = 5;
    private const Item_Type _type = Item_Type.Tool;
    

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

    [Export] public int MAX_Elixir_Amount
    {
        get
        {
            return _max_elixir_amount;
        }

        set
        {
            _max_elixir_amount = value;
        }
    }
    
    [Export] public int Current_Elixir_Amount
    {
        get
        {
            return _current_elixir_amount;
        }

        set
        {
            _current_elixir_amount = value;
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

    [Export] public int Heal_Upgrade
    {
        get
        {
            return _heal_upgrade;
        }

        set
        {
            _heal_upgrade = value;
        }
    }

    [Export] public int Tank_Upgrade
    {
        get
        {
            return _tank_upgrade;
        }

        set
        {
            _tank_upgrade = value;
        }
    }
    
    [Export] public bool Equiped
    {
        get
        {
            return _equiped;
        }

        set
        {
            _equiped = value;
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
    
    public int MAX_Heal_Upgrade
    {
        get
        {
            return _max_heal_upgrade;
        }
    }

    public int MAX_Tank_Upgrade
    {
        get
        {
            return _max_tank_upgrade;
        }
    }

    public Item_Type Type
    {
        get
        {
            return _type;
        }
    }
    
    #endregion

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {   
        Item_Name = "Elixir Tank";
        MAX_Elixir_Amount = 2;
        Current_Elixir_Amount = 1;
        Damaged = false;
        Equipable = true;
        Equiped = false;
        Amount_to_Heal = 25;
        Heal_Upgrade = 0;
        Tank_Upgrade = 0;

        Player_Data_Reference = GetNode<Player_Data_Autoload>("/root/PlayerDataAutoload");
    }

    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    public void Equip()
    {
        Equiped = true;
    }

    public void Refill_Tank(int _amount_to_refill)
	{
        if(_amount_to_refill < MAX_Elixir_Amount && Current_Elixir_Amount <= MAX_Elixir_Amount)
        {
            Current_Elixir_Amount += _amount_to_refill;
            GD.Print("Elixir Tank amount: " + Current_Elixir_Amount);
        }

        else if (_amount_to_refill == MAX_Elixir_Amount)
        {
            Current_Elixir_Amount = MAX_Elixir_Amount;
        }

        else
        {
            GD.Print("Tank is full...");
        }
	}

    public void Heal()
    {
        Player_Data_Reference.Data.Restore_Health(Amount_to_Heal);
    }

    public void Upgrade_Tank_Healing()
    {
        if(Heal_Upgrade < MAX_Heal_Upgrade)
        {
            Amount_to_Heal += 25;
            Heal_Upgrade++;
        }

        else
        {
            GD.Print("MAX HEAL UPGRADE REACHED....");
        }
        
    }

    public void Upgrade_Tank_Capacity()
    {
        if(Tank_Upgrade < MAX_Tank_Upgrade)
        {
            MAX_Elixir_Amount += 1;
            Tank_Upgrade++;
        }

        else
        {
            GD.Print("MAX TANK UPGRADE REACHED....");
        }
    }

    #endregion

    #endregion
    


}