using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   DAMAGE FLASH
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Controls the red damage vignette overlay on the player's screen.
	**  2 - Persistent red tint that increases as health drops, with a heartbeat pulse effect when at 30% or below.
	**  3 - Flashes on hit above the current tint level; critical tier (10% or below) intensifies all effects.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class DamageFlash : TextureRect
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	// --- Persistent Tint ---
	/// <summary>Maximum alpha of the red tint at normal health. The vignette never exceeds this opacity unless a hit flash is active.</summary>
	[ExportGroup("Persistent Tint")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float Alpha_Limit_Normal = 0.35f;
	/// <summary>Maximum alpha of the red tint when health is at or below the critical threshold. Should be higher than normal to signal danger.</summary>
	[Export(PropertyHint.Range, "0.0,1.0,0.01")] public float Alpha_Limit_Critical = 0.55f;
	/// <summary>How strongly the tint scales with health loss at normal health. At 0 the tint never appears; at 1.0 full health loss means full alpha_limit.</summary>
	[Export(PropertyHint.Range, "0.0,1.0,0.01")] public float Alpha_Multiplier_Normal = 0.35f;
	/// <summary>How strongly the tint scales with health loss when in critical state.</summary>
	[Export(PropertyHint.Range, "0.0,1.0,0.01")] public float Alpha_Multiplier_Critical = 0.55f;
	/// <summary>Speed at which the tint lerps toward its target each frame. Higher = snappier response, lower = smoother transitions.</summary>
	[Export(PropertyHint.Range, "0.01,1.0,0.01")] public float Blend_Speed = 10.0f;

	// --- Heartbeat ---
	/// <summary>Health percentage below which the heartbeat pulse effect activates (0.3 = 30%).</summary>
	[ExportGroup("Heartbeat")]
	[Export(PropertyHint.Range, "0.01,0.5,0.01")]
	public float Heartbeat_Trigger = 0.3f;
	/// <summary>Duration in seconds of one full heartbeat cycle at normal health (lub-dub pattern).</summary>
	[Export(PropertyHint.Range, "0.5,5.0,0.1")] public float Heartbeat_Period_Normal = 2.0f;
	/// <summary>Duration in seconds of one full heartbeat cycle in critical state. Shorter = more urgent.</summary>
	[Export(PropertyHint.Range, "0.5,5.0,0.1")] public float Heartbeat_Period_Critical = 1.2f;
	/// <summary>Peak alpha added by the heartbeat bump at normal health. 0 disables the pulse.</summary>
	[Export(PropertyHint.Range, "0.0,0.5,0.01")] public float Heartbeat_Pulse_Normal = 0.15f;
	/// <summary>Peak alpha added by the heartbeat bump in critical state.</summary>
	[Export(PropertyHint.Range, "0.0,0.5,0.01")] public float Heartbeat_Pulse_Critical = 0.25f;

	// --- Flash (on-hit) ---
	/// <summary>Extra alpha added on top of the base tint when the player is hit at normal health.</summary>
	[ExportGroup("Flash")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float Flash_Intensity_Normal = 0.4f;
	/// <summary>Extra alpha added on top of the base tint when the player is hit in critical state.</summary>
	[Export(PropertyHint.Range, "0.0,1.0,0.01")] public float Flash_Intensity_Critical = 0.5f;
	/// <summary>Time in seconds for the flash to ramp up from current alpha to peak. Lower = snappier hit feedback.</summary>
	[Export(PropertyHint.Range, "0.01,0.5,0.01")] public float Flash_Ramp_Up_Time = 0.1f;
	/// <summary>Time in seconds for the flash to fade back down to the base alpha after peaking.</summary>
	[Export(PropertyHint.Range, "0.1,3.0,0.05")] public float Flash_Fade_Down_Time = 0.7f;
	/// <summary>Hard cap on the flash peak alpha. Prevents the screen from going fully red even on stacked hits.</summary>
	[Export(PropertyHint.Range, "0.5,1.0,0.01")] public float Flash_Max_Alpha = 0.85f;

	// --- Critical Threshold ---
	/// <summary>Health percentage that triggers critical mode for all effects — stronger tint, faster heartbeat, brighter flash.</summary>
	[ExportGroup("Thresholds")]
	[Export(PropertyHint.Range, "0.01,0.5,0.01")]
	public float Critical_Health_Threshold = 0.1f;

	// Internal state
	private Tween _flash_tween;
	private float _base_alpha = 0f;
	private int _last_health = -1;
	private bool _subscribed;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		// Start fully transparent — no red tint visible
		Modulate = new Color(1, 1, 1, 0);
		_last_health = Player_Data_Autoload.Data.CURRENT_Health;
	}

	public override void _Process(double delta)
	{
		if (!_subscribed && GetNodeOrNull<Messenger>("/root/Messenger") is Messenger messenger)
		{
			messenger.Player_Health_Changed_ += OnPlayerHealthChanged;
			messenger.Player_Took_Damage_ += OnPlayerTookDamage;
			_subscribed = true;
		}

		// Calculate current health as a percentage (0.0 to 1.0)
		float healthPercent = (float)_last_health / Player_Data_Autoload.Data.MAX_Health;

		bool critical = IsHealthCritical(_last_health);

		float alpha_limit;
		float alpha_multiplier;

		if (critical)
		{
			alpha_limit = Alpha_Limit_Critical;
			alpha_multiplier = Alpha_Multiplier_Critical;
		}
		else
		{
			alpha_limit = Alpha_Limit_Normal;
			alpha_multiplier = Alpha_Multiplier_Normal;
		}

		_base_alpha = Mathf.Clamp((1.0f - healthPercent) * alpha_multiplier, 0f, alpha_limit);

		if (_flash_tween == null || !_flash_tween.IsValid())
		{
			float target_alpha = _base_alpha;

			if (healthPercent <= Heartbeat_Trigger)
			{
				target_alpha = _base_alpha + Get_Heartbeat(critical);
			}

			Modulate = new Color(1, 1, 1, Mathf.Lerp(Modulate.A, target_alpha, (float)(delta * Blend_Speed)));
		}
	}

	private void OnPlayerHealthChanged(double health)
	{
		_last_health = (int)health;
	}

	private void OnPlayerTookDamage(double damage, bool absorbedByArmor)
	{
		// Only flash red when the hit actually reached the health bar
		if (!absorbedByArmor)
		{
			Flash();
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Returns true when the given health is at or below the critical threshold percentage.
	/// </summary>
	private bool IsHealthCritical(int health)
	{
		return health <= Player_Data_Autoload.Data.MAX_Health * Critical_Health_Threshold;
	}

	/// <summary>
	/// Triggers the red flash effect when the player takes damage.
	/// Quickly ramps up the vignette opacity, then fades back to the current base alpha.
	/// If a flash is already in progress, it kills the old one and starts fresh.
	/// </summary>
	public void Flash()
	{
		if (_flash_tween != null && _flash_tween.IsValid())
		{
			_flash_tween.Kill();
		}

		bool critical = IsHealthCritical(_last_health);

		float flash_intensity;
		if (critical)
		{
			flash_intensity = Flash_Intensity_Critical;
		}
		else
		{
			flash_intensity = Flash_Intensity_Normal;
		}

		float flash_target = Mathf.Clamp(_base_alpha + flash_intensity, 0f, Flash_Max_Alpha);

		_flash_tween = CreateTween();
		_flash_tween.TweenProperty(this, "modulate:a", flash_target, Flash_Ramp_Up_Time);
		_flash_tween.TweenProperty(this, "modulate:a", _base_alpha, Flash_Fade_Down_Time);
	}

	/// <summary>
	/// Generates a heartbeat-like pulsation value using two Gaussian bumps.
	/// The pattern is: bump-bump... pause... bump-bump (like a real heartbeat).
	/// </summary>
	/// <param name="critical">If true, the pulse is faster and stronger (10% health or below).</param>
	/// <returns>A value between 0.0 and pulse that gets added to the base alpha.</returns>
	private float Get_Heartbeat(bool critical)
	{
		float time = (float)(Time.GetTicksMsec() * 0.001);

		float period;
		float pulse;

		if (critical)
		{
			period = Heartbeat_Period_Critical;
			pulse = Heartbeat_Pulse_Critical;
		}
		else
		{
			period = Heartbeat_Period_Normal;
			pulse = Heartbeat_Pulse_Normal;
		}

		float t = (time % period) / period;

		float bump1 = GaussianBump(t, 0.05f, 6f);
		float bump2 = GaussianBump(t, 0.15f, 6f);

		return Mathf.Clamp(bump1 + bump2 * 0.6f, 0f, 1f) * pulse;
	}

	/// <summary>
	/// Single bell-curve bump: Exp(-((t - center) * width)^2).
	/// Used to build the lub-dub heartbeat pattern.
	/// </summary>
	private static float GaussianBump(float t, float center, float width)
	{
		return Mathf.Exp(-Mathf.Pow((t - center) * width, 2f));
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
