using BattleTaterz.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   /// <summary>
   /// Represents the results of an updated score.
   /// </summary>
   public class ScoreUpdateResults
   {
      public int UpdatedScore { get; private set; } = 0;

      public int BasePoints { get; private set; } = 0;

      public int BonusPointsRewarded { get; private set; } = 0;

      public ScoreChangeType ChangeType { get; private set; } = ScoreChangeType.Increase;

      public ScoreUpdateResults(int updatedScore, int basePoints, int bonusPointsRewarded, ScoreChangeType changeType)
      {
         UpdatedScore = updatedScore;
         BasePoints = basePoints;
         BonusPointsRewarded = bonusPointsRewarded;
         ChangeType = changeType;
      }

      public string GetChangeTypeAsString()
      {
         if (ChangeType == ScoreChangeType.Increase)
         {
            return "+";
         }
         else
         {
            return "-";
         }
      }
   }
}
