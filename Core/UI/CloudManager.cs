using BattleTaterz.Core;
using BattleTaterz.Core.System;
using BattleTaterz.Core.UI;
using Godot;
using System;

public partial class CloudManager : BaseObjectPool
{
   [Export]
   public int NumCloudSprites { get; set; } = 5;

   [Export]
   public override int PoolSize { get; protected set; } = 15;

   public override void _Ready()
   {
      base._Ready();

      _spawnTimer = GetNode<Timer>("CloudSpawnTimer");
      _spawnTimer.Timeout += _spawnTimer_Timeout;
   }

   protected override PoolObject CreatePoolObject()
   {
      AnimatedCloud cloud = new AnimatedCloud();
      cloud.Name = $"Cloud{Guid.NewGuid}";
      AddChild(cloud);

      _pool.Add(cloud);

      return cloud;
   }

   private void _spawnTimer_Timeout()
   {
      int spawnChance = Globals.RNGesus.Next(0, 4);
      if (spawnChance % 3 == 0)
      {
         Yoink();
      }
   }

   protected override void ConfigurePulledObject(PoolObject poolObject)
   {
      if (_parentRef is GameScene gameScene && poolObject is AnimatedCloud cloud)
      {
         cloud.Sprite.Frame = Globals.RNGesus.Next(NumCloudSprites);

         Godot.Vector2 cloudDimensions = cloud.Sprite.SpriteFrames.GetFrameTexture("default", cloud.Sprite.Frame).GetSize();

         float positionX = 0 - cloudDimensions.X;
         float positionY = Globals.RNGesus.Next((int)cloudDimensions.Y, (int)(gameScene.GetViewportRect().Size.Y - cloudDimensions.Y));
         cloud.GlobalPosition = new Godot.Vector2(positionX, positionY);

         cloud.Show();

         var tween = GetTree().CreateTween();
         tween.SetParallel(true);
         tween.TweenProperty(cloud, "global_position", new Godot.Vector2(positionX + gameScene.GetViewportRect().Size.X + cloudDimensions.X + 200, positionY), 25.0).SetEase(Tween.EaseType.In);
         tween.Finished += (() =>
         {
            tween.Kill();
            cloud.Hide();
            cloud.Recycle();
         });
      }
   }

   private Godot.Timer _spawnTimer = null;
}
