using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   DAMAGE ARMOR FLASH
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Blue/white vignette overlay that flashes when armor absorbs a hit.
	**  2 - Listens for Messenger.Player_Took_Damage and flashes when the hit
	**      was fully absorbed by armor.
	**  3 - No persistent tint or heartbeat — only the flash on hit.
	*
	*-----------------------------------------------------------------------------------------------------------------------**/
public partial class DamageArmorFlash : TextureRect
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	/// <summary>Extra alpha added on top of zero when the armor flash triggers.</summary>
	[ExportGroup("Flash")]
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float Flash_Intensity = 0.35f;

	/// <summary>Time in seconds for the flash to ramp up from current alpha to peak.</summary>
	[Export(PropertyHint.Range, "0.01,0.5,0.01")]
	public float Flash_Ramp_Up_Time = 0.08f;

	/// <summary>Time in seconds for the flash to fade back to transparent after peaking.</summary>
	[Export(PropertyHint.Range, "0.1,3.0,0.05")]
	public float Flash_Fade_Down_Time = 0.5f;

	/// <summary>Hard cap on the flash peak alpha.</summary>
	[Export(PropertyHint.Range, "0.5,1.0,0.01")]
	public float Flash_Max_Alpha = 0.7f;

	// Internal state
	private Tween _flash_tween;
	private bool _subscribed;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		Modulate = new Color(1, 1, 1, 0);
	}

	public override void _Process(double delta)
	{
		if (!_subscribed && GetNodeOrNull<Messenger>("/root/Messenger") is Messenger messenger)
		{
			messenger.Player_Took_Damage_ += OnPlayerTookDamage;
			_subscribed = true;
		}
	}

	private void OnPlayerTookDamage(double damage, bool absorbedByArmor)
	{
		if (absorbedByArmor)
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
	/// Triggers the armor flash overlay when armor absorbs a hit.
	/// Quickly ramps up the vignette opacity, then fades back to transparent.
	/// </summary>
	public void Flash()
	{
		if (_flash_tween != null && _flash_tween.IsValid())
		{
			_flash_tween.Kill();
		}

		float flash_target = Mathf.Clamp(Flash_Intensity, 0f, Flash_Max_Alpha);

		_flash_tween = CreateTween();
		_flash_tween.TweenProperty(this, "modulate:a", flash_target, Flash_Ramp_Up_Time);
		_flash_tween.TweenProperty(this, "modulate:a", 0f, Flash_Fade_Down_Time);
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
