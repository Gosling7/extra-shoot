using Godot;
using System;

namespace ExtraShoot.scripts.UI;

public partial class AmmoUI : Node3D
{
	private Player _player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_player = GetParent<Player>();

		_player.UpdateAmmoUI += OnUpdateAmmoUI;
	}

	private void OnUpdateAmmoUI()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
