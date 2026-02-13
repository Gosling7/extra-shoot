using ExtraShoot.scripts.Inventory;
using Godot;
using System;

namespace ExtraShoot.scripts.UI;

public partial class InventoryItemUI : Control
{
	public InventoryItem Item { get; private set; }

	private InventoryUI _inventoryUI;
	private const int CellSize = 32;
	private int _originalX;
	private int _originalY;

	public override void _Ready()
	{
		_inventoryUI = GetParent().GetParent<InventoryUI>();
	}

	public void Initialize(InventoryItem item)
	{
		Item = item;
		UpdateSize();
	}

	private void UpdateSize()
	{
		CustomMinimumSize = new Vector2(
			Item.Width * CellSize,
			Item.Height * CellSize
		);
		Size = CustomMinimumSize;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		//var preview = Duplicate() as Control;
		// tymczasowe, mozna zmienic na new TextureRect()
		var preview = new ColorPickerButton
		{
			Color = new Color("blue"),
			Size = new Vector2(50, 50),
			MouseFilter = MouseFilterEnum.Ignore
		};

		SetDragPreview(preview);
		_inventoryUI.DraggedItem = Item;
		InventoryManager.Instance.RemoveItem(Item);
		//GD.Print(Item);
		return (Variant)Item;
	}
}
