using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                 PLAYER DATA CLASS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
 	**                                                     PURPOSE
 	*  
 	**  1 - This class is a resource, so it does not stay in a Node in scene directly, it stays on the project files.
 	**  2 - The class Player_Data_Autoload reference this data.
 	*  
 *-----------------------------------------------------------------------------------------------------------------------**/
[GlobalClass]
[Tool]
public partial class Player_Data : Resource, IStatus
{
	#region Variables

	private int _max_health;
	private int _max_stamina;
	private int _max_energy;
	private int _current_health;
	private int _current_stamina;
	private int _current_energy;

	#endregion

	#region Properties

	[ExportCategory ("Properties")]

	[ExportGroup ("MAX_STATS")]
	[Export] public int MAX_Health
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

	[Export] public int MAX_Stamina
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

	[Export] public int MAX_Energy
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

	
	[ExportGroup ("CURRENT_STATS")]
	[Export] public int CURRENT_Health
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

	[Export] public int CURRENT_Stamina
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
	// Called when the node enters the scene tree for the first time.
	[Export] public int CURRENT_Energy
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

	#endregion

	#region Constructors
	public Player_Data()
	{
		
	}

	public Player_Data(int m_health_p, int m_stamina_p, int m_energy_p)
	{
		MAX_Health = m_health_p;
		MAX_Stamina = m_stamina_p;
		MAX_Energy = m_energy_p;

		CURRENT_Health = MAX_Health;
		CURRENT_Stamina = MAX_Stamina;
		CURRENT_Energy = MAX_Energy;
	}

	#endregion

	#region Methods and Interfaces

	public void Restore_Health(int _health)
	{
		if(CURRENT_Health < MAX_Health)
		{
			if(_health >= MAX_Health)
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
	
	#endregion
	
}
