using BattleTaterz.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay
{
   public class Score
   {
      // The current score.
      public int Current { get; private set; } = 0;

      //
      // Consts.
      //

      /// <summary>
      /// The number of points given per tile in a base calculation.
      /// </summary>
      public static int BasePointsPerTile = 2;

      /// <summary>
      /// The number of bonus points given to tiles beyond the minimum 3 in a match.
      /// </summary>
      public static int BonusPerAdditionalTile = 1;

      /// <summary>
      /// Determines what the score should become based on the provided information.
      /// </summary>
      /// <param name="matchDetails"></param>
      /// <param name="level"></param>
      /// <returns>Results of the updated score.</returns>
      public ScoreUpdateResults IncreaseScore(MatchDetails matchDetails, int level)
      {
         // Give the minimum, base points per tile using the level as a modifier.
         int basePointsGained = matchDetails.Tiles.Count * BasePointsPerTile * level;

         // Give bonus points for any tiles matched beyond the minimum 3.
         int bonusPoints = (matchDetails.Tiles.Count - Globals.MinimumMatchCount) * BonusPerAdditionalTile;

         //TODO - have special bonus tiles that, when matched, give a boost to the score.

         // Update the current score.
         Current = Current + basePointsGained + bonusPoints;

         // As a courtesy, return the updated score for UI changes.
         return new ScoreUpdateResults(Current, basePointsGained, bonusPoints, ScoreChangeType.Increase);
      }
   }
}
