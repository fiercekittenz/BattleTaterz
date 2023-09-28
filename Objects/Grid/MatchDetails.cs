using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Objects.Grid
{
   /// <summary>
   /// Information regarding a matched set of tiles.
   /// </summary>
   public class MatchDetails
   {
      public List<MatchedTileInfo> Tiles { get; set; } = new List<MatchedTileInfo>();

      public EvaluationDirection Direction { get; set; }

      public MatchDetails(List<MatchedTileInfo> tiles, EvaluationDirection direction)
      {
         Tiles = tiles;
         Direction = direction;
      }

      private MatchDetails() { }
   }
}
