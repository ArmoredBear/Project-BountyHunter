using Godot;
using System;
using PlayerScript.PlayerInventory;


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
	private Vector2 _position;


	/**----------------------
	 **    ARMOR SEGMENTS
	*------------------------**/

	private int _break_threshold = 50;
	private int _armor_part_value = 25;
	private int _accumulated_armor_damage;


	/**----------------------
	 **    PERSISTENCE
	*------------------------**/

	private string _next_spawn_name = "Spawn_Default";
	private Godot.Collections.Array<ItemInstance> _inventory = new();


	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Properties
	//!---------------------------------------------------------------------------------------------------------

	//! Export exposes the property or variable to the editor.

	[ExportCategory("Properties")]

	[ExportGroup("MAX_STATS")]
	[Export] public int MAX_Health { get => _max_health; set => _max_health = value; }
	[Export] public int MAX_Stamina { get => _max_stamina; set => _max_stamina = value; }
	[Export] public int MAX_Energy { get => _max_energy; set => _max_energy = value; }
	[Export] public int MAX_Armor { get => _max_armor; set => _max_armor = value; }

	[ExportGroup("CURRENT_STATS")]
	[Export] public int CURRENT_Health { get => _current_health; set => _current_health = value; }
	[Export] public int CURRENT_Stamina { get => _current_stamina; set => _current_stamina = value; }
	[Export] public int CURRENT_Energy { get => _current_energy; set => _current_energy = value; }
	[Export] public int CURRENT_Armor { get => _current_armor; set => _current_armor = value; }

	[ExportGroup("OFFENSIVE_STATS")]
	[Export] public int Attack { get => _attack; set => _attack = value; }
	[Export] public int CriticalModifier { get => _critical_modifier; set => _critical_modifier = value; }

	[ExportGroup("STATE_FLAGS")]
	[Export] public bool Alive { get => _alive; set => _alive = value; }
	[Export] public bool Armored { get => _armored; set => _armored = value; }
	[Export] public bool Poisoned { get => _poisoned; set => _poisoned = value; }

	[ExportGroup("POSITION")]
	[Export] public Vector2 Position { get => _position; set => _position = value; }

	[ExportGroup("ARMOR_SEGMENTS")]
	[Export] public int Armor_Break_Threshold { get => _break_threshold; set => _break_threshold = value; }
	[Export] public int Armor_Part_Value { get => _armor_part_value; set => _armor_part_value = value; }
	[Export] public int Accumulated_Armor_Damage { get => _accumulated_armor_damage; set => _accumulated_armor_damage = value; }

	[ExportGroup("SCENE")]
	[Export] public string CurrentScene { get; set; }

	[ExportGroup("PERSISTENCE")]
	[Export] public string NextSpawnName { get => _next_spawn_name; set => _next_spawn_name = value; }
	[Export] public Godot.Collections.Array<ItemInstance> Inventory { get => _inventory; set => _inventory = value; }

	#endregion

	//!---------------------------------------------------------------------------------------------------------
	#region Constructors
	//!---------------------------------------------------------------------------------------------------------

	public Player_Data() { }

	public Player_Data(int m_health, int m_stamina, int m_em, bool alive, bool armored, bool poisoned, Vector2 position = default)
	{
		MAX_Health = m_health;
		MAX_Stamina = m_stamina;
		MAX_Energy = m_em;

		CURRENT_Health = MAX_Health;
		CURRENT_Stamina = MAX_Stamina;
		CURRENT_Energy = MAX_Energy;

		Alive = alive;
		Armored = armored;
		Poisoned = poisoned;
		Position = position;
		CurrentScene = "";
	}

	/// <summary>
	/// Restores every field to the same defaults the constructor uses.
	/// Called when starting a new game so no stale data carries over.
	/// </summary>
	public void Reset()
	{
		MAX_Health = 100;
		MAX_Stamina = 500;
		MAX_Energy = 100;
		MAX_Armor = 100;

		CURRENT_Health = MAX_Health;
		CURRENT_Stamina = MAX_Stamina;
		CURRENT_Energy = MAX_Energy;
		CURRENT_Armor = MAX_Armor;

		Attack = 0;
		CriticalModifier = 0;

		Alive = true;
		Armored = true;
		Poisoned = false;
		Position = Vector2.Zero;

		Armor_Break_Threshold = 50;
		Armor_Part_Value = 25;
		Accumulated_Armor_Damage = 0;

		CurrentScene = "";
		NextSpawnName = "Spawn_Default";
		Inventory.Clear();
	}

    #endregion

    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    #region Stat Recovery

    /**------------------------------------------------------------------------------------------------
		 **              This section handles main stats recovery of player.
	*------------------------------------------------------------------------------------------------**/
    public void Restore_Health(int amount) => CURRENT_Health = Mathf.Min(CURRENT_Health + amount, MAX_Health);
	public void Restore_Stamina(int amount) => CURRENT_Stamina = Mathf.Min(CURRENT_Stamina + amount, MAX_Stamina);
	public void Restore_Energy(int amount) => CURRENT_Energy = Mathf.Min(CURRENT_Energy + amount, MAX_Energy);

	public void CurePoison() => Poisoned = false;

	public void TakeDamage(int damage)
	{
		if (Armored && CURRENT_Armor > 0)
		{
			// Armor absorbs the hit: damage accumulates toward the break
			// threshold. When it fills, one armor part breaks and the bar
			// drops by Armor_Part_Value instantly (health takes nothing).
			Accumulate_Armor_Damage(damage);
		}
		else
		{
			ApplyHealthDamage(damage);
		}
	}

	private void Accumulate_Armor_Damage(int damage)
	{
		_accumulated_armor_damage += damage;

		if (_accumulated_armor_damage < _break_threshold)
		{
			return;
		}

		_accumulated_armor_damage -= _break_threshold;
		CURRENT_Armor = Mathf.Max(0, CURRENT_Armor - _armor_part_value);

		if (CURRENT_Armor <= 0)
		{
			CURRENT_Armor = 0;
			Armored = false;
			_accumulated_armor_damage = 0;
		}
	}

	private void ApplyHealthDamage(int damage)
	{
		CURRENT_Health -= damage;
		if (CURRENT_Health <= 0)
		{
			CURRENT_Health = 0;
			Alive = false;
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
			CURRENT_Armor -= _damage / 4;
		}
		else if (_damage >= MAX_Armor && MAX_Armor < 200)
		{
			CURRENT_Armor -= _damage / 2;
		}

		else if (_damage < MAX_Armor && _damage == CURRENT_Armor)
		{
			CURRENT_Armor = 0;
			Armored = false;
		}

		else if (_damage < MAX_Armor && _damage > CURRENT_Armor)
		{
			CURRENT_Armor = 0;
			Armored = false;
			int damage_to_health = _damage - CURRENT_Armor;
			Health_Damage(damage_to_health);
		}

		else
		{
			CURRENT_Armor -= _damage;
		}
	}

	private void Health_Damage(int _damage)
	{
		int temp_health = CURRENT_Health - _damage;
		if (temp_health > 0)
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
		if (CURRENT_Health <= 0)
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