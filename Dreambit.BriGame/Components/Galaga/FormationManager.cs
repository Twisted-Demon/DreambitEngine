using System;
using Dreambit.ECS;
using Microsoft.Xna.Framework;

namespace Dreambit.BriGame.Components.Galaga;

public class FormationManager : Component
{
   public const int Rows = 5;
   public const int Columns = 10;

   private const int SlotSize = 16;
   private const int ColPitch = 20;
   private const int RowPitch = 18;

   private const int GridLeft = 20;
   private const int GridTop = 64;

   private const float BreathePct = 0.06f;
   private const float BreatheSpeed = 0.8f;

   private const float WobbleAmpX = 1.25f;
   private const float WobbleAmpY = 2.00f;
   private const float WobbleSpeedX = 0.8f;
   private const float WobbleSpeedY = 0.8f;
   
   /// <summary>
   /// Boss Row
   /// Butter Fly Row
   /// Butter Fly Row
   /// Bee Row
   /// Bee Row
   /// </summary>
   private readonly FormationSlot[,] _slots = new FormationSlot[Columns, Rows];

   private Vector2 _spawnLeft = new(-40, 322);
   private Vector2 _spawnRight = new(GalagaConstants.GameWidth + 40, 322);

   public override void OnCreated()
   {
      for (var x = 0; x < Columns; x++)
      {
         for (var y = 0; y < Rows; y++)
         {
            _slots[x, y] = new FormationSlot(new Point(x, y)); 
         }
      }
   }

   public override void OnUpdate()
   {
      GalagaSpawner.UpdateSpawner();
   }

   public static Vector2 GetSlotPosition(FormationSlot slot)
   {
      var col = slot.SlotPosition.X;
      var row = slot.SlotPosition.Y;
      
      
      
      return new Vector2(GridLeft + col * ColPitch, GridTop + row * RowPitch);
   }

   public static Vector3 GetSlotWithOffsetPosition(FormationSlot slot)
   {
      var offset = FormationManager.FormationOffsetRadial(slot).ToVector3();
      var pos = GetSlotPosition(slot).ToVector3();
      
      return pos + offset;
   }

   public static Vector2 FormationOffsetRadial(FormationSlot slot)
   {
      var col = slot.SlotPosition.X;
      var row = slot.SlotPosition.Y;
      float t = Time.TimeSinceSceneLoaded;

      // slot center space so scaling is symmetric
      float slotCenterX = GridLeft + col * ColPitch + SlotSize * 0.5f;
      float slotCenterY = GridTop + row * RowPitch + SlotSize * 0.5f;
      
      //Formation center in the same space
      float centerCol = (Columns - 1) * 0.5f;
      float centerRow = (Rows - 1) * 0.5f;
      float fromCenterX = GridLeft + centerCol * ColPitch + SlotSize * 0.5f;
      float fromCenterY = GridTop + centerRow * RowPitch + SlotSize * 0.5f;
      
      //vector from center to this slot
      float dx = slotCenterX - fromCenterX;
      float dy = slotCenterY - fromCenterY;
      
      //breathing scale around 1.0
      float breathe = Mathf.Sin(t * BreatheSpeed);
      float scale = 1f + (BreathePct * breathe);
      
      //delta caused by scling
      float ox = dx * (scale - 1f);
      float oy = dy * (scale - 1f);

      float phase = (col * 0.55f) + (row * 0.35f);
      ox += MathF.Sin(t * WobbleSpeedX + phase) * WobbleAmpX;
      oy += Mathf.Sin(t * WobbleSpeedY + phase) * WobbleAmpY;

      return new Vector2(ox, oy);
   }
   
   public override void OnAddedToEntity()
   {
      foreach (var slot in _slots)
      {
         for (int i = 0; i < 15; i++)
         {
            GalagaSpawner.SpawnBee(slot, _spawnLeft.ToVector3());
         }
      }
   }
   

   public static FormationManager Spawn()
   {
      return ECS.Entity.Create("formationManager", ["manager"]).AttachComponent<FormationManager>();
   }
}

public class FormationSlot
{
   public Point SlotPosition;

   public FormationSlot(Point slotPosition)
   {
      SlotPosition = slotPosition;
   }

   public GalagaEnemy Enemy { get; private set; } = null;
   
   public void SetEnemy(GalagaEnemy enemy)
   {
      if (Enemy is not null) return;
      Enemy = enemy;
   }

   public void DetachEnemy()
   {
      Enemy = null;
   }
}