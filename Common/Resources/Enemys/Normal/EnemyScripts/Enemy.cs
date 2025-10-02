using Godot;
using System;
using System.Collections.Generic; // Necessário para Dictionary

public partial class Enemy : CharacterBody2D
{
	// --- ENUM: Estados da Máquina de Estados ---
	public enum State
	{
		Patrol,      // Patrulha em um perímetro
		Chase,       // Persegue o jogador
		Attack,      // Ataca o jogador
		Flee         // Fuga com vida baixa
	}

	//!---------------------------------------------------------------------------------------------------------
	#region Exportable Properties and Node References
	//!---------------------------------------------------------------------------------------------------------
	[Export] public AnimatedSprite2D Animation_Sprite;
	[Export] public NavigationAgent2D Agent;
	[Export] public Area2D DetectionArea; // Adicionado para detectar o Player
	[Export] public Area2D AttackArea;    // Adicionado para alcance de ataque

	[Export] public float Movement_Speed { get; set; } = 500f;
	[Export] public float AttackRange { get; set; } = 50f; // Raio da AttackArea
	[Export] public int FleeThreshold { get; set; } = 30; // Vida para iniciar a fuga (<= 30)
	[Export] public Node2D[] PatrolPoints { get; set; } // Pontos de patrulha definidos na cena

	private EnemyStats _stats; // Referência ao script de status
	private CharacterBody2D _player; // Referência ao jogador
	private State _currentState = State.Patrol;
	private int _currentPatrolPointIndex = 0;
	
	// LÓGICA FUZZY (Entradas)
	private float _distanceToPlayer = float.MaxValue;

	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		// 1. Configuração de NÓS
		Animation_Sprite = GetNode<AnimatedSprite2D>("Enemy_Animation");
		Agent = GetNode<NavigationAgent2D>("NavigationAgent2D");
		_stats = GetNode<EnemyStats>("EnemyStats"); // Assumindo a estrutura de cena

		// 2. Conexão de Signals
		_stats.HealthChanged += OnHealthChanged;
		
		DetectionArea = GetNode<Area2D>("DetectionArea");
		AttackArea = GetNode<Area2D>("AttackArea");
		DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
		DetectionArea.BodyExited += OnDetectionAreaBodyExited;

