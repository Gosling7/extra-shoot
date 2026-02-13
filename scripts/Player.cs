using ExtraShoot.scripts.Inventory;
using Godot;

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
    [Export]
    public float RotationSpeed { get; set; } = 10f;


    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector3 _direction;
    private PackedScene _projectileScene;
    private Marker3D _muzzle;
    private float _timeSinceLastShot = 0f;
    private Camera3D _camera;
    private InputEventMouseMotion _eventMouseMotion;
    private MeshInstance3D _weaponMesh;
    private bool _isWeaponHolstered = false;


    public override void _Ready()
    {
        _projectileScene = GD.Load<PackedScene>("res://prefabs/projectile.tscn");
        _muzzle = GetNode<Marker3D>("Muzzle");
        _camera = GetViewport().GetCamera3D();
        _weaponMesh = GetNode<MeshInstance3D>("Pivot/Weapon");

        var sword = new InventoryItem("Sword", 1, 2);
        var sword2 = new InventoryItem("Sword", 1, 2);
        InventoryManager.Instance.TryPlaceItem(sword, 0, 0);
        InventoryManager.Instance.TryPlaceItem(sword2, 2, 0);
    }

    public override void _Process(double delta)
    {
        RotateTowardsCursor();

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

        _timeSinceLastShot += (float)delta;
        if (Input.IsActionPressed("shoot") && !_isWeaponHolstered)
        {
            Shoot();
        }

        if (Input.IsActionJustPressed("holster"))
        {
            _weaponMesh.Visible = !_weaponMesh.Visible;
            if (_weaponMesh.Visible)
            {
                _isWeaponHolstered = false;
            }
            else
            {
                _isWeaponHolstered = true;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        // if (@event is InputEventMouseMotion _eventMouseMotion)
        // {
        //     GD.Print($"Mouse position in Viewport: {_eventMouseMotion.Position}");
        // }
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

    private void Shoot()
    {
        if (_timeSinceLastShot < FireRate)
        {
            return;
        }

        var projectile = _projectileScene.Instantiate<Projectile>();
        projectile.GlobalTransform = _muzzle.GlobalTransform;

        GetTree().CurrentScene.AddChild(projectile);

        _timeSinceLastShot = 0f;
    }

    private void RotateTowardsCursor()
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var mousePosition = GetViewport().GetMousePosition();

        var origin = _camera.ProjectRayOrigin(mousePosition);
        var end = origin + _camera.ProjectRayNormal(mousePosition) * 100;
        var query = PhysicsRayQueryParameters3D.Create(origin, end, CollisionMask, [GetRid()]);
        query.CollideWithAreas = true;
        var result = spaceState.IntersectRay(query);

        var targetPos = (Vector3)result["position"];
        var lookAtTarget = new Vector3(targetPos.X, Position.Y, targetPos.Z);
        LookAt(lookAtTarget);

        // GD.Print($"Mouse position in Viewport: {mousePosition}");
        // GD.Print($"Origin: {origin}");
        // GD.Print($"End: {end}");
        // GD.Print($"Result: {result}");
    }
}