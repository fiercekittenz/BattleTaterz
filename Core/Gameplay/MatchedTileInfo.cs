using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   /// <summary>
   /// A stub of information on a tile that has been matched on the board and needs to be removed
   /// then replaced by the algorithm.
   /// </summary>
   public class MatchedTileInfo
   {
      public Tile TileRef { get; set; } = null;

      public int Row { get; set; } = 0;

      public int Column { get; set; } = 0;

      public MatchedTileInfo(Tile tileRef, int row, int column)
      {
         TileRef = tileRef;
         Row = row;
         Column = column;
      }

      private MatchedTileInfo() { }
   }
}
