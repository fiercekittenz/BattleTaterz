using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.VFX
{
   public partial class DropEffect : Godot.AnimatedSprite2D
   {
      public override void _Ready()
      {
         Hide();
      }

      public void Animate(Godot.Vector2 location)
      {
         Position = new Godot.Vector2(Globals.TileSize / 2, Globals.TileSize / 2);
         Show();

         var finalPosition = new Godot.Vector2(Position.X, Position.Y - Globals.TileSize);

         var droptween = GetTree().CreateTween();
         droptween.SetParallel(true);
         droptween.TweenProperty(this, "position", finalPosition, 0.3f).SetEase(Godot.Tween.EaseType.Out);
         droptween.Finished += (() => {
            droptween.Kill();
            Hide();
         });
      }
   }
}
