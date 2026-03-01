using ExtraShoot.scripts.Enemy;
using Godot;

namespace ExtraShoot.scripts.Enemy;

public class RangedAttackState : IState
{
    public Vector3 Target { get; set; }

    public RangedAttackState(Vector3 target)
    {
        Target = target;
    }
}

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

    protected override void EnterState(IState newState)
    {
        switch (newState)
        {
            case IdleState:
                GD.Print("Entered Idle State");
                Idle();
                return;
            case MoveState:
                GD.Print("Entered Move State");
                Move(newState.Target);
                return;
            case RangedAttackState:
                GD.Print("Entered RangedAttack State");
                // AttackFromRange(newState.Target);
                return;
            default:
                return;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_navigationAgent.IsNavigationFinished()
            || GlobalPosition.DistanceTo(_player.GlobalPosition) > AggroRangeInMetres)
        {
            return;
        }

        if (GlobalPosition.DistanceTo(_player.GlobalPosition) < AttackRangeInMetres)
        {
            // GD.Print("In range to shoot");
            if (!_isShooting)
            {
                EnterState(new RangedAttackState(_player.GlobalPosition));
            }

            AttackFromRange(_player.GlobalPosition);

            return;
        }

        _isShooting = false;
        MoveCharacter();
    }

    // protected override void ProcessMove()
    // {
    //     if (GlobalPosition.DistanceTo(_player.GlobalPosition) < AttackRangeInMetres)
    //     {
    //         GD.Print("In range to shoot");
    //         EnterState(new RangedAttackState(_player.GlobalPosition));
    //         return;
    //     }

    //     _isShooting = false;
    //     MoveCharacter();
    // }

    // private void ProcessShooting()
    // {
    //     if (!_isShooting)
    //     {
    //         return;
    //     }

    //     if (_canShoot)
    //     {
    //         Shoot();
    //     }
    // }

    private void AttackFromRange(Vector3 target)
    {
        _isShooting = true;

        var aimDirection = new Vector3(
            target.X,
            GlobalPosition.Y,
            target.Z);

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