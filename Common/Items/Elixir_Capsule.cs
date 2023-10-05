using Godot;
using System;

public partial class Elixir_Capsule : Item
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	private Messenger _messenger;
	private string _messenger_path;
	private string _elixir_tank_path;
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

	[Export] public string Elixir_Tank_Path
	{
		get
		{
			return _elixir_tank_path;
		}

		set
		{
			_elixir_tank_path = value;
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
		
		Elixir_Tank_Path = Messenger_P.Fixed_Items_List_Parent_Path + "/Elixir_Tank";

		Item_Name = "Elixir Capsule";
		Usable = true;
		Equipable = true;
		Amount = 25;
		
	}

    #endregion


    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    public override void Use(bool _usable)
    {
       //Messenger_P.Fixed_Items_List_Parent_Node.GetNode<Elixir_Tank>(Elixir_Tank_Path).Refill_Tank(Amount);
    }

	#endregion
}
