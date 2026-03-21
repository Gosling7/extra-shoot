using ExtraShoot.scripts.Interfaces;
using Godot;
using System;

namespace ExtraShoot.scripts;

public partial class Projectile : Area3D
{
	[Export] public float Speed = 35f;

	private Vector3 _direction;
	private int _damage = 10;

	public void Initialize(int damage)
	{
		_damage = damage;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += OnBodyEnter;
		_direction = -GlobalTransform.Basis.Z.Normalized();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += _direction * Speed * (float)delta;
	}

	private void OnBodyEnter(Node3D otherBody)
	{
		if (otherBody is IDamageable damageable)
		{
			damageable.TakeDamage(_damage);
			QueueFree();
		}



		if (otherBody is StaticBody3D staticBody)
		{
			if (staticBody.CollisionLayer == 4)
			{
				QueueFree();
			}
		}
	}
}
