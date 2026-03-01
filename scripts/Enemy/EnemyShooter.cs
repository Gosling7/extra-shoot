using ExtraShoot.scripts.Enemy;
using Godot;

namespace ExtraShoot.scripts.Enemy;

public partial class EnemyShooter : EnemyBase
{
    [Export] private float AttackRangeInMetres = 5f;
    [Export] private float ShootCooldown { get; set; } = 1.5f;
    [Export] private PackedScene ProjectileScene { get; set; }

    private bool _canShoot = true;
    private bool _isShooting = false;

    public override void _Ready()
    {
        base._Ready();
    }

    protected override void EnterState(State newState)
    {
        switch (newState)
        {
            case State.Idle:
                GD.Print("Entered Idle State");
                _currentState = newState;
                return;
            case State.Moving:
                GD.Print("Entered Move State");
                _currentState = newState;
                return;
            case State.AttackingRange:
                GD.Print("Entered RangedAttack State");
                _currentState = newState;
                return;
            default:
                return;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (_currentState)
        {
            case State.Idle:
                return;
            case State.Moving:
                ProcessMoving();
                return;
            case State.AttackingRange:
                ProcessAttackingRange();
                return;
        }
    }

    protected override void ProcessMoving()
    {
        if (GlobalPosition.DistanceTo(_player.GlobalPosition) > AggroRangeInMetres)
        {
            EnterState(State.Idle);
        }
        if (GlobalPosition.DistanceTo(_player.GlobalPosition) < AttackRangeInMetres)
        {
            EnterState(State.AttackingRange);
        }

        MoveCharacter();
    }

    private void ProcessAttackingRange()
    {
        if (GlobalPosition.DistanceTo(_player.GlobalPosition) > AttackRangeInMetres)
        {
            EnterState(State.Moving);
        }

        var aimDirection = new Vector3(
            _player.GlobalPosition.X,
            GlobalPosition.Y,
            _player.GlobalPosition.Z);

        LookAt(aimDirection, Vector3.Up);

        if (_canShoot)
        {
            Shoot();
        }
    }

    private void Shoot()
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

}