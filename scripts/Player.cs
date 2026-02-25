using System;
using System.Collections.Generic;
using ExtraShoot.scripts.Utilities;
using Godot;

namespace ExtraShoot.scripts;

public partial class Player : CharacterBody3D
{
    [Export]
    private int BaseMovementSpeed { get; set; } = 9;
    [Export]
    private int MovementSpeedWhileAiming { get; set; } = 3;
    [Export]
    public float OverallMaxAimSpread { get; set; } = 0.1f;
    [Export]
    public float MaxAimSpreadWhileAiming { get; set; } = 0.025f;
    [Export]
    public float SpreadLerpSpeed { get; set; } = 4f;
    [Export]
    public float FireRate { get; set; } = 1.5f;

    private int _movementSpeed;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector3 _direction;
    private Camera3D _camera;
    private Weapon _revolver;
    private Weapon _rifle;
    private bool _isWeaponHolstered = true;
    private Helper _helper;
    private bool _canShoot = true;
    private Viewport _viewport;
    private Node3D _holsteredRevolverVisual;
    private Node3D _holsteredRifleVisual;

    private float _minAimSpread = 0f;
    private float _maxAimSpread;
    private float _currentSpread;

    private int _revolverAmmoCount;
    private int _rifleAmmoCount;

    private Label3D _ammoLabel;

    private List<Weapon> _weapons = [];
    private Weapon _currentWeapon;
    private Dictionary<Weapon, int> _reserveAmmo = [];

    [Signal]
    public delegate void UpdateAmmoUIEventHandler();
    [Signal]
    public delegate void AimSpreadChangedEventHandler(float spread);
    [Signal]
    public delegate void CrosshairVisibilityChangedEventHandler(bool isVisible);

    public override void _Ready()
    {
        _revolver = GetNode<Weapon>("Pivot/Revolver");
        _rifle = GetNode<Weapon>("Pivot/Rifle");
        _weapons.AddRange([_revolver, _rifle]);
        _reserveAmmo[_revolver] = 9;
        _reserveAmmo[_rifle] = 2;
        _currentWeapon = null;

        foreach (var weapon in _weapons)
        {
            weapon.Visible = false;
        }

        _viewport = GetViewport();
        _camera = _viewport.GetCamera3D();
        _holsteredRevolverVisual = GetNode<Weapon>("Pivot/HolsteredRevolver");
        _holsteredRifleVisual = GetNode<Weapon>("Pivot/HolsteredRifle");
        _helper = GetTree().CurrentScene.GetNode<Helper>($"/root/{nameof(Helper)}");
        _ammoLabel = GetNode<Label3D>("Label3D");

        _maxAimSpread = OverallMaxAimSpread;
        //MaxAimSpreadWhileAiming = OverallMaxAimSpread / 4;

        _movementSpeed = BaseMovementSpeed;

        _revolver.WeaponShot += OnWeaponShot;
        _rifle.WeaponShot += OnWeaponShot;

        _revolver.WeaponReloaded += OnWeaponReloaded;
        _rifle.WeaponReloaded += OnWeaponReloaded;
    }

    private void OnWeaponReloaded()
    {
        _reserveAmmo[_currentWeapon] -= _currentWeapon.MagSize;
        UpdateAmmoLabel();
    }

    private void UpdateAmmoLabel()
    {
        if (_currentWeapon is null)
        {
            _ammoLabel.Visible = false;
        }
        if (_currentWeapon is not null)
        {
            _ammoLabel.Visible = true;
            _ammoLabel.Text = $"{_currentWeapon.AmmoCurrentlyInMag}/{_reserveAmmo[_currentWeapon]}";
        }
    }

    private void OnWeaponShot(int usedAmmoCount)
    {
        UpdateAmmoLabel();
    }

    public override void _Process(double delta)
    {
        RotateTowardsCursor();

        var spread = Velocity.Length() > 0.1f ? _maxAimSpread : _minAimSpread;
        _currentSpread = Mathf.Lerp(_currentSpread, spread, (float)(SpreadLerpSpeed * delta));
        if (_currentSpread < 0.001f)
        {
            _currentSpread = 0f;
        }

        // GD.Print($"Current spread: {_currentSpread}");

        UpdateCrosshair(_currentSpread);

        HandleInput();
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

        if (Input.IsActionPressed("aim"))
        {
            _maxAimSpread = MaxAimSpreadWhileAiming;
            _movementSpeed = MovementSpeedWhileAiming;
        }
        else
        {
            _maxAimSpread = OverallMaxAimSpread;
            _movementSpeed = BaseMovementSpeed;
        }

        if (Input.IsActionJustPressed("reload"))
        {
            if (_currentWeapon is not null && _currentWeapon.IsMagEmpty())
            {
                _currentWeapon.Reload(_reserveAmmo[_currentWeapon]);
            }
        }

        if (Input.IsActionJustPressed("shoot"))
        {
            if (_currentWeapon is null)
            {
                return;
            }
            if (!_canShoot || _reserveAmmo[_currentWeapon] <= 0)
            {
                return;
            }

            _currentWeapon.Shoot(_currentSpread);
            _currentSpread = _maxAimSpread;
            _canShoot = false;

            EmitSignal(SignalName.UpdateAmmoUI);

            GetTree().CreateTimer(_currentWeapon.FireRate).Connect("timeout", new Callable(this, nameof(ResetCanShoot)));
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
        if (_currentWeapon == weaponToEquip)
        {
            _currentWeapon.Visible = false;
            _currentWeapon = null;
            EmitSignal(SignalName.CrosshairVisibilityChanged, false);
            UpdateAmmoLabel();
            return;
        }

        if (_currentWeapon is not null)
        {
            _currentWeapon.Visible = false;
            _currentWeapon = weaponToEquip;
            _currentWeapon.Visible = true;
            UpdateAmmoLabel();
            return;
        }

        _currentWeapon = weaponToEquip;
        _currentWeapon.Visible = true;
        EmitSignal(SignalName.CrosshairVisibilityChanged, true);

        UpdateAmmoLabel();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Ground velocity
        _targetVelocity.X = _direction.X * _movementSpeed;
        _targetVelocity.Z = _direction.Z * _movementSpeed;

        // Moving the character
        Velocity = _targetVelocity;
        MoveAndSlide();
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

    private void UpdateCrosshair(float spread)
    {
        EmitSignal(SignalName.AimSpreadChanged, spread);
    }
}