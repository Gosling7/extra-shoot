using System;
using System.Collections.Generic;
using ExtraShoot.scripts.Utilities;
using Godot;

namespace ExtraShoot.scripts;

public partial class Player : CharacterBody3D
{
    [Export] private int BaseMovementSpeed { get; set; } = 9;
    [Export] public float FireRate { get; set; } = 1.5f;
    [Export] private int RevolverReserveAmmo = 9;
    [Export] private int RifleReserveAmmo = 2;

    public int MovementSpeed { get; private set; }
    public bool IsAiming { get; private set; }

    private int _movementSpeedWhileAiming;
    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector3 _direction;
    private Camera3D _camera;
    public Weapon _revolver;
    public Weapon _rifle;
    private bool _isWeaponHolstered = true;
    private Helper _helper;
    private bool _canShoot = true;
    private Viewport _viewport;

    private Label3D _ammoLabel;

    private List<Weapon> _weapons = [];
    public Weapon CurrentWeapon { get; private set; }
    private Dictionary<Weapon, int> _reserveAmmo = [];

    [Signal]
    public delegate void UpdateAmmoUIEventHandler();
    [Signal]
    public delegate void CrosshairVisibilityChangedEventHandler(bool isVisible);

    public override void _Ready()
    {
        _revolver = GetNode<Weapon>("Pivot/Revolver");
        _rifle = GetNode<Weapon>("Pivot/Rifle");
        _weapons.AddRange([_revolver, _rifle]);
        _reserveAmmo[_revolver] = RevolverReserveAmmo;
        _reserveAmmo[_rifle] = RifleReserveAmmo;
        CurrentWeapon = null;

        foreach (var weapon in _weapons)
        {
            weapon.Visible = false;
        }

        _viewport = GetViewport();
        _camera = _viewport.GetCamera3D();
        _helper = GetTree().CurrentScene.GetNode<Helper>($"/root/{nameof(Helper)}");
        _ammoLabel = GetNode<Label3D>("Label3D");

        MovementSpeed = BaseMovementSpeed;

        _revolver.WeaponShot += OnWeaponShot;
        _rifle.WeaponShot += OnWeaponShot;

        _revolver.WeaponReloaded += OnWeaponReloaded;
        _rifle.WeaponReloaded += OnWeaponReloaded;
    }

    public override void _Process(double delta)
    {
        RotateTowardsCursor();

        HandleInput();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Ground velocity
        _targetVelocity.X = _direction.X * MovementSpeed;
        _targetVelocity.Z = _direction.Z * MovementSpeed;

        // Moving the character
        Velocity = _targetVelocity;
        MoveAndSlide();
    }

    private void OnWeaponReloaded()
    {
        _reserveAmmo[CurrentWeapon] -= CurrentWeapon.MagSize;
        UpdateAmmoLabel();
    }

    private void UpdateAmmoLabel()
    {
        if (CurrentWeapon is null)
        {
            _ammoLabel.Visible = false;
        }
        if (CurrentWeapon is not null)
        {
            _ammoLabel.Visible = true;
            _ammoLabel.Text = $"{CurrentWeapon.AmmoCurrentlyInMag}/{_reserveAmmo[CurrentWeapon]}";
        }
    }

    private void OnWeaponShot(int usedAmmoCount)
    {
        UpdateAmmoLabel();
    }

    private void HandleInput()
    {
        _direction = Vector3.Zero;
        if (Input.IsActionPressed("move_right"))
        {
            _direction.X += 1.0f;
        }
        if (Input.IsActionPressed("move_left"))
        {
            _direction.X -= 1.0f;
        }
        if (Input.IsActionPressed("move_back"))
        {
            _direction.Z += 1.0f;
        }
        if (Input.IsActionPressed("move_forward"))
        {
            _direction.Z -= 1.0f;
        }
        _direction = _direction.Normalized();

        if (CurrentWeapon is not null)
        {
            IsAiming = Input.IsActionPressed("aim");
            MovementSpeed = IsAiming
                ? _movementSpeedWhileAiming
                : BaseMovementSpeed;
        }

        if (Input.IsActionJustPressed("reload"))
        {
            if (CurrentWeapon is not null && CurrentWeapon.IsMagEmpty())
            {
                CurrentWeapon.Reload(_reserveAmmo[CurrentWeapon]);
            }
        }

        if (Input.IsActionJustPressed("shoot"))
        {
            if (CurrentWeapon is null)
            {
                return;
            }
            if (!_canShoot || CurrentWeapon.AmmoCurrentlyInMag <= 0)
            {
                return;
            }

            CurrentWeapon.Shoot();
            _canShoot = false;

            EmitSignal(SignalName.UpdateAmmoUI);

            GetTree().CreateTimer(CurrentWeapon.FireRate).Connect("timeout", new Callable(this, nameof(ResetCanShoot)));
        }

        if (Input.IsActionJustPressed("toggle_revolver"))
        {
            ToggleWeapon(_revolver);
        }

        if (Input.IsActionJustPressed("toggle_rifle"))
        {
            ToggleWeapon(_rifle);
        }
    }

    private void ToggleWeapon(Weapon weaponToEquip)
    {
        if (CurrentWeapon == weaponToEquip)
        {
            CurrentWeapon.Visible = false;
            CurrentWeapon = null;
            EmitSignal(SignalName.CrosshairVisibilityChanged, false);
            UpdateAmmoLabel();
            return;
        }

        if (CurrentWeapon is not null)
        {
            CurrentWeapon.Visible = false;
            CurrentWeapon = weaponToEquip;
            CurrentWeapon.Visible = true;
            _movementSpeedWhileAiming = BaseMovementSpeed - CurrentWeapon.MovementSpeedPenaltyWhileAiming;
            UpdateAmmoLabel();
            return;
        }

        CurrentWeapon = weaponToEquip;
        _movementSpeedWhileAiming = BaseMovementSpeed - CurrentWeapon.MovementSpeedPenaltyWhileAiming;
        CurrentWeapon.Visible = true;
        EmitSignal(SignalName.CrosshairVisibilityChanged, true);

        UpdateAmmoLabel();
    }

    private void RotateTowardsCursor()
    {
        var hitResult = _helper.GetHitResultUnderMouse(CollisionMask, [GetRid()]);
        if (hitResult is null || !hitResult.TryGetValue("position", out var pos))
        {
            return;
        }

        var position = (Vector3)pos;
        var lookAtTarget = new Vector3(position.X, Position.Y, position.Z);
        LookAt(lookAtTarget);
    }

    private void ResetCanShoot()
    {
        _canShoot = true;
    }
}