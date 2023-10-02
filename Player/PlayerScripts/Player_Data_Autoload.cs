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
  	**  2 - This only holds the player data in the scene and provides easy acess to the data properties on other scripts
	** 	and interaction with scene nodes on editor.
  	**  3 - This also has a constructor to initiate default data.
  	*  
*-----------------------------------------------------------------------------------------------------------------------**/

[Tool]
public partial class Player_Data_Autoload : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Player_Data _player_data;
	private Timer _poison_timer;
	
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------


	[ExportCategory ("Data")]
	[Export] public Player_Data Data
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
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{	
		Data = new Player_Data(100, 100, 100, true, false, false);
		Poison_Behavior();
        
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
