using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BattleTaterz.Core.UI
{
   /// <summary>
   /// Manages a pool of available labels that will represent animated/floating points.
   /// </summary>
   public partial class AnimatedPointPool : Node2D
   {
      #region Properties

      /// <summary>
      /// The number of animated points managed by the pool.
      /// </summary>
      [Export]
      public int PoolSize { get; private set; } = 20;

      /// <summary>
      /// The maximum time the tile pool is allowed to spin waiting for an available tile.
      /// </summary>
      public static int MaximumWaitInMs = 100;

      /// <summary>
      /// The number of minutes that need to pass before extra, unused tiles in the pool are discarded.
      /// </summary>
      public static int CleanUpTimeInMinutes = 5;

      #endregion

      #region Public Methods

      /// <summary>
      /// Called when the node enters the scene.
      /// </summary>
      public override void _Ready()
      {
         if (!_initialized)
         {
            _parentBoardRef = GetParent<GameBoard>();

            // Create the animated point pool.
            // A pool of objects is used to cycle through, because it is expensive to
            // create, attach, detach, and destroy individual nodes per frame.
            for (int i = 0; i < PoolSize; ++i)
            {
               CreatePoolObject();
            }

            _initialized = true;
         }
      }

      /// <summary>
      /// Game loop processing of the pool.
      /// </summary>
      /// <param name="delta"></param>
      public override void _Process(double delta)
      {
         // Perform periodic clean-up of excess objects in the pool.
         if (DateTime.Now.Subtract(_lastCleanUpTime).TotalMinutes > CleanUpTimeInMinutes && _pool.Count > PoolSize)
         {
            var unusedObjects = _pool.Where(u => u.IsAvailable)?.ToList<AnimatedPoint>();
            if (unusedObjects != null && unusedObjects.Any())
            {
               int overflowAmount = _pool.Count - PoolSize;

               var toRemove = new List<AnimatedPoint>();
               for (int count = 0; count < overflowAmount; ++count)
               {
                  toRemove.Add(unusedObjects[count]);
               }

               foreach (var obj in toRemove)
               {
                  _pool.Remove(obj);
                  _parentBoardRef.RemoveChild(obj);
                  obj.QueueFree();
               }
            }

            _lastCleanUpTime = DateTime.Now;
         }
      }

      /// <summary>
      /// Grabs an available node from the pool and plays the animation.
      /// </summary>
      /// <param name="globalPosition"></param>
      /// <param name="scoreUpdate"></param>
      public void Play(Godot.Vector2 globalPosition, ScoreUpdateResults scoreUpdate)
      {
         // Track the amount of time it takes to find an available point node. If it exceeds the max time allowed,
         // generate a new point node for the pool and return it rather than stall the game.
         DateTime startTime = DateTime.Now;

         //TODO - handle losses

         // Spin until a point node is available for the request.
         AnimatedPoint pointNode = null;
         while (pointNode == null)
         {
            pointNode = _pool.Where(p => p.IsAvailable)?.First();
            if (pointNode == null && DateTime.Now.Subtract(startTime).TotalMilliseconds > MaximumWaitInMs)
            {
               DebugLogger.Instance.Log($"The pool has exceeded the maximum number of seconds ({MaximumWaitInMs}) allowed. Creating a new object for the pool.", LogLevel.Info);
               pointNode = CreatePoolObject();
               break;
            }
         }

         // Display the point node.
         pointNode.Animate(globalPosition, scoreUpdate);
      }

      #endregion

      #region Private Methods

      /// <summary>
      /// Creates a new object for this pool and adds it to the scene.
      /// </summary>
      private AnimatedPoint CreatePoolObject()
      {
         AnimatedPoint animatedPoint = new AnimatedPoint();
         animatedPoint.Name = $"AnimatedPoint{Guid.NewGuid}";
         AddChild(animatedPoint);

         _pool.Add(animatedPoint);

         return animatedPoint;
      }

      #endregion

      #region Private Members

      /// <summary>
      /// A list of AnimatedPoint objects that can be reused for displaying animated point gains or losses.
      /// </summary>
      private List<AnimatedPoint> _pool = new List<AnimatedPoint>();

      /// <summary>
      /// Local ref to the game board for this pool.
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
