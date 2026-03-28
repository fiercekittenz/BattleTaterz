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

            // Fade in → move across → then chain a separate tween for fade out.
            // Two TweenProperty calls for the same property on one Tween causes the
            // second to silently override the first, so the fade-out lives on its own Tween.
            var tween = chompTater.CreateTween();
            tween.SetProcessMode(Tween.TweenProcessMode.Physics);
            tween.TweenProperty(chompTater, "modulate:a", 1.0f, 0.3f).SetEase(Tween.EaseType.In);
            tween.TweenProperty(chompTater, "position", chompEndPosition, 2.0f).SetEase(Tween.EaseType.Out).From(chompStartPosition);
            tween.TweenCallback(Callable.From(() =>
            {
               var fadeOut = chompTater.CreateTween();
               fadeOut.SetProcessMode(Tween.TweenProcessMode.Physics);
               fadeOut.TweenProperty(chompTater, "modulate:a", 0.0f, 0.3f).SetEase(Tween.EaseType.Out);
               fadeOut.Finished += () =>
               {
                  tileOwner.RemoveChild(chompTater);
                  chompTater.QueueFree();
               };
            }));

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
