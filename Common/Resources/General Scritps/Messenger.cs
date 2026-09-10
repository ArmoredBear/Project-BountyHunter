using Godot;
using PlayerScript.PlayerInventory;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   MESSENGER CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Autoload singleton that acts as the game's central signal hub (UI, inventory, item effects, stats).
	**  2 - Connects and emits signals only - it never contains game logic.
	**  3 - GameManager wires this hub up at startup (GameManager._Ready -> Initialize).
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Messenger : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	// Fixed items are reserved for a future logic; kept empty for now.
	private Node _fixed_items_array_parent_node;
	private string _fixed_items_array_parent_path;
	private string[] _fixed_items_array_paths;
	private Item[] _fixed_items;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public Node Fixed_Items_Array_Parent_Node
	{
		get
		{
			return _fixed_items_array_parent_node;
		}

		set
		{
			_fixed_items_array_parent_node = value;
		}
	}

	[Export] public string Fixed_Items_Array_Parent_Path
	{
		get
		{
			return _fixed_items_array_parent_path;
		}

		set
		{
			_fixed_items_array_parent_path = value;
		}
	}

	public string[] Fixed_Items_Array_Paths
	{
		get
		{
			return _fixed_items_array_paths;
		}

		set
		{
			_fixed_items_array_paths = value;
		}
	}

	public Item[] Fixed_Items
	{
		get
		{
			return _fixed_items;
		}

		set
		{
			_fixed_items = value;
		}
	}

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Signals
	//!---------------------------------------------------------------------------------------------------------

	/**------------------------------------------------------------------------
	 **                          Item Use Signals
	 *------------------------------------------------------------------------**/

	[Signal]
	public delegate void Item_Use_EventHandler(ItemInstance item);

	/**------------------------------------------------------------------------
	 **                          Player Stats Signals
	 *------------------------------------------------------------------------**/

	[Signal]
	public delegate void Player_Health_Changed_EventHandler(double health);

	[Signal]
	public delegate void Player_Stamina_Changed_EventHandler(double stamina);

	[Signal]
	public delegate void Player_Armor_Changed_EventHandler(double armor);

	[Signal]
	public delegate void Player_Took_Damage_EventHandler(double damage, bool absorbedByArmor);

	/**------------------------------------------------------------------------
	 **                         Item Pickup Signals
	 *------------------------------------------------------------------------**/

	[Signal]
	public delegate void Pickup_Item_EventHandler();

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	public void Initialize()
	{
		Signals_Setter();
	}

	public void Signals_Setter()
	{
		/**------------------------------------------------------------------------------------------------
		 **               This connects the signals to the proper method using references
		 *------------------------------------------------------------------------------------------------**/

		Item_Use_ += ItemEffects.Use;
	}

	public void Use_Item(ItemInstance item)
	{
		ItemEffects.Use(item);
	}

	public void Emit_Player_Health_Changed(double health)
	{
		EmitSignal(SignalName.Player_Health_Changed_, health);
	}

	public void Emit_Player_Stamina_Changed(double stamina)
	{
		EmitSignal(SignalName.Player_Stamina_Changed_, stamina);
	}

	public void Emit_Player_Armor_Changed(double armor)
	{
		EmitSignal(SignalName.Player_Armor_Changed_, armor);
	}

	public void Emit_Player_Took_Damage(double damage, bool absorbedByArmor)
	{
		EmitSignal(SignalName.Player_Took_Damage_, damage, absorbedByArmor);
	}

	#endregion
}