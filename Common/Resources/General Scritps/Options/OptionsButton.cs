using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   OPTIONSBUTTON
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Toggles the Options panel visibility when the "Options" menu button is pressed.
	**  2 - Follows the same pattern as Credits.cs.
	**  3 - Drives the menu animations based on the panel state. Signs use the player's SpeedScale,
	**        so positive keeps the animation's own direction and negative reverses it:
	**        - Opening: chains reverse violently (Chain1 -> right, Chain2 -> left) then ease to a stop;
	**          the gear reverses its spin.
	**        - Closing: chains snap back to their idle direction and decelerate to their idle speed;
	**          the gear returns to its idle spin.
	**  4 - Plays the OptionsPanel slide-in/slide-out animation together with the reactions, then
	**        hides the panel once the closing slide finishes.
	**
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class OptionsButton : Button
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    [Export] public Panel Options_Panel;

    [Export] public AnimationPlayer Chain1Player;
    [Export] public AnimationPlayer Chain2Player;
    [Export] public AnimationPlayer GearPlayer;

    [Export] public AnimationPlayer SettingsSlidePlayer;

    // Idle speeds captured from each player at startup, so tuning in the editor is respected.
    private float _chain1IdleSpeed;
    private float _chain2IdleSpeed;
    private float _gearIdleSpeed;

    // One tween per player so the three reactions never cancel each other.
    private Tween _chain1Tween;
    private Tween _chain2Tween;
    private Tween _gearTween;

    // Timed trigger for the gear's impact wobble, so it can be cancelled if the panel closes first.
    private Tween _gearImpactTrigger;

    // Set while a closing slide is in flight, so the finish handler only hides the panel when
    // the last requested action was to close (protects against rapid re-opening mid-slide).
    private bool _closing;

    // Violent speed as a multiple of the idle speed.
    private const float ViolentMultiplier = 40.0f;

    // Chains and gear each keep their swing going for this long (at ReactionSpeed 1.0).
    [Export] public float ChainSpinDuration = 0.35f;
    [Export] public float GearSpinDuration = 0.35f;

    // Duration of the return-to-idle swing when the panel closes (at ReactionSpeed 1.0).
    [Export] public float ReturnDuration = 0.25f;

    // Master speed: 1.0 is the base; raise it to make the whole reaction faster while staying synced.
    [Export] public float ReactionSpeed = 1.0f;

    // Short eases the chains/gear into the violent peak (and out of it) so the idle loop
    // hand-off feels continuous instead of an instant speed cut.
    [Export] public float SpinUpTime = 0.1f;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        if (Chain1Player != null) _chain1IdleSpeed = Chain1Player.SpeedScale;
        if (Chain2Player != null) _chain2IdleSpeed = Chain2Player.SpeedScale;
        if (GearPlayer != null) _gearIdleSpeed = GearPlayer.SpeedScale;

        if (SettingsSlidePlayer != null)
        {
            SettingsSlidePlayer.AnimationFinished += OnSettingsAnimationFinished;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    public void OnButtonPressed()
    {
        if (Options_Panel == null)
        {
            return;
        }

        bool isOpening = !Options_Panel.Visible;

        if (isOpening)
        {
            OnOptionsOpened();
            ShowPanel();
        }
        else
        {
            OnOptionsClosed();
            HidePanel();
        }
    }

    private void ShowPanel()
    {
        if (SettingsSlidePlayer == null)
        {
            Options_Panel.Visible = true;
            return;
        }

        _closing = false;
        Options_Panel.Visible = true;
        SettingsSlidePlayer.SpeedScale = ReactionSpeed / ChainSpinDuration;
        SettingsSlidePlayer.Stop();
        SettingsSlidePlayer.Play("SettingsSlide_Crash");
    }

    private void HidePanel()
    {
        if (SettingsSlidePlayer == null)
        {
            Options_Panel.Visible = false;
            return;
        }

        _closing = true;
        SettingsSlidePlayer.SpeedScale = ReactionSpeed / ReturnDuration;
        SettingsSlidePlayer.PlayBackwards("SettingsSlide");
    }

    private void OnSettingsAnimationFinished(StringName animName)
    {
        if (_closing)
        {
            Options_Panel.Visible = false;
        }
    }

    private void OnOptionsOpened()
    {
        // Reverse violently, then ease to a full stop so the chains/gear hold while the panel is open.
        Drive(Chain1Player, ref _chain1Tween, _chain1IdleSpeed * -ViolentMultiplier, 0.0f, ChainSpinDuration / ReactionSpeed);
        Drive(Chain2Player, ref _chain2Tween, _chain2IdleSpeed * -ViolentMultiplier, 0.0f, ChainSpinDuration / ReactionSpeed);
        Drive(GearPlayer, ref _gearTween, _gearIdleSpeed * -ViolentMultiplier, 0.0f, GearSpinDuration / ReactionSpeed);
        QueueGearImpact();
    }

    private void OnOptionsClosed()
    {
        // Cancel a pending impact wobble so it never fires during the closing reaction.
        if (_gearImpactTrigger != null && _gearImpactTrigger.IsValid())
        {
            _gearImpactTrigger.Kill();
        }

        // Snap back to the idle direction at high speed, then decelerate down to the idle speed.
        // The gear may have been paused for its impact bounce, so resume its spin from the pause point.
        if (GearPlayer != null && !GearPlayer.IsPlaying())
        {
            GearPlayer.Play(GearPlayer.CurrentAnimation);
        }

        Drive(Chain1Player, ref _chain1Tween, _chain1IdleSpeed * ViolentMultiplier, _chain1IdleSpeed, ReturnDuration / ReactionSpeed);
        Drive(Chain2Player, ref _chain2Tween, _chain2IdleSpeed * ViolentMultiplier, _chain2IdleSpeed, ReturnDuration / ReactionSpeed);
        Drive(GearPlayer, ref _gearTween, _gearIdleSpeed * ViolentMultiplier, _gearIdleSpeed, ReturnDuration / ReactionSpeed);
    }

    /// <summary>Eases one player's SpeedScale up to <paramref name="startSpeed"/>, then down to
    /// <paramref name="endSpeed"/>. Any previous drive on the same player is killed first so rapid
    /// toggling never leaves two tweens fighting over the property.</summary>
    private void Drive(AnimationPlayer player, ref Tween tween, float startSpeed, float endSpeed, float duration)
    {
        if (player == null)
        {
            return;
        }

        if (tween != null && tween.IsValid())
        {
            tween.Kill();
        }

        tween = player.CreateTween();

        float spinUp = SpinUpTime / ReactionSpeed;
        if (spinUp > 0.0f)
        {
            tween.TweenProperty(player, "speed_scale", startSpeed, spinUp)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
        }

        tween.TweenProperty(player, "speed_scale", endSpeed, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    /// <summary>Fires the impact wobble slightly before the swing finishes easing to zero, so the
    /// bounce reads as the landing instead of a pause-then-bounce. Cancelled if the panel closes
    /// before the trigger elapses.</summary>
    private void QueueGearImpact()
    {
        if (GearPlayer == null)
        {
            return;
        }

        if (_gearImpactTrigger != null && _gearImpactTrigger.IsValid())
        {
            _gearImpactTrigger.Kill();
        }

        float spinUp = SpinUpTime / ReactionSpeed;
        float swing = GearSpinDuration / ReactionSpeed;

        _gearImpactTrigger = GearPlayer.CreateTween();
        _gearImpactTrigger.TweenInterval((spinUp + swing) * 0.6f);
        _gearImpactTrigger.TweenCallback(Callable.From(PlayGearImpact));
    }

    /// <summary>Bounce! The gear overshoots its stop angle and settles back with decaying wobbles,
    /// like the panel slotting into place. The spin player is paused during it so the tween is not
    /// overwritten by the frozen rotation track.</summary>
    private void PlayGearImpact()
    {
        if (GearPlayer == null || GearPlayer.GetParent() is not Control gear)
        {
            return;
        }

        GearPlayer.Pause();

        float baseRotation = gear.Rotation;
        float speed = ReactionSpeed;
        Tween t = gear.CreateTween();
        t.TweenProperty(gear, "rotation", baseRotation - 0.08f, 0.025f / speed)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        t.TweenProperty(gear, "rotation", baseRotation + 0.04f, 0.045f / speed)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        t.TweenProperty(gear, "rotation", baseRotation - 0.015f, 0.04f / speed)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        t.TweenProperty(gear, "rotation", baseRotation, 0.03f / speed)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}