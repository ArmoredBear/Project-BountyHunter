using Godot;
using System;

public partial class Elixir_Capsule : Item
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Messenger _messenger;
	private string _messenger_path;
	private Elixir_Tank _tank_reference;
	private int _amount;

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

	[Export] public Elixir_Tank Tank_Reference
	{
		get
		{
			return _tank_reference;
		}

		set
		{
			_tank_reference = value;
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
		
		Tank_Reference = Messenger_P.Fixed_Items[0] as Elixir_Tank;

		Item_Name = "Elixir Capsule";
		Usable = true;
		Equipable = true;
		Amount = 1;
		
	}

    #endregion


    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    public override void Use(bool _usable)
    {	
		Tank_Reference.Refill_Tank(Amount);
    }

	#endregion
}
