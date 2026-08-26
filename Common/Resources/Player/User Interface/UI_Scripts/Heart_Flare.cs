using Godot;


/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   HEART FLARE
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Draws a lens-flare light ball (ball + cross streaks) over the ECG heartbeat
	**      flash. Each frame it reads the current beat's R-spike position from the
	**      health bar and pushes it into the HeartFlare shader on this fullscreen
	**      ColorRect, so a ball of light pops with every heartbeat and follows the
	**      spike as it scrolls left, fading out.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class Heart_Flare : ColorRect
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    [Export]
    public Player_Healthbar_UI Health_Bar;

    [Export]
    public float Flare_Size = 0.02f;

    [Export]
    public float Flare_Multiplier = 1f;

    //! How fast the flare eases toward the trace tip (higher = snappier). Use a low
    //! value to make it trail/lag behind the tip instead of locking onto it.
    [Export]
    public float Follow_Speed = 12f;

    private Vector2 _current_pos;
    private bool _first_frame = true;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Process(double delta)
    {
        if (!(Material is ShaderMaterial mat) || Health_Bar == null)
        {
            return;
        }

        if (!Health_Bar.Get_Beat_Flash_Point(out Vector2 global_pos, out float strength, out Color color))
        {
            mat.SetShaderParameter("Flare_Strength", 0f);
            return;
        }

        // Ease toward the tip so the flare trails behind it instead of locking on.
        if (_first_frame)
        {
            _current_pos = global_pos;
            _first_frame = false;
        }
        else if (Follow_Speed <= 0f)
        {
            _current_pos = global_pos;
        }
        else
        {
            _current_pos = MathUtils.Damp(_current_pos, global_pos, Follow_Speed, (float)delta);
        }

        // Global (canvas-layer) position of the flare point -> normalized UV of this rect.
        Vector2 local = _current_pos - GlobalPosition;
        Vector2 uv;
        if (Size.X > 0f && Size.Y > 0f)
        {
            uv = local / Size;
        }
        else
        {
            uv = Vector2.Zero;
        }

        mat.SetShaderParameter("Flare_Pos", uv);
        mat.SetShaderParameter("Flare_Strength", Mathf.Clamp(strength * Flare_Multiplier, 0f, 1f));
        mat.SetShaderParameter("Flare_Color", color);
        mat.SetShaderParameter("Flare_Size", Flare_Size);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
