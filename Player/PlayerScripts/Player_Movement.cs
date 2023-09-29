using Godot;
using System;

public partial class Player_Movement : CharacterBody2D
{	
	#region Variables

	private Vector2 _directional_input_vector;
	private float _speed;
	private float _run_speed_modifier;
	private bool _is_running;

	#endregion

	#region Properties

	[Export] public Vector2 Directional_Input_Vector
	{
		get
		{
			return _directional_input_vector;

		}
		set
		{
			_directional_input_vector = value;
		}
	}

	[Export] public float Speed
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

	[Export] public float Run_Modifier
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

	[Export] public bool Is_Running
	{
		get
		{
			return _is_running;
		}

		set
		{
			_is_running = value;
		}
	}

    #endregion

	#region Initialization

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Speed = 200;
		Run_Modifier = 2;
		Is_Running = false;
    }

	#endregion

	#region PhysicsCalculation

    public override void _PhysicsProcess(double delta)
    {
	   Movement_Input();
	   Move();
    }

	#endregion

	#region Methods

	public void Movement_Input()
	{
		Directional_Input_Vector = Input.GetVector("Left","Right","Up","Down");
		
		if(Input.IsActionPressed("Run"))
		{
			Is_Running = true;
		}
		else
		{
			Is_Running = false;
		}
	}

	public void Walk()
	{
		Velocity = Directional_Input_Vector * Speed;
	   	MoveAndSlide();
	}

	public void Run()
	{
		Velocity = Directional_Input_Vector * Speed * Run_Modifier;
		MoveAndSlide();
	}

	public void Move()
	{
		if(Is_Running)
		{
			Run();
		}
		else
		{
			Walk();
		}
	}

	#endregion
}
