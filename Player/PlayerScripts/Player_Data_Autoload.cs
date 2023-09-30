using Godot;
using System;
[Tool]
public partial class Player_Data_Autoload : Node
{
	private Player_Data _player_data;

	[ExportCategory ("Data")]
	[Export] public Player_Data Data
	{
		get
		{
			return _player_data;
		}

		set
		{
			_player_data = value;
		}
	}

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{	
		Data = new Player_Data(100, 100, 100);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		

	}
}
