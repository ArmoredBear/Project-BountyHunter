using Godot;
using System;

public partial class Evade_State : Player_State
{
    private AnimationPlayer _player_animation;
    
    public AnimationPlayer Player_Animation
    {
        get
        {
            return _player_animation;
        }

        set
        {
            _player_animation = value;
        }
    }

    public override void _Ready()
    {
        Player_Animation = GetNode<AnimationPlayer>("%Player_Animation");
    }

    public override void Enter()
    {
        //GetNode<AnimationPlayer>(Player_Animation).Play("Evading");
        GD.Print(Name + ": Evading State was entered") ;
    }

    public override void Exit()
    {
        
    }

    public override void Update(double delta)
    {
        Input_Collector();
    }
    
    public override void HandleInput(InputEvent @event) {}

    public void Input_Collector()
	{
		Game_Pad_Directional_Input_Vector = Input.GetVector("Game_Pad_Left", "Game_Pad_Right", "Game_Pad_Up", "Game_Pad_Down");
		Keyboard_Directional_Input_Vector = Input.GetVector("Keyboard_Left", "Keyboard_Right", "Keyboard_Up", "Keyboard_Down");
	}

    public void Evade()
    {
        
    }
}
