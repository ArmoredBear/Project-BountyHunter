using Godot;
using System;
using System.Threading.Tasks;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   TRANSITION_MANAGER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Provides fade-to-black and fade-from-black screen transitions.
	**  2 - Used by Scene_Manager to smoothly transition between scenes.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class TransitionManager : CanvasLayer
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    public static TransitionManager Instance;

    private ColorRect _fadeRect;
    private bool _isTransitioning;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        Singleton_Setup();

        _fadeRect = GetNode<ColorRect>("FadeRect");
        _fadeRect.Modulate = new Color(0, 0, 0, 0);
    }

    private void Singleton_Setup()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            GD.PrintErr("TransitionManager: Duplicate instance detected. Deleting this one.");
            QueueFree();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Transition Methods
    //!---------------------------------------------------------------------------------------------------------

    public async Task FadeToBlack(float duration = 0.5f)
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        var tween = CreateTween();
        tween.TweenProperty(_fadeRect, "modulate:a", 1.0f, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task FadeFromBlack(float duration = 0.5f)
    {
        var tween = CreateTween();
        tween.TweenProperty(_fadeRect, "modulate:a", 0.0f, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(tween, Tween.SignalName.Finished);

        _isTransitioning = false;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