		// 3. Estado Inicial
		_currentState = State.Patrol;
		Animation_Sprite.Play("Phantom_Idle");
	}


	public override void _PhysicsProcess(double delta)
	{
		// 1. Atualiza Entradas Fuzzy
		UpdateFuzzyInputs();
		
		// 2. Tomada de Decisão (Fuzzy Logic)
		State nextState = DetermineNextStateFuzzy();

		// 3. Transição de Estado
		if (nextState != _currentState)
		{
			ChangeState(nextState);
		}

		// 4. Execução do Estado Atual
		switch (_currentState)
		{
			case State.Patrol:
				HandlePatrol((float)delta);
				break;
			case State.Chase:
				HandleChase((float)delta);
				break;
			case State.Attack:
				HandleAttack((float)delta);
				break;
			case State.Flee:
				HandleFlee((float)delta);
				break;
		}

		MoveAndSlide(); // Aplica a velocidade definida nos Handlers
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region Fuzzy Logic Implementation
	//!---------------------------------------------------------------------------------------------------------

	private void UpdateFuzzyInputs()
	{
		if (_player != null)
		{
			_distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
		}
		else
		{
			_distanceToPlayer = float.MaxValue;
		}
	}
	
	// Dicionário para armazenar prioridades de estado
	private Dictionary<State, float> _statePriorities = new Dictionary<State, float>();

	// Função de pertinência simplificada para "Vida Baixa" (0 a 1)
	private float GetLowHealthMembership()
	{
		float healthFraction = (float)_stats.CurrentHealth / _stats.MaxHealth;
		
		// Vida > 30% -> 0. Não é vida baixa
		if (healthFraction > 0.3f) return 0.0f; 
		
		// Linearmente de 0.3 (0% de pertinência) a 0.0 (100% de pertinência)
		return 1.0f - (healthFraction / 0.3f);
	}
	
	// Função de pertinência simplificada para "Distância Próxima" (0 a 1)
	private float GetNearDistanceMembership()
	{
		float maxRelevantDistance = DetectionArea.GetNode<CollisionShape2D>("CollisionShape2D").Shape.GetRect().Size.X; // Usa o raio da DetectionArea
		
		if (_distanceToPlayer <= AttackRange) return 1.0f; // Se estiver no alcance de ataque, é 1.0 (Muito Próximo)
		if (_distanceToPlayer >= maxRelevantDistance) return 0.0f; // Fora da área de detecção
		
		// Linearmente de 1 (AttackRange) a 0 (maxRelevantDistance)
		return 1.0f - ((_distanceToPlayer - AttackRange) / (maxRelevantDistance - AttackRange));
	}
	
	private State DetermineNextStateFuzzy()
	{
		float lowHealth = GetLowHealthMembership();
		float nearDistance = GetNearDistanceMembership();
		
		// --- REGRAS DE INFERÊNCIA FUZZY ---
		
		// 1. FUGA (Flee): Se a vida estiver BAIXA, FUGIR é prioridade máxima (Peso 1.5)
		_statePriorities[State.Flee] = lowHealth * 1.5f; 
		
		// 2. ATAQUE (Attack): Se estiver PRÓXIMO E Vida NÃO FOR BAIXA (Mathf.Min é o AND Fuzzy)
		// Usamos AttackArea.HasOverlappingBodies() para garantir que o alvo está fisicamente atingível.
		float attackPriority = Mathf.Min(nearDistance, 1.0f - lowHealth);
		_statePriorities[State.Attack] = AttackArea.HasOverlappingBodies() ? attackPriority : 0f;
		
		// 3. PERSEGUIÇÃO (Chase): Se não estiver atacando/fugindo E jogador detectado.
		// A perseguição tem prioridade sobre a patrulha.
		_statePriorities[State.Chase] = (_player != null) ? Mathf.Min(1.0f - nearDistance, 1.0f - lowHealth) : 0f;
		
		// 4. PATRULHA (Patrol): Estado padrão. Prioridade é 1.0 apenas se Chase for 0 (player não visto/muito longe)
		_statePriorities[State.Patrol] = (_player == null || _statePriorities[State.Chase] < 0.1f) ? 1.0f : 0.0f;
		
		// --- DEFUZZIFICAÇÃO (Escolha do Estado com Maior Prioridade) ---
		State bestState = State.Patrol;
		float maxPriority = 0f;

		foreach (var entry in _statePriorities)
		{
			if (entry.Value > maxPriority)
			{
				maxPriority = entry.Value;
				bestState = entry.Key;
			}
		}
		
		// Prioriza Fuga se a vida estiver no limite, mesmo que a distância seja 0 (ataque).
		if (_stats.CurrentHealth <= FleeThreshold && maxPriority < _statePriorities[State.Flee])
		{
			return State.Flee;
		}

		return bestState;
	}
	
	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region State Machine and Movement Handlers
	//!---------------------------------------------------------------------------------------------------------
	
	private void ChangeState(State newState)
	{
		GD.Print($"Transição: {_currentState} -> {newState}");
		_currentState = newState;
		// Reinicia a animação se necessário
		if (newState == State.Attack) Animation_Sprite.Play("Phantom_Attack");
		if (newState == State.Chase) Animation_Sprite.Play("Phantom_Run");
		if (newState == State.Patrol) Animation_Sprite.Play("Phantom_Run");
		if (newState == State.Flee) Animation_Sprite.Play("Phantom_Run");
	}

	private void MoveToTarget(Vector2 targetPosition, float speedMultiplier = 1.0f)
	{
		Agent.TargetPosition = targetPosition;
		
		if (Agent.IsNavigationFinished())
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 nextPathPosition = Agent.GetNextPathPosition();
		Velocity = GlobalPosition.DirectionTo(nextPathPosition) * Movement_Speed * speedMultiplier;
		
		// Lógica de flip do sprite
		if (Velocity.X != 0)
		{
			Animation_Sprite.FlipH = Velocity.X < 0;
		}
	}

	private void HandlePatrol(float delta)
	{
		if (PatrolPoints == null || PatrolPoints.Length == 0)
		{
			Velocity = Vector2.Zero;
			Animation_Sprite.Play("Phantom_Idle");
			return;
		}
		
		Vector2 targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
		
		// Verifica se chegou ao ponto de patrulha
		if (GlobalPosition.DistanceTo(targetPosition) < 20) 
		{
			_currentPatrolPointIndex = (_currentPatrolPointIndex + 1) % PatrolPoints.Length;
			targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
		}

		MoveToTarget(targetPosition, 0.5f); // Patrulha mais lenta
		Animation_Sprite.Play("Phantom_Run");
	}

	private void HandleChase(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Patrol);
			return;
		}
		
		MoveToTarget(_player.GlobalPosition, 1.2f); // Perseguição mais rápida
		Animation_Sprite.Play("Phantom_Run");
	}

	private void HandleAttack(float delta)
	{
		Velocity = Vector2.Zero;
		// Simulação do ataque físico
		// Se o ataque for baseado em animação, use Signals da animação
		
		// Exemplo: Simplesmente causa dano ao jogador se estiver na AttackArea
		// (Você precisará de um script no Player para receber o dano)
		GD.Print("Inimigo: Golpe físico!");
	}

	private void HandleFlee(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Patrol);
			return;
		}
		
		// Define um ponto para correr: O oposto do jogador, a uma distância segura (ex: 500 unidades)
		Vector2 fleeDirection = (GlobalPosition - _player.GlobalPosition).Normalized();
		Vector2 safeTarget = GlobalPosition + fleeDirection * 500f; 
		
		MoveToTarget(safeTarget, 1.5f); // Fuga muito mais rápida
		Animation_Sprite.Play("Phantom_Run");

		// Condição de saída da Fuga
		if (_distanceToPlayer > 600 || _stats.CurrentHealth > FleeThreshold * 1.5f)
		{
			ChangeState(State.Patrol); // Retorna à patrulha
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	
	//!---------------------------------------------------------------------------------------------------------
	#region Signal Handlers (Detection)
	//!---------------------------------------------------------------------------------------------------------
	
	private void OnHealthChanged(int newHealth)
	{
		// A Lógica Fuzzy já lida com a transição para Fuga, mas a mudança de vida é importante.
		GD.Print($"Vida do inimigo alterada para: {newHealth}");
	}
	
	private void OnDetectionAreaBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.IsInGroup("player"))
		{
			_player = player;
			// O Fuzzy Logic assume o controle aqui, provavelmente mudando para Chase
		}
	}

	private void OnDetectionAreaBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
			// O Fuzzy Logic assume o controle aqui, provavelmente mudando para Patrol
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
