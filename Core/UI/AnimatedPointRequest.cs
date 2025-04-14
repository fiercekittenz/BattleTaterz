using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.UI
{
   /// <summary>
   /// A single request for displaying animated points that are placed in a queue
   /// and processed as animated point labels become available in the pool.
   /// </summary>
   public class AnimatedPointRequest
   {
      public Godot.Vector2 Position { get; set; } = new Godot.Vector2(0, 0);

      public int Value { get; set; } = 0;
   }
}
