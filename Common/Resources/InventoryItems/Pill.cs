using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                    	PILL CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	 *  
	 **  1 - Hold the Pill information and methods that will influence player.
	 **  2 - Hold the Pill information to be reused / repurposed on the items on scenario.
	 *  
*-----------------------------------------------------------------------------------------------------------------------**/

[GlobalClass]
public partial class Pill : Item
{	
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private int _healing_time;
	private int _healing_counter;
	private bool _using;

	private const Item_Type Type = Item_Type.Consumable;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------


	[Export] public int HealingTime
	{
		get
		{
			return _healing_time;
		}

		set
		{
			_healing_time = value;
		}
	}

	[Export] public int HealingCounter
	{
		get
		{
			return _healing_counter;
		}

		set
		{
			_healing_counter = value;
		}
	}
	// Called when the node enters the scene tree for the first time.
	[Export] public bool Using
	{
		get
		{
			return _using;
		}
		set
		{
			_using = value;
		}
	}

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public Pill()
	{
		Item_Name = "Pill";
		Usable = true;
		Is_Equipment = false;
		HealingTime = 20;
		HealingCounter = 0;

	}

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	public override void Use(bool _usable)
	{
		if(Using && _usable)
		{
			GD.Print("Item already in use!! Cooldown in effect! ");
			return;
		}

		else if(!Using && _usable)
		{

		}
	}

    public void Regeneration()
	{
		if(HealingCounter < HealingTime)
		{
			HealingCounter++;
			Player_Data_Autoload.Data.Restore_Health(5);
			GD.Print("Pill information: " + " Healing Counter: " + HealingCounter + " Player Health: " + Player_Data_Autoload.Data.CURRENT_Health);
			Using = true;
		}

		else
		{
			Cooldown();
			Using = false;
		}
		
	}

	public void Cooldown()
	{
		GD.Print("Cooldown now in effect... (5s)");
	}

	#endregion
}
