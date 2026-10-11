using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                  FOG DRIVER
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Pushes the camera position, zoom and viewport resolution into every
	**      fog material (group "fog_layer") every frame so Cenario_Fog.gdshader
	**      can compute world-space distance fog: a clear pocket around the
	**      camera and a thick wall of fog in the distance.
	*
	*-----------------------------------------------------------------------------------------------------------------------**/
public partial class FogDriver : Node2D
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	/// <summary>Master switch; when false the fog materials are never fed, so no fog is rendered.</summary>
	[Export]
	public bool Enabled = true;

	//!---------------------------------------------------------------------------------------------------------
	#endregion

	public override void _Ready()
	{
		// The camera exists as soon as the player autoload is in the tree.
		_Find_Camera();
	}

	public override void _Process(double delta)
	{
		if (!Enabled)
		{
			return;
		}

		if (_Resolved_Camera is null)
		{
			_Find_Camera();
			return;
		}

		Vector2 view_size = GetViewport().GetVisibleRect().Size;
		Vector2 camera_position = _Resolved_Camera.GlobalPosition;

		foreach (Node2D layer in GetTree().GetNodesInGroup(Fog_Layer_Group))
		{
			if (layer is Polygon2D polygon && polygon.Material is ShaderMaterial fog_material)
			{
				fog_material.SetShaderParameter(Cam_Pos_Parameter, camera_position);
				fog_material.SetShaderParameter(Cam_Zoom_Parameter, _Resolved_Camera.Zoom.X);
				fog_material.SetShaderParameter(Viewport_Size_Parameter, view_size);
			}
		}
	}

	//!---------------------------------------------------------------------------------------------------------
	#region Helpers
	//!---------------------------------------------------------------------------------------------------------

	private const string Fog_Layer_Group = "fog_layer";
	private const string Camera_Group = "player";
	private static readonly StringName Cam_Pos_Parameter = new StringName("cam_pos");
	private static readonly StringName Cam_Zoom_Parameter = new StringName("cam_zoom");
	private static readonly StringName Viewport_Size_Parameter = new StringName("viewport_size");

	private Camera2D _Resolved_Camera;

	private void _Find_Camera()
	{
		foreach (Node candidate in GetTree().GetNodesInGroup(Camera_Group))
		{
			if (_Find_Camera_In(candidate) is Camera2D camera)
			{
				_Resolved_Camera = camera;
				return;
			}
		}

		_Resolved_Camera = null;
	}

	private static Camera2D _Find_Camera_In(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is Camera2D camera)
			{
				return camera;
			}

			if (_Find_Camera_In(child) is Camera2D nested)
			{
				return nested;
			}
		}

		return null;
	}

	#endregion
}