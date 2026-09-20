using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   AIM SYSTEM
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Handles aim mode: toggling it on/off and keeping the target UI on the aim point.
	**  2 - The aim point comes from the mouse (mouse mode) or is driven by the right analog
	**      stick (gamepad mode). The last device used wins.
	**  3 - While aiming, LMB (mouse) or L2 (gamepad) fires a bullet from the player's
	**      Target_Shoot_Point toward the current aim point.
	**  4 - Attached to the "Control - Target_System" node inside the player UI.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class AimSystem : Control
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private bool _is_aiming;
	private bool _input_is_gamepad;
	private Vector2 _current_target_world;
	private Camera2D _world_camera;
	private Node2D _target_shoot_point;

	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	public static AimSystem Instance;

	/// <summary>True while the player is in aim mode (target visible, LMB/L2 fires).</summary>
	public bool IsAiming
	{
		get
		{
			return _is_aiming;
		}
	}

	/// <summary>The bullet scene spawned each time the player fires.</summary>
	[Export]
	public PackedScene Bullet_Scene;

	/// <summary>Path to the node bullets spawn from (the player's muzzle / shoot point).</summary>
	[Export]
	public NodePath Target_Shoot_Point_Path = "/root/Player/Player_Body/Target_Shoot_Point";

	/// <summary>How fast the target cursor moves (world units per second) when driven by the right stick.</summary>
	[Export]
	public float Aim_Speed = 2600f;

	/// <summary>Max distance in world units the right-stick cursor may travel from the player.</summary>
	[Export]
	public float Aim_Max_Range = 4000f;

	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}

		else if (Instance != null && Instance != this)
		{
			GD.PrintErr("ERROR!! Instance of AimSystem already exist!!");
		}

		Visible = false;

		if (Player.Instance != null)
		{
			_world_camera = Player.Instance.GetNodeOrNull<Camera2D>("Camera2D");
		}

		_target_shoot_point = GetNodeOrNull<Node2D>(Target_Shoot_Point_Path);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion or InputEventMouseButton)
		{
			_input_is_gamepad = false;
		}

		else if (@event is InputEventJoypadMotion or InputEventJoypadButton)
		{
			_input_is_gamepad = true;
		}
	}

	public override void _Process(double delta)
	{
		Handle_Aim_Toggle();
		Update_Target_Position((float)delta);
		Handle_Fire_Input();
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------



	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	private void Handle_Aim_Toggle()
	{
		if (Input.IsActionJustPressed("Aim_Toggle", false))
		{
			Set_Aiming(!_is_aiming);
		}
	}

	private void Update_Target_Position(float delta)
	{
		if (!_is_aiming)
		{
			return;
		}

		Vector2 stick_input = Input.GetVector("Game_Pad_Aim_Left", "Game_Pad_Aim_Right", "Game_Pad_Aim_Up", "Game_Pad_Aim_Down");

		if (stick_input.LengthSquared() > 0.001f)
		{
			_input_is_gamepad = true;
		}

		if (_input_is_gamepad)
		{
			_current_target_world += stick_input * Aim_Speed * delta;
			_current_target_world = Clamp_Target_To_Range(_current_target_world);
		}

		else
		{
			if (_world_camera != null)
			{
				_current_target_world = _world_camera.GetGlobalMousePosition();
			}
		}

		GlobalPosition = GetViewport().GetCanvasTransform() * _current_target_world;
	}

	private void Handle_Fire_Input()
	{
		if (!_is_aiming)
		{
			return;
		}

		if (Input.IsActionJustPressed("Keyboard_Light_Attack", false) || Input.IsActionJustPressed("Game_Pad_Shoot", false))
		{
			Fire();
		}
	}

	private void Set_Aiming(bool aiming)
	{
		_is_aiming = aiming;
		Visible = aiming;

		if (aiming)
		{
			_current_target_world = Get_Initial_Target();
		}
	}

	private Vector2 Get_Initial_Target()
	{
		if (_input_is_gamepad)
		{
			if (Player.Instance != null)
			{
				return Clamp_Target_To_Range(Player.Instance.GlobalPosition + new Vector2(0, -600));
			}
		}

		if (_world_camera != null)
		{
			return _world_camera.GetGlobalMousePosition();
		}

		return Vector2.Zero;
	}

	private Vector2 Clamp_Target_To_Range(Vector2 target)
	{
		if (Player.Instance == null)
		{
			return target;
		}

		Vector2 from_player = target - Player.Instance.GlobalPosition;

		if (from_player.Length() > Aim_Max_Range)
		{
			return Player.Instance.GlobalPosition + from_player.Normalized() * Aim_Max_Range;
		}

		return target;
	}

	private void Fire()
	{
		if (Bullet_Scene == null)
		{
			return;
		}

		if (_target_shoot_point == null)
		{
			return;
		}

		Vector2 start = _target_shoot_point.GlobalPosition;
		Vector2 direction = (_current_target_world - start).Normalized();

		if (direction == Vector2.Zero)
		{
			direction = Vector2.Up;
		}

		Bullet bullet = Bullet_Scene.Instantiate<Bullet>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.GlobalPosition = start;
		bullet.Launch(direction);
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

}