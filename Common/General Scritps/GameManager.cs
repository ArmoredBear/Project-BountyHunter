using Godot;
using System;

public partial class GameManager : Node
{
	public static GameManager Instance;
	
	public override void _Ready()
	{
		base._Ready();
			
		if(Instance == null)
		{
			Instance = this;
		}

		else if (Instance != null && Instance != this)
		{
			GD.PrintErr("ERROR!! Instance of Game Manager already exist!!");
		}

		GD.Print("Game Manager loaded...");
		
	}


}
