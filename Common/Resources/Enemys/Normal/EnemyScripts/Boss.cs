using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// RENOMEADO para Boss para refletir o papel
public partial class Boss : CharacterBody2D
{
	// --- ENUM: Estados da Máquina de Estados (Boss) ---
	public enum State
	{
		Patrol,      	// Patrulha em um perímetro (usado como Recuo inicial)
		Chase,       	// Persegue o jogador
		MeleeAttack,   	// NOVO: Ataque físico de perto
		RangedAttack,  	// NOVO: Ataque de projétil de longe
		Flee,         	// Fuga com vida baixa
		Retreat        	// NOVO: Recuo após ataque físico
	}

	//!---------------------------------------------------------------------------------------------------------
	#region Exportable Properties and Node References
	//!---------------------------------------------------------------------------------------------------------
	
	// NÓS DA CENA (Conectados via Inspector ou NodePath)
	[Export] public AnimatedSprite2D Animation_Sprite;
	[Export] public NavigationAgent2D Agent;
	[Export] public Area2D DetectionArea;
	[Export] public Area2D AttackArea;
	
	// CORREÇÃO: EnemyStats (Sugestão de renomear para BossStats, mas mantido EnemyStats para compatibilidade)
	[Export] private EnemyStats _stats; 

	// PARÂMETROS DA IA E MOVIMENTO
	[Export] public float Movement_Speed { get; set; } = 500f;
	[Export] public float AttackRange { get; set; } = 50f;
	[Export] public int FleeThreshold { get; set; } = 30;
	
	// PatrolPoints agora servem como pontos de Recuo/Estratégicos
	[Export] public Node2D[] PatrolPoints { get; set; }

	// REFERÊNCIAS INTERNAS
	private CharacterBody2D _player; 
	private State _currentState = State.Patrol;
	private int _currentPatrolPointIndex = 0;
	
