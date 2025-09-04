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

    private Enemy_States State_of_Enemy;

    //!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        State_of_Enemy = Enemy_States.Idle;

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

        Enemy_Movement();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------



    //!---------------------------------------------------------------------------------------------------------
	#region Methods
	//!---------------------------------------------------------------------------------------------------------

    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        if(Target != null)
        {
            Agent.TargetPosition = Target.GlobalPosition;
        }
        
    }
    
    public void Enemy_Movement()
    {
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

    //!---------------------------------------------------------------------------------------------------------
	#region Enemy States
	//!---------------------------------------------------------------------------------------------------------
    

    public void Patrol()
    {

    }

    public void Pursue()
    {

    }

    public void Search()
    {

    }

    public void Attack()
    {

    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    
}
