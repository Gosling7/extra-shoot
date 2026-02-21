using Godot;

namespace ExtraShoot.scripts.Inventory;

public partial class InventoryItem : GodotObject
{
    public string Id { get; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Color DebugColor { get; }
    public int GridPositionX { get; set; }
    public int GridPositionY { get; set; }
    public int MaxStackSize { get; private set; }
    public int CurrentStackSize { get; set; }
    public string Type { get; set; }

    public InventoryItem(
        string id,
        int width,
        int height,
        string debugColorCode = "green",
        int maxStackSize = 1,
        int amounToAdd = 1,
        string type = "None")
    {
        Id = id;
        Width = width;
        Height = height;
        MaxStackSize = maxStackSize;
        CurrentStackSize = amounToAdd;
        Type = type;

        DebugColor = new Color(debugColorCode);
    }

    public void Rotate()
    {
        Width = Height;
        Height = Width;
    }
}