using Godot;
using System;

public partial class Stamina_UI : TextureProgressBar
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Player_Data_Autoload _player_data_reference;
	private TextureProgressBar _stamina_bar;
	private int _stamina_regen;

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
		Player_Data_Reference = GetNode<Player_Data_Autoload>("/root/PlayerDataAutoload");

		Stamina_Bar = this;
		Stamina_Bar.Value = Player_Data_Reference.Data.CURRENT_Stamina;
		Stamina_Regen = 1;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("Game_Pad_Run") && Check_Running())
		{
			Stamina_Bar.Value -= Stamina_Regen;
			Player_Data_Reference.Data.CURRENT_Stamina = (int)Stamina_Bar.Value;
			GD.Print("Stamina: " + Stamina_Bar.Value);
		}

		else if (Input.IsActionPressed("Keyboard_Run") && Check_Running())
		{
			Stamina_Bar.Value -= Stamina_Regen;
			Player_Data_Reference.Data.CURRENT_Stamina = (int)Stamina_Bar.Value;
			GD.Print("Stamina: " + Stamina_Bar.Value);
		}

		else
		{
			Stamina_Bar.Value += Stamina_Regen;
			Player_Data_Reference.Data.CURRENT_Stamina = (int)Stamina_Bar.Value;
			GD.Print("Stamina: " + Stamina_Bar.Value);
		}
	}


	public bool Check_Running()
	{
		if (Player_Data_Reference.Player_Movement_P.Player_State == Move_State.Running)
		{
			return true;
		}

		else
		{
			return false;
		}
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	#endregion
}
