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
   public partial class TilePool : Node2D
   {
      #region Properties

      /// <summary>
      /// The number of tile objects managed by the pool.
      /// </summary>
      [Export]
      public int PoolSize { get; private set; } = (Globals.TileCount * Globals.TileCount) * 2;

      /// <summary>
      /// The maximum time the tile pool is allowed to spin waiting for an available tile.
      /// </summary>
      public static int MaximumWaitInMs = 1000;

      #endregion

      #region Public Methods

      public void Initialize()
      {
         if (!_initialized)
         {
            _parentBoardRef = GetParent<GameBoard>();

            // Create the initial tile pool.
            for (int i = 0; i < PoolSize; ++i)
            {
               CreateTile();
            }

            _initialized = true;
         }
      }

      public override void _Process(double delta)
      {
         //TODO - handle any clean-up of tiles that aren't in use and exceed the max pool size.
      }

      public Tile Pull()
      {
         // Track the amount of time it takes to find an available tile. If it exceeds the max time allowed,
         // generate a new tile for the pool and return it rather than stall the game.
         DateTime startTime = DateTime.Now;

         Tile tile = null;
         while (tile == null)
         {
            tile = _tilePool.Where(t => t.IsAvailable && !t.RecyclePostMove).FirstOrDefault();
            if (tile == null && DateTime.Now.Subtract(startTime).TotalMilliseconds > MaximumWaitInMs)
            {
               DebugLogger.Instance.Log($"The tile pool has exceeded the maximum number of seconds ({MaximumWaitInMs}) allowed. Creating a new tile for the pool.", LogLevel.Info);
               tile = CreateTile();
               break;
            }
         }

         // Tile pulled. Flag it as unavailable and return.
         tile.MarkUnavailable();
         return tile;
      }

      public void DoRecycle()
      {
         foreach (var tile in _tilePool)
         {
            if (tile.RecyclePostMove)
            {
               tile.Recycle();
            }
         }
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
         tile.GlobalPosition = new Godot.Vector2(-1, -1);
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

      /// <summary>
      /// Flag indicating if the pool has been initialized or not. It should only be initialized once.
      /// </summary>
      private bool _initialized = false;

      #endregion
   }
}
