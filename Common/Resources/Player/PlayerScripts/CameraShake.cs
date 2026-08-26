using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   CAMERA SHAKE
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Shakes the camera when the player takes damage (health or armor).
	**  2 - Detects health hits by polling CURRENT_Health each frame.
	**  3 - Detects armor hits by polling Accumulated_Armor_Damage (every absorbed hit,
	**      not just segment breaks).
	**  4 - Intensity scales with damage amount relative to max health.
	*
	*-----------------------------------------------------------------------------------------------------------------------**/
public partial class CameraShake : Camera2D
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	/// <summary>Master multiplier applied to all shake amounts. Tweak this to scale intensity globally.</summary>
	[Export(PropertyHint.Range, "0.0,10.0,0.1")]
	public float Intensity_Multiplier = 1.0f;

	/// <summary>Peak shake intensity when the player takes a full-health hit (100% of MAX_Health).</summary>
	[ExportGroup("Shake")]
	[Export(PropertyHint.Range, "0.0,100.0,1.0")]
	public float Max_Intensity = 25.0f;

	/// <summary>How fast the shake decays back to zero. Higher = shorter shake.</summary>
	[Export(PropertyHint.Range, "1.0,30.0,0.5")]
	public float Decay_Rate = 10.0f;

	/// <summary>Multiplier for armor-absorbed hits (1.0 = same as health, 0.5 = half).</summary>
	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	public float Armor_Hit_Scale = 0.75f;

	// Internal state
	private float _shake_amount;
	private int _last_health = -1;
	private int _last_accumulated = -1;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		_last_health = Player_Data_Autoload.Data.CURRENT_Health;
		_last_accumulated = Player_Data_Autoload.Data.Accumulated_Armor_Damage;
	}

	public override void _Process(double delta)
	{
		int health = Player_Data_Autoload.Data.CURRENT_Health;
		int accumulated = Player_Data_Autoload.Data.Accumulated_Armor_Damage;

		// Detect health decrease → direct hit
		if (_last_health >= 0 && health < _last_health)
		{
			int damage = _last_health - health;
			float ratio = (float)damage / Player_Data_Autoload.Data.MAX_Health;
			_shake_amount = Mathf.Max(_shake_amount, Max_Intensity * ratio);
		}

		// Detect any armor damage accumulation → absorbed hit
		if (_last_accumulated >= 0 && accumulated != _last_accumulated)
		{
			// Use a fixed intensity for absorbed hits so even small hits shake
			_shake_amount = Mathf.Max(_shake_amount, Max_Intensity * Armor_Hit_Scale);
		}

		_last_health = health;
		_last_accumulated = accumulated;

		// Decay
		if (_shake_amount > 0.01f)
		{
			_shake_amount = MathUtils.Damp(_shake_amount, 0f, Decay_Rate, (float)delta);
		}
		else
		{
			_shake_amount = 0f;
		}

		// Apply random offset
		if (_shake_amount > 0f)
		{
			float final_shake = _shake_amount * Intensity_Multiplier;
			float shake_x = RandomSignedFloat() * final_shake;
			float shake_y = RandomSignedFloat() * final_shake;
			Offset = new Vector2(shake_x, shake_y);
		}
		else
		{
			Offset = Vector2.Zero;
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Methods
	//!---------------------------------------------------------------------------------------------------------

	private static float RandomSignedFloat()
	{
		return (float)(GD.Randf() * 2.0 - 1.0);
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
