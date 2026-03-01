using Godot;
using System;
using System.Reflection.Emit;

namespace ExtraShoot.scripts.Enemy;

public interface IState
{
    public Vector3 Target { get; set; }
}

class MoveState : IState
{
    public Vector3 Target { get; set; }

    public MoveState(Vector3 target)
    {
        Target = target;
    }
}

class IdleState : IState
{
    public Vector3 Target { get; set; }

    public IdleState()
    {
    }
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
        _detectionArea.BodyExited += OnPlayerLost;

        EnterState(new IdleState());
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_navigationAgent.IsNavigationFinished()
            || GlobalPosition.DistanceTo(_player.GlobalPosition) > AggroRangeInMetres)
        {
            return;
        }

        MoveCharacter();
    }

    protected virtual void EnterState(IState newState)
    {
        switch (newState)
        {
            case IdleState:
                GD.Print("Entered Idle State");
                Idle();
                return;
            case MoveState:
                GD.Print("Entered Move State");
                Move(newState.Target);
                return;
            default:
                return;
        }
    }

    protected virtual void ProcessMove()
    {
        if (_navigationAgent.IsNavigationFinished()
            || GlobalPosition.DistanceTo(_player.GlobalPosition) > AggroRangeInMetres)
        {
            return;
        }

        MoveCharacter();
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


    protected bool CanWalkToPlayer()
    {
        // if (_navigationAgent.IsNavigationFinished())
        // {
        //     return false;
        // }

        // if (_player is null)
        // {
        //     return false;
        // }

        // if (GlobalPosition.DistanceTo(_player.GlobalPosition) > AggroRangeInMetres)
        // {
        //     return false;
        // }

        // return true;
        return !_navigationAgent.IsNavigationFinished()
            || _player is not null
            || GlobalPosition.DistanceTo(_player.GlobalPosition) < AggroRangeInMetres;


        // if (_isShooting)
        // {
        //     return false;
        // }

    }

    protected void Idle()
    {

    }

    protected void Move(Vector3 target)
    {
        // _isShooting = false;
        MovementTarget = target;
    }

    private void OnPlayerDetected(Node3D body)
    {
        if (body is Player player)
        {
            GD.Print("Player Found!");
            var moveState = new MoveState(player.GlobalPosition);

            EnterState(moveState);
        }
    }

    private void OnPlayerLost(Node3D body)
    {
        if (body is Player)
        {
            GD.Print("Player Lost!");
            MovementTarget = Vector3.Zero;

            EnterState(new IdleState());
        }
    }
}
