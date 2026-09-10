using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   ARMOR PERCENT
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Displays the player's current armor as a percentage in a RichTextLabel.
	**  2 - Smoothly tweens the displayed number toward the current armor value.
	**  3 - Changes the label color based on the current armor threshold.
	*
	*-----------------------------------------------------------------------------------------------------------------------**/
public partial class Armor_Percent : RichTextLabel
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    private float _displayedPercent;
    private bool _isAnimating;
    private bool _subscribed;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        _displayedPercent = (int)Math.Round((double)Player_Data_Autoload.Data.CURRENT_Armor / Player_Data_Autoload.Data.MAX_Armor * 100);
        Text = "[center]" + (int)_displayedPercent + "%[/center]";
    }

    public override void _Process(double delta)
    {
        if (!_subscribed && GetNodeOrNull<Messenger>("/root/Messenger") is Messenger messenger)
        {
            messenger.Player_Armor_Changed_ += OnPlayerArmorChanged;
            _subscribed = true;
        }
    }

    private void OnPlayerArmorChanged(double armor)
    {
        float newTarget = (float)Math.Round((double)armor / Player_Data_Autoload.Data.MAX_Armor * 100);

        if ((int)_displayedPercent != (int)newTarget || !_isAnimating)
        {
            _isAnimating = true;
            var tween = CreateTween();
            tween.TweenProperty(this, "displayed_percent", newTarget, 0.5).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.InOut);
            tween.Finished += () => _isAnimating = false;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Properties
    //!---------------------------------------------------------------------------------------------------------

    public float displayed_percent
    {
        get => _displayedPercent;
        set
        {
            _displayedPercent = value;
            Text = "[center]" + (int)Math.Round(_displayedPercent) + "%[/center]";
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
