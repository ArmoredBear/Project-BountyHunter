using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   MESSENGER CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	 *  
	 **  1 - This class is a singleton, it inherits Node but it does not need to be on scene, that is why is called AUTOLOAD,
	**  when the scene starts the object is created automaticaly with this script functions and properties before any other
	** 	object.
	 **  2 - This is a Messenger, it will hold most if not all CUSTOM SIGNALS for handling game events and connections
	 **  to objects in the scene.
	 *
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Messenger : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Node _item_list_parent_node;
	private string _item_list_parent_path;
	private List<string> _item_list_paths;

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
	
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Signals
	//!---------------------------------------------------------------------------------------------------------

	[Signal]
	public delegate void UseItemEventHandler(bool _usable);
	
	[Signal]
	public delegate void PickUpItemEventHandler();
	
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Item_List_Parent_Path = "/root/Main/Player/Player_Items";
		Item_List_Parent_Node = GetNode<Node>(Item_List_Parent_Path);
		Item_List_Paths = new List<string>
        {
            Item_List_Parent_Path + "/Pill",
			Item_List_Parent_Path + "/Elixir"
        };
		
		Signal_Setter();
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

	public void Signal_Setter()
	{
		/**------------------------------------------------------------------------------------------------
		 **               This connects the signals to the proper method using references
		 *------------------------------------------------------------------------------------------------**/

		UseItem += Item_List_Parent_Node.GetNode<Pill>(_item_list_paths[0]).Use;
		UseItem += Item_List_Parent_Node.GetNode<Elixir>(_item_list_paths[1]).Use;

	}

	public void Items_Use_Emitter()
	{
		//! THIS IS FOR TESTING ONLY, IT WILL CHANGE.
		if(Input.IsActionPressed("Evade"))
		{
			EmitSignal(SignalName.UseItem, true );
		}
	}


	#endregion
}
