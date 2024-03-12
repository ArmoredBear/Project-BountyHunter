using Godot;
using System;

public partial class Walking_State : Player_State
{   
    private NodePath player_animation;

    public override void _Ready()
    {
        player_animation = "/root/Main/Player/Player_Body/Player_Animation";
    }

    public override void Enter()
    {
        GetNode<AnimatedSprite2D>(player_animation).Play("Walking");
        GD.Print(Name + ": Walking state was entered");
    }

    public override void Exit()
    {
       
    }

    public override void Update(double delta)
    {
        Input_Collector();
    }

    public override void PhysicsUpdate(double delta)
    {
        Walk(Game_Pad_Directional_Input_Vector);
        Walk(Keyboard_Directional_Input_Vector);
    }

    public void Walk(Vector2 _movement_input)
    {
        Player_body_P.Velocity = _movement_input.Normalized() * Player.Speed;
		Player_body_P.MoveAndSlide();
	}

    public override void HandleInput(InputEvent @event) 
    {
        //Input_Collector();
        Stop_Calculation();
		Movement_Calculation();
    }

    public void Input_Collector()
	{
		Game_Pad_Directional_Input_Vector = Input.GetVector("Game_Pad_Left", "Game_Pad_Right", "Game_Pad_Up", "Game_Pad_Down");
		Keyboard_Directional_Input_Vector = Input.GetVector("Keyboard_Left", "Keyboard_Right", "Keyboard_Up", "Keyboard_Down");
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
			Player_FSM_P.TransitionToState("Running");
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