	// LÓGICA FUZZY (Entradas)
	private float _distanceToPlayer = float.MaxValue;
	private readonly Dictionary<State, float> _statePriorities = new Dictionary<State, float>();

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
			DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
			DetectionArea.BodyExited += OnDetectionAreaBodyExited;
		}

		// 3. Estado Inicial
		_currentState = State.Patrol;
		
		// Tenta iniciar a animação (apenas se a referência foi preenchida)
		if (Animation_Sprite != null)
		{
			// ALTERAÇÃO: Usar nome de animação de Boss
			Animation_Sprite.Play("Boss_Idle"); 
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

		// 4. Execução do Estado Atual (Switch atualizado para novos estados)
		switch (_currentState)
		{
			case State.Patrol:
				HandlePatrol((float)delta);
				break;
			case State.Chase:
				HandleChase((float)delta);
				break;
			case State.MeleeAttack: // NOVO
				HandleMeleeAttack((float)delta);
				break;
			case State.RangedAttack: // NOVO
				HandleRangedAttack((float)delta);
				break;
			case State.Flee:
				HandleFlee((float)delta);
				break;
			case State.Retreat: // NOVO
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
	
	// Lógica Fuzzy revisada para priorizar RangedAttack em vida baixa
	private State DetermineNextStateFuzzy()
	{
		// SEGURANÇA
		if (_stats == null) return State.Patrol; 
		
		float lowHealth = GetLowHealthMembership();
		float nearDistance = GetNearDistanceMembership();
		// Distância Longe é o inverso de Distância Perto
		float farDistance = 1.0f - nearDistance; 
		
		// O estado Retreat é ativado explicitamente, sempre prioridade 0 na tomada de decisão.
		_statePriorities[State.Retreat] = 0f; 
		
		// --- REGRAS DE INFERÊNCIA FUZZY ---
		
		// 1. Fuga (Flee): (Vida Baixa)
		_statePriorities[State.Flee] = lowHealth * 1.5f; 
		
		// 2. Ataque Físico (MeleeAttack): (Perto E NÃO Vida Baixa)
		// O Boss evita o ataque de perto se a vida estiver muito baixa.
		float meleePriority = Mathf.Min(nearDistance, 1.0f - lowHealth);
		// Só ataca se estiver na área de ataque
		_statePriorities[State.MeleeAttack] = (AttackArea != null && AttackArea.HasOverlappingBodies()) ? meleePriority : 0f;
		
		// 3. Ataque de Projétil (RangedAttack): (Longe OU Vida Baixa)
		// A prioridade aumenta com a distância e/ou com a vida baixa.
		float lowHealthRangedBoost = lowHealth * 0.5f; // Bônus para ataque à distância quando com pouca vida
		float rangedPriority = Mathf.Max(farDistance, lowHealth) + lowHealthRangedBoost; // MAX implementa a lógica "OU"
		// Garante que a prioridade não exceda 1.5f (considerando o boost)
		_statePriorities[State.RangedAttack] = Mathf.Min(rangedPriority, 1.5f); 
		
		// 4. Perseguição (Chase): (NÃO Perto E NÃO Vida Baixa)
		_statePriorities[State.Chase] = (_player != null) ? Mathf.Min(farDistance, 1.0f - lowHealth) : 0f;
		
		// 5. Patrulha (Patrol): (Jogador Ausente OU Prioridade de Chase baixa)
		_statePriorities[State.Patrol] = (_player == null || _statePriorities[State.Chase] < 0.1f) ? 1.0f : 0.0f;
		
		// --- DEFUZZIFICAÇÃO (Escolha do Estado com Maior Prioridade) ---
		State bestState = State.Patrol;
		float maxPriority = 0f;

		foreach (var entry in _statePriorities)
		{
			// Prioriza Flee sobre qualquer coisa se a condição for limite (Será checado abaixo)
			if (entry.Key == State.Flee) continue; 
			
			if (entry.Value > maxPriority)
			{
				maxPriority = entry.Value;
				bestState = entry.Key;
			}
		}
		
		// Prioriza Fuga se a vida estiver no limite (a regra mais rígida)
		if (_stats.CurrentHealth <= FleeThreshold && _statePriorities.ContainsKey(State.Flee) && maxPriority < _statePriorities[State.Flee])
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
		
		// SEGURANÇA
		if (Animation_Sprite == null) return; 

		// ATUALIZAÇÃO: Animações específicas para Boss e novos estados
		if (newState == State.MeleeAttack) 
		{
			Animation_Sprite.Play("Boss_Melee");
		}
		else if (newState == State.RangedAttack) 
		{
			Animation_Sprite.Play("Boss_Ranged");
		}
		else if (newState == State.Chase || newState == State.Patrol || newState == State.Flee || newState == State.Retreat)
		{
			Animation_Sprite.Play("Boss_Run");
		}
		else // Default
		{
			Animation_Sprite.Play("Boss_Idle");
		}
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
			if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Idle");
			return;
		}
		
		Vector2 targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
		
		if (GlobalPosition.DistanceTo(targetPosition) < 20) 
		{
			_currentPatrolPointIndex = (_currentPatrolPointIndex + 1) % PatrolPoints.Length;
			targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
		}

		MoveToTarget(targetPosition, 0.5f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Run");
	}

	private void HandleChase(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Patrol);
			return;
		}
		
		MoveToTarget(_player.GlobalPosition, 1.2f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Run");
	}

	// NOVO: Handler para ataque físico
	private void HandleMeleeAttack(float delta)
	{
		Velocity = Vector2.Zero;
		GD.Print("Boss: Golpe físico realizado!");
		
		// * Implemente a lógica de dano aqui (ex: AttackArea.GetOverlappingBodies() e causar dano)
		
		// Após o ataque, transiciona para o Recuo (Retreat)
		// CallDeferred garante que a transição de estado não interrompa o estado atual antes do fim do frame.
		Callable.From(() => ChangeState(State.Retreat)).CallDeferred(); 
	}

	// NOVO: Handler para ataque de projétil
	private void HandleRangedAttack(float delta)
	{
		Velocity = Vector2.Zero;
		GD.Print("Boss: Lançamento de Projétil realizado!");
		
		// * Implemente a lógica de spawn de projétil aqui
		
		// O Boss fica parado enquanto ataca. A Lógica Fuzzy decide o próximo estado.
	}

	// NOVO: Handler para recuo estratégico
	private void HandleRetreat(float delta)
	{
		if (PatrolPoints == null || PatrolPoints.Length == 0)
		{
			// Se não houver pontos de recuo, volta para Chase
			ChangeState(State.Chase); 
			return;
		}
		
		// O Boss recua para o ponto atual da patrulha (que são os seus pontos de recuo)
		Vector2 targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
		
		// Move-se para o ponto de recuo
		MoveToTarget(targetPosition, 0.8f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Run");

		// Verifica se o Boss chegou ao ponto de recuo (distância de 50 unidades)
		if (GlobalPosition.DistanceTo(targetPosition) < 50) 
		{
			// Chegou ao ponto seguro. Agora retoma a agressividade (Chase)
			ChangeState(State.Chase); 
			
			// Opcional: Avança para o próximo ponto de patrulha para o próximo recuo.
			_currentPatrolPointIndex = (_currentPatrolPointIndex + 1) % PatrolPoints.Length;
		}
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
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Run");

		if (_distanceToPlayer > 600 || (_stats != null && _stats.CurrentHealth > FleeThreshold * 1.5f))
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
		GD.Print($"Vida do Boss alterada para: {newHealth}");
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
