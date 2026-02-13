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

    public InventoryItem(string id, int width, int height)
    {
        Id = id;
        Width = width;
        Height = height;

        DebugColor = new Color("green");
    }

    public void Rotate()
    {
        Width = Height;
        Height = Width;
    }
}