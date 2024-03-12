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

	private Player_Data_Autoload _player_data_reference;
	private AnimatedSprite2D _animations;
	private Vector2 game_pad_directional_input_vector;
	private Vector2 _keyboard_directional_input_vector;
	private float _speed;
	private float _run_speed_modifier;
	private State _Player_State_P;
	private bool _is_running;
    
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export]
	public Player_Data_Autoload Player_Data_Reference
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
	public float Speed
	{
		get
		{
			return _speed;
		}

		set
		{
			_speed = value;
		}
	}

	[Export]
	public float Run_Modifier
	{
		get
		{
			return _run_speed_modifier;
		}

		set
		{
			_run_speed_modifier = value;
		}
	}

	[Export]
	public State Player_State_P
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
	#region Signals
	//!---------------------------------------------------------------------------------------------------------

	[Signal]
	public delegate void Player_State_Changer_EventHandler();
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Player_Data_Reference = GetNode<Player_Data_Autoload>("/root/PlayerDataAutoload");

		Speed = 200;
		Run_Modifier = 2;
		Player_State_P = State.Idle;
	}

	public override void _PhysicsProcess(double delta)
	{
		//Movement_Calculation();
		//Stop_Calculation();
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------
	

	#endregion
}
