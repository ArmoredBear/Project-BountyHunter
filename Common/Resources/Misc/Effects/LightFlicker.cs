using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   LIGHTFLICKER
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Cycles a PointLight2D's energy between MinIntensity and MaxIntensity in a loop.
	**  2 - Sculpts the intensity ramp with an optional Curve resource (linear when none is assigned).
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class LightFlicker : PointLight2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // -- Intensity ----------------------------------------------------------------
    // Lowest energy the light reaches at the start of each cycle.
    [ExportCategory("Intensity")]
    [Export(PropertyHint.Range, "0.0,5.0")] public float MinIntensity = 0.1f;

    // Highest energy the light reaches at the end of each cycle.
    [Export(PropertyHint.Range, "0.0,10.0")] public float MaxIntensity = 2.0f;

    // -- Timing -----------------------------------------------------------------
    // Seconds to ramp energy from MinIntensity up to MaxIntensity.
    [ExportCategory("Timing")]
    [Export(PropertyHint.Range, "0.1,30.0")] public float RiseDuration = 2.0f;

    // Seconds to ramp energy from MaxIntensity back down to MinIntensity.
    [Export(PropertyHint.Range, "0.1,30.0")] public float FallDuration = 2.0f;

    // If true the up/down cycle repeats forever; if false it stops at MaxIntensity.
    [Export] public bool Loop = true;

    // -- Curve ------------------------------------------------------------------
    // Optional curve (0..1 in, 0..1 out) that shapes the intensity ramp.
    // Leave empty for a linear ramp.
    [ExportCategory("Curve")]
    [Export] public Curve IntensityCurve;

    // Seconds elapsed inside the current ramp.
    private float _time = 0f;

    // True while ramping up (Min -> Max), false while ramping down (Max -> Min).
    private bool _rising = true;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        Energy = MinIntensity;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        float duration;
        if (_rising)
            duration = RiseDuration;
        else
            duration = FallDuration;

        if (_time >= duration)
        {
            if (!Loop && !_rising)
            {
                Energy = MinIntensity;
                SetProcess(false);
                return;
            }

            // Swap direction: rise reached the top, fall reached the bottom.
            _rising = !_rising;
            _time = 0f;
        }

        float progress = _time / duration;

        // Apply the user curve (linear when none is assigned).
        if (IntensityCurve != null)
            progress = IntensityCurve.Sample(progress);

        if (_rising)
            Energy = Mathf.Lerp(MinIntensity, MaxIntensity, progress);
        else
            Energy = Mathf.Lerp(MaxIntensity, MinIntensity, progress);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
