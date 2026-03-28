using BattleTaterz.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   public class TileAnimationRequest
   {
      public enum AnimationType
      {
         Static,
         Animated,
         Recycling,
         Hold,
         ChompAnimation
      }

      public class ChompAnimationData
      {
         public Godot.Node2D ChompNode { get; set; }
         public Godot.Vector2 StartPosition { get; set; }
         public Godot.Vector2 EndPosition { get; set; }
         public float FadeInDuration { get; set; }
         public float TraversalDuration { get; set; }
         public float FadeOutDuration { get; set; }
         public float PostEatingPause { get; set; }
      }

      public Tile Tile { get; set; } = null;

      public int Row { get; set; } = 0;

      public int Column { get; set; } = 0;

      public int RoundMoved { get; set; } = 0;

      public AnimationType Type { get; set; } = TileAnimationRequest.AnimationType.Static;

      public float StaggerDelay { get; set; } = 0f;

      public bool ShouldPlayDropAnimation { get; set; } = false;

      public ChompAnimationData ChompData { get; set; }

      /// <summary>
      /// Specialized comparison method override.
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj)
      {
         if (obj is TileAnimationRequest otherRequest)
         {
            string logLine = $"Tile.Equals() NAME: {Tile?.Name}/{otherRequest.Tile?.Name}, ROW: {Tile?.Row ?? -1}/{otherRequest.Row}, COLUMN: {Tile?.Column ?? -1}/{otherRequest.Column}, ROUNDMOVED: {RoundMoved}/{otherRequest.RoundMoved}, TYPE: {(int)Type}/{(int)otherRequest.Type}";

            if (Tile?.Name == otherRequest.Tile?.Name &&
                    Row == otherRequest.Row &&
                    Column == otherRequest.Column &&
                    RoundMoved == otherRequest.RoundMoved &&
                    Type == otherRequest.Type)
            {
               DebugLogger.Instance.Log($"\t\tTRUE {logLine}", Enums.LogLevel.Trace);
               return true;
            }

            DebugLogger.Instance.Log($"\t\tFALSE {logLine}", Enums.LogLevel.Trace);
         }

         return false;
      }

      /// <summary>
      /// Provide a legible representation of the request for logging.
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         return $"Tile.ToString() NAME: {Tile?.Name ?? (Type == AnimationType.ChompAnimation ? "[Chomp]" : "[Hold]")}, ROW: {Tile?.Row ?? -1}, COLUMN: {Tile?.Column ?? -1}, ROUNDMOVED: {RoundMoved}, TYPE: {(int)Type}";
      }
   }
}
