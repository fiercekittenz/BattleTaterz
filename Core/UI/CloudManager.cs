using BattleTaterz.Core;
using BattleTaterz.Core.UI;
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

public partial class CloudManager : Node2D
{
   public GameScene GameSceneRef { get; private set; } = null;

   [Export]
   public int MaxClouds { get; set; } = 5;

   [Export]
   public int PoolSize { get; set; } = 10;

   public override void _Ready()
   {
      // Create a pool of sprites that can be recycled for the clouds.
      for (int i = 0; i < PoolSize; ++i)
      {
         AnimatedCloud cloud = new AnimatedCloud();
         cloud.Name = $"Cloud{i}";
         AddChild(cloud);

         _cloudPool.Add(cloud);
      }
   }

   public void Initialize(GameScene gameScene)
   {
      GameSceneRef = gameScene;
      _spawnTimer = GetNode<Timer>("CloudSpawnTimer");
      _spawnTimer.Timeout += _spawnTimer_Timeout;
   }

   public void SpawnCloud(AnimatedCloud cloud)
   {
      if (GameSceneRef != null)
      {
         // Halt availability.
         cloud.IsAvailable = false;

         cloud.SpriteFrames = GD.Load<SpriteFrames>("res://GameObjectResources/clouds.tres");
         cloud.Frame = Globals.RNGesus.Next(MaxClouds);

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

   private void _spawnTimer_Timeout()
   {
      //TODO re-enable when I have time
      //int spawnChance = Globals.RNGesus.Next(0, 4);
      //if (spawnChance % 3 == 0)
      //{
      //   AnimatedCloud cloud = null;
      //   while (cloud == null)
      //   {
      //      cloud = _cloudPool.Where(c => c.IsAvailable).FirstOrDefault();
      //      Task.Delay(TimeSpan.FromMilliseconds(1)).Wait(); // just so it doesn't choke the CPU
      //   }

      //   SpawnCloud(cloud);
      //}
   }

   private System.Collections.Generic.List<AnimatedCloud> _cloudPool = new System.Collections.Generic.List<AnimatedCloud>();

   private Godot.Timer _spawnTimer = null;
}
