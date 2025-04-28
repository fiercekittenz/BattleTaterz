using BattleTaterz.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   /// <summary>
   /// Information regarding a matched set of tiles.
   /// </summary>
   public class MatchDetails
   {
      public List<MatchedTileInfo> Tiles { get; set; } = new List<MatchedTileInfo>();

      public EvaluationDirection Direction { get; set; }

      public Godot.Vector2 GlobalPositionAverage { get; set; }

      public int RoundProcessed { get; set; } = 0;

      public MatchDetails(List<MatchedTileInfo> tiles, EvaluationDirection direction, Godot.Vector2 globalPositionAverage, int roundProcessed)
      {
         Tiles = tiles;
         Direction = direction;
         GlobalPositionAverage = globalPositionAverage;
         RoundProcessed = roundProcessed;
      }

      private MatchDetails() { }
   }
}
