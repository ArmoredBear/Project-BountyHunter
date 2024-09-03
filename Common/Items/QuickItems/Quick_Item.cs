using Godot;
using System;
using System.Collections.Generic;
using Array = Godot.Collections.Array;

public partial class Quick_Item : Item
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Messenger _messenger;
	private string _messenger_path;
	private Elixir_Tank _tank_reference;
	private int _amount;

	private const Item_Type Type = Item_Type.Consumable;

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public Messenger Messenger_P
	{
		get
		{
			return _messenger;
		}

		set
		{
			_messenger = value;
		}
	}
	
	[Export] public string Messenger_Path
	{
		get
		{
			return _messenger_path;
		}

		set
		{
			_messenger_path = value;
		}
	}

	[Export] public int Amount
	{
		get
		{
			return _amount;
		}

		set
		{
			_amount = value;
		}
	}
	
	#endregion


	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		Messenger_Path = "/root/Messenger";
		Messenger_P = GetNode<Messenger>(Messenger_Path);
		
	}

    #endregion


    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------


	#endregion
}
