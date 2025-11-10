using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components;

[Require(typeof(SpriteDrawer))]
public class SnakeBody : Component
{
    public SpriteDrawer Drawer;
    
    public GridManager GridManager;
    
    public Entity ParentBodyPiece { get; set; }
    public Entity ChildBodyPiece { get; set; }

    private SpriteSheet _bottomLeft;
    private SpriteSheet _bottomRight;
    private SpriteSheet _horizontal;
    private SpriteSheet _vertical;
    private SpriteSheet _topRight;
    private SpriteSheet _topLeft;
    
    public int TileSize = 40;

    private SpriteSheet _currentSpriteSheet = null;

    public override void OnAddedToEntity()
    {
        Drawer = Entity.GetComponent<SpriteDrawer>();
        GridManager = Scene.FindEntity("gridManager").GetComponent<GridManager>();

        _bottomLeft = SpriteSheet.Create(1, 1, "Textures/SnakeGame/body_bottomleft");
        _bottomRight = SpriteSheet.Create(1, 1, "Textures/SnakeGame/body_bottomright");
        _horizontal = SpriteSheet.Create(1, 1, "Textures/SnakeGame/body_horizontal");
        _vertical = SpriteSheet.Create(1, 1, "Textures/SnakeGame/body_vertical");
        _topRight = SpriteSheet.Create(1, 1, "Textures/SnakeGame/body_topright");
        _topLeft = SpriteSheet.Create(1, 1, "Textures/SnakeGame/body_topleft");
    }

    public override void OnUpdate()
    {
        HandleSpriteSheets();
    }

    public void HandleSpriteSheets()
    {
        if (ParentBodyPiece == null || ChildBodyPiece == null)
            return; // head/tail special cases handled elsewhere

        var pos       = WorldToCell(Transform.WorldPosToVec2);
        var parentLoc = WorldToCell(ParentBodyPiece.Transform.WorldPosToVec2);
        var childLoc  = WorldToCell(ChildBodyPiece.Transform.WorldPosToVec2);

        var dp = new Point(Math.Sign(parentLoc.X - pos.X), Math.Sign(parentLoc.Y - pos.Y)); // dir to parent
        var dc = new Point(Math.Sign(childLoc.X  - pos.X), Math.Sign(childLoc.Y  - pos.Y)); // dir to child

        // If either neighbor sits on the same cell (during transitions), bail to avoid 0,0 logic.
        if ((dp.X == 0 && dp.Y == 0) || (dc.X == 0 && dc.Y == 0))
            return;

        // Decide sprite using mutually exclusive branches
        if (dp.Y == 0 && dc.Y == 0 && dp.X != 0 && dc.X != 0) // both horizontal
        {
            _currentSpriteSheet = _horizontal;
        }
        else if (dp.X == 0 && dc.X == 0 && dp.Y != 0 && dc.Y != 0) // both vertical
        {
            _currentSpriteSheet = _vertical;
        }
        else
        {
            // Corner: one neighbor on X, the other on Y
            // Map the pair (dp, dc) to a corner. We don't care which is head/tail, only the two axes.
            // Use the sign to pick quadrant:
            int sx = (dp.X != 0 ? dp.X : dc.X); // the X-going neighbor's sign
            int sy = (dp.Y != 0 ? dp.Y : dc.Y); // the Y-going neighbor's sign

            // sx: -1=left, +1=right; sy: -1=up, +1=down
            // Our naming: "topLeft" means the inner corner is at top-left of this tile.
            if (sx < 0 && sy < 0) _currentSpriteSheet = _topLeft;
            else if (sx > 0 && sy < 0) _currentSpriteSheet = _topRight;
            else if (sx < 0 && sy > 0) _currentSpriteSheet = _bottomLeft;
            else /* sx > 0 && sy > 0 */ _currentSpriteSheet = _bottomRight;
        }
        
        Drawer.SpriteSheet = _currentSpriteSheet;
    }

    private Point WorldToCell(Vector2 worldPosition)
    {
        return new Point((int)Mathf.Floor(worldPosition.X / TileSize), (int)Mathf.Floor(worldPosition.Y / TileSize));
    }

    private Vector2 CellCenterToWorld(Point cell)
    {
        return new Vector2((cell.X + 0.5f) * TileSize, (cell.Y + 0.5f) * TileSize);
    }
}