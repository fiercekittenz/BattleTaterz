﻿using Godot;
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
      /// The size, in pixels, of the tiles on the board.
      /// </summary>
      public static int TileSize = 70;

      /// <summary>
      /// Represents the number of tiles in rows and columns for the game board.
      /// </summary>
      public static int TileCount = 9;

      /// <summary>
      /// The number of pixels needed to offset the tile slightly to fit into the center of
      /// its grid location.
      /// </summary>
      public static float TileGridOffset = (Globals.TileSize / 2) + 10;

      /// <summary>
      /// The maximum number of hype levels for multiple cascading matches. There can be many more than this, but 
      /// this value restricts the number of sounds played.
      /// </summary>
      public static int MaxHypeLevel = 5;

      /// <summary>
      /// RNGesus engine.
      /// </summary>
      public static Random RNGesus = new Random(Guid.NewGuid().GetHashCode());
   }
}
