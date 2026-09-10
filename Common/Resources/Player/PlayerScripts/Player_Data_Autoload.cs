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
	private Messenger _messenger;
	private Timer _poison_timer;
	private Timer _pill_regen_timer;
	private int _pill_regen_remaining;
	private bool _pill_regen_active;
	private const int Pill_Regen_Tick_Heal = 5;
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
		Player_Healthbar = GetNode<Player_Healthbar_UI>("/root/Player/Player_UI/Control - PlayerStatus/Control - Status/Control - Health_Bar");
		Player_Armorbar = GetNode<Player_Armorbar_UI>("/root/Player/Player_UI/Control - PlayerStatus/Control - Status/Control - Health_Bar/TextureProgressBar - Armor_Bar");
		_messenger = GetNodeOrNull<Messenger>("/root/Messenger");

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
		Sync_Stats();
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
			foreach (var type in new[] { ItemType.Consumable, ItemType.Weapon, ItemType.Armor, ItemType.Tool, ItemType.Etc })
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
			Apply_Damage(10);
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

	/// <summary>
	/// Single entry point for dealing damage to the player.
	/// Applies the damage, preserves it for the damage screen, and broadcasts the new health and armor.
	/// </summary>
	public void Apply_Damage(int damage)
	{
		int health_before = Data.CURRENT_Health;
		Data.TakeDamage(damage);
		int health_after = Data.CURRENT_Health;
		Preserve_Damage(damage);

		bool absorbed_by_armor = health_after == health_before;

		Messenger messenger = Get_Messenger();
		if (messenger != null)
		{
			messenger.Emit_Player_Took_Damage(damage, absorbed_by_armor);
			messenger.Emit_Player_Health_Changed(Data.CURRENT_Health);
			messenger.Emit_Player_Armor_Changed(Data.CURRENT_Armor);
		}
	}

	/// <summary>
	/// Broadcasts the current values of all player stats, so every subscriber
	/// (bars, flashes, percent labels) can resync after a load or reset.
	/// </summary>
	public void Sync_Stats()
	{
		Messenger messenger = Get_Messenger();
		if (messenger != null)
		{
			messenger.Emit_Player_Health_Changed(Data.CURRENT_Health);
			messenger.Emit_Player_Stamina_Changed(Data.CURRENT_Stamina);
			messenger.Emit_Player_Armor_Changed(Data.CURRENT_Armor);
		}
	}

	/// <summary>
	/// Single entry point for healing the player.
	/// Restores health and broadcasts the new value.
	/// </summary>
	public void Apply_Heal(int amount)
	{
		Data.Restore_Health(amount);

		Messenger messenger = Get_Messenger();
		if (messenger != null)
		{
			messenger.Emit_Player_Health_Changed(Data.CURRENT_Health);
		}
	}

	/// <summary>
	/// Single entry point for changing the player stamina (drain or regen).
	/// Clamps the value and broadcasts it.
	/// </summary>
	public void Modify_Stamina(int amount)
	{
		Data.CURRENT_Stamina = Mathf.Clamp(Data.CURRENT_Stamina + amount, 0, Data.MAX_Stamina);

		Messenger messenger = Get_Messenger();
		if (messenger != null)
		{
			messenger.Emit_Player_Stamina_Changed(Data.CURRENT_Stamina);
		}
	}

	/// <summary>
	/// Starts (or stacks onto) the timed pill regeneration. Heals a small
	/// amount every second until the requested heal has been delivered.
	/// </summary>
	public void Start_Pill_Regen(int totalHeal)
	{
		_pill_regen_remaining += totalHeal;

		if (_pill_regen_active)
		{
			return;
		}

		_pill_regen_active = true;
		_pill_regen_timer = new Timer
		{
			WaitTime = 1.0,
			OneShot = false,
			Autostart = false
		};

		AddChild(_pill_regen_timer);
		_pill_regen_timer.Timeout += Apply_Pill_Regen_Tick;
		_pill_regen_timer.Start();
	}

	private void Apply_Pill_Regen_Tick()
	{
		int heal_this_tick = Mathf.Min(Pill_Regen_Tick_Heal, _pill_regen_remaining);
		Apply_Heal(heal_this_tick);
		_pill_regen_remaining -= heal_this_tick;

		if (_pill_regen_remaining <= 0 || Data.CURRENT_Health >= Data.MAX_Health)
		{
			Stop_Pill_Regen();
		}
	}

	private void Stop_Pill_Regen()
	{
		_pill_regen_remaining = 0;
		_pill_regen_active = false;

		if (_pill_regen_timer != null)
		{
			_pill_regen_timer.Stop();
			_pill_regen_timer.QueueFree();
			_pill_regen_timer = null;
		}
	}

	private Messenger Get_Messenger()
	{
		if (_messenger == null)
		{
			_messenger = GetNodeOrNull<Messenger>("/root/Messenger");
		}

		return _messenger;
	}

	public void Update_Player_Armor_UI()
	{
		Player_Armorbar.Value = Data.CURRENT_Armor;
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
			Add_Starter_Items(inventory);
		}

		// Stop poison timer if active
		if (Poison_Timer != null && Poison_Timer.TimeLeft > 0)
		{
			Poison_Timer.Stop();
		}

		// Sync UI so the health bar, color, and monitor trace reflect the fresh state
		Sync_Stats();
	}

	private void Add_Starter_Items(PlayerInventory inventory)
	{
		ItemData pillData = ResourceLoader.Load<ItemData>("res://Common/Resources/Player/PlayerScripts/PlayerInventory/Pill.tres");
		if (pillData != null)
		{
			inventory.AddItem(new ItemInstance { Data = pillData, Quantity = 1 });
		}

		ItemData swordData = ResourceLoader.Load<ItemData>("res://Common/Resources/Player/PlayerScripts/PlayerInventory/Sword.tres");
		if (swordData != null)
		{
			inventory.AddItem(new ItemInstance { Data = swordData, Quantity = 1 });
		}

		ItemData plateData = ResourceLoader.Load<ItemData>("res://Common/Resources/Player/PlayerScripts/PlayerInventory/Plate.tres");
		if (plateData != null)
		{
			inventory.AddItem(new ItemInstance { Data = plateData, Quantity = 1 });
		}

		ItemData lanternData = ResourceLoader.Load<ItemData>("res://Common/Resources/Player/PlayerScripts/PlayerInventory/Lantern.tres");
		if (lanternData != null)
		{
			inventory.AddItem(new ItemInstance { Data = lanternData, Quantity = 1 });
		}

		ItemData upgradeStoneData = ResourceLoader.Load<ItemData>("res://Common/Resources/Player/PlayerScripts/PlayerInventory/UpgradeStone.tres");
		if (upgradeStoneData != null)
		{
			inventory.AddItem(new ItemInstance { Data = upgradeStoneData, Quantity = 1 });
		}
	}

	#endregion
}
