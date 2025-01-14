using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                  PLAYER DATA CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
 	**                                                   PURPOSE
 	*  
 	**  1 - This class is a resource, so it does not stay in a Node in scene directly, it stays on the project files.
 	**  2 - The class Player_Data_Autoload reference this data.
 	*  
*-----------------------------------------------------------------------------------------------------------------------**/

[GlobalClass]
[Tool]
public partial class Player_Data : Resource, IStatus
{
	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	/**----------------------
	 **    MAX Stats
	*------------------------**/

	private int _max_health;
	private int _max_stamina;
	private int _max_energy;
	private int _max_armor;


	/**----------------------
	 **    CURRENT Stats
	*------------------------**/

	private int _current_health;
	private int _current_stamina;
	private int _current_energy;
	private int _current_armor;


	/**----------------------
	 **    OFFENSIVE Stats
	*------------------------**/

	private int _attack;
	private int _critical_modifier;


	/**----------------------
	 **    OTHER Stats
	*------------------------**/

	private bool _alive;
	private bool _armored;
	private bool _poisoned;


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	//! Export exposes the property or variable to the editor.

	[ExportCategory("Properties")]

	[ExportGroup("MAX_STATS")]
	[Export]
	public int MAX_Health
	{
		get
		{
			return _max_health;
		}

		set
		{
			_max_health = value;
		}
	}
	[Export]
	public int MAX_Stamina
	{
		get
		{
			return _max_stamina;
		}

		set
		{
			_max_stamina = value;
		}
	}
	[Export]
	public int MAX_Energy
	{
		get
		{
			return _max_energy;
		}

		set
		{
			_max_energy = value;
		}
	}
	[Export]
	public int MAX_Armor
	{
		get
		{
			return _max_armor;
		}

		set
		{
			_max_armor = value;
		}
	}


	[ExportGroup("CURRENT_STATS")]
	[Export]
	public int CURRENT_Health
	{
		get
		{
			return _current_health;
		}

		set
		{
			_current_health = value;
		}
	}
	[Export]
	public int CURRENT_Stamina
	{
		get
		{
			return _current_stamina;
		}

		set
		{
			_current_stamina = value;
		}
	}
	[Export]
	public int CURRENT_Energy
	{
		get
		{
			return _current_energy;
		}

		set
		{
			_current_energy = value;
		}
	}
	[Export]
	public int CURRENT_ARMOR
	{
		get
		{
			return _current_armor;
		}

		set
		{
			_current_armor = value;
		}
	}


	[ExportGroup("OFFENSIVE STATS")]
	[Export]
	public int Attack
	{
		get
		{
			return _attack;
		}

		set
		{
			_attack = value;
		}
	}

	[ExportGroup("OTHER STATS")]

	[Export]
	public bool Alive
	{
		get
		{
			return _alive;
		}

		set
		{
			_alive = value;
		}
	}
	[Export]
	public bool Armored
	{
		get
		{
			return _armored;
		}

		set
		{
			_armored = value;
		}
	}
	[Export]
	public bool Poisoned
	{
		get
		{
			return _poisoned;
		}

		set
		{
			_poisoned = value;
		}
	}



	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Constructors
	//!---------------------------------------------------------------------------------------------------------

	public Player_Data()
	{

	}

	public Player_Data(int m_health_p, int m_stamina_p, int m_energy_p, bool p_alive, bool p_armored, bool p_poisoned)
	{
		MAX_Health = m_health_p;
		MAX_Stamina = m_stamina_p;
		MAX_Energy = m_energy_p;

		CURRENT_Health = MAX_Health;
		CURRENT_Stamina = MAX_Stamina;
		CURRENT_Energy = MAX_Energy;

		Alive = p_alive;
		Armored = p_armored;
		Poisoned = p_poisoned;
	}


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Methods and Interfaces
	//!---------------------------------------------------------------------------------------------------------

	#region Stat Recovery

	/**------------------------------------------------------------------------------------------------
		 **              This section handles main stats recovery of player.
	*------------------------------------------------------------------------------------------------**/
	public void Restore_Health(int _health)
	{
		if (CURRENT_Health < MAX_Health)
		{
			if (_health >= MAX_Health)
			{
				CURRENT_Health = MAX_Health;
			}
			else
			{
				CURRENT_Health += _health;
			}
		}
	}

	public void Restore_Stamina(int _stamina)
	{

	}

	public void Restore_Energy(int _energy)
	{

	}

	public void Cure_Poison(bool _poisoned)
	{
		if (_poisoned)
		{
			Poisoned = false;
		}
		else
		{
			return;
		}
	}


	#endregion

	#region Damage and Ailments

	/**------------------------------------------------------------------------------------------------
		 **              This section handles player damage and ailments.
	*------------------------------------------------------------------------------------------------**/

	public void Take_Damage(int _damage)
	{
		Health_Damage(_damage);
	}

	public void Poison(int _poison_damage, int _poison_time, int _poison_counter)
	{
		if (_poison_counter < _poison_time)
		{
			if (!Poisoned)
			{
				Poisoned = true;
				Take_Damage(_poison_damage);
			}

			else
			{
				Take_Damage(_poison_damage);
			}

			_poison_counter++;
		}

		else
		{
			Poisoned = false;
			return;
		}
	}

	private void Calculate_Physical_Damage(int _damage)
	{
		if (Armored && Alive)
		{
			Armor_Damage(_damage);
		}

		else if (!Armored && Alive)
		{
			Health_Damage(_damage);
		}
	}

	private void Calculate_Elemental_Damage()
	{

	}

	private void Armor_Damage(int _damage)
	{
		if (_damage >= MAX_Armor && MAX_Armor == 200)
		{
			CURRENT_ARMOR -= _damage / 4;
		}
		else if (_damage >= MAX_Armor && MAX_Armor < 200)
		{
			CURRENT_ARMOR -= _damage / 2;
		}

		else if (_damage < MAX_Armor && _damage == CURRENT_ARMOR)
		{
			CURRENT_ARMOR = 0;
			Armored = false;
		}

		else if (_damage < MAX_Armor && _damage > CURRENT_ARMOR)
		{
			CURRENT_ARMOR = 0;
			Armored = false;
			int damage_to_health = _damage - CURRENT_ARMOR;
			Health_Damage(damage_to_health);
		}

		else
		{
			CURRENT_ARMOR -= _damage;
		}
	}

	private void Health_Damage(int _damage)
	{
		int temp_health = CURRENT_Health - _damage;
		if(temp_health > 0)
		{
			CURRENT_Health -= _damage;
		}

		else
		{
			CURRENT_Health = 0;
			Change_Alive();
			
		}
		
	}

	public bool Change_Alive()
	{
		if(CURRENT_Health <= 0)
		{
			Alive = false;
			return Alive;
		}

		else
		{
			Alive = true;
			return Alive;			
		}
	}

	#endregion


	#endregion

}
