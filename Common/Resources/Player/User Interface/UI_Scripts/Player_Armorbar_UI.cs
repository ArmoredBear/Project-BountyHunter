using Godot;


/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   PLAYER ARMORBAR UI
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Drives a TextureProgressBar so its fill (Value) always mirrors the
	**      player's CURRENT_Armor. Because armor is segmented, the value only
	**      drops when a part breaks (see Player_Data.Accumulate_Armor_Damage),
	**      so the bar jumps down in fixed chunks instead of draining per hit.
	**  2 - Flashes the armor plate when a part breaks. The flash color and
	**      intensity are tuned on the material's Flash_Color / Flash_Intensity
	**      uniforms; this script only drives the Flash amount from 1.0 down to
	**      0.0 over a short time.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class Player_Armorbar_UI : TextureProgressBar
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // Damage flash state
    private float _armor_flash;
    private int _last_armor = -1;
    private bool _subscribed;
    private const float Armor_Flash_Time = 0.25f;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        MaxValue = Player_Data_Autoload.Data.MAX_Armor;
        Value = Player_Data_Autoload.Data.CURRENT_Armor;
        _last_armor = Player_Data_Autoload.Data.CURRENT_Armor;
    }

    public override void _Process(double delta)
    {
        if (!_subscribed && GetNodeOrNull<Messenger>("/root/Messenger") is Messenger messenger)
        {
            messenger.Player_Armor_Changed_ += OnPlayerArmorChanged;
            _subscribed = true;
        }

        _armor_flash = Mathf.Max(0f, _armor_flash - (float)delta / Armor_Flash_Time);

        if (!(Material is ShaderMaterial mat))
        {
            return;
        }

        mat.SetShaderParameter("Flash", _armor_flash);

        bool glint_on = Player_Data_Autoload.Data.Armored && _last_armor > 0;
        mat.SetShaderParameter("Glint_Enabled", glint_on ? 1.0f : 0.0f);
    }

    /// <summary>
    /// Refreshes the bar fill and arms the flash on every hit absorbed by
    /// armor, or when a part breaks. Called from Messenger.Player_Armor_Changed.
    /// </summary>
    private void OnPlayerArmorChanged(double armor)
    {
        int new_armor = (int)armor;

        bool armor_dropped = new_armor < _last_armor;
        bool armor_absorbed_hit = Player_Data_Autoload.Data.Armored && new_armor >= _last_armor;

        _last_armor = new_armor;

        if (!Mathf.IsEqualApprox(Value, new_armor))
        {
            Value = new_armor;
        }

        if (armor_dropped || armor_absorbed_hit)
        {
            _armor_flash = 1f;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
