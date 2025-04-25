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
      /// Should be at least the number of tiles that can be on the board at any given time plus a few extra.
      /// </summary>
      [Export]
      public int PoolSize { get; private set; } = ( (Globals.TileCount * Globals.TileCount) + ( (Globals.TileCount * Globals.TileCount) / 2 ) );

      /// <summary>
      /// The maximum time the tile pool is allowed to spin waiting for an available tile.
      /// </summary>
      public static int MaximumWaitInMs = 100;

      /// <summary>
      /// The number of minutes that need to pass before extra, unused tiles in the pool are discarded.
      /// </summary>
      public static int CleanUpTimeInMinutes = 5;

      /// <summary>
      /// The maximum number of special tiles that are allowed on the board at any given time.
      /// </summary>
      public static int MaxSpecials = 3;

      #endregion

      #region Public Methods

      /// <summary>
      /// Initializes the tile pool. Can only be done once.
      /// </summary>
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

      /// <summary>
      /// Game loop processing of the tile pool.
      /// </summary>
      /// <param name="delta"></param>
      public override void _Process(double delta)
      {
         // Perform periodic clean-up of excess objects in the pool.
         if (DateTime.Now.Subtract(_lastCleanUpTime).TotalMinutes > CleanUpTimeInMinutes && _tilePool.Count > PoolSize)
         {
            var unusedTiles = _tilePool.Where(t => t.IsAvailable)?.ToList<Tile>();
            if (unusedTiles != null && unusedTiles.Any())
            {
               int overflowAmount = _tilePool.Count - PoolSize;

               var tilesToRemove = new List<Tile>();
               for (int count = 0; count < overflowAmount; ++count)
               {
                  tilesToRemove.Add(unusedTiles[count]);
               }

               foreach (var tile in tilesToRemove)
               {
                  _tilePool.Remove(tile);
                  _parentBoardRef.RemoveChild(tile);
                  tile.QueueFree();
               }
            }

            _lastCleanUpTime = DateTime.Now;
         }
      }

      /// <summary>
      /// Yoinks an available tile from the pool.
      /// </summary>
      /// <returns></returns>
      public Tile Pull()
      {
         // Track the amount of time it takes to find an available tile. If it exceeds the max time allowed,
         // generate a new tile for the pool and return it rather than stall the game.
         DateTime startTime = DateTime.Now;

         Tile tile = null;
         while (tile == null)
         {
            tile = _tilePool.Where(t => t.IsAvailable)?.First();
            if (tile == null && DateTime.Now.Subtract(startTime).TotalMilliseconds > MaximumWaitInMs)
            {
               DebugLogger.Instance.Log($"The tile pool has exceeded the maximum number of seconds ({MaximumWaitInMs}) allowed. Creating a new tile for the pool.", LogLevel.Info);
               tile = CreateTile();
               break;
            }
         }

         // Tile pulled. Flag it as unavailable and return.
         tile.MarkUnavailable();

         // Assign behavior to the tile.
         SetTileBehavior(tile);

         return tile;
      }

      /// <summary>
      /// Recycles all tiles marked for recycling. Should only be done when a move is finished processing.
      /// </summary>
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

      /// <summary>
      /// Instantiates a new tile node.
      /// </summary>
      /// <returns></returns>
      /// <exception cref="Exception"></exception>
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

      /// <summary>
      /// Responsible for setting a tile's behavior.
      /// Behavior defines the effect of the tile on gameplay. There is a maximum number of "special"
      /// tiles that can be on the board at any given time. Some will have positive behavior while 
      /// others will have negative behavior.
      /// </summary>
      /// <param name="tile"></param>
      private void SetTileBehavior(Tile tile)
      {
         var activeSpecials = _tilePool.Where(t => !t.IsAvailable && t.Behavior > BehaviorMode.None)?.ToList<Tile>();
         if (activeSpecials.Count() < MaxSpecials)
         {
            //TODO this is a really REALLY simplistic way of determining chance of spawning a double point tile.
            int doublePointsChance = Globals.RNGesus.Next(0, (int)SpecialRate.DoublePoints);
            if (doublePointsChance == 0)
            {
               tile.Behavior = BehaviorMode.DoublePoints;
               tile.ChangeBorder(TileBorder.DoublePoints);
            }
         }
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
      /// The last time a clean-up of extra tiles was performed.
      /// </summary>
      private DateTime _lastCleanUpTime = DateTime.Now;

      /// <summary>
      /// Flag indicating if the pool has been initialized or not. It should only be initialized once.
      /// </summary>
      private bool _initialized = false;

      #endregion
   }
}
