using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Classe principal renomeada para Skeleton
public partial class Skeleton : CharacterBody2D
{
	// --- ENUM: Estados da Máquina de Estados ---
	public enum State
	{
		Patrol,      // Patrulha em um perímetro
		Chase,       // Persegue o jogador
		Attack,      // Ataca o jogador
		Flee,        // Fuga com vida baixa
		Retreat      // NOVO: Recuo tático quando isolado do grupo
	}

	//!---------------------------------------------------------------------------------------------------------
	#region Exportable Properties and Node References
	//!---------------------------------------------------------------------------------------------------------
	
	// NÓS DA CENA (Conectados via Inspector ou NodePath)
	[Export] public AnimatedSprite2D Animation_Sprite;
	[Export] public NavigationAgent2D Agent;
	[Export] public Area2D DetectionArea;
	[Export] public Area2D AttackArea;
	
	// CORREÇÃO: EnemyStats agora é exportado para conexão obrigatória no Inspector
	// Nota: Assumimos que a classe EnemyStats continua a ser usada, mas pode ser renomeada para SkeletonStats se necessário.
	[Export] private EnemyStats _stats; 

	// PARÂMETROS DA IA E MOVIMENTO
	[Export] public float Movement_Speed { get; set; } = 500f;
	[Export] public float AttackRange { get; set; } = 50f;
	[Export] public int FleeThreshold { get; set; } = 30;
	[Export] public Node2D[] PatrolPoints { get; set; }

	// REFERÊNCIAS INTERNAS
	private CharacterBody2D _player; 
	private State _currentState = State.Patrol;
	private int _currentPatrolPointIndex = 0;
	
	// LÓGICA FUZZY (Entradas)
	private float _distanceToPlayer = float.MaxValue;
	private readonly Dictionary<State, float> _statePriorities = new Dictionary<State, float>();

	// Variável para rastrear aliados próximos do mesmo tipo
	private int _nearbyAlliesCount = 0; 

	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes (Robustness Check)
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		// 1. VERIFICAÇÃO DE NULIDADE CRÍTICA (Se o usuário esqueceu de conectar os [Export])
		if (_stats == null)
		{
			GD.PrintErr("ERRO FATAL: A propriedade '_stats' [Export] não foi conectada no Inspector. Conecte o nó 'EnemyStats' para continuar. Desativando IA.");
			SetPhysicsProcess(false);
			return; 
		}

		// 2. Conexão de Signals (Só se _stats estiver OK)
		_stats.HealthChanged += OnHealthChanged;
		
		// Conexão de Signals em áreas (apenas se as áreas estiverem conectadas)
		if (DetectionArea != null)
		{
			// Os handlers foram modificados para rastrear tanto o player quanto outros inimigos.
			DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
			DetectionArea.BodyExited += OnDetectionAreaBodyExited;
		}

		// 3. Estado Inicial
		_currentState = State.Patrol;
		
		// Tenta iniciar a animação (apenas se a referência foi preenchida)
		if (Animation_Sprite != null)
		{
			// Nota: As animações 'Phantom_' devem ser ajustadas para 'Skeleton_' se o sprite for trocado.
			Animation_Sprite.Play("Phantom_Idle");
		}
		
