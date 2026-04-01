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
      /// Board positions whose tiles were eliminated by this behavior.
      /// Tiles dropping into these positions should NOT play DropAnimation.
      /// </summary>
      public HashSet<(int, int)> EliminatedPositions { get; set; }

      /// <summary>
      /// Extra delay (in seconds) that cascade animations should wait
      /// before starting, so the behavior's visual can finish first.
      /// </summary>
      public float CascadeDelaySeconds { get; set; } = 0f;
   }
}
