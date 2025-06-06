using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
 *!                                              PLAYER DATA AUTOLOAD CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
  	**                                                 	 PURPOSE
  	*
	**  1 - This class is a singleton, it inherits Node but it does not need to be on scene EDITOR,
	** 	that is why is called AUTOLOAD, when the scene starts the object is created automaticaly with this script
	** 	functions and properties before any other object.
  	**  2 - This holds the player data in the scene using RESOURCE and provides easy acess to the data properties on other scripts
	** 	and interaction with scene nodes on editor.
  	**  3 - This also has a constructor to initiate default data.
  	*  
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Player_Data_Autoload : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private static Player _player;
	private static Player_Data _player_data;
	private Player_Healthbar_UI _player_heathbar;
	private Timer _poison_timer;
	private int _counter;
	
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	public static Player_Data_Autoload Instance;
	
	public static Player_Data Data
	{
		get
		{
			return _player_data;
		}

		set
		{
			_player_data = value;
		}
	}
	
	[Export] public Timer Poison_Timer;
	
	public Player_Healthbar_UI Player_Healthbar
	{
		get
		{
			return _player_heathbar;
		}

		set
		{
			_player_heathbar = value;
		}
	}

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{	
		if(Instance == null)
		{
			Instance = this;
		}

		else if (Instance != null && Instance != this)
		{
			GD.PrintErr("ERROR!! Instance of Player_Data_Autoload already exist!!");
		}

		Data = new Player_Data(100, 100, 100, true, false, false);
		Player_Healthbar = GetNode<Player_Healthbar_UI>("/root/Player/Player_UI/Status/Health_Bar");
			
		
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

        
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------
	
	/**------------------------------------------------------------------------------------------------
		 **               Main methods of interaction with Player Data
	*------------------------------------------------------------------------------------------------**/

	public void Poison_Behavior()
	{
		Poison_Timer = new Timer
        {
            WaitTime = 1,
			OneShot = false,
			Autostart = false
        };

        AddChild(Poison_Timer);
		
		Poison_Timer.Timeout += () => GD.Print("Player Health: " + Data.CURRENT_Health);
		Poison_Timer.Timeout += Check_Alive_Caller;
		Poison_Timer.Timeout += Poison_Caller;

	}

	public void Update_Loaded_Data()
	{
		Player_Healthbar.Health_Monitor.Value = Data.CURRENT_Health;
		Player_Healthbar.Lines.Value = Data.CURRENT_Health;
	}

	public void Update_Data_To_Save()
	{
		Data.CURRENT_Health = (int)Player_Healthbar.Health_Monitor.Value;
	}

	/**------------------------------------------------------------------------------------------------
		 **               Simple functions to enable connection to Signals
	*------------------------------------------------------------------------------------------------**/


	public void Poison_Caller()
	{
		if(Data.Alive)
		{
			Data.Poison(10,20,0);
		}

		else
		{
			Poison_Timer.Stop();
		}
	}

	public void Check_Alive_Caller()
	{
		if(!Data.Alive)
		{
			GD.Print("Player is dead...");
		}

		else
		{
			return;
		}
	}

	#endregion
}
