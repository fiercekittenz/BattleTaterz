using BattleTaterz.Core;
using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay;
using BattleTaterz.Core.UI;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

public partial class CloudManager : Node2D
{
   public GameScene GameSceneRef { get; private set; } = null;

   [Export]
   public int NumCloudSprites { get; set; } = 5;

   [Export]
   public int PoolSize { get; set; } = 15;

   /// <summary>
   /// The maximum time the pool is allowed to spin waiting for an available object.
   /// </summary>
   public static int MaximumWaitInMs = 100;

   /// <summary>
   /// The number of minutes that need to pass before extra, unused clouds in the pool are discarded.
   /// </summary>
   public static int CleanUpTimeInMinutes = 5;

   public override void _Ready()
   {
      // Create a pool of sprites that can be recycled for the clouds.
      for (int i = 0; i < PoolSize; ++i)
      {
         CreateCloud();
      }
   }

   /// <summary>
   /// Game loop processing of the cloud object pool.
   /// </summary>
   /// <param name="delta"></param>
   public override void _Process(double delta)
   {
      // Perform periodic clean-up of excess objects in the pool.
      if (DateTime.Now.Subtract(_lastCleanUpTime).TotalMinutes > CleanUpTimeInMinutes && _cloudPool.Count > PoolSize)
      {
         var unusedClouds = _cloudPool.Where(t => t.IsAvailable).DefaultIfEmpty().ToList();
         if (unusedClouds != null && unusedClouds.Any())
         {
            int overflowAmount = _cloudPool.Count - PoolSize;

            var cloudsToRemove = new List<AnimatedCloud>();
            for (int count = 0; count < overflowAmount; ++count)
            {
               cloudsToRemove.Add(unusedClouds[count]);
            }

            foreach (var cloud in cloudsToRemove)
            {
               _cloudPool.Remove(cloud);
               GameSceneRef.RemoveChild(cloud);
               cloud.QueueFree();
            }
         }

         _lastCleanUpTime = DateTime.Now;
      }
   }

   public void Initialize(GameScene gameScene)
   {
      if (!_initialized)
      {
         GameSceneRef = gameScene;
         _spawnTimer = GetNode<Timer>("CloudSpawnTimer");
         _spawnTimer.Timeout += _spawnTimer_Timeout;

         _initialized = true;
      }
   }

   public void SpawnCloud(AnimatedCloud cloud)
   {
      if (GameSceneRef != null)
      {
         // Halt availability.
         cloud.IsAvailable = false;

         cloud.SpriteFrames = GD.Load<SpriteFrames>("res://GameObjectResources/clouds.tres");
         cloud.Frame = Globals.RNGesus.Next(NumCloudSprites);

         Godot.Vector2 cloudDimensions = cloud.SpriteFrames.GetFrameTexture("default", cloud.Frame).GetSize();

         float positionX = 0 - cloudDimensions.X;
         float positionY = Globals.RNGesus.Next((int)cloudDimensions.Y, (int)(GameSceneRef.GetViewportRect().Size.Y - cloudDimensions.Y));
         cloud.GlobalPosition = new Godot.Vector2(positionX, positionY);

         cloud.Show();

         var tween = GetTree().CreateTween();
         tween.SetParallel(true);
         tween.TweenProperty(cloud, "global_position", new Godot.Vector2(positionX + GameSceneRef.GetViewportRect().Size.X + cloudDimensions.X + 200, positionY), 25.0).SetEase(Tween.EaseType.In);
         tween.Finished += (() =>
         {
            tween.Kill();
            cloud.Hide();
            cloud.IsAvailable = true;
         });
      }
   }

   private AnimatedCloud CreateCloud()
   {
      AnimatedCloud cloud = new AnimatedCloud();
      cloud.Name = $"Cloud{Guid.NewGuid}";
      AddChild(cloud);

      _cloudPool.Add(cloud);

      return cloud;
   }

   private void _spawnTimer_Timeout()
   {
      // Track the amount of time it takes to find an available cloud. If it exceeds the max time allowed,
      // generate a new cloud for the pool and return it rather than stall the game.
      DateTime startTime = DateTime.Now;

      int spawnChance = Globals.RNGesus.Next(0, 4);
      if (spawnChance % 3 == 0)
      {
         //TODO - this should be happening in the Process() method, so that it's timed with the game loop (frames). 
         //       Doing it here risks the game locking up or appearing like it's hitching.
         AnimatedCloud cloud = null;
         while (cloud == null)
         {
            cloud = _cloudPool.Where(c => c.IsAvailable).DefaultIfEmpty().First();
            if (cloud == null && DateTime.Now.Subtract(startTime).TotalMilliseconds > MaximumWaitInMs)
            {
               DebugLogger.Instance.Log($"The cloud pool has exceeded the maximum number of seconds ({MaximumWaitInMs}) allowed. Creating a new tile for the pool.", LogLevel.Info);
               cloud = CreateCloud();
               break;
            }
         }

         SpawnCloud(cloud);
      }
   }

   private System.Collections.Generic.List<AnimatedCloud> _cloudPool = new System.Collections.Generic.List<AnimatedCloud>();

   private Godot.Timer _spawnTimer = null;

   /// <summary>
   /// The last time a clean-up of extra tiles was performed.
   /// </summary>
   private DateTime _lastCleanUpTime = DateTime.Now;

   /// <summary>
   /// Flag indicating if the pool has been initialized or not. It should only be initialized once.
   /// </summary>
   private bool _initialized = false;
}
