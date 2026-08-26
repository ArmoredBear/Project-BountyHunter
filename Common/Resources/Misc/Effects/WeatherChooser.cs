using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   WEATHER CHOOSER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Picks rain or snow at random each time the Main Menu scene runs.
	**  2 - 50/50 chance: the scene shows either rain by itself or snow by itself.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class WeatherChooser : Control
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // Fullscreen ColorRect carrying the rain shader.
    [ExportCategory("Weather")]
    [Export] public ColorRect RainRect;

    // GpuParticles2D node rendering the snowfall.
    [Export] public GpuParticles2D SnowParticles;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        bool showRain = GD.Randf() < 0.5f;
        bool showSnow = !showRain;

        if (RainRect != null)
            RainRect.Visible = showRain;

        if (SnowParticles != null)
            SnowParticles.Visible = showSnow;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
