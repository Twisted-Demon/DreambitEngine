using LDtk;
using Microsoft.Xna.Framework;

namespace PixelariaEngine.ECS;

public class AStarGrid : Component
{
    private Node[,] _nodes;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int CellSize { get; private set; }

    public AStarGrid InitializeGrid( LDtkIntGrid collisionGrid, int xOffset = 0, int yOffset = 0)
    {
        Width = collisionGrid.GridSize.X;
        Height = collisionGrid.GridSize.Y;
        CellSize = collisionGrid.TileSize;
        _nodes = new Node[Width, Height];

        for (var i = 0; i < collisionGrid.Values.Length; i++)
        {
            var nodePos = i.IndexToPoint(Width, Height);

            var isWalkable = collisionGrid.Values[i] == 0;

            _nodes[nodePos.X, nodePos.Y] = new Node(nodePos.X, nodePos.Y, isWalkable);
        }
        
        return this;
    }

    public void SetWalkable(int x, int y, bool isWalkable)
    {
        if (IsInBounds(x, y))
            _nodes[x, y] = new Node(x, y, isWalkable);
    }
    
    public bool IsInBounds(int x, int y) => 
        x >= 0 && x < Width && y >= 0 && y < Height;

    public override void OnDebugDraw()
    {
        //foreach (var node in _nodes)
        //{
        //    var pos = new Vector2(node.X * CellSize , node.Y * CellSize);
////
        //    if (node.IsWalkable)
        //        Core.SpriteBatch.DrawPoint(pos, Color.Blue, 1f);
        //    else 
        //        Core.SpriteBatch.DrawPoint(pos, Color.Red, 1f);
        //}
    }

    public Node GetNode(int x, int y)
    {
        return IsInBounds(x, y) ? _nodes[x, y] : default;
    }

    public bool IsWalkable(Vector2 position)
    {
        var posX = Mathf.FloorToInt(position.X / CellSize);
        var posY = Mathf.FloorToInt(position.Y / CellSize);
        
        var posToCheck = new Point(posX, posY);
        
        var node = GetNode(posToCheck.X, posToCheck.Y);

        return node.IsWalkable;
    }
}