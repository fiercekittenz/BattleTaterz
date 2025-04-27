using BattleTaterz.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay.TileBehaviors
{
   public class DoublePointsBehavior : TileBehavior
   {
      public override TileBorder Graphic
      {
         get { return TileBorder.DoublePoints; }
      }

      protected override TriggerResult InternalTrigger(GameBoard tileOwner, MatchDetails matchDetails, int matchPoints)
      {
         TriggerResult result = new TriggerResult();

         result.ScoreChange = matchPoints * 2;

         return result;
      }
   }
}
