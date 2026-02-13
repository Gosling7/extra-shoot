using Godot;

namespace ExtraShoot.scripts.Inventory;

public class InventoryGrid
{
    private InventoryItem[,] _grid;

    public int Width { get; }
    public int Height { get; }

    public InventoryGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new InventoryItem[width, height];
    }

    public bool PlaceItem(InventoryItem item, int startX, int startY)
    {
        if (!CanPlaceItem(item, startX, startY))
        {
            return false;
        }

        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                _grid[startX + x, startY + y] = item;
            }
        }

        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (_grid[x, y] == item)
                {
                    _grid[x, y] = null;
                }
            }
        }
    }    

    public InventoryItem GetItem(int x, int y)
    {
        return _grid[x, y];
    }

    private bool CanPlaceItem(InventoryItem item, int itemStartPositionX, int itemStartPositionY)
    {
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {   
                var gridHeight = _grid.GetLength(1);
                var isEnoughSpaceVertically = gridHeight < itemStartPositionY + item.Height;
                if (isEnoughSpaceVertically)
                {
                    //GD.Print("Too long");
                    return false;
                }
                if (_grid[itemStartPositionX + x, itemStartPositionY + y] is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }
}