using Godot;
using System;
using System.IO;
using PlayerScript.PlayerInventory;

/**-----------------------------------------------------------------------------------------------------------------------
 *!                                              PLAYER DATA AUTOLOAD CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
  	**                                                 	 PURPOSE
  	*
	**  1 - This class is a singleton, it inherits Node but it does not need to be on scene EDITOR,
	** 	that is why is called AUTOLOAD, when the scene starts the object is created automaticaly with this script
	** 	functions and properties before any other object.
  	**  2 - This holds the player data in the scene using RESOURCE and provides easy acess to the data properties on other scripts
	** 	and interaction with scene nodes on editor.
  	**  3 - This also has a constructor to initiate default data.
  	*  
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Player_Data_Autoload : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private static Player _player;
	private static Player_Data _player_data;
	private Player_Healthbar_UI _player_heathbar;
	private Player_Armorbar_UI _player_armorbar;
	private Timer _poison_timer;
	private int _counter;
	private int current_stored_damage;
	private int previous_stored_damage;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public Timer Poison_Timer;

	public Player_Healthbar_UI Player_Healthbar
	{
		get
		{
			return _player_heathbar;
		}

		set
		{
			_player_heathbar = value;
		}
	}

	public Player_Armorbar_UI Player_Armorbar
	{
		get
		{
			return _player_armorbar;
		}

		set
		{
			_player_armorbar = value;
		}
	}
	
	public static Player_Data_Autoload Instance;

	public static Player_Data Data
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

	public int Current_Stored_Damage
	{
		get
		{
			return current_stored_damage;
		}
	}

	public int Previous_Stored_Damage
	{
		get
		{
			return previous_stored_damage;
		}
	}

	public string NextSpawnName { get; set; } = "Spawn_Default";

	public bool ShouldSetLoadedPosition { get; set; } = false;
	public Vector2 LoadedPosition { get; set; }

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}

		else if (Instance != null && Instance != this)
		{
			GD.PrintErr("ERROR!! Instance of Player_Data_Autoload already exist!!");
		}

		Data = new Player_Data(100, 500, 100, true, false, false, Vector2.Zero);
		Data.MAX_Armor = 100;
		Data.CURRENT_Armor = 100;
		Data.Armored = true;
		Player_Healthbar = GetNode<Player_Healthbar_UI>("/root/Player/Player_UI/Control - Status/Control - Health_Bar");
		Player_Armorbar = GetNode<Player_Armorbar_UI>("/root/Player/Player_UI/Control - Status/Control - Health_Bar/TextureProgressBar - Armor_Bar");



	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{


	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	/**------------------------------------------------------------------------------------------------
		 **               Main methods of interaction with Player Data
	*------------------------------------------------------------------------------------------------**/

	public void Poison_Behavior()
	{
		Poison_Timer = new Timer
		{
			WaitTime = 1,
			OneShot = false,
			Autostart = false
		};

		AddChild(Poison_Timer);

		Poison_Timer.Timeout += () => GD.Print("Player Health: " + Data.CURRENT_Health);
		Poison_Timer.Timeout += Check_Alive_Caller;
		Poison_Timer.Timeout += Poison_Caller;

	}

	public void Update_Loaded_Data()
	{
		// Hide any open menus so the player spawns straight into gameplay
		var menu = GetNodeOrNull<CanvasLayer>("/root/Player/Player_UI/CanvasLayer - Player_Menu");
		if (menu != null)
		{
			menu.Visible = false;
		}

		GD.Print("Loading health from save: " + Data.CURRENT_Health);
		GD.Print("Loading stamina from save: " + Data.CURRENT_Stamina);
		Player_Healthbar.Lines.Value = Data.CURRENT_Health;
		if (Player_Healthbar.Lines_CatchUp != null) Player_Healthbar.Lines_CatchUp.Value = Data.CURRENT_Health;
		ShouldSetLoadedPosition = true;
		LoadedPosition = Data.Position;
		NextSpawnName = Data.NextSpawnName;

		// Restore inventory
		var inventory = GetNodeOrNull<PlayerInventory>("/root/Player/Inventory");
		if (inventory != null)
		{
			inventory.Clear();
			foreach (var item in Data.Inventory)
			{
				inventory.AddItem(item);
			}
		}

		// Restart poison timer if player was poisoned when saved
		if (Data.Poisoned && Poison_Timer != null)
		{
			if (Poison_Timer.IsStopped())
			{
				Poison_Timer.Start();
			}
		}

		// Also set position immediately if player exists
		GD.Print("Loaded position: " + Data.Position);
		GD.Print("Loaded scene: " + Path.GetFileNameWithoutExtension(Data.CurrentScene));
		var playerBody = GetNodeOrNull<CharacterBody2D>("/root/Player/Player_Body");
		if (playerBody != null)
		{
			GD.Print("Player body found, current GlobalPosition: " + playerBody.GlobalPosition + ", setting to: " + Data.Position);
			playerBody.GlobalPosition = Data.Position;
			GD.Print("After set, GlobalPosition: " + playerBody.GlobalPosition);
		}
		else
		{
			GD.Print("Player body not found at /root/Player/Player_Body");
		}

		// Scene verification
		string currentScenePath = GetTree().CurrentScene.SceneFilePath;
		if (!string.IsNullOrEmpty(Data.CurrentScene) && currentScenePath != Data.CurrentScene)
		{
			GD.Print("Scene mismatch detected. Current scene: " + currentScenePath + ", Saved scene: " + Data.CurrentScene + ". Loading saved scene.");
			GetTree().ChangeSceneToFile(Data.CurrentScene);
		}
		else
		{
			GD.Print("Scene verification passed. Current scene matches saved scene: " + currentScenePath);
		}
	}

	public void Update_Data_To_Save()
	{
		var playerBody = GetNode<CharacterBody2D>("/root/Player/Player_Body");
		Data.Position = playerBody.GlobalPosition;
		Data.CurrentScene = GetTree().CurrentScene.SceneFilePath;
		Data.NextSpawnName = NextSpawnName;

		// Serialize inventory into the save resource
		var inventory = GetNodeOrNull<PlayerInventory>("/root/Player/Inventory");
		if (inventory != null)
		{
			Data.Inventory.Clear();
			foreach (var type in new[] { ItemType.Consumable, ItemType.Weapon, ItemType.Armor, ItemType.Tool })
			{
				foreach (var item in inventory.GetItemsByType(type))
				{
					Data.Inventory.Add(item);
				}
			}
		}

		GD.Print("Saving position: " + Data.Position);
		GD.Print("Saving health: " + Data.CURRENT_Health);
		GD.Print("Saving stamina: " + Data.CURRENT_Stamina);
		GD.Print("Saving scene: " + Path.GetFileNameWithoutExtension(Data.CurrentScene));
	}

	/**------------------------------------------------------------------------------------------------
		 **               Simple functions to enable connection to Signals
	*------------------------------------------------------------------------------------------------**/


	public void Poison_Caller()
	{
		if (Data.Alive)
		{
			Data.Poison(10, 20, 0);
		}

		else
		{
			Poison_Timer.Stop();
		}
	}

	public void Check_Alive_Caller()
	{
		if (!Data.Alive)
		{
			GD.Print("Player is dead...");
			// Hide the player
			if (Player.Instance != null)
			{
				Player.Instance.Visible = false;
			}
			// Load GameOver scene
			Scene_Manager.Instance.Change_Scene(e_Game_Scenes.GameOver);
		}

		else
		{
			return;
		}
	}

	public void Preserve_Damage(int _damage)
	{
		if (current_stored_damage == 0)
		{
			current_stored_damage = _damage;
		}

		else
		{
			previous_stored_damage = current_stored_damage;
			current_stored_damage = _damage;
		}
	}

	public void Update_Player_Health_UI(int _value)
	{
		Player_Healthbar.Change_Health(_value);
	}

	public void Update_Player_Armor_UI()
	{
		Player_Armorbar.Update_Armor_Bar();
	}

	/// <summary>
	/// Full state reset for starting a new game.
	/// Restores Player_Data to defaults, clears inventory, and resets all autoload state.
	/// </summary>
	public void Reset()
	{
		Data.Reset();

		current_stored_damage = 0;
		previous_stored_damage = 0;
		NextSpawnName = "Spawn_Default";
		ShouldSetLoadedPosition = false;
		LoadedPosition = Vector2.Zero;

		// Clear inventory
		var inventory = GetNodeOrNull<PlayerInventory>("/root/Player/Inventory");
		if (inventory != null)
		{
			inventory.Clear();
		}

		// Stop poison timer if active
		if (Poison_Timer != null && Poison_Timer.TimeLeft > 0)
		{
			Poison_Timer.Stop();
		}

		// Sync UI so the health bar, color, and monitor trace reflect the fresh state
		if (Player_Healthbar != null)
		{
			Player_Healthbar.Lines.Value = Data.CURRENT_Health;
			if (Player_Healthbar.Lines_CatchUp != null) Player_Healthbar.Lines_CatchUp.Value = Data.CURRENT_Health;
		}

		if (Player_Armorbar != null)
		{
			Player_Armorbar.Update_Armor_Bar();
		}
	}

	#endregion
}
