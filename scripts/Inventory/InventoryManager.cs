using Godot;
using System;

namespace ExtraShoot.scripts.Inventory;

public partial class InventoryManager : Node
{
	public static InventoryManager Instance { get; private set; }
	public InventoryGrid Grid { get; private set; }

	[Signal]
	public delegate void InventoryChangedEventHandler();

    public override void _Ready()
    {
        Instance = this;
		Grid = new InventoryGrid(4, 3);
    }

	public bool TryPlaceItem(InventoryItem item, int x, int y)
	{
		var success = Grid.PlaceItem(item, x, y);
		if (success)
		{
			item.GridPositionX = x;
			item.GridPositionY = y;			
			EmitSignal(SignalName.InventoryChanged);
		}

		return success;
	}

	public void RemoveItem(InventoryItem item)
	{
		Grid.RemoveItem(item);
		EmitSignal(SignalName.InventoryChanged);
	}

	public void ReturnItemToSlotBeforeDrag(InventoryItem item)
	{
		TryPlaceItem(item, item.GridPositionX, item.GridPositionY);
	}
}
