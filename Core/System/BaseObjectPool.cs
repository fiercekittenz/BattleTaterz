using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.System
{
   /// <summary>
   /// Base functionality for an object pool.
   /// </summary>
   public abstract partial class BaseObjectPool : Node2D
   {
      #region Properties

      /// <summary>
      /// The number of objects managed by the pool.
      /// </summary>
      [Export]
      public virtual int PoolSize { get; protected set; } = 10;

      /// <summary>
      /// The maximum time the pool is allowed to spin waiting for an available object.
      /// </summary>
      public virtual int MaximumWaitInMs { get; protected set; } = 100;

      /// <summary>
      /// The number of minutes that need to pass before extra, unused objects in the pool are discarded.
      /// </summary>
      public static int CleanUpTimeInMinutes = 5;

      #endregion

      #region Base Implementation

      /// <summary>
      /// Initializes the object pool. Can only be done once.
      /// </summary>
      public void Initialize()
      {
         if (!_initialized)
         {
            _parentRef = GetParent<Node>();

            // Create the initial object pool.
            for (int i = 0; i < PoolSize; ++i)
            {
               CreatePoolObject();
            }

            _initialized = true;
         }
      }

      /// <summary>
      /// Game loop processing of the object pool.
      /// </summary>
      /// <param name="delta"></param>
      public sealed override void _Process(double delta)
      {
         // Perform periodic clean-up of excess objects in the pool.
         if (DateTime.Now.Subtract(_lastCleanUpTime).TotalMinutes > CleanUpTimeInMinutes && _pool.Count > PoolSize)
         {
            var unusedObjects = _pool.Where(o => o.IsAvailable)?.ToList<PoolObject>();
            if (unusedObjects != null && unusedObjects.Any())
            {
               int overflowAmount = _pool.Count - PoolSize;

               var objectToRemove = new List<PoolObject>();
               for (int count = 0; count < overflowAmount; ++count)
               {
                  objectToRemove.Add(unusedObjects[count]);
               }

               foreach (var currentObject in objectToRemove)
               {
                  _pool.Remove(currentObject);
                  _parentRef.RemoveChild(currentObject);
                  currentObject.QueueFree();
               }
            }

            _lastCleanUpTime = DateTime.Now;
         }
      }

      /// <summary>
      /// Yoinks an available object from the pool.
      /// </summary>
      /// <returns></returns>
      public PoolObject Yoink()
      {
         // Track the amount of time it takes to find an available pool object. If it exceeds the max time allowed,
         // generate a new one for the pool and return it rather than stall the game.
         DateTime startTime = DateTime.Now;

         PoolObject poolObject = null;
         while (poolObject == null)
         {
            poolObject = _pool.Where(o => o.IsAvailable)?.ToList().First();
            if (poolObject == null && DateTime.Now.Subtract(startTime).TotalMilliseconds > MaximumWaitInMs)
            {
               DebugLogger.Instance.Log($"The object pool has exceeded the maximum number of seconds ({MaximumWaitInMs}) allowed. Creating a new object for the pool.", LogLevel.Info);
               poolObject = CreatePoolObject();
               break;
            }
         }

         // Object pulled. Flag it as unavailable and return.
         poolObject.MarkUnavailable();

         // Configure the pulled object.
         ConfigurePulledObject(poolObject);

         return poolObject;
      }

      /// <summary>
      /// Recycles all objects marked for recycling.
      /// </summary>
      public void DoRecycle()
      {
         foreach (var currentObject in _pool)
         {
            if (currentObject.MarkedForRecycling)
            {
               currentObject.Recycle();
            }
         }
      }

      #endregion

      #region Abstract Methods

      /// <summary>
      /// Creates a pool object.
      /// </summary>
      /// <returns></returns>
      protected abstract PoolObject CreatePoolObject();

      /// <summary>
      /// Configures the pulled object.
      /// Used to perform specific actions on derived PoolObjects.
      /// </summary>
      /// <param name="poolObject"></param>
      protected abstract void ConfigurePulledObject(PoolObject poolObject);

      #endregion

      #region Protected Members

      /// <summary>
      /// A list of pool objects available for use.
      /// </summary>
      protected List<PoolObject> _pool = new List<PoolObject>();

      /// <summary>
      /// Local ref to the game board for this object pool.
      /// </summary>
      protected Node _parentRef = null;

      /// <summary>
      /// The last time a clean-up of extra objects was performed.
      /// </summary>
      protected DateTime _lastCleanUpTime = DateTime.Now;

      /// <summary>
      /// Flag indicating if the pool has been initialized or not. It should only be initialized once.
      /// </summary>
      protected bool _initialized = false;

      #endregion
   }
}
