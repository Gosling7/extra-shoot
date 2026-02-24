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

    public int AmmoCurrentlyInMag { get; private set; }

    private Helper _helper;
    private uint _collisionMask;
    private Player _player;
    private MeshInstance3D _tracer;
    private Marker3D _muzzle;

    [Signal]
    public delegate void WeaponShotEventHandler(int usedAmmoCount);
    [Signal]
    public delegate void WeaponReloadedEventHandler();

    public override void _Ready()
    {
        _helper = GetTree().CurrentScene.GetNode<Helper>($"/root/{nameof(Helper)}");
        _player = GetParent().GetParent<Player>();
        _collisionMask = _player.CollisionMask;
        _muzzle = GetNode<Marker3D>("Muzzle");
        _tracer = GetNode<MeshInstance3D>("Muzzle/Tracer");

        AmmoCurrentlyInMag = MagSize;
    }

    public void Shoot(float spread)
    {
        if (AmmoCurrentlyInMag <= 0)
        {
            GD.Print("Out of ammo in mag");
            return;
        }

        var hitResult = _helper.GetHitResultUnderMouseWithSpread(_collisionMask, [_player.GetRid()],
            spread);
        if (!hitResult.TryGetValue("position", out var hitPosition))
        {
            return;
        }

        SpawnTracer((Vector3)hitPosition);
        AmmoCurrentlyInMag--;
        EmitSignal(SignalName.WeaponShot, 1);

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
