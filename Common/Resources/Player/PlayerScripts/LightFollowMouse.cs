using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   LIGHTFOLLOWMOUSE
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Rotates the light toward the mouse cursor position at all times.
	**  2 - Smooth rotation, same swing behavior as LightFlip but always aimed at the mouse.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class LightFollowMouse : PointLight2D
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

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        FaceDirection(GetGlobalMousePosition() - GlobalPosition);
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
        Offset = new Vector2(1834, 0);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
 
}