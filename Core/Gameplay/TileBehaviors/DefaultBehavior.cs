using BattleTaterz.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay.TileBehaviors
{
   public class DefaultBehavior : TileBehavior
   {
      public override TileBorder Graphic
      {
         get { return TileBorder.Default; }
      }

      protected override TriggerResult InternalTrigger(GameBoard tileOwner, MatchDetails matchDetails, int matchPoints)
      {
         // Does nothing.
         return new TriggerResult();
      }
   }
}
