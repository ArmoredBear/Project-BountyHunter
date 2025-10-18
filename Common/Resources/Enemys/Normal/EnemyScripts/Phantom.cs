using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// CORREÇÃO: Nome da classe alterado de Enemy para Phantom
public partial class Phantom : CharacterBody2D
{
	// --- ENUM: Estados da Máquina de Estados ---
	public enum State
	{
		Patrol,      // Patrulha com NavMesh
		Chase,       // Persegue o jogador com NavMesh
		Attack,      // Golpe físico
		Flee         // Fuga com NavMesh
	}

	//!---------------------------------------------------------------------------------------------------------
	#region Exportable Properties and Node References
	//!---------------------------------------------------------------------------------------------------------
	
	// NÓS DA CENA (Conectados via [Export] no Inspector)
	[Export] public AnimatedSprite2D Animation_Sprite;
	[Export] public NavigationAgent2D Agent;
	[Export] public Area2D DetectionArea;
	[Export] public Area2D AttackArea;
	
	// CRÍTICO: EnemyStats (agora deve ser EnemyStats.cs)
	[Export] private EnemyStats _stats; 

	// PARÂMETROS DA IA
	[Export] public float Movement_Speed { get; set; } = 250f;
	[Export] public float AttackRange { get; set; } = 50f;
	[Export] public int FleeThreshold { get; set; } = 30; // 30% de vida para fugir
	[Export] public Node2D[] PatrolPoints { get; set; } // Array de Marker2D

	// REFERÊNCIAS INTERNAS
	private CharacterBody2D _player; 
	private State _currentState = State.Patrol;
	private int _currentPatrolPointIndex = 0;
	
	private float _distanceToPlayer = float.MaxValue;
	private readonly Dictionary<State, float> _statePriorities = new Dictionary<State, float>();

	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes (NavMesh Robustness)
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		// 1. VERIFICAÇÃO DE NULIDADE CRÍTICA
		if (_stats == null)
		{
			GD.PrintErr("ERRO FATAL: A propriedade '_stats' [Export] não foi conectada. Conecte o nó 'EnemyStats' no Inspector. Desativando IA.");
			SetPhysicsProcess(false);
			return; 
		}

		// 2. Conexão de Signals
		_stats.HealthChanged += OnHealthChanged;
		
		if (DetectionArea != null)
		{
			DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
			DetectionArea.BodyExited += OnDetectionAreaBodyExited;
		}

