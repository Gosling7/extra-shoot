using System;
using ExtraShoot.scripts.Inventory;
using ExtraShoot.scripts.Utilities;
using Godot;

namespace ExtraShoot.scripts;

public partial class Player : CharacterBody3D
{
    // How fast the player moves in meters per second.
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
    private Marker3D _muzzle;
    private Camera3D _camera;
    private Weapon _weapon;
    private WeaponHeavy _weaponHeavy;
    private bool _isWeaponHolstered = true;
    private Window _inventoryUI;
    private Helper _helper;
    private bool _canShoot = true;
    private Viewport _viewport;
    private Node3D _holsteredWeaponVisual;
    private Node3D _holsteredWeaponHeavyVisual;

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


    public override void _Ready()
    {
        _viewport = GetViewport();
        _muzzle = GetNode<Marker3D>("Pivot/Weapon/Muzzle");
        _camera = _viewport.GetCamera3D();
        _weapon = GetNode<Weapon>("Pivot/Weapon");
        _weaponHeavy = GetNode<WeaponHeavy>("Pivot/WeaponHeavy");
        _holsteredWeaponVisual = GetNode<Weapon>("Pivot/HolsteredWeapon");
        _holsteredWeaponHeavyVisual = GetNode<WeaponHeavy>("Pivot/HolsteredWeaponHeavy");
        _inventoryUI = GetNode<Window>("InventoryUI");
        _helper = GetTree().CurrentScene.GetNode<Helper>($"/root/{nameof(Helper)}");
        _newCrosshair = GetTree().CurrentScene.GetNode<Control>("UI/NewCrosshair");

        _crosshairUp = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/up");
        _crosshairDown = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/down");
        _crosshairLeft = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/left");
        _crosshairRight = GetTree().CurrentScene.GetNode<TextureRect>("UI/NewCrosshair/right");

        _crosshairUpDefaultPosition = _crosshairUp.Position;
        _crosshairDownDefaultPosition = _crosshairDown.Position;
        _crosshairLeftDefaultPosition = _crosshairLeft.Position;
        _crosshairRightDefaultPosition = _crosshairRight.Position;

        _weapon.Visible = false;
        _weaponHeavy.Visible = false;
        _holsteredWeaponVisual.Visible = true;
        _holsteredWeaponHeavyVisual.Visible = true;

        _maxAimSpread = OverallMaxAimSpread;
        //MaxAimSpreadWhileAiming = OverallMaxAimSpread / 4;

        _movementSpeed = BaseMovementSpeed;

        var sword = new InventoryItem("Sword", 1, 2, "green", 1);
        var ammo = new InventoryItem("Ammo", 1, 1, "orange", 16, 1, "SmallAmmo");
        InventoryManager.Instance.TryPlaceItem(sword, 0, 0);
        InventoryManager.Instance.TryPlaceItem(ammo, 1, 0);
    }

    public override void _Process(double delta)
    {
        RotateTowardsCursor();

        // aim spread
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

        if (Input.IsActionJustPressed("reload"))
        {
            if (_weapon.Visible && _weapon.IsMagEmpty())
            {
                _weapon.Reload();
            }
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

        if (Input.IsActionJustPressed("shoot") && !_isWeaponHolstered && _canShoot)
        {
            if (_weapon.Visible)
            {
                _weapon.Shoot(_currentSpread);
            }
            if (_weaponHeavy.Visible)
            {
                _weaponHeavy.Shoot(_currentSpread);
            }

            _currentSpread = _maxAimSpread;
            _canShoot = false;

            GetTree().CreateTimer(_weapon.FireRate).Connect("timeout", new Callable(this, nameof(ResetCanShoot)));
        }

        if (Input.IsActionJustPressed("holster"))
        {
            _weapon.Visible = !_weapon.Visible;
            if (_weapon.Visible)
            {
                _isWeaponHolstered = false;
                _holsteredWeaponVisual.Visible = false;
                Input.MouseMode = Input.MouseModeEnum.Hidden;
                _newCrosshair.Visible = true;

                if (_weaponHeavy.Visible)
                {
                    _weaponHeavy.Visible = false;
                    _holsteredWeaponHeavyVisual.Visible = true;
                }
            }
            else
            {
                _isWeaponHolstered = true;
                _holsteredWeaponVisual.Visible = true;
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _newCrosshair.Visible = false;
            }
        }

        if (Input.IsActionJustPressed("holster_heavy"))
        {
            _weaponHeavy.Visible = !_weaponHeavy.Visible;
            if (_weaponHeavy.Visible)
            {
                _isWeaponHolstered = false;
                _holsteredWeaponHeavyVisual.Visible = false;
                Input.MouseMode = Input.MouseModeEnum.Hidden;
                _newCrosshair.Visible = true;

                if (_weapon.Visible)
                {
                    _weapon.Visible = false;
                    _holsteredWeaponVisual.Visible = true;
                }
            }
            else
            {
                _isWeaponHolstered = true;
                _holsteredWeaponHeavyVisual.Visible = true;
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _newCrosshair.Visible = false;
            }
        }

        if (Input.IsActionJustPressed("toggle_inventory"))
        {
            _inventoryUI.Visible = !_inventoryUI.Visible;
        }
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

        // GD.Print($"Mouse position in Viewport: {mousePosition}");
        // GD.Print($"Origin: {origin}");
        // GD.Print($"End: {end}");
        // GD.Print($"Result: {result}");
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