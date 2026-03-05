using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Phantom : CharacterBody2D
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
	[Export] public AudioStreamPlayer2D HitSound;

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

	// LÓGICA FUZZY (Entradas)
	private float _distanceToPlayer = float.MaxValue;
	private readonly Dictionary<State, float> _statePriorities = new Dictionary<State, float>();

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
			Animation_Sprite.Play("Phantom_Idle");
		}

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
		_statePriorities[State.Flee] = lowHealth * 1.5f;

		// Attack Priority: Min(Near, Not Low Health). Requires an existing target in the area.
		float attackInference = Mathf.Min(nearDistance, 1.0f - lowHealth);

		// REPLACED TERNARY:
		if (AttackArea != null && AttackArea.HasOverlappingBodies())
		{
			_statePriorities[State.Attack] = attackInference;
		}
		else
		{
			_statePriorities[State.Attack] = 0f;
		}

		// Chase Priority: Min(Not Near, Not Low Health). Requires the player to be present.

		// REPLACED TERNARY:
		if (_player != null)
		{
			_statePriorities[State.Chase] = Mathf.Min(1.0f - nearDistance, 1.0f - lowHealth);
		}
		else
		{
			_statePriorities[State.Chase] = 0f;
		}

		// Patrol Priority: High if no player is present OR if the Chase priority is negligible.

		// REPLACED TERNARY:
		if (_player == null || _statePriorities[State.Chase] < 0.1f)
		{
			_statePriorities[State.Patrol] = 1.0f;
		}
		else
		{
			_statePriorities[State.Patrol] = 0.0f;
		}

		// --- DEFUZZIFICATION (Select State with Highest Priority) ---
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

		// Hard Coded Override/Exception: Force Flee if health is critically low, 
		// even if another state had a slightly higher calculated priority.
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
		GD.Print("SCRIPT - PHANTOM - : Transition: " + _currentState + " -> " + newState);
		_currentState = newState;

		if (newState == State.Attack)
		{
			_isAttacking = true;
			GD.Print("Entered State Attack");
			Velocity = Vector2.Zero;

			if (Animation_Player != null)
			{
				GD.Print("CHANGE STATE -- Playing Phantom_Attack animation");
				Animation_Player.Play("Phantom_Attack");
			}
		}
		else if (newState == State.Chase)
		{
			if (Animation_Sprite != null)
			{
				Animation_Sprite.Play("Phantom_Pursuit");
			}
		}
		else if (newState == State.Patrol)
		{
			if (Animation_Sprite != null)
			{
				Animation_Sprite.Play("Phantom_Pursuit");
			}
		}
		else if (newState == State.Flee)
		{
			if (Animation_Sprite != null)
			{
				Animation_Sprite.Play("Phantom_Pursuit");
			}
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
			Animation_Sprite.FlipH = Velocity.X > 0;
		}
	}

	private void HandlePatrol()
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
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Pursuit");
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
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Pursuit");
	}

	private void HandleAttack(double delta)
	{
		_attackCooldown -= delta;

		if (_attackCooldown <= 0 && _isAttacking == false)
		{
			_isAttacking = true;
			Velocity = Vector2.Zero;

			if (Animation_Player != null)
			{
				Animation_Player.Play("Phantom_Attack");
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
		if (Animation_Sprite != null) Animation_Sprite.Play("Phantom_Pursuit");

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
		GD.Print($"SCRIPT - PHANTOM - : Vida do inimigo alterada para: {newHealth}");
	}

	public void OnDetectionAreaBodyEntered(Node2D body)
	{
		if (body is CharacterBody2D player && player.IsInGroup("player"))
		{
			_player = player;
		}
	}

	public void OnDetectionAreaBodyExited(Node2D body)
	{
		if (body == _player)
		{
			_player = null;
		}
	}

	public void On_Phantom_Damage_Collider_Area_Entered(Node2D _area)
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
			HitSound?.Play();
		}

	}

	public void On_AttackHitbox_AreaEntered(Node2D _area)
	{
		if (_area.IsInGroup("player"))
		{
			GD.Print("Phantom hit the player!");
			Player_Data_Autoload.Data.TakeDamage(10);
			Player_Data_Autoload.Instance.Preserve_Damage(10);
			Player_Data_Autoload.Instance.Update_Player_Health_UI(10);

			// Apply damage here
		}
	}

	private void OnAnimationPlayerFinished(StringName animName)
	{
		if (animName == "Phantom_Attack")
		{
			EndAttack();
		}
	}

	#endregion
	//!---------------------------------------------------------------------------------------------------------
}
