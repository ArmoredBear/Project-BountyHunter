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
    private int _last_accumulated = -1;
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
    }

    public override void _Process(double delta)
    {
        Update_Armor_Bar();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Mirrors the player's armor into the bar fill and plays a short flash
    /// on EVERY hit absorbed by armor — including hits that only accumulate
    /// toward the break threshold, and harder ones that break a part.
    /// </summary>
    public void Update_Armor_Bar()
    {
        int maxArmor = Player_Data_Autoload.Data.MAX_Armor;
        int armor = Player_Data_Autoload.Data.CURRENT_Armor;
        int accumulated = Player_Data_Autoload.Data.Accumulated_Armor_Damage;

        if (MaxValue != maxArmor)
        {
            MaxValue = maxArmor;
        }

        if (!Mathf.IsEqualApprox(Value, armor))
        {
            Value = armor;
        }

        if (!(Material is ShaderMaterial mat))
        {
            return;
        }

        if (_last_armor < 0 || _last_accumulated < 0)
        {
            _last_armor = armor;
            _last_accumulated = accumulated;
        }
        else if (armor < _last_armor || accumulated != _last_accumulated)
        {
            _armor_flash = 1f;
        }

        _last_armor = armor;
        _last_accumulated = accumulated;

        float delta = (float)GetProcessDeltaTime();
        _armor_flash = Mathf.Max(0f, _armor_flash - delta / Armor_Flash_Time);

        mat.SetShaderParameter("Flash", _armor_flash);

        bool glint_on = Player_Data_Autoload.Data.Armored && armor > 0;
        mat.SetShaderParameter("Glint_Enabled", glint_on ? 1.0f : 0.0f);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
