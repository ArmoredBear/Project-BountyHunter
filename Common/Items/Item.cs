using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                    	ITEM CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Hold items information and methods that will influence player.
	**  2 - Hold items information to be reused / repurposed on the items on scenario.
	*  
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Item : Resource, IUse
{	
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	protected string _item_name;
	protected string _description;
	protected bool _usable;
	protected bool _is_equipment;
	protected int _money_value;
	
	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	[Export] public string Item_Name
	{
		get
		{
			return _item_name;
		}

		set
		{
			_item_name = value;
		}
	}

	[Export] public string Description
	{
		get
		{
			return _description;
		}

		set
		{
			_description = value;
		}
	}

	[Export] public int Value
	{
		get
		{
			return _money_value;
		}

		set
		{
			_money_value = value;
		}
	}

	[Export] public bool Usable
	{
		get
		{
			return _usable;
		}

		set
		{
			_usable = value;
		}
	}

	[Export] public bool Is_Equipment
	{
		get
		{
			return _is_equipment;
		}

		set
		{
			_is_equipment = value;
		}
	}

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Constructors
	//!---------------------------------------------------------------------------------------------------------

	public Item()
	{
		Item_Name = "default";
		Description = "default";
		Usable = false;
		Is_Equipment = false;
		Value = 0;
	}

    #endregion

	//!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	public virtual void Use(bool _usable)
	{
		
	}

    public virtual void Use(bool _usable, int _value)
	{
		
	}

	public virtual void Use(bool _usable, int _value, int _modifier)
	{

	}

	public virtual void Use(bool _usable, int _value, bool _modifier)
	{

	}

	#endregion

}
