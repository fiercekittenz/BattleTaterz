using BattleTaterz.Core.Enums;
using Godot;
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
         // Track the grid positions that were eliminated by this behavior so cascading drops that land here won't play DropAnimation.
         TriggerResult result = new TriggerResult();
         result.EliminatedPositions = new HashSet<(int, int)>();
         // fadeIn + traversal + fadeOut + postEatingPause
         result.CascadeDelaySeconds = 0.3f + 2.0f + 0.3f + 1.0f;

         MatchedTileInfo referenceTile = matchDetails.Tiles.First();
         if (referenceTile != null)
         {
            int targetRow = referenceTile.Row;
            int targetColumn = referenceTile.Column;

            ChompTater chompTater = GD.Load<PackedScene>("res://Scenes/ChompTater.tscn").Instantiate<ChompTater>();
            chompTater.Hide();
            tileOwner.AddChild(chompTater);

            AnimatedSprite2D chompAnimation = chompTater.GetNode<AnimatedSprite2D>("Sprite");
            Godot.Vector2 chompStartPosition;
            Godot.Vector2 chompEndPosition;

            if (matchDetails.Direction == EvaluationDirection.Horizontal)
            {
               // Start one tile left of column 0 for "enters from offscreen" effect
               chompStartPosition = new Godot.Vector2(Globals.TileGridOffset - Globals.TileSize, (targetRow * Globals.TileSize) + Globals.TileGridOffset);
               chompEndPosition = new Vector2((Globals.TileCount * Globals.TileSize) + Globals.TileGridOffset, chompStartPosition.Y);
            }
            else
            {
               // Start one tile above row 0 for "enters from offscreen" effect
               chompTater.Rotate((float)Math.PI/2);
               chompStartPosition = new Godot.Vector2((targetColumn * Globals.TileSize) + Globals.TileGridOffset, Globals.TileGridOffset - Globals.TileSize);
               chompEndPosition = new Vector2(chompStartPosition.X, (Globals.TileCount * Globals.TileSize) + Globals.TileGridOffset);
            }

            chompTater.Position = chompStartPosition;
            chompTater.Modulate = new Color(1, 1, 1, 0);
            chompAnimation?.Play();
            chompTater.Show();

            // Populate TriggerResult so GameBoard._Process() creates the tween
            // when this round starts, keeping cascade chomps in sync.
            result.ChompTaterNode = chompTater;
            result.ChompStartPosition = chompStartPosition;
            result.ChompEndPosition = chompEndPosition;
            result.ChompFadeInDuration = 0.3f;
            result.ChompTraversalDuration = 2.0f;
            result.ChompFadeOutDuration = 0.3f;

            // Record every position in the target row/column so cascade drops
            // landing here won't play DropAnimation.
            for (int i = 0; i < Globals.TileCount; ++i)
            {
               if (matchDetails.Direction == EvaluationDirection.Horizontal)
                  result.EliminatedPositions.Add((targetRow, i));
               else
                  result.EliminatedPositions.Add((i, targetColumn));
            }

            // Remove non-matched tiles in the row/column with stagger-timed recycling
            float fadeInDuration = 0.3f;
            float traversalDuration = 2.0f;
            float totalSpan = (float)(Globals.TileCount + 1);
            float timePerTileWidth = traversalDuration / totalSpan;

            for (int targetDir = 0; targetDir < Globals.TileCount; ++targetDir)
            {
               Tile tileInRow = null;
               int row, col;
               if (matchDetails.Direction == EvaluationDirection.Horizontal)
               {
                  row = targetRow; col = targetDir;
                  tileInRow = tileOwner.TileAt(targetRow, targetDir);
               }
               else
               {
                  row = targetDir; col = targetColumn;
                  tileInRow = tileOwner.TileAt(targetDir, targetColumn);
               }

               if (tileInRow != null)
               {
                  var existing = matchDetails.Tiles.Where(t => t.TileRef == tileInRow);
                  if (existing != null && !existing.Any())
                  {
                     // Calculate stagger delay: when chomp reaches this tile
                     float recycleDelay = fadeInDuration + ((targetDir + 1) * timePerTileWidth);

                     tileOwner.RequestTileAnimate(tileInRow, row, col,
                        matchDetails.RoundProcessed, TileAnimationRequest.AnimationType.Recycling, recycleDelay);
                     tileOwner.NullifyTileAt(row, col);
                  }
               }
            }
         }

         return result;
      }
   }
}
