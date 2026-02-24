using Godot;
using System;

namespace ExtraShoot.scripts;

public partial class Container : Area3D
{
    private Window _containerWindow;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // InputRayPickable = true;

        // _containerWindow = GetTree().CurrentScene.GetNode<Window>("UI/ContainerWindow");
        // _containerWindow.Visible = false;
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        // if (@event is InputEventMouseButton mouseEvent &&
        //     mouseEvent.Pressed &&
        //     mouseEvent.ButtonIndex == MouseButton.Left)
        // {
        //     GD.Print("Container clicked");
        //     _containerWindow.Visible = !_containerWindow.Visible;
        // }
    }
}
