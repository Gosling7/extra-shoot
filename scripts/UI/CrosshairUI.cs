using Godot;
using System;

namespace ExtraShoot.scripts.UI;

public partial class CrosshairUI : Control
{
	private Player _player;
	private TextureRect _crosshairUp;
	private TextureRect _crosshairDown;
	private TextureRect _crosshairLeft;
	private TextureRect _crosshairRight;

	private Vector2 _crosshairUpDefaultPosition;
	private Vector2 _crosshairDownDefaultPosition;
	private Vector2 _crosshairLeftDefaultPosition;
	private Vector2 _crosshairRightDefaultPosition;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_player = GetTree().CurrentScene.GetNode<Player>("Player");

		_crosshairUp = GetNode<TextureRect>("up");
		_crosshairDown = GetNode<TextureRect>("down");
		_crosshairLeft = GetNode<TextureRect>("left");
		_crosshairRight = GetNode<TextureRect>("right");

		_crosshairUpDefaultPosition = _crosshairUp.Position;
		_crosshairDownDefaultPosition = _crosshairDown.Position;
		_crosshairLeftDefaultPosition = _crosshairLeft.Position;
		_crosshairRightDefaultPosition = _crosshairRight.Position;

		_player.AimSpreadChanged += OnAimSpreadChanged;
		_player.CrosshairVisibilityChanged += OnCrosshairVisibilityChanged;
	}

	private void OnAimSpreadChanged(float spread)
	{
		UpdateCrosshair(spread);
	}

	private void OnCrosshairVisibilityChanged(bool isVisible)
	{
		if (isVisible)
		{
			Input.MouseMode = Input.MouseModeEnum.Hidden;
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		Visible = isVisible;
	}

	private void UpdateCrosshair(float spread)
	{
		var mousePosition = GetViewport().GetMousePosition();

		Position = mousePosition - Size / 2f;

		var spreadMultiplier = 500f;
		var crosshairSpread = spread * spreadMultiplier;

		_crosshairUp.Position = _crosshairUpDefaultPosition + new Vector2(0, -crosshairSpread);
		_crosshairDown.Position = _crosshairDownDefaultPosition + new Vector2(0, crosshairSpread);
		_crosshairLeft.Position = _crosshairLeftDefaultPosition + new Vector2(-crosshairSpread, 0);
		_crosshairRight.Position = _crosshairRightDefaultPosition + new Vector2(crosshairSpread, 0);
	}
}
