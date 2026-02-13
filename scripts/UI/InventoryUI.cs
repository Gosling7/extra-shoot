using ExtraShoot.scripts.Inventory;
using Godot;
using System;
using System.Collections.Generic;

namespace ExtraShoot.scripts.UI;

public partial class InventoryUI : Control
{
	[Export]
	public int CellSize { get; set; } = 50;
	public InventoryItem DraggedItem { private get; set; }

	private Control _itemsLayer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// CellSize = 50; // poprzednia properta z jakiegos powodu nie inicjalizowala sie z wartoscia

		// Set minimum size so it receives input		
		CustomMinimumSize = new Vector2(
			InventoryManager.Instance.Grid.Width * CellSize,
			InventoryManager.Instance.Grid.Height * CellSize
		);

		_itemsLayer = GetNode<Control>("ItemsLayer");
		InventoryManager.Instance.InventoryChanged += Refresh;
		QueueRedraw();
		GD.Print("InventoryUI RectSize: ", GetGlobalRect());
	}

	public override void _Draw()
	{
		for (int x = 0; x < InventoryManager.Instance.Grid.Width; x++)
		{
			for (int y = 0; y < InventoryManager.Instance.Grid.Height; y++)
			{
				var rect = new Rect2(x * CellSize, y * CellSize, CellSize, CellSize);

				var item = InventoryManager.Instance.Grid.GetItem(x, y);
				if (item is not null)
				{
					DrawRect(rect, item.DebugColor);
				}

				DrawRect(rect, new Color("black"), false, 1);
			}
		}
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		GD.Print("CanDropData called on InventoryUI");
		if (data.As<InventoryItem>() is null)
		{
			return false;
		}

		int gridX = (int)(atPosition.X / CellSize);
		int gridY = (int)(atPosition.Y / CellSize);
		if (gridX < 0 || gridY < 0)
		{
			GD.Print("CanDropData called on InventoryUI outside grid");
			return false;
		}
		if (gridX >= InventoryManager.Instance.Grid.Width || gridY >= InventoryManager.Instance.Grid.Height)
		{
			GD.Print("CanDropData called on InventoryUI outside grid");
			return false;
		}

		GD.Print("CanDropData called on InventoryUI returned true");
		return true;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		GD.Print("DropData called on InventoryUI at ", atPosition);

		var item = data.As<InventoryItem>();

		int gridX = (int)(atPosition.X / CellSize);
		int gridY = (int)(atPosition.Y / CellSize);

		if (!InventoryManager.Instance.TryPlaceItem(item, gridX, gridY))
		{
			GD.Print("There wasn't enough space, moving item to 0,0");
			InventoryManager.Instance.ReturnItemToSlotBeforeDrag(DraggedItem);
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationDragEnd)
		{
			if (!IsDragSuccessful())
			{
				GD.Print("Drag failed — returning item");
				InventoryManager.Instance.ReturnItemToSlotBeforeDrag(DraggedItem);
			}
		}
	}

	private void Refresh()
	{
		foreach (var child in _itemsLayer.GetChildren())
		{
			child.QueueFree();
		}

		var drawn = new HashSet<InventoryItem>();
		for (int x = 0; x < InventoryManager.Instance.Grid.Width; x++)
		{
			for (int y = 0; y < InventoryManager.Instance.Grid.Height; y++)
			{
				var item = InventoryManager.Instance.Grid.GetItem(x, y);
				if (item is null || drawn.Contains(item))
				{
					continue;
				}

				drawn.Add(item);

				var itemUI = new InventoryItemUI();
				_itemsLayer.AddChild(itemUI);
				itemUI.Initialize(item);
				itemUI.Position = new Vector2(x * CellSize, y * CellSize);
			}
		}
	}
}
