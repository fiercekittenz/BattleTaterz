using BattleTaterz.Objects.Grid;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameBoard : Node2D
{
   #region Public Properties

   /// <summary>
   /// Represents the number of tiles in rows and columns for the game board.
   /// </summary>
   [Export]
   public int TileCount { get; set; } = 8;

   /// <summary>
   /// Defines the size of an individual tile in the grid.
   /// </summary>
   [Export]
   public int TileSize { get; set; } = 64;

   /// <summary>
   /// Defines the size of a gem inside a single tile.
   /// </summary>
   [Export]
   public int GemSize { get; set; } = 32;

   /// <summary>
   /// The minimum number of gems that need to match horizontally or vertically.
   /// </summary>
   [Export]
   public int MinimumMatchCount { get; set; } = 3;

   /// <summary>
   /// The current score.
   /// </summary>
   public int Score { get; set; } = 0;

   #endregion

   #region Public Methods

   /// <summary>
   /// Called when the node enters the scene tree for the first time. 
   /// </summary>
   public override void _Ready()
   {
      _screenSize = GetViewportRect().Size;
      _startPosition = GlobalPosition;

      while (true)
      {
         if (Generate())
         {
            break;
         }
      }
   }

   /// <summary>
   /// Called every frame. 'delta' is the elapsed time since the previous frame. 
   /// </summary>
   /// <param name="delta"></param>
   public override void _Process(double delta)
   {
   }

   /// <summary>
   /// Generates the entire game board.
   /// </summary>
   public bool Generate()
   {
      // Make the whole board invisible until we are done.
      Hide();

      // Clear the board before generating a new one.
      Clear();

      // Reinstantiate the game board grid with the tile count as it may have been changed.
      _gameBoard = new Tile[TileCount, TileCount];

      // Iterate and create rows, columns, and the gems for each tile.
      for (int row = 0; row < TileCount; ++row)
      {
         for (int column = 0; column < TileCount; ++column)
         {
            var tile = GD.Load<PackedScene>("res://Objects/Grid/Tile.tscn").Instantiate<Tile>();
            if (tile != null)
            {
               AddChild(tile);
               tile.GlobalPosition = new Vector2(column * TileSize, row * TileSize);

               var gem = GD.Load<PackedScene>("res://Objects/Grid/Gem.tscn").Instantiate<Gem>();
               if (gem != null)
               {
                  int randomized = Random.Shared.Next(0, Convert.ToInt32(Gem.GemType.GemType_Count));
                  gem.CurrentGem = (Gem.GemType)Enum.ToObject(typeof(Gem.GemType), randomized);

                  tile.GemRef = gem;
                  tile.AddChild(gem);
                  gem.GlobalPosition = new Vector2(column * TileSize, row * TileSize);
               }

               _gameBoard[row, column] = tile;
            }
         }
      }

      var matches = CheckForMatches();
      if (matches.Any())
      {
         // We cannot use a board that has matches when it is first generated, because
         // it could cause a cascade of point aggregation that isn't attributed to the
         // player's intelligence.
         return false;
      }

      // Reposition the entire board.
      float centeredX = (_screenSize.X / 2) - ((TileSize * TileCount) / 2);
      float centeredY = (_screenSize.Y / 2) - ((TileSize * TileCount) / 2);
      GlobalPosition = new Vector2(centeredX, centeredY);

      // Now show!
      Show();

      // The game's afoot!
      return true;
   }

   /// <summary>
   /// Clears the game board of all tiles.
   /// </summary>
   public void Clear()
   {
      GlobalPosition = _startPosition;

      var tiles = GetChildren().OfType<Tile>().ToList();
      foreach (var tile in tiles)
      {
         RemoveChild(tile);
      }
   }

   /// <summary>
   /// Algorithm for checking the player's game board for matched gems.
   /// </summary>
   public List<MatchDetails> CheckForMatches()
   {
      List<MatchDetails> matches = new List<MatchDetails>();

      for (int row = 0; row < TileCount; ++row)
      {
         for (int column = 0; column < TileCount; ++column)
         {
            // Evaluate horizontal-only
            List<MatchedTileInfo> horizontalmatches = new List<MatchedTileInfo>();
            if (ExamineTile(row, column, Gem.GemType.UNKNOWN, EvaluationDirection.Horizontal, ref horizontalmatches))
            {
               foreach (var matched in horizontalmatches)
               {
                  var border = matched.TileRef?.GetNode<AnimatedSprite2D>("Border");
                  border.SpriteFrames = GD.Load<SpriteFrames>($"res://Objects/Grid/border1.tres");
               }

               matches.Add(new MatchDetails(horizontalmatches, EvaluationDirection.Horizontal));
            }

            // Now evaluate vertical-only
            List<MatchedTileInfo> verticalmatches = new List<MatchedTileInfo>();
            if (ExamineTile(row, column, Gem.GemType.UNKNOWN, EvaluationDirection.Vertical, ref verticalmatches))
            {
               foreach (var matched in verticalmatches)
               {
                  var border = matched.TileRef?.GetNode<AnimatedSprite2D>("Border");
                  border.SpriteFrames = GD.Load<SpriteFrames>($"res://Objects/Grid/border1.tres");
               }

               matches.Add(new MatchDetails(verticalmatches, EvaluationDirection.Vertical));
            }
         }
      }

      return matches;
   }

   #endregion

   #region Debug Methods

   /// <summary>
   /// A debug button action to generate a whole new board.
   /// </summary>
   public void OnDebugResetButtonPressed()
   {
      while (true)
      {
         if (Generate())
         {
            break;
         }
      }
   }

   /// <summary>
   /// A debug button for evaluating matches on the current board.
   /// </summary>
   public void OnDebugEvaluateButtonPressed()
   {
      _debugTempMatchList = CheckForMatches();
   }

   /// <summary>
   /// A debug button for handling the matches found in the previous evaluation.
   /// </summary>
   public void OnDebugHandleMatchesButtonPressed()
   {
      HandleMatches(_debugTempMatchList);
   }

   #endregion

   #region Private Methods

   /// <summary>
   /// Evaluates the tile and moves on to the next based on the provided direction.
   /// </summary>
   private bool ExamineTile(int row, int column, Gem.GemType previousGem, EvaluationDirection direction, ref List<MatchedTileInfo> matches)
   {
      if (row >= 0 && row < TileCount && column >= 0 && column < TileCount)
      {
         // Look at the current tile and compare against the previous tile. If the tile is
         // of type "unknown" then add it to the matches list.
         Tile currentTile = _gameBoard[row, column];
         if (currentTile != null)
         {
            if (currentTile.GemRef.CurrentGem == previousGem || previousGem == Gem.GemType.UNKNOWN)
            {
               matches.Add(new MatchedTileInfo(currentTile, row, column));
            }
            else if (previousGem != Gem.GemType.UNKNOWN && currentTile.GemRef.CurrentGem != previousGem)
            {
               // The current gem doesn't match the previous gem, so we can now examine the match list
               // and bail early with the results.
               if (matches.Count >= MinimumMatchCount)
               {
                  return true;
               }

               return false;
            }

            if (direction == EvaluationDirection.Horizontal)
            {
               int nextColumn = column + 1;

               // If we have reached the end of the row, see if we have enough horizontal matches.
               if (nextColumn >= TileCount)
               {
                  if (matches.Count >= MinimumMatchCount)
                  {
                     return true;
                  }

                  return false;
               }

               ExamineTile(row, nextColumn, currentTile.GemRef.CurrentGem, direction, ref matches);
            }
            else if (direction == EvaluationDirection.Vertical)
            {
               int nextRow = row + 1;

               // If we have reached the end of the column, see if we have enough vertical matches.
               if (nextRow >= TileCount)
               {
                  if (matches.Count >= MinimumMatchCount)
                  {
                     return true;
                  }

                  return false;
               }

               ExamineTile(nextRow, column, currentTile.GemRef.CurrentGem, direction, ref matches);
            }
         }
      }

      return matches.Count >= MinimumMatchCount;
   }

   /// <summary>
   /// Handles the matches by going through each tile matched, calculating the score, removing, and replacing tiles
   /// by shifting downward.
   /// </summary>
   private void HandleMatches(List<MatchDetails> matches)
   {
      if (!matches.Any())
      {
         return;
      }

      foreach (var match in matches)
      {
         // Remove the matched tiles from the board.
         foreach (var tile in match.Tiles)
         {
            //TODO - basic scoring for now, but will want to make this more elaborate later.
            ++Score;

            // Remove the tile node from the scene.
            RemoveChild(tile.TileRef);

            // Remove the tile from the game board grid.
            _gameBoard[tile.Row, tile.Column] = null;
         }
      }

      // Collapse the board such that null tiles are only above valid tiles.
      for (int column = 0; column < TileCount; ++column)
      {
         CompressColumn((TileCount - 1), column);
      }

      //TODO
      // After all holes are plugged with new tiles, evaluate the board for any bonus matches made through the drop.
   }

   private void CompressColumn(int startingRow, int column)
   {
      // Move up the column starting from the specified row to
      // find the first null entry in the grid. Once it has been
      // identified, that will be the starting point for the collapse of
      // the column.
      for (int row = startingRow; row > 0; --row)
      {
         if (_gameBoard[row, column] == null)
         {
            CollapseTiles(row, column, row /* cache in the recursive method the actual starting point */);
            break;
         }
      }
   }

   /// <summary>
   /// Moves the higher tile down, stomping the original tile and setting it to null.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   private void CollapseTiles(int row, int column, int startingRow)
   {
      int aboveRow = row - 1;
      if (aboveRow >= 0)
      { 
         Tile currentTile = _gameBoard[row, column];

         // If the tile above is valid, and the current tile is null, start the swap operation to compress.
         Tile higherTile = _gameBoard[aboveRow, column];
         if (higherTile != null && currentTile == null)
         {
            // Visually slide this tile down.
            higherTile.GlobalPosition = new Vector2(higherTile.GlobalPosition.X, higherTile.GlobalPosition.Y + TileSize);

            // Swap the data between grid slots to shift the non-null slot into the null.
            _gameBoard[row, column] = _gameBoard[aboveRow, column];
            _gameBoard[aboveRow, column] = null;

            int belowRow = row + 1;
            if (belowRow < TileCount && _gameBoard[belowRow, column] == null)
            {
               CollapseTiles(belowRow, column, startingRow);
            }
            else
            {
               CollapseTiles(row, column, startingRow);
            }
         }
         // Else, we need to continue to move up until we have a valid higher tile and a potential null.
         else
         {
            CollapseTiles(aboveRow, column, startingRow);
         }
      }
      else if (_gameBoard[startingRow, column] == null)
      {
         // If we're still null at the bottom, see if there are any more tiles to compress down.
         // If there are any tiles, start all over again until we've moved everything down.
         bool compressionComplete = true;
         for (int check = startingRow; check >= 0; --check)
         {
            if (_gameBoard[check, column] != null)
            {
               compressionComplete = false;
            }
         }

         if (!compressionComplete)
         { 
            CollapseTiles(startingRow, column, startingRow);
         }
      }
   }

   #endregion

   #region Private Members

   // Cache of the starting position for the board's top-leftmost corner.
   private Vector2 _startPosition = new Vector2(0, 0);

   // Cache of the screen size used in calculating board placement.
   private Vector2 _screenSize = new Vector2(0, 0);

   // Grid layout representation of the game board.
   private Tile[,] _gameBoard;

   // Temporary debug list for holding the matches while sussing out the algorithm for replacement.
   private List<MatchDetails> _debugTempMatchList;

   #endregion
}
