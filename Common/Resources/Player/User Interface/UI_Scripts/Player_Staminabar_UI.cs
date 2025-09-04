using Godot;
using System;

public partial class Player_Staminabar_UI : TextureProgressBar
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private TextureProgressBar _stamina_bar;
	private int _stamina_regen;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export]
	public TextureProgressBar Stamina_Bar
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

	[Export] public int Stamina_Regen;



	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Stamina_Bar = this;
		Stamina_Bar.Value = Player_Data_Autoload.Data.CURRENT_Stamina;
		Stamina_Regen = 1;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("Game_Pad_Run") && Check_Running())
		{
			Stamina_Bar.Value -= Stamina_Regen;
			Player_Data_Autoload.Data.CURRENT_Stamina = (int)Stamina_Bar.Value;
		}

		else if (Input.IsActionPressed("Keyboard_Run") && Check_Running())
		{
			Stamina_Bar.Value -= Stamina_Regen;
			Player_Data_Autoload.Data.CURRENT_Stamina = (int)Stamina_Bar.Value;
		}

		else
		{
			Stamina_Bar.Value += Stamina_Regen;
			Player_Data_Autoload.Data.CURRENT_Stamina = (int)Stamina_Bar.Value;
		}
	}


	public bool Check_Running()
	{
		if (Player.Instance.Player_State_P == Player_States.Running)
		{
			return true;
		}

		return false;
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	#endregion
}
