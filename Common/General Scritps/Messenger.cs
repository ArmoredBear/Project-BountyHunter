using Godot;
using System;

public partial class Messenger : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Node _item_list_node;

	#endregion

	#region Properties
	[Export] Node Item_List_Node
	{
		get
		{
			return _item_list_node;
		}

		set
		{
			_item_list_node = value;
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------


	#endregion
}
