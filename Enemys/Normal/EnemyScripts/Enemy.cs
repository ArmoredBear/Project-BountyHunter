using Godot;
using System;
using System.Threading.Tasks;

public partial class Enemy : CharacterBody2D
{

    [Export] public NavigationAgent2D Agent;
    [Export] public Vector2 Agent_Current_Position;
    [Export] public Vector2 Agent_Next_Path_Position;
    [Export] public Node2D Target;
    [Export] public float Movement_Speed;


    public override void _Ready()
    {
        base._Ready();

        Agent_Current_Position = GlobalTransform.Origin;
        Agent_Next_Path_Position = Agent.GetNextPathPosition();
        Movement_Speed = 500;

        if(Target == null)
        {
            Target = Player.Instance;
        }

        if(Agent == null)
        {
            Agent = GetNode<NavigationAgent2D>("NavigationAgent2D"); 
        }

        Callable.From(ActorSetup).CallDeferred();
    }


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if(Target != null)
        {
            Agent.TargetPosition = Target.GlobalPosition;
        }

        if (Agent.IsNavigationFinished())
        {
            return;
        }

        Agent_Current_Position = GlobalPosition;
        Agent_Next_Path_Position = Agent.GetNextPathPosition();
        Velocity = Agent_Current_Position.DirectionTo(Agent_Next_Path_Position) * Movement_Speed;
        MoveAndSlide();
    }


    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        if(Target != null)
        {
            Agent.TargetPosition = Target.GlobalPosition;
        }
        
    }
}
