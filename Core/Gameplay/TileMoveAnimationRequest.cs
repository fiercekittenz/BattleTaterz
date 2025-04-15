using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   public class TileMoveAnimationRequest
   {
      public Tile Tile { get; set; } = null;

      public int Row { get; set; } = 0;

      public int Column { get; set; } = 0;
   }
}
