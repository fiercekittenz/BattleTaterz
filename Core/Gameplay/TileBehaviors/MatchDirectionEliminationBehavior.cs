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
         TriggerResult result = new TriggerResult();

         MatchedTileInfo referenceTile = matchDetails.Tiles.First();
         if (referenceTile != null)
         {
            int targetRow = referenceTile.Row;
            int targetColumn = referenceTile.Column;

            //TODO Test code for the chomptater animation
            ChompTater chompTater = GD.Load<PackedScene>("res://Scenes/ChompTater.tscn").Instantiate<ChompTater>();
            chompTater.Hide();
            tileOwner.AddChild(chompTater);

            AnimatedSprite2D chompAnimation = chompTater.GetNode<AnimatedSprite2D>("Sprite");
            Godot.Vector2 chompStartPosition;
            Godot.Vector2 chompEndPosition;

            if (matchDetails.Direction == EvaluationDirection.Horizontal)
            {
               // Spawn ChompTater in column 0, row [referenceTile's row]
               chompStartPosition = new Godot.Vector2(Globals.TileGridOffset, (targetRow * Globals.TileSize) + Globals.TileGridOffset);
               chompEndPosition = new Vector2((Globals.TileCount * Globals.TileSize) + Globals.TileGridOffset, chompStartPosition.Y);
            }
            else
            {
               // Spawn ChompTater in row 0, column [referenceTile's column]
               chompTater.Rotate((float)Math.PI/2);
               chompStartPosition = new Godot.Vector2((targetColumn * Globals.TileSize) + Globals.TileGridOffset, Globals.TileGridOffset);
               chompEndPosition = new Vector2(chompStartPosition.X, (Globals.TileCount * Globals.TileSize) + Globals.TileGridOffset);
            }

            chompTater.Position = chompStartPosition;
            chompAnimation?.Play();
            chompTater.Show();

            var tween = tileOwner.GetTree().CreateTween();
            tween.SetProcessMode(Tween.TweenProcessMode.Physics);
            tween.SetParallel(false);
            tween.TweenProperty(chompTater, "position", chompEndPosition, 2.0f).SetEase(Tween.EaseType.Out).From(chompStartPosition);
            tween.TweenProperty(chompTater, "modulate:a", 1.0f, 0.1f).SetEase(Tween.EaseType.In);
            tween.Finished += (() =>
            {
               tween.Kill();
               tileOwner.RemoveChild(chompTater);
               chompTater.QueueFree();
            });
            //TODO end test code

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
