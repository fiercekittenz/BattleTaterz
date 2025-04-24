using BattleTaterz.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   public class TileMoveRequest
   {
      public enum MoveType
      {
         Static,
         Animated
      }

      public Tile Tile { get; set; } = null;

      public int Row { get; set; } = 0;

      public int Column { get; set; } = 0;

      public int RoundMoved { get; set; } = 0;

      public MoveType Type { get; set; } = TileMoveRequest.MoveType.Static;

      public override bool Equals(object obj)
      {
         if (obj is TileMoveRequest otherRequest)
         {
            string logLine = $"Tile.Equals() NAME: {Tile.Name}/{otherRequest.Tile.Name}, ROW: {Tile.Row}/{otherRequest.Row}, COLUMN: {Tile.Column}/{otherRequest.Column}, ROUNDMOVED: {RoundMoved}/{otherRequest.RoundMoved}, TYPE: {(int)Type}/{(int)otherRequest.Type}";

            if (Tile.Name == otherRequest.Tile.Name &&
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

      public override string ToString()
      {
         return $"Tile.ToString() NAME: {Tile.Name}, ROW: {Tile.Row}, COLUMN: {Tile.Column}, ROUNDMOVED: {RoundMoved}, TYPE: {(int)Type}";
      }
   }
}
