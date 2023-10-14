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

public partial class Player_Movement : CharacterBody2D
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Player_Data_Autoload _player_data_reference;
	private Vector2 game_pad_directional_input_vector;
	private Vector2 _keyboard_directional_input_vector;
	private float _speed;
	private float _run_speed_modifier;
	private Move_State _player_state;
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
	public Move_State Player_State
	{
		get
		{
			return _player_state;
		}

		set
		{
			_player_state = value;
		}
	}


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
		Player_State = Move_State.Idle;
	}

	public override void _PhysicsProcess(double delta)
	{
		Movement_Calculation();
		Stop_Calculation();
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	public void Input_Collector()
	{
		Game_Pad_Directional_Input_Vector = Input.GetVector("Game_Pad_Left", "Game_Pad_Right", "Game_Pad_Up", "Game_Pad_Down");
		Keyboard_Directional_Input_Vector = Input.GetVector("Keyboard_Left", "Keyboard_Right", "Keyboard_Up", "Keyboard_Down");
	}

	public void Movement_Calculation()
	{
		Input_Collector();

		if (Check_Pressed_Move_Control_Type() == 0)
		{
			Set_Running();
			Move(Game_Pad_Directional_Input_Vector);
		}

		else if (Check_Pressed_Move_Control_Type() == 1)
		{
			Set_Running();
			Move(Keyboard_Directional_Input_Vector);
		}
	}

	public void Stop_Calculation()
	{
		if(Check_Released_Move_Control_Type() == 0)
		{
			Player_State = Move_State.Idle;
		}

		else if(Check_Released_Move_Control_Type() == 1)
		{
			Player_State = Move_State.Idle;
		}
	}

	public int Check_Released_Move_Control_Type()
	{
		if (Input.IsActionJustReleased("Game_Pad_Up"))
		{
			return 0;
		}

		if (Input.IsActionJustReleased("Game_Pad_Down"))
		{
			return 0;
		}

		if (Input.IsActionJustReleased("Game_Pad_Left"))
		{
			return 0;
		}

		if (Input.IsActionJustReleased("Game_Pad_Right"))
		{
			return 0;
		}


		if (Input.IsActionJustReleased("Keyboard_Up"))
		{
			return 1;
		}

		if (Input.IsActionJustReleased("Keyboard_Down"))
		{
			return 1;
		}

		if (Input.IsActionJustReleased("Keyboard_Left"))
		{
			return 1;
		}

		if (Input.IsActionJustReleased("Keyboard_Right"))
		{
			return 1;
		}

		return 2;
	}

	public int Check_Pressed_Move_Control_Type()
	{
		if (Input.IsActionPressed("Game_Pad_Up"))
		{
			return 0;
		}

		if (Input.IsActionPressed("Game_Pad_Down"))
		{
			return 0;
		}

		if (Input.IsActionPressed("Game_Pad_Left"))
		{
			return 0;
		}

		if (Input.IsActionPressed("Game_Pad_Right"))
		{
			return 0;
		}


		if (Input.IsActionPressed("Keyboard_Up"))
		{
			return 1;
		}

		if (Input.IsActionPressed("Keyboard_Down"))
		{
			return 1;
		}

		if (Input.IsActionPressed("Keyboard_Left"))
		{
			return 1;
		}

		if (Input.IsActionPressed("Keyboard_Right"))
		{
			return 1;
		}

		return 2;
	}

	public void Set_Running()
	{
		if (Input.IsActionPressed("Game_Pad_Run") || Input.IsActionPressed("Keyboard_Run"))
		{
			Player_State = Move_State.Running;
		}

		else
		{
			Player_State = Move_State.Walking;
		}
	}

	public void Walk(Vector2 _movement_input)
	{
		Velocity = _movement_input.Normalized() * Speed;
		MoveAndSlide();
	}

	public void Run(Vector2 _movement_input)
	{
		if (Player_Data_Reference.Data.CURRENT_Stamina > 0)
		{
			Velocity = _movement_input.Normalized() * Speed * Run_Modifier;
			MoveAndSlide();
		}

		else
		{
			Walk(_movement_input);
		}
	}

	public void Move(Vector2 _movement_input)
	{
		if (Player_State == Move_State.Running)
		{
			Run(_movement_input);
		}

		else if (Player_State == Move_State.Walking)
		{
			Walk(_movement_input);
		}

		else
		{
			Player_State = Move_State.Idle;
		}
	}

	#endregion
}
