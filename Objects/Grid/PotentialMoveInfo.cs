using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Objects.Grid
{
    public class PotentialMoveInfo
   {
      public Tile TileRef { get; set; } = null;

      public int Row { get; set; } = 0;

      public int Column { get; set; } = 0;

      public MoveDirection Direction { get; set; } = MoveDirection.NONE;

      public PotentialMoveInfo(Tile tileRef, int row, int column, MoveDirection direction)
      {
         TileRef = tileRef;
         Row = row;
         Column = column;
         Direction = direction;
      }

      private PotentialMoveInfo() { }
   }
}
