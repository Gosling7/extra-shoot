using ExtraShoot.scripts;
using ExtraShoot.scripts.Inventory;
using ExtraShoot.scripts.Utilities;
using Godot;

namespace ExtraShoot.scripts;

public partial class Player : CharacterBody3D
{
    // How fast the player moves in meters per second.
    [Export]
    public int Speed { get; set; } = 14;
    [Export]
    public float FireRate { get; set; } = 1f;
    // The downward acceleration when in the air, in meters per second squared.
    [Export]
    public int FallAcceleration { get; set; } = 75;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector3 _direction;
    private Marker3D _muzzle;
    private Camera3D _camera;
    private Weapon _weapon;
    private bool _isWeaponHolstered = true;
    private Control _inventoryUI;
    private Helper _helper;
    private bool _canShoot = true;
    private TextureRect _crosshair;
    private Viewport _viewport;

    public override void _Ready()
    {
        _viewport = GetViewport();
        _muzzle = GetNode<Marker3D>("Pivot/Weapon/Muzzle");
        _camera = _viewport.GetCamera3D();
        _weapon = GetNode<Weapon>("Pivot/Weapon");
        _inventoryUI = GetNode<Control>("InventoryUI");
        _helper = GetTree().CurrentScene.GetNode<Helper>($"/root/{nameof(Helper)}");
        _crosshair = GetTree().CurrentScene.GetNode<TextureRect>("UI/Crosshair");

        _weapon.Visible = false;

        var sword = new InventoryItem("Sword", 1, 2);
        var sword2 = new InventoryItem("Sword", 1, 2);
        InventoryManager.Instance.TryPlaceItem(sword, 0, 0);
        InventoryManager.Instance.TryPlaceItem(sword2, 2, 0);
    }

    public override void _Process(double delta)
    {
        RotateTowardsCursor();

        UpdateCrosshair();

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

        if (Input.IsActionJustPressed("shoot") && !_isWeaponHolstered && _canShoot)
        {
            _weapon.Shoot();

            _canShoot = false;

            GetTree().CreateTimer(_weapon.FireRate).Connect("timeout", new Callable(this, nameof(ResetCanShoot)));
        }

        if (Input.IsActionJustPressed("holster"))
        {
            _weapon.Visible = !_weapon.Visible;
            if (_weapon.Visible)
            {
                _isWeaponHolstered = false;
                Input.MouseMode = Input.MouseModeEnum.Hidden;
                _crosshair.Visible = true;
            }
            else
            {
                _isWeaponHolstered = true;
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _crosshair.Visible = false;
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
        _targetVelocity.X = _direction.X * Speed;
        _targetVelocity.Z = _direction.Z * Speed;

        // Vertical velocity
        if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
        {
            _targetVelocity.Y -= FallAcceleration * (float)delta;
        }

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
    
    private void UpdateCrosshair()
    {
        var mousePosition = _viewport.GetMousePosition();
        _crosshair.Position = new Vector2(
            mousePosition.X - _crosshair.Size.X / 2,
            mousePosition.Y - _crosshair.Size.Y / 2);
    }
}