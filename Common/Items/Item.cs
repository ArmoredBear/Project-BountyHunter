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
	
[Tool]
public partial class Item : Node, IUse
{	
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	protected string _item_name;
	protected string _description;
	protected bool _usable;
	protected bool _equipable;
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

	[Export] public bool Equipable
	{
		get
		{
			return _equipable;
		}

		set
		{
			_equipable = value;
		}
	}

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Constructors
	//!---------------------------------------------------------------------------------------------------------

	public  override void _Ready()
	{
		Item_Name = "default";
		Description = "default";
		Usable = false;
		Equipable = false;
		Value = 0;
	}

    public override void _Process(double delta)
    {
        
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
