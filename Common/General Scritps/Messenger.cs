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

	private Node _item_list_parent_node;
	private Node _fixed_items_list_parent_node;
	private string _item_list_parent_path;
	private string _fixed_items_list_parent_path;
	private List<string> _item_list_paths;
	private List<string> _fixed_items_list_paths;
	private List<Item> _items;
	private List<Item> _fixed_items;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public Node Item_List_Parent_Node
	{
		get
		{
			return _item_list_parent_node;
		}

		set
		{
			_item_list_parent_node = value;
		}
	}

	[Export] public Node Fixed_Items_List_Parent_Node
	{
		get
		{
			return _fixed_items_list_parent_node;
		}

		set
		{
			_fixed_items_list_parent_node = value;
		}
	}
	
	[Export] public string Item_List_Parent_Path
	{
		get
		{
			return _item_list_parent_path;
		}

		set
		{
			_item_list_parent_path = value;
		}
	}

	[Export] public string Fixed_Items_List_Parent_Path
	{
		get
		{
			return _fixed_items_list_parent_path;
		}

		set
		{
			_fixed_items_list_parent_path = value;
		}
	}
	
	public List<string> Item_List_Paths
	{
		get
		{
			return _item_list_paths;
		}

		set
		{
			_item_list_paths = value;
		}

	}

	public List<string> Fixed_Items_List_Paths
	{
		get
		{
			return _fixed_items_list_paths;
		}

		set
		{
			_fixed_items_list_paths = value;
		}
	}
	
	public List<Item> Items
	{
		get
		{
			return _items;
		}

		set
		{
			_items = value;
		}
	}
	
	public List<Item> Fixed_Items
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
	public delegate void Use_Pill_EventHandler(bool _usable);
	[Signal]
	public delegate void Use_Elixir_Capsule_EventHandler(bool _usable);

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
		Item_List_Parent_Path = "/root/Main/Player/Player_Items/";
		Item_List_Parent_Node = GetNode<Node>(Item_List_Parent_Path);

		Fixed_Items_List_Parent_Path = "/root/Main/Player/Player_Items/Fixed_Items/";
		Fixed_Items_List_Parent_Node = GetNode<Node>(Fixed_Items_List_Parent_Path);
		

		Item_List_Paths = new List<string>
        {
            Item_List_Parent_Path + "/Other_Items/Pill/",
			Item_List_Parent_Path + "/Other_Items/Elixir_Capsule/"
        };

		Items = new List<Item>
		{
			GetNode<Item>(Item_List_Paths[0]),
			GetNode<Item>(Item_List_Paths[1])

		};
		
		Fixed_Items_List_Paths = new List<string>
		{
			Fixed_Items_List_Parent_Path + "/Elixir_Tank/"
		};

		Fixed_Items = new List<Item>
		{
			GetNode<Item>(Fixed_Items_List_Paths[0])
		};
		
		Signals_Setter();
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
		
		Use_Pill_ += Items[0].Use;
		Use_Elixir_Capsule_ += Items[1].Use;

	}

	public void Items_Use_Emitter()
	{
		if(Input.IsActionJustReleased("Game_Pad_Evade"))
		{
			EmitSignal(SignalName.Use_Elixir_Capsule_, Items[1].Usable);
		}

		if(Input.IsActionJustReleased("Keyboard_Evade"))
		{
			EmitSignal(SignalName.Use_Elixir_Capsule_, Items[1].Usable);
		}
	}


	#endregion
}
