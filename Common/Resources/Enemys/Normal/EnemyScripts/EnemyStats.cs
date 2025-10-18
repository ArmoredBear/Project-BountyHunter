using Godot;

// Define um Signal para notificar o Inimigo quando a vida mudar
public partial class EnemyStats : Node
{
	[Signal]
	public delegate void HealthChangedEventHandler(int newHealth);

	//! --- STATUS PRIMÁRIOS ---
	
	[Export]
	public int MaxHealth { get; set; } = 100;
	
	[Export(PropertyHint.Range, "1,100,1")] // Limita o valor entre 1 e 100 no Inspector
	public int Attack { get; set; } = 10; // Dano base que este inimigo causa
	
	[Export(PropertyHint.Range, "0,50,1")] // Limita o valor entre 0 e 50 no Inspector
	public int Defense { get; set; } = 5; // Valor de redução de dano

	private int _currentHealth;

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
		// Garante que a defesa mínima seja 0, embora o PropertyHint já ajude
		Defense = Mathf.Max(0, Defense);
	}

	public int CurrentHealth
	{
		get => _currentHealth;
		set
		{
			int oldHealth = _currentHealth;
			// Garante que a vida está entre 0 e MaxHealth
			_currentHealth = Mathf.Clamp(value, 0, MaxHealth);
			if (_currentHealth != oldHealth)
			{
				// Emite o signal para que o Enemy.cs reaja
				EmitSignal(SignalName.HealthChanged, _currentHealth);
			}
		}
	}
	
	// Método modificado para calcular dano reduzido pela defesa
	public void TakeDamage(int rawDamage)
	{
		// Calcula o dano efetivo: Dano Bruto - Defesa (mínimo de 1 dano)
		int effectiveDamage = Mathf.Max(1, rawDamage - Defense);
		
		GD.Print($"Dano Bruto: {rawDamage}. Defesa: {Defense}. Dano Efetivo: {effectiveDamage}");
		
		CurrentHealth -= effectiveDamage;
	}
	
	// Método para ser usado pelo Enemy.cs ao atacar o jogador
	public int GetAttackDamage()
	{
		// Pode ser modificado para incluir lógica de dano crítico ou aleatório
		return Attack;
	}
}
