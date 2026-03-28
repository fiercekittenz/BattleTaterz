using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay.TileBehaviors
{
   /// <summary>
   /// Extended trigger result for the Chomp Tater behavior.
   /// Carries animation data so GameBoard._Process() can create
   /// the tween when the correct round starts processing.
   /// </summary>
   public class ChompTaterTriggerResult : TriggerResult
   {
      public Godot.Node2D ChompTaterNode { get; set; }
      public Godot.Vector2 ChompStartPosition { get; set; }
      public Godot.Vector2 ChompEndPosition { get; set; }
      public float ChompFadeInDuration { get; set; }
      public float ChompTraversalDuration { get; set; }
      public float ChompFadeOutDuration { get; set; }
   }
}
