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
         // Level is 0-based, so adding one is necessary for the right math.
         int basePointsGained = matchDetails.Tiles.Count * BasePointsPerTile * (level + 1);

         // Give bonus points for any tiles matched beyond the minimum 3.
         int bonusPoints = (matchDetails.Tiles.Count - Globals.MinimumMatchCount) * BonusPerAdditionalTile;

         // Points assigned from specials, when applicable.
         int specialPoints = 0;

         // Handle double point tiles.
         var doublePointTiles = matchDetails.Tiles.Where(t => t.TileRef.Behavior == BehaviorMode.DoublePoints)?.ToList();
         if (doublePointTiles.Any())
         {
            specialPoints += (int)Math.Pow(basePointsGained + bonusPoints, doublePointTiles.Count() * 2);
         }

         // Update the current score.
         Current = Current + basePointsGained + bonusPoints + specialPoints;

         // As a courtesy, return the updated score for UI changes.
         return new ScoreUpdateResults(Current, basePointsGained, bonusPoints, specialPoints, ScoreChangeType.Increase);
      }
   }
}
