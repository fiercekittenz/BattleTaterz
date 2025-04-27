using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay.TileBehaviors;
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

      /// <summary>
      /// The number of points given per tile in a base calculation.
      /// </summary>
      public static int BasePointsPerTile = 2;

      /// <summary>
      /// The number of bonus points given to tiles beyond the minimum 3 in a match.
      /// </summary>
      public static int BonusPerAdditionalTile = 1;

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="parentGameBoard"></param>
      public Score(GameBoard parentGameBoard)
      {
         _parentGameBoard = parentGameBoard;
      }

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

         // Handle special tile behavior.
         int behaviorPoints = 0;
         foreach (var tile in matchDetails.Tiles)
         {
            TriggerResult triggerResult = tile.TileRef.Behavior.Trigger(_parentGameBoard, matchDetails, basePointsGained + bonusPoints);
            behaviorPoints += triggerResult.ScoreChange;
         }

         // Update the current score.
         int subTotal = basePointsGained + bonusPoints + behaviorPoints;
         Current += basePointsGained + bonusPoints + behaviorPoints;

         // As a courtesy, return the updated score for UI changes.
         ScoreChangeType scoreDirection = Current > subTotal ? ScoreChangeType.Increase : ScoreChangeType.Decrease;
         return new ScoreUpdateResults(Current, basePointsGained, bonusPoints, behaviorPoints, scoreDirection);
      }

      /// <summary>
      /// Default Constructor
      /// </summary>
      private Score() { }

      /// <summary>
      /// Internal reference to the parent game board tracked by this score.
      /// </summary>
      private GameBoard _parentGameBoard;
   }
}
