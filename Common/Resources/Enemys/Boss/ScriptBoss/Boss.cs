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
		Idle,      	        // OCIOSO/STANDBY: Boss fica parado e espera o jogador
		Chase,       	    // Persegue o jogador
		MeleeAttack,   	    // Ataque físico de perto
		RangedAttack,  	    // Ataque de projétil de longe
		Flee,         	    // Fuga com vida baixa
		Retreat,        	// Recuo após ataque físico
		LongRangeDash       // Ataque de longo alcance com movimento (Dash)
	}

	//!---------------------------------------------------------------------------------------------------------
	#region Exportable Properties and Node References
	//!---------------------------------------------------------------------------------------------------------
	
	// NÓS DA CENA (Conectados via Inspector ou NodePath)
	[Export] public AnimatedSprite2D Animation_Sprite;
	[Export] public NavigationAgent2D Agent;
	[Export] public Area2D DetectionArea;
	[Export] public Area2D AttackArea;
	
	// CORREÇÃO: EnemyStats
	[Export] private EnemyStats _stats; 

	// PARÂMETROS DA IA E MOVIMENTO
	[Export] public float Movement_Speed { get; set; } = 500f;
	[Export] public float AttackRange { get; set; } = 50f;
	[Export] public int FleeThreshold { get; set; } = 30;
	
	// Cooldown para limitar a frequência do LongRangeDash (Ajustável no Inspector)
	[Export] public float LongRangeDashCooldown { get; set; } = 5.0f; // 5 segundos de recarga
	
	// PatrolPoints agora servem EXCLUSIVAMENTE como PONTOS FIXOS/ESTRATÉGICOS DE RECUO.
	// O nome 'PatrolPoints' é mantido para compatibilidade com o Inspector.
	[Export] public Node2D[] PatrolPoints { get; set; }
	
	// NOVO: Intervalo para gravação da posição do jogador
	[Export] public float PlayerPositionUpdateInterval { get; set; } = 1.0f; // 1 segundo

	// REFERÊNCIAS INTERNAS
	private CharacterBody2D _player; 
	private State _currentState = State.Idle; 
    // ALTERAÇÃO: _currentRetreatPointIndex agora armazena o índice do ponto de recuo alvo atual.
	private int _currentRetreatPointIndex = 0; 
	private Vector2 _lastPlayerPosition = Vector2.Zero;
	private float _timeSinceLastPositionUpdate = 0f;
	private float _timeUntilDashReady = 0f; // Contador de cooldown
	
	// LÓGICA FUZZY (Entradas)
	private float _distanceToPlayer = float.MaxValue;
	private readonly Dictionary<State, float> _statePriorities = new Dictionary<State, float>();

    // GERADOR DE NÚMEROS ALEATÓRIOS
    private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

	// Pontos de referencia para comportamento do BOSS
	[Export] public Node2D[] Direction_Points { get; set; }

	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region Initialization and Processes
	//!---------------------------------------------------------------------------------------------------------

	public override void _Ready()
	{
		// 1. VERIFICAÇÃO DE NULIDADE CRÍTICA
		if (_stats == null)
		{
			GD.PrintErr("ERRO FATAL: A propriedade '_stats' [Export] não foi conectada no Inspector. Conecte o nó 'EnemyStats' para continuar. Desativando IA.");
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

		// 3. Estado Inicial
		_currentState = State.Idle;
		
		if (Animation_Sprite != null)
		{
			Animation_Sprite.Play("Boss_Idle"); 
		}

		// 4. CORREÇÃO NAVMESH
		Callable.From(ActorSetup).CallDeferred();
		
        // Inicializa o gerador de números aleatórios
        _rng.Randomize();

		if(Direction_Points == null)
		{
			Direction_Points = new Node2D[4];
		}
        
        // Inicializa o primeiro ponto de recuo aleatoriamente
        if (PatrolPoints != null && PatrolPoints.Length > 0)
        {
            _currentRetreatPointIndex = _rng.RandiRange(0, PatrolPoints.Length - 1);
        }
	}

	private async void ActorSetup()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		ChangeState(State.Idle); 
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		
		// Grava a última posição do jogador a cada X segundos
		if (_player != null)
		{
			_timeSinceLastPositionUpdate += (float)delta;
			
			if (_timeSinceLastPositionUpdate >= PlayerPositionUpdateInterval)
			{
				_lastPlayerPosition = _player.GlobalPosition;
				_timeSinceLastPositionUpdate = 0f;
				// GD.Print($"Posição do Jogador gravada: {_lastPlayerPosition}");
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_stats == null) return; 
		
		// Atualiza o contador de cooldown do LongRangeDash
		if (_timeUntilDashReady > 0)
		{
			_timeUntilDashReady -= (float)delta;
		}

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
			case State.Idle: 
				HandleIdle((float)delta); 
				break;
			case State.Chase:
				HandleChase((float)delta);
				break;
			case State.MeleeAttack: 
				HandleMeleeAttack((float)delta);
				break;
			case State.RangedAttack: 
				HandleRangedAttack((float)delta);
				break;
			case State.Flee:
				HandleFlee((float)delta);
				break;
			case State.Retreat: 
				HandleRetreat((float)delta);
				break;
			case State.LongRangeDash: 
				HandleLongRangeDash((float)delta);
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
				maxRelevantDistance = collisionShape.Shape.GetRect().Size.X * DetectionArea.Scale.X;
			}
		}
		catch (Exception)
		{
			// Fallback: usa 600f
		}
		
		if (_distanceToPlayer <= AttackRange) return 1.0f; 
		if (_distanceToPlayer >= maxRelevantDistance) return 0.0f; 
		
		return 1.0f - ((_distanceToPlayer - AttackRange) / (maxRelevantDistance - AttackRange));
	}
	
	private State DetermineNextStateFuzzy()
	{
		if (_stats == null) return State.Idle; 
		
		float lowHealth = GetLowHealthMembership();
		float nearDistance = GetNearDistanceMembership();
		float farDistance = 1.0f - nearDistance; 
		
		_statePriorities.Clear();
		
		_statePriorities[State.Retreat] = 0f; 
		
		// --- REGRAS DE INFERÊNCIA FUZZY ---
		
		// 1. Fuga (Flee): (Vida Baixa)
		_statePriorities[State.Flee] = lowHealth * 1.5f; 
		
		// 2. Ataque Físico (MeleeAttack): (Perto E NÃO Vida Baixa)
		float meleePriority = Mathf.Min(nearDistance, 1.0f - lowHealth);
		_statePriorities[State.MeleeAttack] = (AttackArea != null && AttackArea.HasOverlappingBodies()) ? meleePriority : 0f;
		
		// 3. LongRangeDash: (Longe E NÃO Vida Baixa E Jogador Presente)
		float longRangeDashPriority = (_player != null && _lastPlayerPosition != Vector2.Zero) ? Mathf.Min(farDistance, 1.0f - lowHealth) : 0f;
		
		// Verifica o cooldown! Prioridade zero se o cooldown não zerou.
		if (_timeUntilDashReady > 0)
		{
			_statePriorities[State.LongRangeDash] = 0f;
		}
		else
		{
			_statePriorities[State.LongRangeDash] = longRangeDashPriority * farDistance;
		}
		
		// 4. Ataque de Projétil (RangedAttack): (Longe OU Vida Baixa)
		float lowHealthRangedBoost = lowHealth * 0.5f; 
		float rangedPriority = Mathf.Max(farDistance * 0.5f, lowHealth) + lowHealthRangedBoost; 
		_statePriorities[State.RangedAttack] = Mathf.Min(rangedPriority, 1.5f); 
		
		// 5. Perseguição (Chase): (NÃO Perto E NÃO Vida Baixa)
		_statePriorities[State.Chase] = (_player != null) ? Mathf.Min(farDistance, 1.0f - lowHealth) : 0f;
		
		// 6. IDLE (IDLE): (Jogador Ausente)
		_statePriorities[State.Idle] = (_player == null || _statePriorities[State.Chase] < 0.1f) ? 1.0f : 0.0f;
		
		// --- DEFUZZIFICAÇÃO (Escolha do Estado com Maior Prioridade) ---
		State bestState = State.Idle; 
		float maxPriority = 0f;

		foreach (var entry in _statePriorities)
		{
			// O estado Idle só é escolhido se o jogador não estiver presente.
			if (entry.Key == State.Flee || entry.Key == State.Idle) continue; 
			
			if (entry.Value > maxPriority)
			{
				maxPriority = entry.Value;
				bestState = entry.Key;
			}
		}
		
		// Se não há prioridade alta (maxPriority = 0), e o jogador não está presente, ele fica em Idle.
		if (_player == null || maxPriority < 0.1f)
		{
			return State.Idle;
		}
		
		// Prioriza Fuga se a vida estiver no limite
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

		if (Animation_Sprite == null) return;
		
		// Seta o cooldown assim que o Boss entra no estado de Dash
		if (newState == State.LongRangeDash)
		{
			_timeUntilDashReady = LongRangeDashCooldown;
		}
        
        // NOVO: Seleciona o próximo ponto de recuo aleatoriamente ao entrar no estado Retreat
        if (newState == State.Retreat && PatrolPoints != null && PatrolPoints.Length > 0)
        {
            // Garante que o ponto aleatório não seja o ponto onde ele já está (se possível)
            int newIndex;
            do
            {
                newIndex = _rng.RandiRange(0, PatrolPoints.Length - 1);
            } while (PatrolPoints.Length > 1 && newIndex == _currentRetreatPointIndex);
            
            _currentRetreatPointIndex = newIndex;
            GD.Print($"Boss Recuando para o ponto fixo: {_currentRetreatPointIndex}");
        }


		// ATUALIZAÇÃO: Animações específicas para Boss e novos estados
		if (newState == State.MeleeAttack)
		{
			Animation_Sprite.Play("Boss_Melee");
		}
		else if (newState == State.RangedAttack)
		{
			Animation_Sprite.Play("Boss_Ranged");
		}
		else if (newState == State.LongRangeDash)
		{
			Animation_Sprite.Play("Boss_Run"); 
		}
		else if (newState == State.Chase || newState == State.Flee || newState == State.Retreat)
		{
			Animation_Sprite.Play("Boss_Run");
		}
		else // State.Idle
		{
			Animation_Sprite.Play("Boss_Idle");
		}
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
		Velocity = GlobalPosition.DirectionTo(nextPathPosition) * Movement_Speed * speedMultiplier;
		
		if (Velocity.X != 0 && Animation_Sprite != null)
		{
			Animation_Sprite.FlipH = Velocity.X < 0;
		}
	}

	private void HandleIdle(float delta)
	{
		Velocity = Vector2.Zero;
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Idle");
	}

	private void HandleChase(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Idle); 
			return;
		}
		
		MoveToTarget(_player.GlobalPosition, 1.2f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Run");
	}

	private void HandleMeleeAttack(float delta)
	{
		Velocity = Vector2.Zero;
		GD.Print("Boss: Golpe físico realizado!");
		
		// * Lógica de dano
		
		Callable.From(() => ChangeState(State.Retreat)).CallDeferred(); 
	}

	private void HandleRangedAttack(float delta)
	{
		Velocity = Vector2.Zero;
		GD.Print("Boss: Lançamento de Projétil realizado!");
		
		// * Lógica de spawn de projétil
	}
	
	private void HandleLongRangeDash(float delta)
	{
		if (_player == null || _lastPlayerPosition == Vector2.Zero)
		{
			ChangeState(State.Chase);
			return;
		}

		Vector2 target = _lastPlayerPosition;
		Vector2 direction = GlobalPosition.DirectionTo(target);
		
		Velocity = direction * Movement_Speed * 3.0f; 
		
		if (Animation_Sprite != null) 
		{
			Animation_Sprite.Play("Boss_Run"); 
			Animation_Sprite.FlipH = Velocity.X < 0;
		}

		if (GlobalPosition.DistanceTo(target) < 50) 
		{
			GD.Print("Boss: Dash de Longo Alcance Concluído no alvo!");
			ChangeState(State.Retreat); 
			_lastPlayerPosition = Vector2.Zero; 
		}
	}


	private void HandleRetreat(float delta)
	{
		// Garante que há pontos de recuo fixos configurados
		if (PatrolPoints == null || PatrolPoints.Length == 0)
		{
			ChangeState(State.Chase); 
			return;
		}
		
		// Pega a posição do ponto de recuo alvo atual (selecionado aleatoriamente em ChangeState)
		Vector2 targetPosition = PatrolPoints[_currentRetreatPointIndex].GlobalPosition;
		
		// Move-se para o ponto fixo usando navegação
		MoveToTarget(targetPosition, 0.8f); 
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Run");

		// Verifica se o Boss chegou ao ponto fixo
		if (GlobalPosition.DistanceTo(targetPosition) < 50) 
		{
			// Chegou ao ponto seguro, retoma a perseguição
			ChangeState(State.Chase); 
			
            // NOTA: A seleção do PRÓXIMO ponto aleatório agora acontece *dentro* do ChangeState
            // quando o Boss TRANSICIONA para Retreat. Não é necessário avançar o índice aqui.
		}
	}

	private void HandleFlee(float delta)
	{
		if (_player == null)
		{
			ChangeState(State.Idle); 
			return;
		}

		Vector2 fleeDirection = (GlobalPosition - _player.GlobalPosition).Normalized();
		Vector2 safeTarget = GlobalPosition + fleeDirection * 500f;

		MoveToTarget(safeTarget, 1.5f);
		if (Animation_Sprite != null) Animation_Sprite.Play("Boss_Run");

		if (_distanceToPlayer > 600 || (_stats != null && _stats.CurrentHealth > FleeThreshold * 1.5f))
		{
			ChangeState(State.Idle); 
		}
	}
	
	public void Calculate_Distance(Vector2 _last_player_position)
	{
		// ... (lógica original)
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
			_lastPlayerPosition = Vector2.Zero; // Reseta a posição do alvo
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}