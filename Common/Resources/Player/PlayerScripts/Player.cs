using System;
using System.Diagnostics;
using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
 *!                                              	 PLAYER MOVEMENT
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
  	**                                                 	 PURPOSE
  	*
	**  1 - This class holds and controls the player movement behavior.
  	**  2 - This is for movement only, dont mix actions here.
  	*  
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Player : CharacterBody2D
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private AnimatedSprite2D _animations;
	private Vector2 game_pad_directional_input_vector;
	private Vector2 _keyboard_directional_input_vector;
	private Player_States _Player_State_P;
	private bool _is_running;
    
	#endregion
	//!---------------------------------------------------------------------------------------------------------

	

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------
	public static Player Instance;

	[Export]
	public Vector2 Game_Pad_Directional_Input_Vector
	{
		get
		{
			return game_pad_directional_input_vector;

		}
		set
		{
			game_pad_directional_input_vector = value;
		}
	}

	[Export]
	public Vector2 Keyboard_Directional_Input_Vector
	{
		get
		{
			return _keyboard_directional_input_vector;
		}

		set
		{
			_keyboard_directional_input_vector = value;
		}
	}

	[Export]
	public Player_States Player_State_P
	{
		get
		{
			return _Player_State_P;
		}

		set
		{
			_Player_State_P = value;
		}
	}


	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Signals
	//!---------------------------------------------------------------------------------------------------------

	//[Signal]
	//public delegate void Player_State_Changer_EventHandler();
	
	#endregion
	//!---------------------------------------------------------------------------------------------------------



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
			GD.PrintErr("ERROR!! Instance of Player already exist!!");
		}
		
		Player_State_P = Player_States.Idle;

		if(GetTree().CurrentScene.Name != "Main")
		{
			this.Visible = false;
		}
	}

    public override void _Process(double delta)
    {
        

    }

    public override void _PhysicsProcess(double delta)
	{
		
    }


	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------
	

	#endregion
	//!---------------------------------------------------------------------------------------------------------

}
