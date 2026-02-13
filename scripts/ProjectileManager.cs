using Godot;
using System;

public partial class ProjectileManager : Node
{	
	public static PackedScene Projectile;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Projectile = GD.Load<PackedScene>("res://prefabs/projectile.tscn");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void SpawnProjectile()
	{
		var projectile = Projectile.Instantiate();
		GetTree().Root.AddChild(projectile);	
	}
}
