using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   LIGHTFLIP
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Rotates the light toward the player's current movement direction.
	**  2 - Applies a horizontal offset to the light based on the facing direction.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class LightFlip : PointLight2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    [Export] public float Rotation_Speed = (float)Math.Tau * 3;
	[Export] public float Theta;
	[Export] public Vector2 Direction;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

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

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

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
            Offset = new Vector2(1834, 0);
        }

        else if (Direction.X < 0)
        {
            Offset = new Vector2(1834, 0);
        }

        else if (Direction.Y < 0)
        {
            Offset = new Vector2(1834, 0);
        }

        else if (Direction.Y > 0)
        {
            Offset = new Vector2(1834, 0);
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
 
}
