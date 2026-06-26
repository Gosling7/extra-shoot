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

    private AnimationPlayer _animationPlayer;
    private AnimationTree _animationTree;
    private AnimationNodeStateMachinePlayback _legsStateMachine;
    [Export] private NodePath SkeletonPath;
    private Skeleton3D _skeleton;
    private int _spineIndex;
    private Node3D _visualRoot;
    private Node3D _armature;
    private float _armatureYOffset;

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

        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animationTree = GetNode<AnimationTree>("AnimationTree");
        _legsStateMachine = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/StateMachine/playback");
        _skeleton = GetNode<Skeleton3D>(SkeletonPath);
        _spineIndex = _skeleton.FindBone("mixamorig_Spine1");

        _visualRoot = GetNode<Node3D>("Pivot");
        _armature = GetNode<Node3D>("Pivot/Armature"); // adjust if your node is named differently
        // _armatureYOffset = _armature.Rotation.Y;
        _armatureYOffset = 0;
        GD.Print($"Armature Y offset: {Mathf.RadToDeg(_armatureYOffset)}"); // should be ~180

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
        RotateBodyTowardsMouse();
        AimUpperBodyTowardsMouse();
        HandleInput();
    }

    public override void _PhysicsProcess(double delta)
    {
        _targetVelocity.X = _direction.X * MovementSpeed;
        _targetVelocity.Z = _direction.Z * MovementSpeed;

        Velocity = _targetVelocity;
        MoveAndSlide();

        if (Velocity.Length() < 0.1f)
        {
            _legsStateMachine.Travel("idle");
            return;
        }

        var moveDirection = new Vector2(Velocity.X, Velocity.Z).Normalized();

        // Correct the facing direction by the armature's Y offset
        var pivotForward = -_visualRoot.GlobalTransform.Basis.Z;
        var correctedForward = pivotForward.Rotated(Vector3.Up, _armatureYOffset);
        var facingDirection = new Vector2(correctedForward.X, correctedForward.Z).Normalized();
        var facingRight = new Vector2(facingDirection.Y, -facingDirection.X);

        var forward = facingDirection.Dot(moveDirection);
        var right = facingRight.Dot(moveDirection);

        if (Mathf.Abs(forward) >= Mathf.Abs(right))
        {
            _legsStateMachine.Travel(forward > 0 ? "run" : "run_backward");
        }
        else
        {
            _legsStateMachine.Travel(right > 0 ? "strafe_right" : "strafe_left");
        }
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
            _direction.X += 1.0f;
        if (Input.IsActionPressed("move_left"))
            _direction.X -= 1.0f;
        if (Input.IsActionPressed("move_back"))
            _direction.Z += 1.0f;
        if (Input.IsActionPressed("move_forward"))
            _direction.Z -= 1.0f;
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
                CurrentWeapon.Reload(_reserveAmmo[CurrentWeapon]);
        }

        if (Input.IsActionJustPressed("shoot"))
        {
            if (CurrentWeapon is null) return;
            if (!_canShoot || CurrentWeapon.AmmoCurrentlyInMag <= 0) return;

            CurrentWeapon.Shoot();
            _canShoot = false;
            EmitSignal(SignalName.UpdateAmmoUI);
            GetTree().CreateTimer(CurrentWeapon.FireRate).Connect("timeout", new Callable(this, nameof(ResetCanShoot)));
        }

        if (Input.IsActionJustPressed("toggle_revolver"))
            ToggleWeapon(_revolver);

        if (Input.IsActionJustPressed("toggle_rifle"))
            ToggleWeapon(_rifle);
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

    private void AimUpperBodyTowardsMouse()
    {
        var mousePos = GetViewport().GetMousePosition();
        var from = _camera.ProjectRayOrigin(mousePos);
        var dir = _camera.ProjectRayNormal(mousePos);
        float t = -from.Y / dir.Y;
        var worldMousePos = from + dir * t;

        Vector3 toMouse = (worldMousePos - GlobalPosition).Normalized();

        // Use corrected facing direction, same as in _PhysicsProcess
        var pivotForward = -_visualRoot.GlobalTransform.Basis.Z;
        Vector3 facing = pivotForward.Rotated(Vector3.Up, _armatureYOffset);

        float angle = Mathf.Atan2(
            toMouse.X * facing.Z - toMouse.Z * facing.X,
            toMouse.Dot(facing)
        );

        angle = Mathf.Clamp(angle, Mathf.DegToRad(-30f), Mathf.DegToRad(30f));

        _skeleton.SetBoneGlobalPoseOverride(_spineIndex, Transform3D.Identity, 0.0f, false);
        var animatedPose = _skeleton.GetBoneGlobalPose(_spineIndex);

        var skeletonBasisInv = _skeleton.GlobalTransform.Basis.Inverse();
        var localUp = (skeletonBasisInv * Vector3.Up).Normalized();

        var extraRot = new Basis(new Quaternion(localUp, angle));
        var targetPose = new Transform3D(extraRot * animatedPose.Basis, animatedPose.Origin);

        _skeleton.SetBoneGlobalPoseOverride(_spineIndex, targetPose, 1.0f, true);
    }

    private void RotateBodyTowardsMouse()
    {
        var mousePos = GetViewport().GetMousePosition();
        var from = _camera.ProjectRayOrigin(mousePos);
        var dir = _camera.ProjectRayNormal(mousePos);
        float t = -from.Y / dir.Y;
        var worldMousePos = from + dir * t;

        var lookTarget = new Vector3(worldMousePos.X, GlobalPosition.Y, worldMousePos.Z);

        if (lookTarget.DistanceTo(GlobalPosition) > 0.01f)
            _visualRoot.LookAt(lookTarget, Vector3.Up);
    }

    private void ResetCanShoot()
    {
        _canShoot = true;
    }
}