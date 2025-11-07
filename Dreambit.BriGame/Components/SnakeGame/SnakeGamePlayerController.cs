using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dreambit.BriGame.Components;

public class SnakeGamePlayerController : Component
{
    public SnakeHead Head;

    public int TileSize = 40;

    public float SpeedCellsPerSec = 5f;
    public float EpsilonPixels = 0.75f;

    public Point HeadCell;
    public Point Direction;
    public Point QueuedTurn;

    public SnakeBody ChildBodyPiece { get; set; }
    
    public Vector2 HeadWorldPosition => Transform.WorldPosToVec2;
    public Vector2 TargetCellCenter;
    public Point TargetCell => WorldToCell(TargetCellCenter);
    
    public GridManager GridManager;
    
    // Helpers
    static bool IsOpposite(Point a, Point b) => (a.X == -b.X && a.Y == -b.Y);
    static bool IsCardinal(Point p) => (p.X == 0) ^ (p.Y == 0);
    static Point Up    => new Point(0, -1);
    static Point Down  => new Point(0,  1);
    static Point Left  => new Point(-1, 0);
    static Point Right => new Point(1,  0);

    public override void OnAddedToEntity()
    {
        HeadCell = new Point(3, 0);
        Direction = new Point(1, 0);
        Transform.Position = CellCenterToWorld(HeadCell).ToVector3();
        TargetCellCenter = CellCenterToWorld(GetNextCell());

        Head = Entity.GetComponent<SnakeHead>();
        
        GridManager = Scene.FindEntity("gridManager").GetComponent<GridManager>();
    }

    public override void OnUpdate()
    {
        HandleInput();
        float speedPx = SpeedCellsPerSec * TileSize;
        var toTarget = TargetCellCenter - HeadWorldPosition;
        float dist = toTarget.Length();

        // 1) move head world position towards current target center
        if (dist > 0f)
        {
            var step = MathF.Min(speedPx * Time.DeltaTime, dist);
            Transform.Position += (toTarget.ToVector3() / dist) * step;
        }
        
        // 2) if we reached the intersection, snap and advance the grid step
        if (Vector2.DistanceSquared(HeadWorldPosition, TargetCellCenter) <= EpsilonPixels * EpsilonPixels)
        {
            Transform.Position = TargetCellCenter.ToVector3();
            HeadCell = GetNextCell();

            if (QueuedTurn != Point.Zero && !IsOpposite(Direction, QueuedTurn))
                Direction = QueuedTurn;

            QueuedTurn = Point.Zero;
            TargetCellCenter = CellCenterToWorld(GetNextCell());

            GridManager.CheckForFood(HeadCell);
        }

        Head.SetDirection(Direction);
    }

    public void HandleInput()
    {
        if (Input.IsKeyHeld(Keys.Up))        { TryQueueTurn(Up);    return; }
        if (Input.IsKeyHeld(Keys.Down))      { TryQueueTurn(Down);  return; }
        if (Input.IsKeyHeld(Keys.Left))      { TryQueueTurn(Left);  return; }
        if (Input.IsKeyHeld(Keys.Right))     { TryQueueTurn(Right); return; }
    }

    private Point WorldToCell(Vector2 worldPosition)
    {
        return new Point((int)Mathf.Floor(worldPosition.X / TileSize), (int)Mathf.Floor(worldPosition.Y / TileSize));
    }

    private Vector2 CellCenterToWorld(Point cell)
    {
        return new Vector2((cell.X + 0.5f) * TileSize, (cell.Y + 0.5f) * TileSize);
    }

    public Point GetNextCell()
    {
        return HeadCell + Direction;
    }
    
    void TryQueueTurn(Point desired)
    {
        if (!IsCardinal(desired)) return;
        if (IsOpposite(Direction, desired)) return; // no instant 180s
        // Only buffer if it changes axis (so it’ll apply cleanly at the next cell center)
        if ((Direction.X == 0 && desired.X != 0) || (Direction.Y == 0 && desired.Y != 0))
            QueuedTurn = desired;
    }
}