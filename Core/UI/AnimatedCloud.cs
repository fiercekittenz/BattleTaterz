using BattleTaterz.Core.System;
using Godot;

namespace BattleTaterz.Core.UI
{
   public partial class AnimatedCloud : PoolObject
   {
      public AnimatedSprite2D Sprite { get; private set; } = null;

      public override void _Ready()
      {
         base._Ready();

         Sprite = GD.Load<PackedScene>("res://Scenes/animated_cloud_sprite.tscn").Instantiate<AnimatedSprite2D>();
         AddChild(Sprite);
      }
   }
}
