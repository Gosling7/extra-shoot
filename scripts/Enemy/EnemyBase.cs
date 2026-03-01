using Godot;
using System;

namespace ExtraShoot.scripts.Enemy;

public enum State
{
    Idle,
    Moving,
    AttackingRange
}

public partial class EnemyBase : CharacterBody3D
{
    [Export] protected float LookForTargetIntervalInSec = 0.5f;
    [Export] protected float AggroRangeInMetres = 20f;
    [Export] protected int MovementSpeed = 5;
    [Export] protected int Damage = 5;

    protected Area3D _detectionArea;

    protected NavigationAgent3D _navigationAgent = null;
    protected Player _player;
    protected State _currentState = State.Idle;

    public Vector3 MovementTarget
    {
        get { return _navigationAgent.TargetPosition; }
        set { _navigationAgent.TargetPosition = value; }
    }

    public override void _Ready()
    {
        _navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _navigationAgent.PathDesiredDistance = 0.5f;
        _navigationAgent.TargetDesiredDistance = 0.5f;

        _player = GetTree().CurrentScene.GetNode<Player>("Player");

        _detectionArea = GetNode<Area3D>("EnemyDetectionArea");
        _detectionArea.BodyEntered += OnPlayerDetected;
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (_currentState)
        {
            case State.Idle:
                return;
            case State.Moving:
                ProcessMoving();
                return;
            default:
                return;
        }
    }

    protected virtual void ProcessMoving()
    {
        // without checking this, enemy can go around the corner and move too far from player
        // on its own and stop moving
        // _navigationAgent.IsNavigationFinished(); 
        if (GlobalPosition.DistanceTo(_player.GlobalPosition) > AggroRangeInMetres)
        {
            EnterState(State.Idle);
        }

        MoveCharacter();
    }


    protected virtual void EnterState(State newState)
    {
        switch (newState)
        {
            case State.Idle:
                GD.Print("Entered Idle State");
                _currentState = newState;
                return;
            case State.Moving:
                GD.Print("Entered Move State");
                _currentState = newState;
                return;
            default:
                return;
        }
    }

    protected void MoveCharacter()
    {
        MovementTarget = _player.GlobalPosition;

        Vector3 currentAgentPosition = GlobalTransform.Origin;
        Vector3 nextPathPosition = _navigationAgent.GetNextPathPosition();
        var velocity = currentAgentPosition.DirectionTo(nextPathPosition) * MovementSpeed;

        Velocity = velocity;
        MoveAndSlide();
    }

    protected void Move(Vector3 target)
    {
        MovementTarget = target;
    }

    private void OnPlayerDetected(Node3D body)
    {
        if (body is Player player)
        {
            EnterState(State.Moving);
        }
    }
}
