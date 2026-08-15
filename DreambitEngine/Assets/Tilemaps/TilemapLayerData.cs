#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

public enum TilemapRenderOrder
{
    RightDown,
    RightUp,
    LeftDown,
    LeftUp
}

public readonly record struct TilemapAnimationFrame(
    Rectangle SourceRectangle,
    int DurationMilliseconds,
    Texture2D? Texture = null);

public sealed class TilemapAnimation
{
    private readonly TilemapAnimationFrame[] _frames;

    public TilemapAnimation(IEnumerable<TilemapAnimationFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        _frames = [..frames];
        if (_frames.Length == 0)
            throw new ArgumentException("A tile animation must contain at least one frame.", nameof(frames));

        var totalDuration = 0;
        foreach (var frame in _frames)
        {
            if (frame.SourceRectangle.Width <= 0 || frame.SourceRectangle.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(frames), "Tile animation frames need positive source rectangles.");
            if (frame.DurationMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(frames), "Tile animation frame durations must be positive.");
            totalDuration = checked(totalDuration + frame.DurationMilliseconds);
        }

        TotalDurationMilliseconds = totalDuration;
    }

    public IReadOnlyList<TilemapAnimationFrame> Frames => _frames;
    public int TotalDurationMilliseconds { get; }

    public TilemapAnimationFrame GetFrame(float elapsedMilliseconds)
    {
        if (!float.IsFinite(elapsedMilliseconds))
            elapsedMilliseconds = 0f;
        var playhead = elapsedMilliseconds % TotalDurationMilliseconds;
        if (playhead < 0f)
            playhead += TotalDurationMilliseconds;

        foreach (var frame in _frames)
        {
            if (playhead < frame.DurationMilliseconds)
                return frame;
            playhead -= frame.DurationMilliseconds;
        }

        return _frames[^1];
    }
}

/// <summary>
/// One tile prepared for Dreambit's renderer. Positions and sizes are expressed
/// in world units and are local to the entity that owns the tilemap renderer.
/// </summary>
public readonly record struct TilemapTile(
    Vector2 Position,
    Vector2 Size,
    Rectangle SourceRectangle,
    Color Tint,
    SpriteEffects Effects = SpriteEffects.None,
    Texture2D? Texture = null,
    float Rotation = 0f,
    TilemapAnimation? Animation = null,
    Point? Cell = null)
{
    public RectangleF Bounds => new(Position.X, Position.Y, Size.X, Size.Y);
}

/// <summary>
/// Renderer-ready tile layer data with a fixed spatial grid. This model has no
/// dependency on a specific map editor and can be populated by any importer.
/// </summary>
public sealed class TilemapLayerData
{
    private static readonly IReadOnlyList<TilemapTile> EmptyCell = Array.Empty<TilemapTile>();
    private readonly Dictionary<long, List<TilemapTile>> _tilesByCell = [];

    public TilemapLayerData(
        int columns,
        int rows,
        Vector2 cellSize,
        IEnumerable<TilemapTile> tiles,
        TilemapRenderOrder renderOrder = TilemapRenderOrder.RightDown)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentNullException.ThrowIfNull(tiles);

