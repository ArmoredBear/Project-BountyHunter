using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   MATH UTILS
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  Shared math helpers used across multiple scripts.
	*
	*-----------------------------------------------------------------------------------------------------------------------**/
public static class MathUtils
{
	/// <summary>Hermite smoothstep: maps t from 0..1 to a smooth ease-in-out curve.</summary>
	public static float SmoothStep(float t)
	{
		return t * t * (3f - 2f * t);
	}

	/// <summary>Frame-rate-independent exponential damp toward a float target.</summary>
	public static float Damp(float current, float target, float rate, float dt)
	{
		return Mathf.Lerp(current, target, 1f - Mathf.Exp(-rate * dt));
	}

	/// <summary>Frame-rate-independent exponential damp toward a Vector2 target.</summary>
	public static Vector2 Damp(Vector2 current, Vector2 target, float rate, float dt)
	{
		return current.Lerp(target, 1f - Mathf.Exp(-rate * dt));
	}
}
