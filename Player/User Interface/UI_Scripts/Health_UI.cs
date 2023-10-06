using Godot;
using System;

public partial class Health_UI : TextureRect
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Tween _health_tween;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public Tween Health_Tween
	{
		get
		{
			return _health_tween;
		}

		set
		{
			_health_tween = value;
		}
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		Tween_Health();	
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(!Health_Tween.IsRunning())
		{
			Tween_Health();
		}
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	private void Tween_Health()
	{
		Health_Tween = CreateTween();
		Health_Tween.TweenProperty(this,"position", new Vector2(470,0),5);
		Health_Tween.Finished += Set_Position;
	}

	private void Set_Position()
	{
		Position = new Vector2(30,0);
	}

	#endregion
}
