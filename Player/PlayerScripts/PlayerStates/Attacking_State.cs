using Godot;
using System;

public partial class Attacking_State : Player_State
{
    private NodePath _player_animation;
    private CollisionShape2D _attack_collider;
    
    [Export]
    public NodePath Player_Animation
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
    
    [Export]
    public CollisionShape2D Attack_Collider
    {
        get
        {
            return _attack_collider;
        }

        set
        {
            _attack_collider = value;
        }
    }

    public override void _Ready()
    {
        Player_Animation = "/root/Main/Player/Player_Body/Player_Animation";
        Attack_Collider = GetNode<CollisionShape2D>("/root/Main/Player/Player_Body/Player_FSM/Attacking/Area2D/Attack_Collider");
    }

    public override void Enter()
    {
        //GetNode<AnimationPlayer>(Player_Animation).Play("Attacking");
        GD.Print(Name + ": Attacking State was entered") ;
        Light_Attack(false);
    }

    public override void Exit()
    {
        
    }

    public override void Update(double delta)
    {
        Input_Collector();
    }

    public override void PhysicsUpdate(double delta)
    {
        
    }

    public override void HandleInput(InputEvent @event) 
    {
       if(Input.IsActionJustPressed("Game_Pad_Light_Attack", false))
	    {
			Player_FSM_P.TransitionToState("Attacking"); 
		}

        else if(Input.IsActionJustReleased("Game_Pad_Light_Attack", false))
        {
            Light_Attack(true);
            Player_FSM_P.TransitionToState("Idle");     
        }
    }

    public void Input_Collector()
	{
		Game_Pad_Directional_Input_Vector = Input.GetVector("Game_Pad_Left", "Game_Pad_Right", "Game_Pad_Up", "Game_Pad_Down");
		Keyboard_Directional_Input_Vector = Input.GetVector("Keyboard_Left", "Keyboard_Right", "Keyboard_Up", "Keyboard_Down");
	}

    private void Light_Attack(bool _trigger)
    {
        Attack_Collider.Disabled = _trigger;
        GD.Print("Button pressed");
    }
}
