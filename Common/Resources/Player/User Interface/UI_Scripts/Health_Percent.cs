using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   HEALTH PERCENT
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Displays the player's current health as a percentage in a RichTextLabel.
	**  2 - Smoothly tweens the displayed number toward the current health value.
	**  3 - Changes the label color based on the current health threshold.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class Health_Percent : RichTextLabel
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
        _displayedPercent = (int)Math.Round((double)Player_Data_Autoload.Data.CURRENT_Health / Player_Data_Autoload.Data.MAX_Health * 100);
        Text = "[center]" + (int)_displayedPercent + "%[/center]";
        Change_Color();
    }

    public override void _Process(double delta)
    {
        if (!_subscribed && GetNodeOrNull<Messenger>("/root/Messenger") is Messenger messenger)
        {
            messenger.Player_Health_Changed_ += OnPlayerHealthChanged;
            _subscribed = true;
        }
    }

    private void OnPlayerHealthChanged(double health)
    {
        int targetPercent = (int)Math.Round((double)health / Player_Data_Autoload.Data.MAX_Health * 100);

        if ((int)_displayedPercent != targetPercent || !_isAnimating)
        {
            _isAnimating = true;
            var tween = CreateTween();
            tween.TweenProperty(this, "displayed_percent", targetPercent, 0.5).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.InOut);
            tween.Finished += () => _isAnimating = false;
        }

        Change_Color();
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

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    public void Change_Color()
    {
        int currentHealth = Player_Data_Autoload.Data.CURRENT_Health;
        int maxHealth = Player_Data_Autoload.Data.MAX_Health;
        int percent = (int)Math.Round((double)currentHealth / maxHealth * 100);

        if (percent == 100)
        {
            SelfModulate = new Color("00ff80");
        }
        else if (percent >= 75 && percent < 100)
        {
            SelfModulate = new Color("ffff00");
        }
        else if (percent >= 50 && percent < 75)
        {
            SelfModulate = new Color("ffaf3e");
        }
        else if (percent >= 25 && percent < 50)
        {
            SelfModulate = new Color("ff6400");
        }
        else if (percent < 25)
        {
            SelfModulate = new Color("ff0000");
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}