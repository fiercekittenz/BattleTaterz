using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Enums
{
   /// <summary>
   /// Describes when a tile behavior should be triggered during the processing of a turn.
   /// </summary>
   public enum BehaviorTriggerStage
   {
      CurrentMatchRound,
      Scoring,
      EndOfTurn
   }
}
