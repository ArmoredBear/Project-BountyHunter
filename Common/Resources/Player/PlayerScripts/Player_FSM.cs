using Godot;
using System;
using System.Collections.Generic;
public partial class Player_FSM : Node
{
    private Dictionary<string, Player_State> _states;
    private Player_State _current_state;

    [Export] 
    public NodePath _initial_state;
    [Export]
    public Player_State Current_Player_State
    {
        get
        {
            return _current_state;
        
        }

        set
        {
            _current_state = value;
        }
    }

    public override void _Ready()
    {
        _initial_state = "%Idle";

        _current_state = new();

        _states = new Dictionary<string, Player_State>();
        foreach (Node node in GetChildren())
        {
            if (node is Player_State temp_state)
            {
                _states[node.Name] = temp_state;
                temp_state.Player_FSM_P = this;
                temp_state.Start();
                temp_state.Exit();
            }
        }

        
        Current_Player_State = GetNode<Player_State>(_initial_state);
        Current_Player_State.Enter();
        GD.Print(Current_Player_State.Name);

        
    }

    public override void _Process(double delta)
    {
        _current_state.Update(delta);
    }
    public override void _PhysicsProcess(double delta)
    {
        _current_state.PhysicsUpdate(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        _current_state.HandleInput(@event);
    }

    public void TransitionToState(string key)
    {
        if(!_states.ContainsKey(key) || _current_state == _states[key])
        {
            return;
        }

        _current_state.Exit();
        _current_state = _states[key];
        _current_state.Enter();
    } 

}
