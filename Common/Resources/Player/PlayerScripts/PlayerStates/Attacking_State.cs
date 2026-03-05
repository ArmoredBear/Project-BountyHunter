using Godot;
using System;

public partial class Attacking_State : Player_State
{
    private AnimatedSprite2D _player_animation;
    private CollisionShape2D _attack_collider;
    
    [Export]
    public AnimatedSprite2D Player_Animation
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
        Player_Animation = GetNode<AnimatedSprite2D>("%Player_Animation");
        Attack_Collider = GetNode<CollisionShape2D>("%Player_Attack_Collider_Shape");
    }

    public override void Enter()
    {
        if (Player.Instance == null) return;

        Input_Collector();
        bool facingRight = Player.Instance.IsFacingRight;
        if (Game_Pad_Directional_Input_Vector.X < 0 || Keyboard_Directional_Input_Vector.X < 0)
        {
            facingRight = false;
        }
        else if (Game_Pad_Directional_Input_Vector.X > 0 || Keyboard_Directional_Input_Vector.X > 0)
        {
            facingRight = true;
        }
        // else keep last

        GD.Print("Attacking: Facing right? " + facingRight + " (input X: " + Game_Pad_Directional_Input_Vector.X + ", " + Keyboard_Directional_Input_Vector.X + ")");
        Player_Animation.Play("Light_Attack");
        Player_Animation.FlipH = !facingRight;
        GD.Print("Animation FlipH set to: " + Player_Animation.FlipH);

        // Flip collider position based on facing
        Vector2 colliderPos = Attack_Collider.Position;
        GD.Print("Collider pos before: " + colliderPos);
        if (!facingRight)
        {
            colliderPos.X = -Mathf.Abs(colliderPos.X);
        }
        else
        {
            colliderPos.X = Mathf.Abs(colliderPos.X);
        }
        Attack_Collider.Position = colliderPos;
        GD.Print("Collider pos after: " + Attack_Collider.Position);

        Light_Attack(false);

        // Play sword swoosh sound only if not in MainMenu
        if (GetTree().CurrentScene.Name != "Main_Menu")
        {
            AudioStreamPlayer2D swordPlayer = GetNode<AudioStreamPlayer2D>("Sword_Swoosh");
            if (swordPlayer != null)
            {
                swordPlayer.Play();
            }
        }
    }

    public override void Exit()
    {
        // Reset animation flip to movement facing
        if (Player.Instance != null)
        {
            Player_Animation.FlipH = !Player.Instance.IsFacingRight;
        }
        Light_Attack(true);
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
        if (Input.IsActionJustPressed("Keyboard_Light_Attack", false))
        {
            Player_FSM_P.TransitionToState("Attacking");
        }
        
        if(Input.IsActionJustPressed("Game_Pad_Light_Attack", false))
        {
			Player_FSM_P.TransitionToState("Attacking"); 
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
        //GD.Print("Button pressed");
    }

    public void On_Player_Animation_Finished()
    {
        Player_FSM_P.TransitionToState("Idle");
        Light_Attack(true);
    }

    
}
