using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core
{
    public static class Globals
   {
      /// <summary>
      /// The minimum number of gems that need to match horizontally or vertically.
      /// </summary>
      public static int MinimumMatchCount = 3;

      /// <summary>
      /// The number of animated points that should be kept ready in the pool.
      /// </summary>
      public static int AnimatedPointPoolSize = 20;
   }
}
