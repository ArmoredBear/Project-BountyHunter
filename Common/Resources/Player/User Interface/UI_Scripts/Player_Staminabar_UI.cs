using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   PLAYER STAMINABAR UI
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Displays the player's current stamina as segmented vertical bars via shader.
	**  2 - Drains stamina while running and regenerates it when idle.
	**  3 - Validates the player state before allowing stamina to be spent.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class Player_Staminabar_UI : TextureProgressBar
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private TextureProgressBar _stamina_bar;
	private int _stamina_regen;
	private ShaderMaterial _shader_mat;

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
		Stamina_Bar.Value = Stamina_Bar.MaxValue;
		Stamina_Regen = 2;

		if (Stamina_Bar.Material is ShaderMaterial mat)
		{
			_shader_mat = mat;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("Game_Pad_Run") && Check_Running())
		{
			Player_Data_Autoload.Data.CURRENT_Stamina = Mathf.Max(0, Player_Data_Autoload.Data.CURRENT_Stamina - 1);
		}

		else if (Input.IsActionPressed("Keyboard_Run") && Check_Running())
		{
			Player_Data_Autoload.Data.CURRENT_Stamina = Mathf.Max(0, Player_Data_Autoload.Data.CURRENT_Stamina - 1);
		}

		else
		{
			Player_Data_Autoload.Data.CURRENT_Stamina = Mathf.Min(Player_Data_Autoload.Data.MAX_Stamina, Player_Data_Autoload.Data.CURRENT_Stamina + Stamina_Regen);
		}

		// Keep Value at max so TextureProgressBar does not clip fragments
		Stamina_Bar.Value = Stamina_Bar.MaxValue;

		// Drive shader segmented bars
		if (_shader_mat != null)
		{
			float progress = (float)Player_Data_Autoload.Data.CURRENT_Stamina / (float)Player_Data_Autoload.Data.MAX_Stamina;
			_shader_mat.SetShaderParameter("Progress", progress);
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
