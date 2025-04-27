using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay.TileBehaviors;
using BattleTaterz.Core.System;
using BattleTaterz.Core.UI;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   /// <summary>
   /// A pool of Tiles that can be recycled for performance.
   /// </summary>
   public partial class TilePool : BaseObjectPool
   {
      #region Properties

      /// <summary>
      /// The number of tile objects managed by the pool.
      /// Should be at least the number of tiles that can be on the board at any given time plus a few extra.
      /// </summary>
      [Export]
      public override int PoolSize { get; protected set; } = ((Globals.TileCount * Globals.TileCount) + ((Globals.TileCount * Globals.TileCount) / 2));

      /// <summary>
      /// The maximum number of special tiles that are allowed on the board at any given time.
      /// </summary>
      public static int MaxSpecials = 3;

      #endregion

      #region Inherited Methods

      /// <summary>
      /// Instantiates a new tile node.
      /// </summary>
      /// <returns></returns>
      protected override PoolObject CreatePoolObject()
      {
         if (!(_parentRef is GameBoard))
         {
            throw new Exception("Parent node is not of type GameBoard.");
         }

         Tile tile = GD.Load<PackedScene>("res://GameObjectResources/Grid/Tile.tscn").Instantiate<Tile>();
         if (tile == null)
         {
            DebugLogger.Instance.Log("Could not instantiate tile.", LogLevel.Info);
            throw new Exception("Could not instantiate tile.");
         }

         tile.Name = $"Tile{Guid.NewGuid()}";
         tile.OwningBoard = _parentRef as GameBoard;
         tile.GlobalPosition = new Godot.Vector2(-1, -1);

         return tile;
      }

      /// <summary>
      /// Responsible for setting a tile's behavior.
      /// Behavior defines the effect of the tile on gameplay. There is a maximum number of "special"
      /// tiles that can be on the board at any given time. Some will have positive behavior while 
      /// others will have negative behavior.
      /// </summary>
      /// <param name="poolObject"></param>
      protected override void ConfigurePulledObject(PoolObject poolObject)
      {
         var activeSpecials = _pool.Where(o => o is Tile t && !t.IsAvailable && t.Behavior != null)?.ToList<PoolObject>();
         if (activeSpecials.Count() < MaxSpecials && poolObject is Tile tile)
         {
            //TODO this is a really REALLY simplistic way of determining chance of spawning a double point tile.
            int doublePointsChance = Globals.RNGesus.Next(0, (int)SpecialRate.DoublePoints);
            if (doublePointsChance == 0)
            {
               tile.Behavior = new DoublePointsBehavior();
               tile.ChangeBorder(TileBorder.DoublePoints);
            }
         }
      }

      #endregion
   }
}
