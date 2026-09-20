using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   BULLET
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - A simple projectile fired by the player's shooting system.
	**  2 - Flies straight in a fixed direction until it hits something or its lifetime runs out.
	**  3 - Damage wiring is intentionally not implemented yet.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Bullet : Area2D
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Vector2 _direction = Vector2.Zero;
	private float _life_timer;

	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	/// <summary>Base movement speed in world units per second.</summary>
	[Export]
	public float Speed = 2500f;

	/// <summary>Seconds before the bullet despawns on its own if it never hits anything.</summary>
	[Export]
	public float Lifetime = 3.0f;

	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		BodyEntered += On_Body_Entered;
		AreaEntered += On_Area_Entered;
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		_life_timer += step;

		if (_life_timer >= Lifetime)
		{
			QueueFree();
			return;
		}

		GlobalPosition += _direction * Speed * step;
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	/// <summary>Sets the flight direction and points the bullet visual along it.</summary>
	public void Launch(Vector2 direction)
	{
		_direction = direction;
		Rotation = direction.Angle();
	}

	private void On_Body_Entered(Node2D body)
	{
		QueueFree();
	}

	private void On_Area_Entered(Area2D area)
	{
		QueueFree();
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

}