using BattleTaterz.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.Gameplay.TileBehaviors
{
   /// <summary>
   /// Removes all tiles on the row containing the tile with this behavior.
   /// </summary>
   public class MatchDirectionEliminationBehavior : TileBehavior
   {
      public override TileBorder Graphic
      {
         get { return TileBorder.RowElimination; }
      }

      public override BehaviorTriggerStage TriggerStage
      {
         get { return BehaviorTriggerStage.CurrentMatchRound; }
      }

      protected override TriggerResult InternalTrigger(GameBoard tileOwner, MatchDetails matchDetails, int matchPoints)
      {
         TriggerResult result = new TriggerResult();

         MatchedTileInfo referenceTile = matchDetails.Tiles.First();
         if (referenceTile != null)
         {
            int targetRow = referenceTile.Row;
            int targetColumn = referenceTile.Column;

            // Build a list of the tiles in this row and remove them. Make sure the list excludes any tiles in the match
            // as those have already been handled at this point.
            for (int targetDir = 0; targetDir < Globals.TileCount; ++targetDir)
            {
               Tile tileInRow = null;
               if (matchDetails.Direction == EvaluationDirection.Horizontal)
               {
                  tileInRow = tileOwner.TileAt(targetRow, targetDir);
               }
               else
               {
                  tileInRow = tileOwner.TileAt(targetDir, targetColumn);
               }

               if (tileInRow != null)
               {
                  var existing = matchDetails.Tiles.Where(t => t.TileRef == tileInRow);
                  if (existing != null && !existing.Any())
                  {
                     // Put in a request to animate the recycling of this tile.
                     //TODO - add an option to denote what the recycling effect should be (e.g. bomb, slow disappear, potato cat chomping away the row, etc.)
                     tileOwner.RequestTileAnimate(tileInRow, tileInRow.Row, tileInRow.Column, matchDetails.RoundProcessed, TileAnimationRequest.AnimationType.Recycling);

                     // Null out this section of the board.
                     tileOwner.NullifyTileAt(tileInRow.Row, tileInRow.Column);
                  }
               }
            }
         }

         return result;
      }
   }
}
