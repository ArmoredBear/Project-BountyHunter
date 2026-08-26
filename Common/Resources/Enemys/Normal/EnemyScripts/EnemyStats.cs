using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   ENEMYSTATS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Stores enemy health data (MaxHealth and CurrentHealth).
	**  2 - Emits a HealthChanged signal and provides TakeDamage for enemy controllers.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

// Define um Signal para notificar o Inimigo quando a vida mudar
public partial class EnemyStats : Node
{
	//!---------------------------------------------------------------------------------------------------------
	#region Signals
	//!---------------------------------------------------------------------------------------------------------

	[Signal]
	public delegate void HealthChangedEventHandler(int newHealth);

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Variables
	//!---------------------------------------------------------------------------------------------------------

	[Export]
	public int MaxHealth { get; set; } = 100;

	private int _currentHealth;

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	//!---------------------------------------------------------------------------------------------------------
	#region Methods
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
	}

	public int CurrentHealth
	{
		get => _currentHealth;
		set
		{
			int oldHealth = _currentHealth;
			_currentHealth = Mathf.Clamp(value, 0, MaxHealth);
			if (_currentHealth != oldHealth)
			{
				// Emite o signal para o Enemy.cs
				EmitSignal(SignalName.HealthChanged, _currentHealth);
			}
		}
	}

	// Simples método para receber dano
	public bool TakeDamage(int damage)
	{
		int _temp_health = CurrentHealth - damage;

		if (_temp_health > 0)
		{
			CurrentHealth -= damage;
			return true;
		}

		else if (_temp_health <= 0)
		{
			CurrentHealth = 0;
			GD.Print("Enemy is dead");
			return false;
		}
		
		return true;
		
	}
	
	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