		// 4. CORREÇÃO NAVMESH: Garante que o setup de navegação aconteça após o primeiro frame de física.
		Callable.From(ActorSetup).CallDeferred();
	}

	// CORREÇÃO NAVMESH: Espera a sincronização do servidor de navegação
	private async void ActorSetup()
	{
		// Espera o primeiro frame de física para garantir que o NavigationServer sincronize.
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		
		// O estado de patrulha pode ser iniciado agora.
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
			case State.Retreat: // Handler de Recuo Tático
				HandleRetreat((float)delta);
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
		// SEGURANÇA: Já verificamos em _Ready, mas garantimos aqui
		if (_stats == null) return 0.0f;
		
		float healthFraction = (float)_stats.CurrentHealth / _stats.MaxHealth;
		
		if (healthFraction > 0.3f) return 0.0f; 
		
		return 1.0f - (healthFraction / 0.3f);
	}
	
	private float GetNearDistanceMembership()
	{
		// SEGURANÇA: Se a área de detecção não estiver conectada
		if (DetectionArea == null) return 0.0f;
		
		float maxRelevantDistance = 600f; 
		
		try 
		{
			// O cálculo da geometria depende de um CollisionShape2D filho EXATO
			var collisionShape = DetectionArea.GetNode<CollisionShape2D>("CollisionShape2D");
			if (collisionShape != null && collisionShape.Shape != null)
			{
				// ATENÇÃO: Se o Shape for um RectangleShape2D, GetRect().Size.X funciona; para círculos, use Radius.
				// Usando GetRect().Size.X como proxy para o raio/tamanho da área.
				maxRelevantDistance = collisionShape.Shape.GetRect().Size.X * DetectionArea.Scale.X;
			}
		}
		catch (Exception)
		{
			// Falha silenciosa: usa o fallback
		}
		
		if (_distanceToPlayer <= AttackRange) return 1.0f; 
		if (_distanceToPlayer >= maxRelevantDistance) return 0.0f; 
		
		return 1.0f - ((_distanceToPlayer - AttackRange) / (maxRelevantDistance - AttackRange));
	}
	
	/// <summary>
	/// Calcula a pertinência do estado "Isolado" (Player presente, mas sem aliados próximos).
	/// </summary>
	private float GetIsolateMembership()
	{
		// Se não há jogador ou se há aliados, não está isolado.
		if (_player == null || _nearbyAlliesCount > 0) return 0.0f;
		
		// A sensação de isolamento é maior quanto mais perto do jogador o inimigo estiver.
		return GetNearDistanceMembership();
	}
	
	/// <summary>
	/// Calcula a pertinência do estado "Tem Reforços" (2 ou mais aliados).
	/// </summary>
	private float GetReinforcementsMembership()
	{
		// Alto reforço se tiver 2 ou mais aliados
		if (_nearbyAlliesCount >= 2) return 1.0f;
		
		// Reforço moderado se tiver 1 aliado
		if (_nearbyAlliesCount == 1) return 0.5f;
		
		return 0.0f;
	}

	private State DetermineNextStateFuzzy()
	{
		// SEGURANÇA: Já verificamos em _Ready, mas garantimos aqui
		if (_stats == null) return State.Patrol; 
		
		float lowHealth = GetLowHealthMembership();
		float nearDistance = GetNearDistanceMembership();
		
		// NOVOS INPUTS
		float isolate = GetIsolateMembership();
		float reinforcements = GetReinforcementsMembership();
		
		// --- REGRAS DE INFERÊNCIA FUZZY ---
		
		// 1. FUGA (Flee): Prioridade se a vida estiver criticamente baixa
		_statePriorities[State.Flee] = lowHealth * 1.5f; 
		
		// 2. RECÚO (Retreat): Prioridade se estiver isolado E a vida não estiver criticamente baixa
		float retreatPriority = isolate * (1.0f - lowHealth);
		_statePriorities[State.Retreat] = retreatPriority * 1.2f; // Multiplicador para dar mais peso ao Retreat
		
		// 3. ATAQUE (Attack): Prioridade se estiver perto E vida OK
		// Agressividade aumentada com reforços.
		float baseAttack = Mathf.Min(nearDistance, 1.0f - lowHealth);
		float attackPriority = baseAttack * (1.0f + reinforcements * 0.5f); // +50% de agressividade com reforços
		
		_statePriorities[State.Attack] = (AttackArea != null && AttackArea.HasOverlappingBodies()) ? attackPriority : 0f;
		
		// 4. PERSEGUIÇÃO (Chase): Prioridade se tiver jogador E não estiver perto E vida OK
		// Reduz a prioridade de Chase se isolado. Aumenta a prioridade de Chase com reforços.
		float baseChase = Mathf.Min(1.0f - nearDistance, 1.0f - lowHealth);
		float chasePriority = (_player != null) ? baseChase * (1.0f - isolate * 0.5f) : 0f; // Reduz em até 50% se isolado
		_statePriorities[State.Chase] = chasePriority * (1.0f + reinforcements * 0.5f); // Aumenta em até 50% com reforços
		
		// 5. PATRULHA (Patrol)
		_statePriorities[State.Patrol] = (_player == null || (_statePriorities[State.Chase] < 0.1f && _statePriorities[State.Retreat] < 0.1f)) ? 1.0f : 0.0f;
		
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
		
		// Prioriza Fuga se a vida estiver no limite (Segurança do original)
		if (_stats.CurrentHealth <= FleeThreshold && maxPriority < _statePriorities[State.Flee])
		{
			return State.Flee;
		}
		
		// Prioriza Recuo se for a melhor opção e não estiver em estado de combate extremo
		if (bestState != State.Flee && bestState != State.Attack && _statePriorities.TryGetValue(State.Retreat, out float currentRetreatPriority) && currentRetreatPriority > maxPriority)
		{
			return State.Retreat;
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
		
		// SEGURANÇA
		if (Animation_Sprite == null) return; 

		// NOVO: Adicionado tratamento para State.Retreat
		if (newState == State.Attack) Animation_Sprite.Play("Phantom_Attack");
		else if (newState == State.Chase) Animation_Sprite.Play("Phantom_Run");
		else if (newState == State.Patrol) Animation_Sprite.Play("Phantom_Run");
		else if (newState == State.Flee) Animation_Sprite.Play("Phantom_Run");
		else if (newState == State.Retreat) Animation_Sprite.Play("Phantom_Run");
	}

	private void MoveToTarget(Vector2 targetPosition, float speedMultiplier = 1.0f)
	{
		// SEGURANÇA
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
		Velocity = GlobalPosition.DirectionTo(nextPathPosition) * Movement_Speed * speedMultiplier;
		
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
		
		// Velocidade de perseguição aumentada para 1.2f (velocidade normal do Chase)
		MoveToTarget(_player.GlobalPosition, 1.2f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Run");
	}

	private void HandleAttack(float delta)
	{
		Velocity = Vector2.Zero;
		GD.Print("Inimigo: Golpe físico!");
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
		
		// Fuga com velocidade máxima (1.5f)
		MoveToTarget(safeTarget, 1.5f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Run");

		if (_distanceToPlayer > 600 || (_stats != null && _stats.CurrentHealth > FleeThreshold * 1.5f))
		{
			ChangeState(State.Patrol); 
		}
	}
	
	/// <summary>
	/// Handler para o estado de Recuo Tático (quando isolado).
	/// </summary>
	private void HandleRetreat(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Patrol);
			return;
		}
		
		// Move na direção oposta ao jogador.
		Vector2 retreatDirection = (GlobalPosition - _player.GlobalPosition).Normalized();
		
		// Alvo seguro a uma distância moderada (e.g., 300f).
		Vector2 safeTarget = GlobalPosition + retreatDirection * 300f; 
		
		// Recua com velocidade ligeiramente acima do normal (1.1f)
		MoveToTarget(safeTarget, 1.1f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Run");

		// Condição para sair do estado de Retreat:
		// Se o inimigo se afastou o suficiente (distância > 300) OU se um aliado apareceu.
		if (_distanceToPlayer > 300 || _nearbyAlliesCount > 0)
		{
			ChangeState(State.Patrol); 
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------

	
	//!---------------------------------------------------------------------------------------------------------
	#region Signal Handlers (Detection)
	//!---------------------------------------------------------------------------------------------------------
	
	private void OnHealthChanged(int newHealth)
	{
		GD.Print($"Vida do inimigo alterada para: {newHealth}");
	}
	
	// MODIFICADO: Agora rastreia Player E Aliados
	private void OnDetectionAreaBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.IsInGroup("player"))
		{
			_player = player;
		}
		// NOVO: Verifica se o corpo é outro inimigo do mesmo tipo (agora classe Skeleton)
		else if (body is Skeleton ally && ally != this)
		{
			_nearbyAlliesCount++;
			GD.Print($"Aliado detectado. Total: {_nearbyAlliesCount}");
		}
	}

	// MODIFICADO: Agora rastreia Player E Aliados
	private void OnDetectionAreaBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
		}
		// NOVO: Decrementa contagem de aliados (agora classe Skeleton)
		else if (body is Skeleton ally && ally != this)
		{
			_nearbyAlliesCount = Mathf.Max(0, _nearbyAlliesCount - 1);
			GD.Print($"Aliado saiu. Total: {_nearbyAlliesCount}");
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
