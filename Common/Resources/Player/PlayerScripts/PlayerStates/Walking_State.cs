using Godot;
using System;

public partial class Walking_State : Player_State
{   
    private AnimatedSprite2D _player_animation;
	private float _walk_speed;

	public AnimatedSprite2D Player_Animation
	{
		get
		{
			return _player_animation;
		}

		set
		{
			_player_animation = value;
		}
	}

	public float Walk_Speed
	{
		get
		{
			return _walk_speed;
		}

		set
		{
			_walk_speed = value;
		}
	}

	public override void _Ready()
	{
		Player_Animation = GetNode<AnimatedSprite2D>("%Player_Body/Player_FSM/Player_Animation");
		Walk_Speed = Player_Base_Speed;
	}

	public override void Enter()
	{
		
		
	}

    public override void Exit()
    {
       
    }

    public override void Update(double delta)
    {
		Calculate_Walk_Direction();
        Input_Collector();
		Stop_Calculation();
		Movement_Calculation();
		
    }

    public override void PhysicsUpdate(double delta)
    {
        Walk(Game_Pad_Directional_Input_Vector);
        Walk(Keyboard_Directional_Input_Vector);
    }

    public override void HandleInput(InputEvent @event) 
	{
		
	}

    public void Input_Collector()
	{
		Game_Pad_Directional_Input_Vector = Input.GetVector("Game_Pad_Left", "Game_Pad_Right", "Game_Pad_Up", "Game_Pad_Down");
		Keyboard_Directional_Input_Vector = Input.GetVector("Keyboard_Left", "Keyboard_Right", "Keyboard_Up", "Keyboard_Down");
	}

	public void Calculate_Walk_Direction()
	{
		if (Input.IsActionPressed("Game_Pad_Up"))
		{
			if (Input.IsActionPressed("Game_Pad_Up") && Input.IsActionPressed("Game_Pad_Left"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else if (Input.IsActionPressed("Game_Pad_Up") && Input.IsActionPressed("Game_Pad_Right"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else
			{
				Player_Animation.Play("Walk_Up");
			}
		}
		

		if (Input.IsActionPressed("Game_Pad_Down"))
		{
			if (Input.IsActionPressed("Game_Pad_Down") && Input.IsActionPressed("Game_Pad_Left"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else if (Input.IsActionPressed("Game_Pad_Down") && Input.IsActionPressed("Game_Pad_Right"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else
			{
				Player_Animation.Play("Walk_Down");
			}

		}

		if (Input.IsActionPressed("Game_Pad_Left"))
		{
			if (Input.IsActionPressed("Game_Pad_Left") && Input.IsActionPressed("Game_Pad_Up"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else if (Input.IsActionPressed("Game_Pad_Left") && Input.IsActionPressed("Game_Pad_Down"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else
			{
				Player_Animation.Play("Walk_Left");
			}
		}

		if (Input.IsActionPressed("Game_Pad_Right"))
		{
			if (Input.IsActionPressed("Game_Pad_Right") && Input.IsActionPressed("Game_Pad_Up"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else if (Input.IsActionPressed("Game_Pad_Right") && Input.IsActionPressed("Game_Pad_Down"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else
			{
				Player_Animation.Play("Walk_Right");
			}
		}
		

		if (Input.IsActionPressed("Keyboard_Up"))
		{
			if (Input.IsActionPressed("Keyboard_Up") && Input.IsActionPressed("Keyboard_Left"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else if (Input.IsActionPressed("Keyboard_Up") && Input.IsActionPressed("Keyboard_Right"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else
			{
				Player_Animation.Play("Walk_Up");
			}
		}

		if (Input.IsActionPressed("Keyboard_Down"))
		{
			if (Input.IsActionPressed("Keyboard_Down") && Input.IsActionPressed("Keyboard_Left"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else if (Input.IsActionPressed("Keyboard_Down") && Input.IsActionPressed("Keyboard_Right"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else
			{
				Player_Animation.Play("Walk_Down");
			}
		}

		if (Input.IsActionPressed("Keyboard_Left"))
		{
			if (Input.IsActionPressed("Keyboard_Left") && Input.IsActionPressed("Keyboard_Up"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else if (Input.IsActionPressed("Keyboard_Left") && Input.IsActionPressed("Keyboard_Down"))
			{
				Player_Animation.Play("Walk_Left");
			}

			else
			{
				Player_Animation.Play("Walk_Left");
			}
		}

		if (Input.IsActionPressed("Keyboard_Right"))
		{
			if (Input.IsActionPressed("Keyboard_Right") && Input.IsActionPressed("Keyboard_Up"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else if (Input.IsActionPressed("Keyboard_Right") && Input.IsActionPressed("Keyboard_Down"))
			{
				Player_Animation.Play("Walk_Right");
			}

			else
			{
				Player_Animation.Play("Walk_Right");
			}
		}
	}

	public void Walk(Vector2 _movement_input)
	{
		Player_body_P.Velocity = _movement_input.Normalized() * Walk_Speed;
		Player_body_P.MoveAndSlide();
		
	}

    public void Movement_Calculation()
	{
		if (Check_Pressed_Move_Control_Type() == 0)
		{
			Movement();
		}

		else if (Check_Pressed_Move_Control_Type() == 1)
		{
			Movement();	
		}
	}

	public void Stop_Calculation()
	{
		if(Check_Released_Move_Control_Type() == 0)
		{
			Player_FSM_P.TransitionToState("Idle");
		}

		else if(Check_Released_Move_Control_Type() == 1)
		{
			Player_FSM_P.TransitionToState("Idle");
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

	public void Movement()
	{
		if (Input.IsActionPressed("Game_Pad_Run") || Input.IsActionPressed("Keyboard_Run"))
		{
			if (Player_Data_Autoload.Data.CURRENT_Stamina == 100)
			{
				Player.Instance.Player_State_P = Player_States.Running;
				Player_FSM_P.TransitionToState("Running");
			}
		}	
		

		else if(!Input.IsActionJustReleased("Game_Pad_Run") || !Input.IsActionJustReleased("Keyboard_Run"))
		{
            Player_FSM_P.TransitionToState("Walking");
		}

		else
		{
			Player_FSM_P.TransitionToState("Idle");
		}
	}
}
