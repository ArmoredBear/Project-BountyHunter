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

[Tool]
public partial class Pill : Item
{	
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Player_Data_Autoload _player_data_reference;
	private Timer _health_regen_timer;
	private Timer _cooldown_timer;
	private int _healing_time;
	private int _healing_counter;
	private bool _using;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public Player_Data_Autoload PlayerDataReference
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

	[Export] public Timer Health_Regen_Timer
	{
		get
		{
			return _health_regen_timer;
		}

		set
		{
			_health_regen_timer = value;
		}
	}

	[Export] public Timer Cooldown_Timer
	{
		get
		{
			return _cooldown_timer;
		}
		set
		{
			_cooldown_timer = value;
		}
	}

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

	public override void _Ready()
	{
		Health_Regen_Timer = GetNode<Timer>("health_regen_timer");
		Cooldown_Timer = GetNode<Timer>("cooldown_timer");
		Usable = true;
		Equipable = false;
		HealingTime = 20;
		HealingCounter = 0;
		Health_Regen_Timer.Timeout += Regeneration;
		Cooldown_Timer.Timeout += () => GD.Print("Cooldown done.");

		PlayerDataReference = GetNode<Player_Data_Autoload>("/root/PlayerDataAutoload");
	}

	 public override void _Process(double delta)
    {
        if(Cooldown_Timer.TimeLeft > 0)
		{
			GD.Print("Cooldown timer - Time left: " + Cooldown_Timer.TimeLeft);
		}
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
			Health_Regen_Timer.Start();
		}
	}

    public void Regeneration()
	{
		if(HealingCounter < HealingTime)
		{
			HealingCounter++;
			PlayerDataReference.Data.Restore_Health(5);
			GD.Print("Pill information: " + " Healing Counter: " + HealingCounter + " Player Health: " + PlayerDataReference.Data.CURRENT_Health);
			Using = true;
		}

		else
		{
			Health_Regen_Timer.Stop();
			Cooldown();
			Using = false;
		}
		
	}

	public void Cooldown()
	{
		Cooldown_Timer.Start();
		GD.Print("Cooldown now in effect... (5s)");
	}

	#endregion
}
