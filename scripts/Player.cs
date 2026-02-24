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

    private Control _newCrosshair;
    private TextureRect _crosshairUp;
    private TextureRect _crosshairDown;
    private TextureRect _crosshairLeft;
    private TextureRect _crosshairRight;

    private Vector2 _crosshairUpDefaultPosition;
    private Vector2 _crosshairDownDefaultPosition;
    private Vector2 _crosshairLeftDefaultPosition;
    private Vector2 _crosshairRightDefaultPosition;

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
        _newCrosshair = GetTree().CurrentScene.GetNode<Control>("UI/NewCrosshair");
        _ammoLabel = GetNode<Label3D>("Label3D");

        _crosshairUp = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/up");
        _crosshairDown = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/down");
        _crosshairLeft = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/left");
        _crosshairRight = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/right");

        _crosshairUpDefaultPosition = _crosshairUp.Position;
        _crosshairDownDefaultPosition = _crosshairDown.Position;
        _crosshairLeftDefaultPosition = _crosshairLeft.Position;
        _crosshairRightDefaultPosition = _crosshairRight.Position;

        _maxAimSpread = OverallMaxAimSpread;
        //MaxAimSpreadWhileAiming = OverallMaxAimSpread / 4;

        _movementSpeed = BaseMovementSpeed;

        _revolver.WeaponShot += OnWeaponShot;
        _rifle.WeaponShot += OnWeaponShot;

        _revolver.WeaponReloaded += OnWeaponReloaded;
        _rifle.WeaponReloaded += OnWeaponReloaded;

        // UpdateAmmoLabel();
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
        if (_currentWeapon is not null && !_ammoLabel.Visible)
        {
            _ammoLabel.Visible = true;
            _ammoLabel.Text = $"{_currentWeapon.AmmoCurrentlyInMag}/{_reserveAmmo[_currentWeapon]}";
        }
    }

    private void OnWeaponShot(int usedAmmoCount)
    {
        // _reserveAmmo[_currentWeapon]--;
        // GD.Print($"{_currentWeapon.Name} ammo: {_reserveAmmo[_currentWeapon]}");

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
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _newCrosshair.Visible = false;
            UpdateAmmoLabel();
            return;
        }

        _currentWeapon = weaponToEquip;
        _currentWeapon.Visible = true;

        Input.MouseMode = Input.MouseModeEnum.Hidden;
        _newCrosshair.Visible = true;

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
        var mousePosition = _viewport.GetMousePosition();

        _newCrosshair.Position = new Vector2(
            mousePosition.X - _newCrosshair.Size.X / 2,
            mousePosition.Y - _newCrosshair.Size.Y / 2);

        var spreadMultiplier = 9f;
        var crosshairSpread = spread * 100 * spreadMultiplier;
        // GD.Print(crosshairSpread);

        if (_currentSpread > 0.001f)
        {
            _crosshairUp.Position = new Vector2(
                _crosshairUpDefaultPosition.X,
                _crosshairUpDefaultPosition.Y - crosshairSpread);
            _crosshairDown.Position = new Vector2(
                _crosshairDownDefaultPosition.X,
                _crosshairDownDefaultPosition.Y + crosshairSpread);

            _crosshairLeft.Position = new Vector2(
                _crosshairLeftDefaultPosition.X - crosshairSpread, _crosshairLeftDefaultPosition.Y);
            _crosshairRight.Position = new Vector2(
                _crosshairRightDefaultPosition.X + crosshairSpread, _crosshairRightDefaultPosition.Y);
        }

        if (_currentSpread < 0.001f)
        {
            _crosshairUp.Position = _crosshairUpDefaultPosition;
            _crosshairDown.Position = _crosshairDownDefaultPosition;

            _crosshairLeft.Position = _crosshairLeftDefaultPosition;
            _crosshairRight.Position = _crosshairRightDefaultPosition;
        }
    }
}