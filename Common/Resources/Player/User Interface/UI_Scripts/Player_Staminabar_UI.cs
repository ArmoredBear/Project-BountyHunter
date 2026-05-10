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
		Stamina_Regen = 2;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("Game_Pad_Run") && Check_Running())
		{
			Player_Data_Autoload.Data.CURRENT_Stamina = Mathf.Max(0, Player_Data_Autoload.Data.CURRENT_Stamina - 1);
			Stamina_Bar.Value = Player_Data_Autoload.Data.CURRENT_Stamina;
		}

		else if (Input.IsActionPressed("Keyboard_Run") && Check_Running())
		{
			Player_Data_Autoload.Data.CURRENT_Stamina = Mathf.Max(0, Player_Data_Autoload.Data.CURRENT_Stamina - 1);
			Stamina_Bar.Value = Player_Data_Autoload.Data.CURRENT_Stamina;
		}

		else
		{
			Player_Data_Autoload.Data.CURRENT_Stamina = Mathf.Min(Player_Data_Autoload.Data.MAX_Stamina, Player_Data_Autoload.Data.CURRENT_Stamina + Stamina_Regen);
			Stamina_Bar.Value = Player_Data_Autoload.Data.CURRENT_Stamina;
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
