using Godot;
using System;
using System.Linq;

public partial class Player_Healthbar_UI : Control
{
    private TextureProgressBar _health_monitor;
    private TextureProgressBar _lines;
    private float _shader_fade_speed;
    private ShaderMaterial _shader;


    public TextureProgressBar Health_Monitor
    {
        get
        {
            return _health_monitor;
        }

        set
        {
            _health_monitor = value;
        }
    }
    public TextureProgressBar Lines
    {
        get
        {
            return _lines;
        }

        set
        {
            _lines = value;
        }
    }
    public float Shader_Fade_Speed
    {
        get
        {
            return _shader_fade_speed;
        }

        set
        {
            _shader_fade_speed = value;
        }
    }
    public ShaderMaterial Shader_P
    {
        get
        {
            return _shader;
        }

        set
        {
            _shader = value;
        }
    }
    
    public override async void _Ready()
    {
        Health_Monitor = GetNode<TextureProgressBar>("Health_Monitor");
        Lines = GetNode<TextureProgressBar>("Lines");
        Shader_P = (ShaderMaterial)Health_Monitor.Material;
       

        await ToSignal(GetTree().CreateTimer(2f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(4f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(3f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(10);

        await ToSignal(GetTree().CreateTimer(10f), SceneTreeTimer.SignalName.Timeout);
        Change_Health(-90);


        

    }

    public override async void _Process(double delta)
    {
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        Change_Fade_Shader();
        Change_Color();
    }


    public void Change_Health(double _damage)
    {
        double old_value = Lines.Value;
        
        if(_damage > 0)
        {
            Catch_Up_Change(Lines.Value, _damage);
        }

        else if(_damage < 0)
        {
            Catch_Up_Change(Health_Monitor.Value, _damage);
        }
    }

    public void Catch_Up_Change(double value_to_change, double change_value)
    {
        var tween_lines = CreateTween();
        var tween_healthbar = CreateTween();

        double _line_time = 1.0;
        double _healthbar_time = 0.3;

        tween_lines.TweenProperty(Lines, "value", value_to_change - change_value, _line_time);
        tween_healthbar.TweenProperty(Health_Monitor, "value", value_to_change - change_value,_healthbar_time);
    }


    public bool Check_Full_Health()
    {
        if(Health_Monitor.Value == Health_Monitor.MaxValue)
        {
            return true;
        }
        else

        return false;
    }


    public void Change_Fade_Shader()
    {
        if(Health_Monitor.Value == 100)
        {
            Shader_P.SetShaderParameter("Fade_Speed", -1f);
        }
        
        else if(Health_Monitor.Value >= 75 && Health_Monitor.Value < 100)
        {
            Shader_P.SetShaderParameter("Fade_Speed", -2);
        }

        else if(Health_Monitor.Value >= 50 && Health_Monitor.Value < 75)
        {
            Shader_P.SetShaderParameter("Fade_Speed", -4);
        }

        else if(Health_Monitor.Value >= 25 && Health_Monitor.Value < 50)
        {
            Shader_P.SetShaderParameter("Fade_Speed", -6);
        }

        else if(Health_Monitor.Value < 25)
        {
            Shader_P.SetShaderParameter("Fade_Speed", -8);
        }
    }

    public void Change_Color()
    {
        if(Health_Monitor.Value == 100)
        {
            Health_Monitor.TintProgress = new Color("00ff80");
            Lines.TintProgress = new Color("00ff80");
        }
        
        else if(Health_Monitor.Value >= 75 && Health_Monitor.Value < 100)
        {
            Health_Monitor.TintProgress = new Color("ffff00");
            Lines.TintProgress = new Color("ffff00");
        }

        else if(Health_Monitor.Value >= 50 && Health_Monitor.Value < 75)
        {
            Health_Monitor.TintProgress = new Color("ffaf3e");
            Lines.TintProgress = new Color("ffaf3e");
        }

        else if(Health_Monitor.Value >= 25 && Health_Monitor.Value < 50)
        {
            Health_Monitor.TintProgress = new Color("ff6400");
            Lines.TintProgress = new Color("ff6400");
        }

        else if(Health_Monitor.Value < 25)
        {
            Health_Monitor.TintProgress = new Color("ff0000");
            Lines.TintProgress = new Color("ff0000");
        }
    }
}
