using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   BOSS
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Boss enemy controller implementing a fuzzy-logic driven state machine (Patrol, Chase, Attack, Flee).
	**  2 - Manages navigation via NavigationAgent2D, combat hitboxes, and player damage signals.
	**  3 - Connects stats and signals in _Ready with null-safety checks.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class Boss : CharacterBody2D
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

	// NÓS DA CENA (Conectados via Inspector ou NodePath)
	[Export] public AnimatedSprite2D Animation_Sprite;
	[Export] public AnimationPlayer Animation_Player;
	[Export] public NavigationAgent2D Agent;
	[Export] public Area2D DetectionArea;
	[Export] public Area2D AttackArea;
	[Export] public Area2D AttackHitbox;
	[Export] public CollisionShape2D AttackHitboxShape;
	[Export] public AnimationPlayer HitFlash_Animation;

	// CORREÇÃO: EnemyStats agora é exportado para conexão obrigatória no Inspector
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

	private const float RepathDistance = 32f;
	private const float RepathInterval = 0.2f;
	private const double NearDecisionInterval = 1.0 / 15.0;
	private const double FarDecisionInterval = 1.0 / 5.0;

	private Vector2 _lastRequestedTarget;
	private bool _hasRequestedTarget = false;
	private double _repathTimer = 0.0;
	private double _decisionTimer = 0.0;
	private bool _isPlayerInAttackRange = false;
	private bool _isFarFromPlayer = false;

	// LÓGICA FUZZY (Entradas)
	private float _distanceToPlayer = float.MaxValue;
	private float _maxRelevantDistance = 600f;

	private double _attackCooldown = 0;
	private bool _isAttacking = false;

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
		CacheDetectionRadius();

		// Conexão de Signals em áreas (apenas se as áreas estiverem conectadas)
		if (DetectionArea != null)
		{
			DetectionArea.BodyEntered += OnDetectionAreaBodyEntered;
			DetectionArea.BodyExited += OnDetectionAreaBodyExited;
		}

		if (AttackArea != null)
		{
			AttackArea.BodyEntered += OnAttackAreaBodyEntered;
			AttackArea.BodyExited += OnAttackAreaBodyExited;
		}

		if (AttackHitbox != null)
		{
			AttackHitbox.BodyEntered += On_AttackHitbox_AreaEntered;
		}

		// 3. Estado Inicial
		_currentState = State.Patrol;
		_decisionTimer = GD.Randf() * NearDecisionInterval;

		// Tenta iniciar a animação (apenas se a referência foi preenchida)
		PlaySpriteAnimation("Boss_Idle");

		// 4. CORREÇÃO NAVMESH: Garante que o setup de navegação aconteça após o primeiro frame de física.
		Callable.From(ActorSetup).CallDeferred();

		if (Animation_Player != null)
		{
			Animation_Player.AnimationFinished += OnAnimationPlayerFinished;
		}

	}

	// CORREÇÃO NAVMESH: Espera a sincronização do servidor de navegação
	private async void ActorSetup()
	{
		// Espera o primeiro frame de física para garantir que o NavigationServer sincronize.
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		// O estado de patrulha pode ser iniciado agora.
		ChangeState(State.Patrol);
		_repathTimer = GD.Randf() * RepathInterval;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Garante que a IA não tente rodar se o setup falhou criticamente
		if (_stats == null) return;

		if (_repathTimer > 0.0)
		{
			_repathTimer -= delta;
		}

		if (_decisionTimer > 0.0)
		{
			_decisionTimer -= delta;
		}
		else
		{
			// 1. Atualiza Entradas Fuzzy
			UpdateFuzzyInputs();

			bool isFar = _distanceToPlayer > _maxRelevantDistance;
			if (isFar != _isFarFromPlayer)
			{
				_isFarFromPlayer = isFar;
			}

			if (_isFarFromPlayer)
			{
				_decisionTimer = FarDecisionInterval;
			}
			else
			{
				_decisionTimer = NearDecisionInterval;
			}

			// 2. Tomada de Decisão (Fuzzy Logic)
			State nextState = DetermineNextStateFuzzy();

			// 3. Transição de Estado
			if (nextState != _currentState)
			{
				ChangeState(nextState);
			}
		}

		// 4. Execução do Estado Atual
		switch (_currentState)
		{
			case State.Patrol:
				HandlePatrol();
				break;
			case State.Chase:
				HandleChase();
				break;
			case State.Attack:
				HandleAttack(delta);
				break;
			case State.Flee:
				HandleFlee();
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

	private void CacheDetectionRadius()
	{
		if (DetectionArea == null) return;

		CollisionShape2D collisionShape = DetectionArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		if (collisionShape == null || collisionShape.Shape == null) return;

		_maxRelevantDistance = collisionShape.Shape.GetRect().Size.X * DetectionArea.Scale.X;
	}

	private float GetNearDistanceMembership()
	{
		// SEGURANÇA: Se a área de detecção não estiver conectada
		if (DetectionArea == null) return 0.0f;

		if (_distanceToPlayer <= AttackRange) return 1.0f;
		if (_distanceToPlayer >= _maxRelevantDistance) return 0.0f;

		return 1.0f - ((_distanceToPlayer - AttackRange) / (_maxRelevantDistance - AttackRange));
	}

	private State DetermineNextStateFuzzy()
	{
		// Safety check: Ensure the stats component is available
		if (_stats == null)
		{
			return State.Patrol;
		}

		if (_isAttacking)
		{
			return State.Attack;
		}

		// --- FUZZY INPUTS (Fuzzification) ---

		// Calculate membership values for fuzzy sets
		float lowHealth = GetLowHealthMembership();
		float nearDistance = GetNearDistanceMembership();

		// --- INFERENCE RULES (Calculate State Priorities) ---

		// Flee Priority: Higher when health is low. Uses a constant weight (1.5f).
		float fleePriority = lowHealth * 1.5f;

		// Attack Priority: Min(Near, Not Low Health). Requires an existing target in the area.
		float attackPriority = 0f;
		float attackInference = Mathf.Min(nearDistance, 1.0f - lowHealth);

		if (_isPlayerInAttackRange)
		{
			attackPriority = attackInference;
		}

		// Chase Priority: Min(Not Near, Not Low Health). Requires the player to be present.
		float chasePriority = 0f;

		if (_player != null)
		{
			chasePriority = Mathf.Min(1.0f - nearDistance, 1.0f - lowHealth);
		}

		// Patrol Priority: High if no player is present OR if the Chase priority is negligible.
		float patrolPriority = 0.0f;

		if (_player == null || chasePriority < 0.1f)
		{
			patrolPriority = 1.0f;
		}

		// --- DEFUZZIFICATION (Select State with Highest Priority) ---
		State bestState = State.Patrol;
		float maxPriority = patrolPriority;

		if (fleePriority > maxPriority)
		{
			maxPriority = fleePriority;
			bestState = State.Flee;
		}

		if (attackPriority > maxPriority)
		{
			maxPriority = attackPriority;
			bestState = State.Attack;
		}

		if (chasePriority > maxPriority)
		{
			maxPriority = chasePriority;
			bestState = State.Chase;
		}

		// Hard Coded Override/Exception: Force Flee if health is critically low,
		// even if another state had a slightly higher calculated priority.
		if (_stats.CurrentHealth <= FleeThreshold && maxPriority < fleePriority)
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
		GD.Print("SCRIPT - BOSS - : Transition: " + _currentState + " -> " + newState);
		_currentState = newState;
		_hasRequestedTarget = false;
		_repathTimer = 0.0;

		if (newState == State.Attack)
		{
			_isAttacking = true;
			GD.Print("Entered State Attack");
			Velocity = Vector2.Zero;

			if (Animation_Player != null)
			{
				GD.Print("CHANGE STATE -- Playing Boss_Attack animation");
				Animation_Player.Play("Boss_Attack");
			}
		}
		else if (newState == State.Chase)
		{
			PlaySpriteAnimation("Boss_Pursuit");
		}
		else if (newState == State.Patrol)
		{
			PlaySpriteAnimation("Boss_Pursuit");
		}
		else if (newState == State.Flee)
		{
			PlaySpriteAnimation("Boss_Pursuit");
		}
	}

	private void PlaySpriteAnimation(StringName animationName)
	{
		if (Animation_Sprite == null) return;

		if (Animation_Sprite.Animation == animationName && Animation_Sprite.IsPlaying()) return;

		Animation_Sprite.Play(animationName);
	}

	private void MoveToTarget(Vector2 targetPosition, float speedMultiplier = 1.0f)
	{
		// SEGURANÇA
		if (Agent == null)
		{
			Velocity = Vector2.Zero;
			return;
		}

		bool targetMoved = !_hasRequestedTarget ||
			_lastRequestedTarget.DistanceSquaredTo(targetPosition) > RepathDistance * RepathDistance;
		bool targetDrifted = _hasRequestedTarget && _lastRequestedTarget != targetPosition;

		if (targetMoved || (_repathTimer <= 0.0 && targetDrifted))
		{
			_lastRequestedTarget = targetPosition;
			_hasRequestedTarget = true;
			_repathTimer = RepathInterval;
			Agent.TargetPosition = targetPosition;
		}

		if (Agent.IsNavigationFinished())
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 nextPathPosition = Agent.GetNextPathPosition();
		Velocity = GlobalPosition.DirectionTo(nextPathPosition) * Movement_Speed * speedMultiplier;

		if (Velocity.X != 0 && Animation_Sprite != null)
		{
			Animation_Sprite.FlipH = Velocity.X > 0;
		}
	}

	private void HandlePatrol()
	{
		if (PatrolPoints == null || PatrolPoints.Length == 0)
		{
			Velocity = Vector2.Zero;
			PlaySpriteAnimation("Boss_Idle");
			return;
		}

		Vector2 targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;

		if (GlobalPosition.DistanceTo(targetPosition) < 20)
		{
			_currentPatrolPointIndex = (_currentPatrolPointIndex + 1) % PatrolPoints.Length;
			targetPosition = PatrolPoints[_currentPatrolPointIndex].GlobalPosition;
			_hasRequestedTarget = false;
		}

		MoveToTarget(targetPosition, 0.5f);
		PlaySpriteAnimation("Boss_Pursuit");
	}

	private void HandleChase()
	{
		if (_player == null)
		{
			GD.Print("Player is null!!");
			ChangeState(State.Patrol);
			return;
		}

		MoveToTarget(_player.GlobalPosition, 1.2f);
		PlaySpriteAnimation("Boss_Pursuit");
	}

	private void HandleAttack(double delta)
	{
		_attackCooldown -= delta;

		if (_attackCooldown <= 0 && _isAttacking == false && _isPlayerInAttackRange)
		{
			_isAttacking = true;
			Velocity = Vector2.Zero;

			if (Animation_Player != null)
			{
				Animation_Player.Play("Boss_Attack");
			}

			_attackCooldown = 1.0; // 1 second between attack starts
		}
	}

	private void HandleFlee()
	{
		if (_player == null)
		{
			ChangeState(State.Patrol);
			return;
		}

		Vector2 fleeDirection = (GlobalPosition - _player.GlobalPosition).Normalized();
		Vector2 safeTarget = GlobalPosition + fleeDirection * 500f;

		MoveToTarget(safeTarget, 1.5f);
		PlaySpriteAnimation("Boss_Pursuit");

		if (_distanceToPlayer > 600 || (_stats != null && _stats.CurrentHealth > FleeThreshold * 1.5f))
		{
			ChangeState(State.Patrol);
		}
	}

	public void EnableHitbox()
	{
		if (AttackHitboxShape != null)
		{
			AttackHitboxShape.Disabled = false;
			GD.Print("Hitbox enabled at impact frame!");
		}
	}

	public void DisableHitbox()
	{
		if (AttackHitboxShape != null)
		{
			AttackHitboxShape.Disabled = true;
			GD.Print("Hitbox disabled after impact window.");
		}
	}

	public void EndAttack()
	{
		_isAttacking = false;
		_decisionTimer = 0.0;
		GD.Print("EndAttack called");
		//DisableHitbox(); // safety reset
	}


	#endregion
	//!---------------------------------------------------------------------------------------------------------


	//!---------------------------------------------------------------------------------------------------------
	#region Signal Handlers (Detection)
	//!---------------------------------------------------------------------------------------------------------

	public void OnHealthChanged(int newHealth)
	{
		GD.Print($"SCRIPT - BOSS - : Vida do inimigo alterada para: {newHealth}");
	}

	public void OnDetectionAreaBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.IsInGroup("player"))
		{
			_player = player;
			_decisionTimer = 0.0;
			_repathTimer = 0.0;
		}
	}

	public void OnDetectionAreaBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
			_decisionTimer = 0.0;
		}
	}

	private void OnAttackAreaBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.IsInGroup("player"))
		{
			_isPlayerInAttackRange = true;
		}
	}

	private void OnAttackAreaBodyExited(Node2D body)
	{
		if (body is CharacterBody2D player && player.IsInGroup("player"))
		{
			_isPlayerInAttackRange = false;
		}
	}

	public void On_Boss_Damage_Collider_Area_Entered(Node2D _area)
	{
		if (_area.IsInGroup("player_attack"))
		{
			GD.Print("Inimigo atacado" + "Nome do collider: " + _area.Name);
			HitFlash_Animation.Play("Hit_Flash");

			if (_stats.TakeDamage(10) == false)
			{
				GetTree().QueueDelete(this);
			}

			else
			{
				GD.Print("Inimigo toma " + 20 + " de dano");
			}
		}

	}

	public void On_AttackHitbox_AreaEntered(Node2D _area)
	{
		if (_area.IsInGroup("player"))
		{
			GD.Print("Boss hit the player!");
			Player_Data_Autoload.Instance.Apply_Damage(10);

			// Apply damage here
		}
	}

	private void OnAnimationPlayerFinished(StringName animName)
	{
		if (animName == "Boss_Attack")
		{
			EndAttack();
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
