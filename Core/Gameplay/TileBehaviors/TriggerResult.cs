using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay.TileBehaviors
{
   /// <summary>
   /// The result of a tile behavior being triggered.
   /// </summary>
   public class TriggerResult
   {
      /// <summary>
      /// Changes that need to be applied to the score.
      /// May be positive or negative.
      /// </summary>
      public int ScoreChange { get; set; } = 0;

      /// <summary>
      /// Denotes if the behavior triggered successfully or not.
      /// </summary>
      public bool IsSuccessful { get; set; } = true;
   }
}
