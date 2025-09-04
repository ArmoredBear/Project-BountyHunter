using Godot;
using System;

public partial class LightFlip : PointLight2D
{

    [Export] public float Rotation_Speed = (float)Math.Tau * 3;
	[Export] public float Theta;
	[Export] public Vector2 Direction;

    public override void _Ready()
    {
        base._Ready();
        
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        FaceDirection(Player.Instance.Velocity);
        RotateLight(delta);
		
    }

    public void FaceDirection(Vector2 _direction)
	{
		Direction = _direction;
	}

    public void RotateLight(double _delta)
    {
        Theta = Mathf.Wrap(Mathf.Atan2(Direction.Y, Direction.X) - Rotation, -Mathf.Pi, Mathf.Pi);
		Rotation += (float)(Math.Clamp(Rotation_Speed * _delta, 0, Math.Abs(Theta)) * Math.Sign(Theta));

        if(Direction.X > 0)
        {
            Offset = new Vector2(1800, 0);
        }

        else if (Direction.X < 0)
        {
            Offset = new Vector2(1800, 0);
        }

        else if (Direction.Y < 0)
        {
            Offset = new Vector2(1800, 0);
        }

        else if (Direction.Y > 0)
        {
            Offset = new Vector2(1800, 0);
        }
    }
 
}
