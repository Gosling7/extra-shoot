using Godot;

namespace ExtraShoot.scripts.Utilities;

partial class Helper : Node3D
{
    private Viewport _viewport;
    private Camera3D _camera;
    private World3D _world3D;

    public override void _Ready()
    {
        Name = nameof(Helper);

        _viewport = GetViewport();
        _camera = _viewport.GetCamera3D();
        _world3D = GetWorld3D();
    }

    public Godot.Collections.Dictionary GetHitResultUnderMouse(
        uint collisionMask,
        Godot.Collections.Array<Rid> rid,
        bool collideWithAreas = true)
    {
        var mousePosition = _viewport.GetMousePosition();
        var origin = _camera.ProjectRayOrigin(mousePosition);
        var end = origin + _camera.ProjectRayNormal(mousePosition) * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end, collisionMask, rid);
        query.CollideWithAreas = collideWithAreas;
        return _world3D.DirectSpaceState.IntersectRay(query);
    }

    public Godot.Collections.Dictionary GetHitResultUnderMouseWithSpread(
        uint collisionMask,
        Godot.Collections.Array<Rid> rid,
        float aimSpread,
        bool collideWithAreas = true)
    {
        var mousePosition = _viewport.GetMousePosition();
        var origin = _camera.ProjectRayOrigin(mousePosition);

        var spreadX = (float)GD.RandRange(-aimSpread, aimSpread);
        var spreadY = (float)GD.RandRange(-aimSpread, aimSpread);

        var direction = _camera.ProjectRayNormal(mousePosition);
        direction += _camera.Transform.Basis.X * spreadX;
        direction += _camera.Transform.Basis.Y * spreadY;
        direction = direction.Normalized();

        var end = origin + direction * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(origin, end, collisionMask, rid);
        query.CollideWithAreas = collideWithAreas;
        return _world3D.DirectSpaceState.IntersectRay(query);
    }
}