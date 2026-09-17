using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                  CAMERA CONTROL
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Shakes the camera when the player takes damage (health or armor).
	**  2 - Listens for Messenger.Player_Took_Damage.
	**  3 - Intensity scales with damage amount relative to max health; absorbed
	**      armor hits use a fixed multiplier so even small hits register.
	*
	*-----------------------------------------------------------------------------------------------------------------------**/
public partial class CameraControl : Camera2D
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	/// <summary>Master multiplier applied to all shake amounts. Tweak this to scale intensity globally.</summary>
	[Export(PropertyHint.Range, "0.0,10.0,0.1")]
	public float Intensity_Multiplier = 1.0f;

	/// <summary>How quickly the camera catches up to the player. Higher = snappier, lower = floatier.</summary>
	[ExportGroup("Smoothing")]
	[Export(PropertyHint.Range, "0.5,30.0,0.5")]
	public float Smoothing_Speed = 5.0f;

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
	private bool _subscribed;
	private Vector2 _smoothed_center;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		// Start on the player so the camera does not slide in on spawn.
		_smoothed_center = GlobalPosition;
	}

	public override void _Process(double delta)
	{
		if (!_subscribed && GetNodeOrNull<Messenger>("/root/Messenger") is Messenger messenger)
		{
			messenger.Player_Took_Damage_ += OnPlayerTookDamage;
			_subscribed = true;
		}

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

		// Smoothly follow the player (catch-up), then snap the smoothed centre to
		// the pixel grid so world-space art lines up with screen pixels. We do the
		// smoothing ourselves instead of using Camera2D's built-in smoothing,
		// because that moves the camera by sub-pixel amounts between frames, which
		// makes the world-space grass shimmer. Snapping after the damp keeps the
		// catch-up feel while every rendered frame still lands on the pixel grid.
		float dt = (float)delta;
		Vector2 target = GetParentOrNull<Node2D>()?.GlobalPosition ?? _smoothed_center;
		_smoothed_center = new Vector2(
			MathUtils.Damp(_smoothed_center.X, target.X, Smoothing_Speed, dt),
			MathUtils.Damp(_smoothed_center.Y, target.Y, Smoothing_Speed, dt));

		float pixel_world_size = 1.0f / Zoom.X;
		GlobalPosition = (_smoothed_center / pixel_world_size).Round() * pixel_world_size;
	}

	private void OnPlayerTookDamage(double damage, bool absorbedByArmor)
	{
		if (absorbedByArmor)
		{
			// Fixed-intensity shake for absorbed hits so even small hits register
			_shake_amount = Mathf.Max(_shake_amount, Max_Intensity * Armor_Hit_Scale);
		}
		else
		{
			float ratio = (float)damage / Player_Data_Autoload.Data.MAX_Health;
			_shake_amount = Mathf.Max(_shake_amount, Max_Intensity * ratio);
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
