using Godot;

// Define um Signal para notificar o Inimigo quando a vida mudar
public partial class EnemyStats : Node
{
	[Signal]
	public delegate void HealthChangedEventHandler(int newHealth);

	[Export]
	public int MaxHealth { get; set; } = 100;

	private int _currentHealth;

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
	public void TakeDamage(int damage)
	{
		CurrentHealth -= damage;
	}
}
