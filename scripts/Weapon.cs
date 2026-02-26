using ExtraShoot.scripts.Interfaces;
using ExtraShoot.scripts.Utilities;
using Godot;
using System;

namespace ExtraShoot.scripts;

public partial class Weapon : Node3D
{
    [Export] public int MagSize = 3;
    [Export] public float FireRate = 0.5f;
    [Export] public float ReloadTime = 1.5f;
    [Export] public int Damage = 10;
    [Export] public float DelayToApplyDamageInSec = 0.1f;
    [Export] public int MovementSpeedPenaltyWhileAiming { get; private set; } = 5;
    [Export] public float MaxAimSpreadWhileAiming { get; private set; } = 0.05f;
    [Export] public float BaseMaxAimSpread { get; private set; } = 0.15f;

    public int AmmoCurrentlyInMag { get; private set; }

    [Export] private float _recoilPerShot = 0.1f;
    private float _maxAimSpread;
    [Export] private float _recoilRecoverySpeed = 5f;
    [Export] private float _movementAimSpreadIncreaseSpeed = 20f;
    [Export] private float _movementAimSpreadDecreaseSpeed = 7f;
    private float _currentAimSpread;
    private float _movementAimSpread;
    private float _recoilAimSpread;

    private Helper _helper;
    private uint _collisionMask;
    private Player _player;
    private MeshInstance3D _tracer;
    private Marker3D _muzzle;

    [Signal]
    public delegate void WeaponShotEventHandler(int usedAmmoCount);
    [Signal]
    public delegate void WeaponReloadedEventHandler();
    [Signal]
    public delegate void AimSpreadChangedEventHandler(float spread);

    public override void _Ready()
    {
        _helper = GetTree().CurrentScene.GetNode<Helper>($"/root/{nameof(Helper)}");
        _player = GetParent().GetParent<Player>();
        _collisionMask = _player.CollisionMask;
        _muzzle = GetNode<Marker3D>("Muzzle");
        _tracer = GetNode<MeshInstance3D>("Muzzle/Tracer");

        _maxAimSpread = BaseMaxAimSpread;

        AmmoCurrentlyInMag = MagSize;
    }

    public override void _Process(double delta)
    {
        if (_player.CurrentWeapon != this)
        {
            return;
        }

        UpdateAimSpread(delta);
        EmitSignal(SignalName.AimSpreadChanged, _currentAimSpread);
    }

    public void Shoot()
    {
        if (AmmoCurrentlyInMag <= 0)
        {
            GD.Print("Out of ammo in mag");
            return;
        }

        var hitResult = _helper.GetHitResultUnderMouseWithSpread(_collisionMask, [_player.GetRid()],
            _currentAimSpread);
        if (!hitResult.TryGetValue("position", out var hitPosition))
        {
            return;
        }

        SpawnTracer((Vector3)hitPosition);
        AmmoCurrentlyInMag--;
        EmitSignal(SignalName.WeaponShot, 1);

        _recoilAimSpread += _recoilPerShot;
        GD.Print($"RecoilAimSpread: {_recoilAimSpread}");
        GD.Print($"RecoilPerShot: {_recoilPerShot}");

        if (!hitResult.TryGetValue("collider", out var hitNode)
            || (Node3D)hitNode is not IDamageable damageable)
        {
            return;
        }

        var timer = GetTree().CreateTimer(DelayToApplyDamageInSec);
        timer.Timeout += () => damageable?.TakeDamage(Damage);
    }

    public void Reload(int ammoCount)
    {
        if (ammoCount <= 0)
        {
            GD.Print("No ammo for reload");
            return;
        }

        GD.Print("Reloading");

        var timer = GetTree().CreateTimer(ReloadTime);
        timer.Timeout += () =>
        {
            AmmoCurrentlyInMag = MagSize;
            GD.Print("Weapon reloaded");
            EmitSignal(SignalName.WeaponReloaded);
        };
    }

    public bool IsMagEmpty() => AmmoCurrentlyInMag <= 0;

    private void UpdateAimSpread(double delta)
    {
        _maxAimSpread = _player.IsAiming ? MaxAimSpreadWhileAiming : BaseMaxAimSpread;

        var moveRatio = Mathf.Clamp(_player.Velocity.Length() / _player.MovementSpeed, 0f, 1f);

        var targetMovementSpread = moveRatio * _maxAimSpread;

        var aimSpreadSpeed = targetMovementSpread > _movementAimSpread
            ? _movementAimSpreadIncreaseSpeed
            : _movementAimSpreadDecreaseSpeed;

        _movementAimSpread = Mathf.Lerp(_movementAimSpread, targetMovementSpread, (float)(aimSpreadSpeed * delta));

        _recoilAimSpread = Mathf.Lerp(_recoilAimSpread, 0f, (float)(_recoilRecoverySpeed * delta));


        _currentAimSpread = _movementAimSpread + _recoilAimSpread;
    }

    private void SpawnTracer(Vector3 targetPosition)
    {
        _muzzle.LookAt(targetPosition);
        var start = _muzzle.GlobalTransform.Origin;
        var distance = start.DistanceTo(targetPosition);

        if (_tracer.Mesh is CylinderMesh cylinder)
        {
            cylinder.Height = distance;
        }
        _tracer.Position = new Vector3(
            _tracer.Position.X,
            _tracer.Position.Y,
            _tracer.Position.Z - distance / 2);

        // Spawn identical tracer with the same position and rotation but visible
        // if that duplicate is spawned once and just has the position and visibility updated
        // it might not be that stupid
        var duplicateTracer = _tracer.Duplicate();

        GetTree().CurrentScene.AddChild(duplicateTracer);

        var duplicate3D = duplicateTracer as Node3D;
        duplicate3D.GlobalPosition = _tracer.GlobalPosition;
        duplicate3D.GlobalRotation = _tracer.GlobalRotation;
        duplicate3D.Visible = true;

        var timer = GetTree().CreateTimer(0.15f);
        timer.Timeout += () => ResetTracer(distance, duplicateTracer);
    }

    private void ResetTracer(float distance, Node duplicateTracer)
    {
        duplicateTracer.QueueFree();
        _tracer.Position = new Vector3(
            _tracer.Position.X,
            _tracer.Position.Y,
            _tracer.Position.Z + distance / 2);
    }
}
