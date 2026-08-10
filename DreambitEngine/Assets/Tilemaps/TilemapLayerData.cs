#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dreambit;

/// <summary>
/// One tile prepared for Dreambit's renderer. Positions and sizes are expressed
/// in world units and are local to the entity that owns the tilemap renderer.
/// </summary>
public readonly record struct TilemapTile(
    Vector2 Position,
    Vector2 Size,
    Rectangle SourceRectangle,
    Color Tint,
    SpriteEffects Effects = SpriteEffects.None)
{
    public RectangleF Bounds => new(Position.X, Position.Y, Size.X, Size.Y);
}

/// <summary>
/// Renderer-ready tile layer data with a fixed spatial grid. This model has no
/// dependency on LDtk and can be populated by any map importer.
/// </summary>
public sealed class TilemapLayerData
{
    private static readonly IReadOnlyList<TilemapTile> EmptyCell = Array.Empty<TilemapTile>();
    private readonly List<TilemapTile>?[] _tilesByCell;

    public TilemapLayerData(
        int columns,
        int rows,
        Vector2 cellSize,
        IEnumerable<TilemapTile> tiles)
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
        var gridWidth = columns * cellSize.X;
        var gridHeight = rows * cellSize.Y;
        _tilesByCell = new List<TilemapTile>?[checked(columns * rows)];

        var orderedTiles = new List<TilemapTile>();
        var maxTileWidth = 0f;
        var maxTileHeight = 0f;
        var maxTileRight = gridWidth;
        var maxTileBottom = gridHeight;

        foreach (var tile in tiles)
        {
            ValidateTile(tile);

            var column = (int)MathF.Floor(tile.Position.X / cellSize.X);
            var row = (int)MathF.Floor(tile.Position.Y / cellSize.Y);
            if (column < 0 || column >= columns || row < 0 || row >= rows)
                throw new ArgumentOutOfRangeException(
                    nameof(tiles),
                    $"Tile at {tile.Position} lies outside the {columns}x{rows} tilemap grid.");

            var cellIndex = row * columns + column;
            (_tilesByCell[cellIndex] ??= []).Add(tile);
            orderedTiles.Add(tile);
            maxTileWidth = MathF.Max(maxTileWidth, tile.Size.X);
            maxTileHeight = MathF.Max(maxTileHeight, tile.Size.Y);
            maxTileRight = MathF.Max(maxTileRight, tile.Position.X + tile.Size.X);
            maxTileBottom = MathF.Max(maxTileBottom, tile.Position.Y + tile.Size.Y);
        }

        Bounds = new RectangleF(0f, 0f, maxTileRight, maxTileBottom);
        Tiles = orderedTiles.ToArray();
        MaximumTileSize = new Vector2(maxTileWidth, maxTileHeight);
    }

    public int Columns { get; }
    public int Rows { get; }
    public Vector2 CellSize { get; }
    public RectangleF Bounds { get; }
    public IReadOnlyList<TilemapTile> Tiles { get; }
    public Vector2 MaximumTileSize { get; }
    public int TileCount => Tiles.Count;

    public IReadOnlyList<TilemapTile> GetTiles(int column, int row)
    {
        if (column < 0 || column >= Columns)
            throw new ArgumentOutOfRangeException(nameof(column));
        if (row < 0 || row >= Rows)
            throw new ArgumentOutOfRangeException(nameof(row));

        return _tilesByCell[row * Columns + column] ?? EmptyCell;
    }

    /// <summary>
    /// Finds the grid cells that could contain tiles overlapping the supplied
    /// local-space view. The range is expanded for tiles larger than a cell.
    /// </summary>
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
            (int)MathF.Floor((localView.Left - MaximumTileSize.X) / CellSize.X),
            0,
            Columns - 1);
        minimumRow = Math.Clamp(
            (int)MathF.Floor((localView.Top - MaximumTileSize.Y) / CellSize.Y),
            0,
            Rows - 1);
        maximumColumn = Math.Clamp(
            (int)MathF.Floor(localView.Right / CellSize.X),
            0,
            Columns - 1);
        maximumRow = Math.Clamp(
            (int)MathF.Floor(localView.Bottom / CellSize.Y),
            0,
            Rows - 1);
        return true;
    }

    private static void ValidateTile(TilemapTile tile)
    {
        if (!float.IsFinite(tile.Position.X) || !float.IsFinite(tile.Position.Y))
            throw new ArgumentOutOfRangeException(nameof(tile), "Tile position must be finite.");
        if (!float.IsFinite(tile.Size.X) || tile.Size.X <= 0f ||
            !float.IsFinite(tile.Size.Y) || tile.Size.Y <= 0f)
            throw new ArgumentOutOfRangeException(nameof(tile), "Tile size must be positive and finite.");
        if (tile.SourceRectangle.Width <= 0 || tile.SourceRectangle.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(tile), "Tile source rectangle must have a positive size.");
    }
}