        if (!float.IsFinite(cellSize.X) || cellSize.X <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell width must be positive and finite.");
        if (!float.IsFinite(cellSize.Y) || cellSize.Y <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell height must be positive and finite.");

        Columns = columns;
        Rows = rows;
        CellSize = cellSize;
        RenderOrder = renderOrder;
        var gridWidth = columns * cellSize.X;
        var gridHeight = rows * cellSize.Y;
        var orderedTiles = new List<TilemapTile>();
        var maxTileWidth = 0f;
        var maxTileHeight = 0f;
        var minTileLeft = 0f;
        var minTileTop = 0f;
        var maxTileRight = gridWidth;
        var maxTileBottom = gridHeight;
        var minimumTileOffsetX = 0f;
        var minimumTileOffsetY = 0f;
        var maximumTileExtentX = cellSize.X;
        var maximumTileExtentY = cellSize.Y;

        foreach (var tile in tiles)
        {
            ValidateTile(tile);

            var column = tile.Cell?.X ?? (int)MathF.Floor(tile.Position.X / cellSize.X);
            var row = tile.Cell?.Y ?? (int)MathF.Floor(tile.Position.Y / cellSize.Y);
            if (column < 0 || column >= columns || row < 0 || row >= rows)
                throw new ArgumentOutOfRangeException(
                    nameof(tiles),
                    $"Tile at {tile.Position} belongs to cell ({column}, {row}) outside the {columns}x{rows} tilemap grid.");

            var cellKey = GetCellKey(column, row);
            if (!_tilesByCell.TryGetValue(cellKey, out var cellTiles))
            {
                cellTiles = [];
                _tilesByCell.Add(cellKey, cellTiles);
            }
            cellTiles.Add(tile);
            orderedTiles.Add(tile);
            maxTileWidth = MathF.Max(maxTileWidth, tile.Size.X);
            maxTileHeight = MathF.Max(maxTileHeight, tile.Size.Y);
            minTileLeft = MathF.Min(minTileLeft, tile.Position.X);
            minTileTop = MathF.Min(minTileTop, tile.Position.Y);
            maxTileRight = MathF.Max(maxTileRight, tile.Position.X + tile.Size.X);
            maxTileBottom = MathF.Max(maxTileBottom, tile.Position.Y + tile.Size.Y);
            var cellOriginX = column * cellSize.X;
            var cellOriginY = row * cellSize.Y;
            minimumTileOffsetX = MathF.Min(minimumTileOffsetX, tile.Position.X - cellOriginX);
            minimumTileOffsetY = MathF.Min(minimumTileOffsetY, tile.Position.Y - cellOriginY);
            maximumTileExtentX = MathF.Max(maximumTileExtentX, tile.Position.X + tile.Size.X - cellOriginX);
            maximumTileExtentY = MathF.Max(maximumTileExtentY, tile.Position.Y + tile.Size.Y - cellOriginY);
        }

        Bounds = new RectangleF(
            minTileLeft,
            minTileTop,
            maxTileRight - minTileLeft,
            maxTileBottom - minTileTop);
        Tiles = orderedTiles.ToArray();
        MaximumTileSize = new Vector2(maxTileWidth, maxTileHeight);
        MinimumTileOffset = new Vector2(minimumTileOffsetX, minimumTileOffsetY);
        MaximumTileExtent = new Vector2(maximumTileExtentX, maximumTileExtentY);
    }

    public int Columns { get; }
    public int Rows { get; }
    public Vector2 CellSize { get; }
    public RectangleF Bounds { get; }
    public IReadOnlyList<TilemapTile> Tiles { get; }
    public Vector2 MaximumTileSize { get; }
    public Vector2 MinimumTileOffset { get; }
    public Vector2 MaximumTileExtent { get; }
    public TilemapRenderOrder RenderOrder { get; }
    public int TileCount => Tiles.Count;

    public IReadOnlyList<TilemapTile> GetTiles(int column, int row)
    {
        if (column < 0 || column >= Columns)
            throw new ArgumentOutOfRangeException(nameof(column));
        if (row < 0 || row >= Rows)
            throw new ArgumentOutOfRangeException(nameof(row));

        return _tilesByCell.TryGetValue(GetCellKey(column, row), out var tiles)
            ? tiles
            : EmptyCell;
    }

    public bool TryGetVisibleCellRange(
        RectangleF localView,
        out int minimumColumn,
        out int minimumRow,
        out int maximumColumn,
        out int maximumRow)
    {
        if (!Bounds.Intersects(localView) || TileCount == 0)
        {
            minimumColumn = minimumRow = maximumColumn = maximumRow = 0;
            return false;
        }

        minimumColumn = Math.Clamp(
            (int)MathF.Floor((localView.Left - MaximumTileExtent.X) / CellSize.X),
            0,
            Columns - 1);
        minimumRow = Math.Clamp(
            (int)MathF.Floor((localView.Top - MaximumTileExtent.Y) / CellSize.Y),
            0,
            Rows - 1);
        maximumColumn = Math.Clamp(
            (int)MathF.Floor((localView.Right - MinimumTileOffset.X) / CellSize.X),
            0,
            Columns - 1);
        maximumRow = Math.Clamp(
            (int)MathF.Floor((localView.Bottom - MinimumTileOffset.Y) / CellSize.Y),
            0,
            Rows - 1);
        return true;
    }

    private long GetCellKey(int column, int row) => (long)row * Columns + column;

    private static void ValidateTile(TilemapTile tile)
    {
        if (!float.IsFinite(tile.Position.X) || !float.IsFinite(tile.Position.Y))
            throw new ArgumentOutOfRangeException(nameof(tile), "Tile position must be finite.");
        if (!float.IsFinite(tile.Size.X) || tile.Size.X <= 0f ||
            !float.IsFinite(tile.Size.Y) || tile.Size.Y <= 0f)
            throw new ArgumentOutOfRangeException(nameof(tile), "Tile size must be positive and finite.");
        if (tile.SourceRectangle.Width <= 0 || tile.SourceRectangle.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(tile), "Tile source rectangle must have a positive size.");
        if (!float.IsFinite(tile.Rotation))
            throw new ArgumentOutOfRangeException(nameof(tile), "Tile rotation must be finite.");
    }
}