		// 3. CORREÇÃO NAVMESH: Inicia a configuração após o primeiro frame de física.
		Callable.From(ActorSetup).CallDeferred();
	}

	// CORREÇÃO NAVMESH: Espera a sincronização do servidor de navegação.
	private async void ActorSetup()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		
		// Inicia o estado de patrulha.
		ChangeState(State.Patrol); 
	}

	public override void _PhysicsProcess(double delta)
	{
		// Garante que a IA não tente rodar se o setup falhou criticamente
		if (_stats == null) return; 

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

		MoveAndSlide(); 
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
	
	private float GetLowHealthMembership()
	{
		if (_stats == null) return 0.0f;
		
		float healthFraction = (float)_stats.CurrentHealth / _stats.MaxHealth;
		
		if (healthFraction > 0.3f) return 0.0f; 
		
		return 1.0f - (healthFraction / 0.3f);
	}
	
	private float GetNearDistanceMembership()
	{
		if (DetectionArea == null) return 0.0f;
		
		float maxRelevantDistance = 600f; 
		
		try 
		{
			var collisionShape = DetectionArea.GetNode<CollisionShape2D>("CollisionShape2D");
			if (collisionShape != null && collisionShape.Shape != null)
			{
				// Tenta usar o tamanho da área de detecção
				maxRelevantDistance = collisionShape.Shape.GetRect().Size.X * DetectionArea.Scale.X;
			}
		}
		catch (Exception) {}
		
		if (_distanceToPlayer <= AttackRange) return 1.0f; 
		if (_distanceToPlayer >= maxRelevantDistance) return 0.0f; 
		
		return 1.0f - ((_distanceToPlayer - AttackRange) / (maxRelevantDistance - AttackRange));
	}
	
	private State DetermineNextStateFuzzy()
	{
		if (_stats == null) return State.Patrol; 
		
		float lowHealth = GetLowHealthMembership();
		float nearDistance = GetNearDistanceMembership();
		
		// --- REGRAS DE INFERÊNCIA FUZZY ---
		
		_statePriorities[State.Flee] = lowHealth * 1.5f; 
		
		float attackPriority = Mathf.Min(nearDistance, 1.0f - lowHealth);
		_statePriorities[State.Attack] = (AttackArea != null && AttackArea.HasOverlappingBodies()) ? attackPriority : 0f;
		
		_statePriorities[State.Chase] = (_player != null) ? Mathf.Min(1.0f - nearDistance, 1.0f - lowHealth) : 0f;
		
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
		
		// Prioriza Fuga se a vida estiver no limite
		if (_stats.CurrentHealth <= FleeThreshold && maxPriority < _statePriorities[State.Flee])
		{
			return State.Flee;
		}

		return bestState;
	}
	
	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region State Machine and Navigation Handlers
	//!---------------------------------------------------------------------------------------------------------
	
	private void ChangeState(State newState)
	{
		GD.Print($"Transição: {_currentState} -> {newState}");
		_currentState = newState;
		
		if (Animation_Sprite == null) return; 

		if (newState == State.Attack) Animation_Sprite.Play("Phantom_Attack");
		else if (newState == State.Chase) Animation_Sprite.Play("Phantom_Run");
		else if (newState == State.Patrol) Animation_Sprite.Play("Phantom_Run");
		else if (newState == State.Flee) Animation_Sprite.Play("Phantom_Run");
	}

	private void MoveToTarget(Vector2 targetPosition, float speedMultiplier = 1.0f)
	{
		if (Agent == null) 
		{
			Velocity = Vector2.Zero;
			return;
		}
		
		Agent.TargetPosition = targetPosition;
		
		if (Agent.IsNavigationFinished())
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 nextPathPosition = Agent.GetNextPathPosition();
		
		Vector2 direction = GlobalPosition.DirectionTo(nextPathPosition);
		Velocity = direction * Movement_Speed * speedMultiplier;
		
		if (Velocity.X != 0 && Animation_Sprite != null)
		{
			Animation_Sprite.FlipH = Velocity.X < 0;
		}
	}

	private void HandlePatrol(float delta)
	{
		if (PatrolPoints == null || PatrolPoints.Length == 0)
		{
			Velocity = Vector2.Zero;
			if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Idle");
			return;
		}
		
		Vector2 targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
		
		if (GlobalPosition.DistanceTo(targetPosition) < 20) 
		{
			_currentPatrolPointIndex = (_currentPatrolPointIndex + 1) % PatrolPoints.Length;
			targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
		}

		MoveToTarget(targetPosition, 0.5f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Run");
	}

	private void HandleChase(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Patrol);
			return;
		}
		
		MoveToTarget(_player.GlobalPosition, 1.2f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Run");
	}

	private void HandleAttack(float delta)
	{
		Velocity = Vector2.Zero;
		
		// O dano pode ser obtido via _stats.GetAttackDamage()
		GD.Print($"Phantom: Golpe físico! Causa {_stats.GetAttackDamage()} de dano.");
	}

	private void HandleFlee(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Patrol);
			return;
		}
		
		Vector2 fleeDirection = (GlobalPosition - _player.GlobalPosition).Normalized();
		Vector2 safeTarget = GlobalPosition + fleeDirection * 500f; 
		
		MoveToTarget(safeTarget, 1.5f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Run");

		if (_distanceToPlayer > 600 || (_stats != null && _stats.CurrentHealth > FleeThreshold * 1.5f))
		{
			ChangeState(State.Patrol); 
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region Signal Handlers
	//!---------------------------------------------------------------------------------------------------------
	
	private void OnHealthChanged(int newHealth)
	{
		// Usado pela Lógica Fuzzy para mudar para o estado Flee
		GD.Print($"Vida do Phantom alterada para: {newHealth}");
	}
	
	private void OnDetectionAreaBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.IsInGroup("player"))
		{
			_player = player;
		}
	}

	private void OnDetectionAreaBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
