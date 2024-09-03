using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   MESSENGER CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - This class is a singleton, it inherits Node but it does not need to be on scene EDITOR,
	** 	that is why is called AUTOLOAD, when the scene starts the object is created automaticaly with this script
	** 	functions and properties before any other object.
	**  2 - This is a Messenger, it will hold the main PLAYER CUSTOM SIGNALS for handling game events and connections
	**  to objects in the scene.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Messenger : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Node _quick_items_array_parent_node;
	private Node _fixed_items_array_parent_node;
	private string _quick_items_array_parent_path;
	private string _fixed_items_array_parent_path;
	private string[] _quick_items_array_path;
	private string[] _fixed_items_array_paths;
	private Item[] _quick_items;
	private Item[] _fixed_items;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public Node Quick_Items_Array_Parent_Node
	{
		get
		{
			return _quick_items_array_parent_node;
		}

		set
		{
			_quick_items_array_parent_node = value;
		}
	}

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
	
	[Export] public string Quick_Items_Array_Parent_Path
	{
		get
		{
			return _quick_items_array_parent_path;
		}

		set
		{
			_quick_items_array_parent_path = value;
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
	
	public string[] Quick_Items_Array_Path
	{
		get
		{
			return _quick_items_array_path;
		}

		set
		{
			_quick_items_array_path = value;
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
	
	public Item[] Quick_Items
	{
		get
		{
			return _quick_items;
		}

		set
		{
			_quick_items = value;
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
	 **                          Usable Items Signals
	 *------------------------------------------------------------------------**/

	
	[Signal]
	public delegate void Usable_Item_EventHandler(bool _usable);

	/**------------------------------------------------------------------------
	 **                         Item Pickup Signals
	 *------------------------------------------------------------------------**/
	
	[Signal]
	public delegate void Pickup_ItemEventHandler();
	
	

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(GetTree().CurrentScene.Name == "Main")
		{
			Quick_Items_Array_Parent_Path = "/root/Main/Player/Player_Items/Quick_Items/";
			Fixed_Items_Array_Parent_Path = "/root/Main/Player/Player_Items/Fixed_Items/";

			Quick_Items_Array_Parent_Node = GetNode(Quick_Items_Array_Parent_Path);
			Fixed_Items_Array_Parent_Node = GetNode(Fixed_Items_Array_Parent_Path);

			Quick_Items_Array_Path = new string[1]
			{
				Quick_Items_Array_Parent_Path + "Usable_Item"
			};

			Quick_Items = new Item[1]
			{
				GetNode<Item>(Quick_Items_Array_Path[0])
			};

			Signals_Setter();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Items_Use_Emitter();
	}

	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	public void Signals_Setter()
	{
		/**------------------------------------------------------------------------------------------------
		 **               This connects the signals to the proper method using references
		 *------------------------------------------------------------------------------------------------**/
		
		Usable_Item_ += Quick_Items[0].Use;

	}

	/**------------------------------------------------------------------------------------------------
		 *!               Temporary use of emitters to use some items for testing...
	*------------------------------------------------------------------------------------------------**/

	public void Items_Use_Emitter()
	{
		if(Input.IsActionJustReleased("Game_Pad_UseItem"))
		{
			EmitSignal(SignalName.Usable_Item_, Quick_Items[0].Usable);
		}

		if(Input.IsActionJustReleased("Keyboard_UseItem"))
		{
			EmitSignal(SignalName.Usable_Item_, Quick_Items[0].Usable);
		}
	}


	#endregion
}
