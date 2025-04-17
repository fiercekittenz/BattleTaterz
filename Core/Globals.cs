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

      /// <summary>
      /// The size, in pixels, of the tiles on the board.
      /// </summary>
      public static int TileSize = 64;

      /// <summary>
      /// Represents the number of tiles in rows and columns for the game board.
      /// </summary>
      public static int TileCount { get; set; } = 9;

      /// <summary>
      /// Defines the size of a gem inside a single tile.
      /// </summary>
      public static int GemSize { get; set; } = 32;

      /// <summary>
      /// The maximum number of hype levels for multiple cascading matches. There can be many more than this, but 
      /// this value restricts the number of sounds played.
      /// </summary>
      public static int MaxHypeLevel { get; set; } = 3;
   }
}
