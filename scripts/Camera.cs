using Godot;
using System;

public partial class Camera : Camera3D
{
    [Export] public Vector3 Offset = new(0, 10, 10);
    [Export] public float FollowSpeed = 5f;

    private Node3D _playerNode;
    private Node3D _cameraPivotNode;

    public override void _Ready()
    {
		_playerNode = GetTree().CurrentScene.GetNode<Player>("Player");
		_cameraPivotNode = GetTree().CurrentScene.GetNode<Node3D>("CameraPivot");
    }

    public override void _Process(double delta)
    {
        if (_playerNode == null) return;

        Vector3 targetPos = _playerNode.GlobalPosition;
        _cameraPivotNode.Position = targetPos;
    }
}
