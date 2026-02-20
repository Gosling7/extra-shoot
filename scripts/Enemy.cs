using ExtraShoot.scripts.Interfaces;
using ExtraShoot.scripts.Utilities;
using Godot;
using System;

namespace ExtraShoot.scripts;

public partial class Enemy : CharacterBody3D, IDamageable
{
    [Export]
    public int MaxHealth { get; set; } = 30;
    [Export]
    public float MovementSpeed { get; set; } = 4.0f;
    [Export]
    public float AggroRangeInMeters { get; set; } = 30.0f;
    [Export]
    public float AttackRangeInMeters { get; set; } = 10.0f;
    [Export]
    public float ShootCooldown { get; set; } = 1.5f;
    [Export]
    public int Damage { get; set; } = 5;
    [Export]
    public PackedScene ProjectileScene { get; set; }
    [Export]
    public float PauseAfterShootingInSec { get; set; } = 1.0f;

    public Vector3 MovementTarget
    {
        get { return _navigationAgent.TargetPosition; }
        set { _navigationAgent.TargetPosition = value; }
    }

    private bool _canShoot = true;
    private Vector3 _movementTargetPosition = new Vector3(-3.0f, 0.0f, 2.0f);
    private NavigationAgent3D _navigationAgent;
    private int _health;
    private ProgressBar _healthbar;
    private GameTimer _lookForEnemyTimer;
    private GameTimer _aggroRangeTimer;
    private Area3D _detectionArea;
    private Node3D _currentMoveTarget;
    private Node3D _currentAggroTarget;
    private bool _isInRangeToShoot = false;

    public override void _Ready()
    {
        _navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        // These values need to be adjusted for the actor's speed
        // and the navigation layout.
        _navigationAgent.PathDesiredDistance = 0.5f;
        _navigationAgent.TargetDesiredDistance = 0.5f;

        _health = MaxHealth;
        _healthbar = GetNode<ProgressBar>("HealthBarRoot/SubViewport/HealthBarUI/ProgressBar");
        _healthbar.MaxValue = MaxHealth;
        _healthbar.Value = _health;
        _detectionArea = GetNode<Area3D>("EnemyDetectionArea");

        // Make sure to not await during _Ready.
        Callable.From(ActorSetup).CallDeferred();

        _detectionArea.BodyEntered += OnBodyEntered;
        _detectionArea.BodyExited += OnBodyExited;

        _lookForEnemyTimer = new GameTimer(this);
        _aggroRangeTimer = new GameTimer(this);

        StartLookingForEnemy();
    }

    public override void _PhysicsProcess(double delta)
    {
        // if movement finished or dropped aggro
        if (_navigationAgent.IsNavigationFinished() || _currentAggroTarget is null)
        {
            return;
        }

        // if in range to shoot -> shoot
        if (GlobalPosition.DistanceTo(_currentAggroTarget.GlobalPosition) < AttackRangeInMeters)
        {
            GD.Print("In range to Shoot");
            _isInRangeToShoot = true;
            Velocity = Vector3.Zero;
            MoveAndSlide();

            var aimDirection = new Vector3(
                _currentAggroTarget.GlobalPosition.X,
                GlobalPosition.Y,
                _currentAggroTarget.GlobalPosition.Z);
                
            LookAt(aimDirection, Vector3.Up);
            // spawn projectile
            if (_canShoot)
            {
                ShootAtTarget(_currentAggroTarget.GlobalPosition);
                _isInRangeToShoot = true;
            }

            return;
        }
        else
        {
            _isInRangeToShoot = false;
        }

        if (_currentMoveTarget is not null)
        {
            MovementTarget = _currentMoveTarget.GlobalPosition;
        }

        Vector3 currentAgentPosition = GlobalTransform.Origin;
        Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();
        var direction = currentAgentPosition.DirectionTo(nextPathPosition) * MovementSpeed;

        Velocity = direction;
        MoveAndSlide();

        var targetDirection = new Vector3(direction.X, Position.Y, direction.Z);
        LookAt(GlobalPosition + targetDirection, Vector3.Up);
    }

    public void TakeDamage(int amount)
    {
        _health -= amount;
        _healthbar.Value = _health;

        if (_health <= 0)
        {
            Die();
        }
    }

    private void StartLookingForEnemy()
    {
        _lookForEnemyTimer.Start(
            callback: LookForEnemy,
            interval: 1f,
            loop: true,
            initialDelay: 0f
        );

        void LookForEnemy()
        {
            if (_currentMoveTarget is not null)
            {
                GD.Print($"Enemy sees {_currentMoveTarget.Name}");
                _currentAggroTarget = _currentMoveTarget;

                StopLookingForEnemy();
                StartAggro();

                MovementTarget = _currentMoveTarget.GlobalPosition;
            }
        }
    }

    private void StopLookingForEnemy()
    {
        _lookForEnemyTimer.Stop();
    }

    private void StartAggro()
    {
        _aggroRangeTimer.Start(
            callback: CheckIfEnemyInRange,
            interval: 1f,
            loop: true,
            initialDelay: 0f
        );

        void CheckIfEnemyInRange()
        {
            var distance = this.GlobalPosition.DistanceTo(_currentAggroTarget.GlobalPosition);
            if (distance > AggroRangeInMeters)
            {
                GD.Print("LOST AGGRO");

                _currentAggroTarget = null;
                ClearAggroTimer();

                StartLookingForEnemy();
            }
        }
    }

    private void ShootAtTarget(Vector3 targetPosition)
    {
        if (ProjectileScene is null)
        {
            return;
        }

        var projectileInstance = ProjectileScene.Instantiate<Projectile>();
        projectileInstance.Initialize(Damage);
        projectileInstance.GlobalTransform = GlobalTransform;
        GetParent().AddChild(projectileInstance);

        // Start cooldown
        _canShoot = false;
        GetTree().CreateTimer(ShootCooldown).Timeout += () => _canShoot = true;
    }

    private void ClearAggroTimer()
    {
        _aggroRangeTimer.Stop();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is CharacterBody3D player)
        {
            _currentMoveTarget = player;
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body == _currentMoveTarget)
        {
            _currentMoveTarget = null;
        }
    }

    private async void ActorSetup()
    {
        // Wait for the first physics frame so the NavigationServer can sync.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        // Now that the navigation map is no longer empty, set the movement target.
        //MovementTarget = _movementTargetPosition;
    }

    private void Die()
    {
        GD.Print("Daed");

        QueueFree();
    }
}
