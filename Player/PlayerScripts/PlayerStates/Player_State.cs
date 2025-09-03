using Godot;
using System;

public partial class Player_State : Node
{
    private Player _player;
    private Player_FSM _player_fsm;
    private CharacterBody2D _player_body;
    private float _player_base_speed = 1000;
    private Vector2 game_pad_directional_input_vector;
	private Vector2 _keyboard_directional_input_vector;

    [Export]
    public Player Player
    {
        get
        {
            return _player;
        }

        set
        {
            _player = value;
        }
    }

    [Export]
	public Vector2 Game_Pad_Directional_Input_Vector
	{
		get
		{
			return game_pad_directional_input_vector;

		}
		set
		{
			game_pad_directional_input_vector = value;
		}
	}

	[Export]
	public Vector2 Keyboard_Directional_Input_Vector
	{
		get
		{
			return _keyboard_directional_input_vector;
		}

		set
		{
			_keyboard_directional_input_vector = value;
		}
	}  
    
    [Export]
    public Player_FSM Player_FSM_P
    {
        get
        {
            return _player_fsm;
        }
        
        set
        {
            _player_fsm = value;
        }
    }

    [Export]
    public CharacterBody2D Player_body_P
    {
        get
        {
            return _player_body;
        }

        set
        {
            _player_body = value;
        }
    }

    [Export]
    public float Player_Base_Speed
    {
        get
        {
            return _player_base_speed;
        }

        set
        {
            _player_base_speed = value;
        }
        
    }



    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    public virtual void Enter() { }

    public virtual void Exit() {}

    public virtual void Start()
    {
        Player = GetNode<Player>("/root/Player/%Player_Body");
        Player_body_P = GetNode<CharacterBody2D>("/root/Player/%Player_Body");
        Player_FSM_P = GetNode<Player_FSM>("/root/Player/Player_Body/%Player_FSM");
    }

    public virtual void Update(double delta) {}
    public virtual void PhysicsUpdate(double delta) {}
    public virtual void HandleInput(InputEvent @event) {}
	
	#endregion
}
