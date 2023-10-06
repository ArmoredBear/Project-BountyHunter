using Godot;
using System;

public partial class Stamina_UI : TextureProgressBar
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------
	
	private TextureProgressBar _stamina_bar;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public TextureProgressBar Stamina_Bar
	{
		get
		{
			return _stamina_bar;
		}

		set
		{
			_stamina_bar = value;
		}
	}

	#endregion
	
	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Stamina_Bar = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(Input.IsActionPressed("Run"))
		{
			Stamina_Bar.Value--;
		}

		else
		{
			Stamina_Bar.Value++;
		}
	}

	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	#endregion
}
