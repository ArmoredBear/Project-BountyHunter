using Godot;
using System;

public partial class Health_Percent : RichTextLabel
{
    private float _displayedPercent;
    private float _targetPercent;
    private bool _isAnimating;

    public override void _Ready()
    {
        _displayedPercent = (int)Math.Round((double)Player_Data_Autoload.Data.CURRENT_Health / Player_Data_Autoload.Data.MAX_Health * 100);
        Text = "[center]" + (int)_displayedPercent + "%[/center]";
        Change_Color();
    }

    public override void _Process(double delta)
    {
        _targetPercent = (int)Math.Round((double)Player_Data_Autoload.Data.CURRENT_Health / Player_Data_Autoload.Data.MAX_Health * 100);

        if ((int)_displayedPercent != (int)_targetPercent || !_isAnimating)
        {
            _isAnimating = true;
            var tween = CreateTween();
            tween.TweenProperty(this, "displayed_percent", _targetPercent, 0.5).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.InOut);
            tween.Finished += () => _isAnimating = false;
        }

        Change_Color();
    }

    public float displayed_percent
    {
        get => _displayedPercent;
        set
        {
            _displayedPercent = value;
            Text = "[center]" + (int)Math.Round(_displayedPercent) + "%[/center]";
        }
    }

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
}