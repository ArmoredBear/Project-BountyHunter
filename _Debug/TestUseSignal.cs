using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
 **                                                    TESTING CLASS
 *-----------------------------------------------------------------------------------------------------------------------**/

 /**-----------------------------------------------------------------------------------------------------------------------
  *    ! This class has the purpose of only testing how signals work...
  *  
  *  
  *  
  *  
  *-----------------------------------------------------------------------------------------------------------------------**/

  
public partial class TestUseSignal : Node
{
	private Node _list_node;

	public Node List_Node
	{
		get
		{
			return _list_node;
		}
		
		set
		{
			_list_node = value;
		}
	}
	
	[Signal]
	public delegate void Use_Item_Signal_EventHandler(bool _usable);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		List_Node = GetNode<Node>("../Player/Player_Items");
		Pill _item = List_Node.GetChild(0) as Pill;
		Use_Item_Signal_ += _item.Use;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
