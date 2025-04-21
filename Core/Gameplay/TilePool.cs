using BattleTaterz.Core.Enums;
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
   public class TilePool
   {
      #region Properties

      /// <summary>
      /// The number of tile objects managed by the pool.
      /// </summary>
      public int PoolSize { get; private set; } = 100;

      /// <summary>
      /// The maximum number of seconds the tile pool is allowed to spin waiting for an available tile.
      /// </summary>
      public static int MaximumWaitInSeconds = 10;

      #endregion

      #region Public Methods

      public TilePool(GameBoard parent, int poolSize)
      {
         PoolSize = poolSize;
         _parentBoardRef = parent;

         // Create the initial tile pool.
         for (int i = 0; i < PoolSize; ++i)
         {
            CreateTile();
         }
      }

      public Tile Pull()
      {
         // Track the amount of time it takes to find an available tile. If it exceeds the max time allowed,
         // generate a new tile for the pool and return it rather than stall the game.
         DateTime startTime = DateTime.Now;

         Tile tile = null;
         while (tile == null)
         {
            tile = _tilePool.Where(t => t.IsAvailable).FirstOrDefault();
            Task.Delay(TimeSpan.FromMilliseconds(1)).Wait(); // just so it doesn't choke the CPU

            if (tile == null && DateTime.Now.Subtract(startTime).TotalSeconds > MaximumWaitInSeconds)
            {
               DebugLogger.Instance.Log($"The tile pool has exceeded the maximum number of seconds ({MaximumWaitInSeconds}) allowed. Creating a new tile for the pool.", LogLevel.Info);
               tile = CreateTile();
               break;
            }
         }

         // Tile pulled. Flag it as unavailable and return.
         tile.MarkUnavailable();
         return tile;
      }

      #endregion

      #region Private Methods

      private Tile CreateTile()
      {
         Tile tile = GD.Load<PackedScene>("res://GameObjectResources/Grid/Tile.tscn").Instantiate<Tile>();
         if (tile == null)
         {
            DebugLogger.Instance.Log("Could not instantiate tile.", LogLevel.Info);
            throw new Exception("Could not instantiate tile.");
         }

         tile.Name = $"Tile{Guid.NewGuid()}";
         tile.OwningBoard = _parentBoardRef;
         _tilePool.Add(tile);
         _parentBoardRef.AddChild(tile);

         return tile;
      }

      #endregion

      #region Private Members

      /// <summary>
      /// A list of Tile objects available for use.
      /// </summary>
      private List<Tile> _tilePool = new List<Tile>();

      /// <summary>
      /// Local ref to the game board for this tile pool.
      /// </summary>
      private GameBoard _parentBoardRef = null;

      #endregion
   }
}
