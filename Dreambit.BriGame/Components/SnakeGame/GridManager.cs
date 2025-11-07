using System;
using System.Collections.Generic;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components;

public class GridManager : Component
{
    public SnakeHead SnakeHead { get; private set; }
    public List<SnakeBody> SnakeBodies { get; private set; } = [];
    public SnakeTail SnakeTail { get; private set; }
    
    public FoodComponent SpawnedFood { get; private set; }

    public int TileSize = 40;


    public override void OnCreated()
    {
        var foodSpawn = GetRandomPoint();
        
        SpawnFood(foodSpawn);
    }

    public override void OnAddedToEntity()
    {
        SpawnPlayer();
    }
    
    public void SpawnBodyPiece(Point cell, Entity parent, Entity child)
    {
        var location = CellToWorld(cell);
        var bodyPieceEntity = Entity.Create("bodyPiece", createAt: location.ToVector3());
        var bodyComponent = bodyPieceEntity.AttachComponent<SnakeBody>();

        bodyComponent.ParentBodyPiece = parent;
        bodyComponent.ChildBodyPiece = child;
    }
    
    public void SpawnPlayer()
    {
        var headSpawn = CellToWorld(new Point(2, 0));
        var headEntity = Scene.CreateEntity("snakeHead", ["player"], createAt: headSpawn.ToVector3());
        SnakeHead = headEntity.AttachComponent<SnakeHead>();
        headEntity.AttachComponent<SnakeGamePlayerController>();
    }

    public void SpawnFood(Point cell)
    {
        if (SpawnedFood is not null)
        {
            Entity.Destroy(SpawnedFood.Entity);
        }
        
        var foodSpawn = CellToWorld(cell);
        var food = Entity.Create("food", createAt: foodSpawn.ToVector3()).AttachComponent<FoodComponent>();
        food.CellPosition = cell;
        SpawnedFood = food;   
    }

    public void CheckForFood(Point cellToCheck)
    {
        if (SpawnedFood is null) return;

        if (SpawnedFood.CellPosition == cellToCheck)
            SpawnFood(GetRandomPoint());
    }

    private Point GetRandomPoint()
    {
        var rand = new Random();
        var pointX = rand.Next(0, 21);
        var pointY = rand.Next(0, 21);
        return new Point(pointX, pointY);
    }
    
    private Point WorldToCell(Vector2 worldPosition)
    {
        return new Point((int)Mathf.Floor(worldPosition.X / TileSize), (int)Mathf.Floor(worldPosition.Y / TileSize));
    }

    private Vector2 CellToWorld(Point cell)
    {
        return new Vector2((cell.X + 0.5f) * TileSize, (cell.Y + 0.5f) * TileSize);
    }
}