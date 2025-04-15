using BattleTaterz.Core;
using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay;
using BattleTaterz.Core.UI;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Object representing an individual game board. There may be multiple game boards present at any given time (e.g. Multiplayer).
/// </summary>
public partial class GameBoard : Node2D
{
   #region Public Properties

   /// <summary>
   /// Represents the number of tiles in rows and columns for the game board.
   /// </summary>
   [Export]
   public int TileCount { get; set; } = 9;

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
   /// The maximum number of hype levels for multiple cascading matches. There can be many more than this, but 
   /// this value restricts the number of sounds played.
   /// </summary>
   [Export]
   public int MaxHypeLevel { get; set; } = 3;

   /// <summary>
   /// The current score.
   /// </summary>
   public Score Score { get; private set; } = new Score();

   /// <summary>
   /// Denotes if the game should log out data on swaps, matches, and board state.
   /// </summary>
   public bool LogDebugInfo { get; set; } = true;

   #endregion

   #region Public Methods

   /// <summary>
   /// Called when the node enters the scene tree for the first time. 
   /// </summary>
   public override void _Ready()
   {
      DebugLogger.Instance.ShouldLog(LogDebugInfo);
      DebugLogger.Instance.Log("Battle Taterz play session ready.");

      // Set basic viewport constraints.
      _screenSize = GetViewportRect().Size;
      _startPosition = GlobalPosition;

      // Cache specific nodes.
      _uiNode = GetNode<Godot.Node2D>("UI");
      _audioNode = GetParent().GetNode<Node>("Audio");

      // Create the animated points pool.
      _animatedPointsManager = new AnimatedPointsManager(_uiNode, Globals.AnimatedPointPoolSize);

      // Start game background music.
      var backgroundMusic = _audioNode.GetNode<AudioStreamPlayer>("MainAudio_BackgroundMusic");
      backgroundMusic?.Play();

      // Generate the initial board.
      // TODO: This is a bit excessive. Now that match detection and collapsing exists, this can be simplified.
      LogDebugInfo = false;
      while (true)
      {
         if (Generate())
         {
            break;
         }

         _ = Task.Delay(TimeSpan.FromMilliseconds(1)); // just so it doesn't choke the CPU
      }
      LogDebugInfo = true;

      AnimatedPoint animatedPoint = new AnimatedPoint();
      animatedPoint.TopLevel = true;
      animatedPoint.Name = $"AnimatedPoint1";
      animatedPoint.Owner = _uiNode;
      _uiNode.AddChild(animatedPoint);

      // The game board is now ready for input.
      _isReady = true;
   }

   /// <summary>
   /// Called every frame. 'delta' is the elapsed time since the previous frame. 
   /// </summary>
   /// <param name="delta"></param>
   public override void _Process(double delta)
   {
   }

   /// <summary>
   /// Handle any clean-up when the game board is removed from the scene.
   /// </summary>
   public override void _ExitTree()
   {
      //_animatedPointsManager.Stop();
      base._ExitTree();
   }

   /// <summary>
   /// Generates the entire game board. Only performs one generation loop and returns false if matches were found.
   /// It is the responsibility of the caller to invoke again if this doesn't yield the desired results.
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
            GenerateTile(row, column);
         }
      }

      DebugLogger.Instance.LogGameBoard("Generate() Results", TileCount, ref _gameBoard);

      var matches = CheckForMatches();
      if (matches.Any())
      {
         // We cannot use a board that has matches when it is first generated, because
         // it could cause a cascade of point aggregation that isn't attributed to the
         // player's intelligence.
         DebugLogger.Instance.Log("Generate() resulted in matches. Board needs to be shuffled again.");
         return false;
      }

      // Reposition the entire board.
      float centeredX = (_screenSize.X / 2) - ((TileSize * TileCount) / 2);
      float centeredY = (_screenSize.Y / 2) - ((TileSize * TileCount) / 2);
      GlobalPosition = new Vector2(centeredX, centeredY);

      // Now show!
      Show();

      // Play a sound to indicate that the board is ready for play.
      // Do not play this at the very beginning of the game, only when the board has been regenerated
      // due to no valid moves.
      if (_isReady)
      {
         var boardReadySound = _audioNode.GetNode<AudioStreamPlayer>("MainAudio_GameBoardReady");
         boardReadySound?.Play();
      }

      // The game's afoot!
      DebugLogger.Instance.Log("Generate() complete.");
      return true;
   }

   /// <summary>
   /// Clears the game board of all tiles.
   /// </summary>
   public void Clear()
   {
      // Reset any selections prior to clearing the board.
      _primarySelection = null;
      _secondarySelection = null;

      // Persist the position.
      GlobalPosition = _startPosition;

      var tiles = GetChildren().OfType<Tile>().ToList();
      foreach (var tile in tiles)
      {
         tile.GemRef.OnGemMouseEvent -= Gem_OnGemMouseEvent;
         RemoveChild(tile);
         tile.QueueFree();
      }
   }

   /// <summary>
   /// Clears the current selection.
   /// </summary>
   public void ClearSelection()
   {
      if (_primarySelection != null)
      {
         var border = _primarySelection.GetNode<AnimatedSprite2D>("Border");
         border.SpriteFrames = GD.Load<SpriteFrames>($"res://GameObjectResources/Grid/border_default.tres");
      }

      if (_secondarySelection != null)
      {
         var border = _secondarySelection.GetNode<AnimatedSprite2D>("Border");
         border.SpriteFrames = GD.Load<SpriteFrames>($"res://GameObjectResources/Grid/border_default.tres");
      }

      _primarySelection = null;
      _secondarySelection = null;

      DebugLogger.Instance.Log("Selections cleared.");
   }

   /// <summary>
   /// Algorithm for checking the player's game board for matched gems.
   /// </summary>
   public List<MatchDetails> CheckForMatches()
   {
      DebugLogger.Instance.Log("Checking for matches...");

      List <MatchDetails> matches = new List<MatchDetails>();

      for (int row = 0; row < TileCount; ++row)
      {
         for (int column = 0; column < TileCount; ++column)
         {
            // Evaluate horizontal-only
            List<MatchedTileInfo> horizontalmatches = new List<MatchedTileInfo>();
            if (EvaluateTileForMatch(row, column, Gem.GemType.UNKNOWN, EvaluationDirection.Horizontal, ref horizontalmatches))
            {
               var firstTile = horizontalmatches.FirstOrDefault().TileRef;
               float slice = horizontalmatches.Count / 2.0f;
               float midPoint = firstTile.GlobalPosition.X + (slice * TileSize) - (TileSize / 2.0f);

               matches.Add(new MatchDetails(horizontalmatches, EvaluationDirection.Horizontal, new Godot.Vector2(midPoint, firstTile.GlobalPosition.Y)));               
            }

            // Now evaluate vertical-only
            List<MatchedTileInfo> verticalmatches = new List<MatchedTileInfo>();
            if (EvaluateTileForMatch(row, column, Gem.GemType.UNKNOWN, EvaluationDirection.Vertical, ref verticalmatches))
            {
               var firstTile = horizontalmatches.FirstOrDefault().TileRef;
               float slice = horizontalmatches.Count / 2.0f;
               float midPoint = firstTile.GlobalPosition.Y + (slice * TileSize) - (TileSize / 2.0f);
               
               matches.Add(new MatchDetails(verticalmatches, EvaluationDirection.Vertical, new Godot.Vector2(firstTile.GlobalPosition.X, midPoint)));
            }
         }
      }

      DebugLogger.Instance.Log($"Match search complete. {matches.Count} matches found.");
      return matches;
   }

   /// <summary>
   /// Looks at the current state of the board's tiles and determines if there are possible moves.
   /// </summary>
   /// <returns>List of potential moves that could be played to create a match.</returns>
   public List<PotentialMoveInfo> GetPossibleMoves()
   {
      DebugLogger.Instance.Log("GetPossibleMoves() begin...");

      List <PotentialMoveInfo> possibleMoves = new List<PotentialMoveInfo>();

      for (int row = 0; row < TileCount; ++row)
      {
         for (int column = 0; column < TileCount; ++column)
         {
            Tile currentTile = _gameBoard[row, column];
            if (currentTile != null)
            {
               MoveDirection possibleDirection = IsPotentialMatch(row, column, currentTile.GemRef.CurrentGem);
               if (possibleDirection != MoveDirection.NONE)
               {
                  DebugLogger.Instance.Log($"\tPossible move located at [{row}, {column}] (Direction = {(int)possibleDirection}) (Gem = {(int)currentTile.GemRef.CurrentGem})");
                  possibleMoves.Add(new PotentialMoveInfo(currentTile, row, column, possibleDirection));
               }
            }
         }
      }
      DebugLogger.Instance.Log($"GetPossibleMoves() complete. {possibleMoves.Count} possible moves found.");
      return possibleMoves;
   }

   /// <summary>
   /// Swaps the primary and secondary tiles provided.
   /// </summary>
   /// <param name="primary"></param>
   /// <param name="secondary"></param>
   public void SwapSelectedTiles(Tile primary, Tile secondary)
   {
      if (LogDebugInfo)
      {
         // Only check this information if we want to log debug info to avoid the additional cycles.
         var primaryCoordinates = GetTileCoordinates(primary);
         var secondaryCoordinates = GetTileCoordinates(secondary);
         DebugLogger.Instance.Log($"Attempting to swap [{primaryCoordinates.Item1}, {primaryCoordinates.Item2}]({(int)primary.GemRef.CurrentGem}) with [{secondaryCoordinates.Item1}, {secondaryCoordinates.Item2}]({(int)secondary.GemRef.CurrentGem})");
      }

      // Cache the game board coordinates and positions of each before swapping.
      Tuple<int, int> originalPrimaryCoordinates = new Tuple<int, int>(primary.Row, primary.Column);
      Tuple<int, int> originalSecondaryCoordinates = new Tuple<int, int>(secondary.Row, secondary.Column);

      Vector2 originalPrimaryPosition = new Vector2(primary.GlobalPosition.X, primary.GlobalPosition.Y);
      Vector2 originalSecondaryPosition = new Vector2(secondary.GlobalPosition.X, secondary.GlobalPosition.Y);

      // Swap primary and secondary selections in the game board, only. Don't waste cycles moving
      // and triggering rendering until we know if matches are found as a result.
      _gameBoard[originalPrimaryCoordinates.Item1, originalPrimaryCoordinates.Item2] = secondary;
      _gameBoard[originalSecondaryCoordinates.Item1, originalSecondaryCoordinates.Item2] = primary;

      // Verify the swap will lead to a match.
      List<MatchDetails> matches = CheckForMatches();
      if (matches.Any())
      {
         // Matches found, so go ahead and swap positions.
         DebugLogger.Instance.Log("\tSwap approved. Matches found.");

         primary.UpdateCoordinates(originalSecondaryCoordinates.Item1, originalSecondaryCoordinates.Item2);
         primary.GlobalPosition = originalSecondaryPosition;

         secondary.UpdateCoordinates(originalPrimaryCoordinates.Item1, originalPrimaryCoordinates.Item2);
         secondary.GlobalPosition = originalPrimaryPosition;

         // Handle the results of the matches.
         HandleMatches(matches);
      }
      else
      {
         // No matches. Swap everything back to the original positions and play a womp womp sound.
         DebugLogger.Instance.Log("\tNo matches would have resulted from this swap.");

         _gameBoard[originalPrimaryCoordinates.Item1, originalPrimaryCoordinates.Item2] = primary;
         _gameBoard[originalSecondaryCoordinates.Item1, originalSecondaryCoordinates.Item2] = secondary;

         var badMoveSound = _audioNode.GetNode<AudioStreamPlayer>("MainAudio_BadMove");
         badMoveSound?.Play();
      }

      // Clear any selections.
      ClearSelection();

      // If matches were found and handled, verify that the board still has playable moves.
      if (matches.Any())
      {
         List<PotentialMoveInfo> potentialMoves = GetPossibleMoves();
         if (!potentialMoves.Any())
         {
            // No moves are possible with the current board.
            // A new board needs to be generated.
            //TODO - figure out how this should be handled in multiplayer battle scenarios. It isn't the player's fault if this happens.
            DebugLogger.Instance.Log("\tNo possible moves detected on this game board.");
            var noMoreMovesSound = _audioNode.GetNode<AudioStreamPlayer>("MainAudio_NoMoreMoves");
            noMoreMovesSound?.Play();
         }
      }
   }

   #endregion

   #region Debug Methods

   /// <summary>
   /// A debug button action to generate a whole new board.
   /// </summary>
   public void OnDebugResetButtonPressed()
   {
      DebugLogger.Instance.Log("Debug reset requested.");
      while (true)
      {
         if (Generate())
         {
            break;
         }
      }
   }

   /// <summary>
   /// A debug button for handling the matches found in the previous evaluation.
   /// </summary>
   public void OnDebugHandleMatchesButtonPressed()
   {
      DebugLogger.Instance.Log("Debug handle matches button pressed.");
      bool working = true;
      while (working)
      {
         var matches = CheckForMatches();
         if (matches.Any())
         {
            HandleMatches(matches);
         }
         else
         {
            break;
         }
      }
   }

   #endregion

   #region Private Methods

   /// <summary>
   /// Given a list of Tile objects, locate their row and column coordinates in the game board and return them as a list of tuples.
   /// </summary>
   /// <param name="tilesToLocate"></param>
   /// <returns>A list of tuples representing the coordinates of the tiles in the provided list.</returns>
   private Tuple<int, int> GetTileCoordinates(Tile tileToLocate)
   {
      for (int row = 0; row < TileCount; ++row)
      {
         for (int column = 0; column < TileCount; ++column)
         {
            if (_gameBoard[row, column] == tileToLocate)
            {
               return new Tuple<int, int>(row, column);
            }
         }
      }

      return null;
   }

   /// <summary>
   /// Examines the provided tile coordinates to determine if it could be moved to make a match.
   /// Movement prediction is actually very simple. Using the game board array, adding and subtracting
   /// to access surrounding tiles for evaluation is very performant. It looks like a lot of code, but
   /// the underlying instructions are not complex.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <param name="gemType"></param>
   /// <returns>The direction in which the tile may be moved to make a match. Returns MoveDirection.NONE if no move is detected.</returns>
   private MoveDirection IsPotentialMatch(int row, int column, Gem.GemType gemType)
   {
      DebugLogger.Instance.Log($"Evaluate [{row}, {column}]({(int)gemType}) for potential matches...");

      //
      // Match 3 Prediction 101:
      //
      // There are two cases where a move is possible:
      //
      //    1. Current tile is adjacent to a non-matching tile but the non-matching tile
      //       has 2 matching on its opposite side.
      //    2. Current tile is adjacent to a non-matching tile, skip 1 and it does match,
      //       look at opposite sides of middle (non-matching) tile to see if it would match
      //       current tile.
      //
      // For each tile, look to the left/right/up/down and evaluate for the possible match conditions.
      //

      // Potential Move Type: Linear Slide (x o x x)
      {
         // Look left.
         if (column >= 3 &&
             _gameBoard[row, column - 1].GemRef.CurrentGem != gemType &&
             _gameBoard[row, column - 2].GemRef.CurrentGem == gemType &&
             _gameBoard[row, column - 3].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match to the left.");
            return MoveDirection.Left;
         }

         // Look right.
         if (column <= TileCount - 4 &&
             _gameBoard[row, column + 1].GemRef.CurrentGem != gemType &&
             _gameBoard[row, column + 2].GemRef.CurrentGem == gemType &&
             _gameBoard[row, column + 3].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match to the right.");
            return MoveDirection.Right;
         }

         // Look up.
         if (row >= 3 &&
             _gameBoard[row - 1, column].GemRef.CurrentGem != gemType &&
             _gameBoard[row - 2, column].GemRef.CurrentGem == gemType &&
             _gameBoard[row - 3, column].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match above.");
            return MoveDirection.Up;
         }

         // Look down.
         if (row <= TileCount - 4 &&
             _gameBoard[row + 1, column].GemRef.CurrentGem != gemType &&
             _gameBoard[row + 2, column].GemRef.CurrentGem == gemType &&
             _gameBoard[row + 3, column].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match below.");
            return MoveDirection.Down;
         }
      }

      // Potential Move Type: Wedge Slide (x o x -> move another x in from above or below the middle tile)
      {
         // Look left.
         if (column >= 1 && column < TileCount && row >= 1 && row < TileCount - 1 &&
             _gameBoard[row - 1, column - 1].GemRef.CurrentGem == gemType &&
             _gameBoard[row + 1, column - 1].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide match to the left.");
            return MoveDirection.Left;
         }

         // Look right.
         if (column >= 0 && column < TileCount - 1 && row >= 1 && row < TileCount - 1 &&
             _gameBoard[row - 1, column + 1].GemRef.CurrentGem == gemType &&
             _gameBoard[row + 1, column + 1].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide match to the right.");
            return MoveDirection.Right;
         }

         // Look up.
         if (column >= 1 && column < TileCount - 1 && row >= 1 && row < TileCount - 1 &&
             _gameBoard[row + 1, column - 1].GemRef.CurrentGem == gemType &&
             _gameBoard[row + 1, column + 1].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide match up.");
            return MoveDirection.Up;
         }

         // Look down.
         if (column >= 1 && column < TileCount - 1 && row >= 0 && row < TileCount - 1 &&
             _gameBoard[row + 1, column - 1].GemRef.CurrentGem == gemType &&
             _gameBoard[row + 1, column + 1].GemRef.CurrentGem == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide down.");
            return MoveDirection.Down;
         }
      }

      DebugLogger.Instance.Log("\tNo possible moves found.");
      return MoveDirection.NONE;
   }

   /// <summary>
   /// Evaluates the tile for existing matches and moves on to the next based on the provided direction.
   /// Note: Does not predict the potential of a match using this tile, only the existance of a valid match in the present.
   /// </summary>
   private bool EvaluateTileForMatch(int row, int column, Gem.GemType previousGem, EvaluationDirection direction, ref List<MatchedTileInfo> matches)
   {
      DebugLogger.Instance.Log($"EvaluateTileForMatch() [{row}, {column}]({(int)previousGem}) (Direction = {(int)direction})");

      if (row >= 0 && row < TileCount && column >= 0 && column < TileCount)
      {
         // Look at the current tile and compare against the previous tile. If the tile is
         // of type "unknown" then add it to the matches list, because that is the starting gem for this check.
         // The minimum match count check at the end will determine if there was truly a match or not.
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
               if (matches.Count >= Globals.MinimumMatchCount)
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
                  if (matches.Count >= Globals.MinimumMatchCount)
                  {
                     return true;
                  }

                  return false;
               }

               EvaluateTileForMatch(row, nextColumn, currentTile.GemRef.CurrentGem, direction, ref matches);
            }
            else if (direction == EvaluationDirection.Vertical)
            {
               int nextRow = row + 1;

               // If we have reached the end of the column, see if we have enough vertical matches.
               if (nextRow >= TileCount)
               {
                  if (matches.Count >= Globals.MinimumMatchCount)
                  {
                     return true;
                  }

                  return false;
               }

               EvaluateTileForMatch(nextRow, column, currentTile.GemRef.CurrentGem, direction, ref matches);
            }
         }
      }

      DebugLogger.Instance.Log($"EvaluateTileForMatch() {matches.Count} found.");
      return matches.Count >= Globals.MinimumMatchCount;
   }

   /// <summary>
   /// Handles the matches by going through each tile matched, calculating the score, removing, and replacing tiles
   /// by shifting downward.
   /// </summary>
   private async void HandleMatches(List<MatchDetails> matches, int level = 1)
   {
      foreach (var match in matches)
      {
         // Update the score and display points gained animation.
         var scoreUpdateResults = Score.IncreaseScore(match, level);
         DebugLogger.Instance.Log($"HandleMatches() scoreUpdateResults (BasePoints = {scoreUpdateResults.BasePoints}) (Bonus = {scoreUpdateResults.BonusPointsRewarded})");

         _animatedPointsManager.Play(match.GlobalPositionAverage, scoreUpdateResults);
         _uiNode.GetNode<Godot.Label>("ScoreVal").Text = scoreUpdateResults.UpdatedScore.ToString();

         // Remove the matched tiles from the board.
         foreach (var tile in match.Tiles)
         {
            DebugLogger.Instance.Log($"HandleMatches() removing [{tile.Row}, {tile.Column}]({(int)tile.TileRef.GemRef.CurrentGem})...");

            // Remove all of the event handlers from the tile and gem before removing from the scene.
            if (tile.TileRef != null && tile.TileRef.GemRef != null)
            {
               DebugLogger.Instance.Log($"\tRemoving OnGemMouseEvent() handler");
               tile.TileRef.GemRef.OnGemMouseEvent -= Gem_OnGemMouseEvent;
            }

            // Remove the tile node from the scene.
            DebugLogger.Instance.Log($"\tRemoving child from GameBoard node");
            RemoveChild(tile.TileRef);
            tile.TileRef.QueueFree();

            // Remove the tile from the game board grid.
            DebugLogger.Instance.Log($"\tSetting [{tile.Row}, {tile.Column}] to null");
            _gameBoard[tile.Row, tile.Column] = null;
         }
      }

      // Collapse the board such that null tiles are only above valid tiles.
      for (int column = 0; column < TileCount; ++column)
      {
         // Move up the column starting from the bottom row to
         // find the first null entry in the grid. Once it has been
         // identified, that will be the starting point for the collapse of
         // the column.
         for (int row = (TileCount - 1); row > 0; --row)
         {
            if (_gameBoard[row, column] == null)
            {
               CompressColumn(row, column, row /* cache in the recursive method the actual starting point */);
               break;
            }
         }
      }

      // After all holes are plugged with new tiles, evaluate the board for any bonus matches made through the drop.
      ReplaceRemovedTiles();

      // Play an escalating sound chime.
      string soundName = $"Sound_MatchHypeLevel{level}";
      var soundToPlay = _audioNode.GetNode<AudioStreamPlayer>(soundName);
      soundToPlay?.Play();

      // Need to keep checking for matches after the collapse until no more matches are found.
      var newMatches = CheckForMatches();
      if (newMatches.Any())
      {
         if (level >= MaxHypeLevel)
         {
            // Reset the level so the sounds will pitch up again.
            level = 0;
         }

         await Task.Delay(TimeSpan.FromMilliseconds(500));
         HandleMatches(newMatches, level + 1);
      }
   }

   /// <summary>
   /// Moves the higher tiles down, stomping the original tile and setting it to null until the only null
   /// tiles left are above compressed tiles.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   private void CompressColumn(int row, int column, int startingRow)
   {
      bool compressed = false;
      DebugLogger.Instance.Log($"CompressColumn() [{row}, {column}] starting from row {startingRow}");

      int aboveRow = row - 1;
      if (aboveRow >= 0)
      {
         Tile currentTile = _gameBoard[row, column];

         // If the tile above is valid, and the current tile is null, start the swap operation to compress.
         Tile higherTile = _gameBoard[aboveRow, column];
         DebugLogger.Instance.Log($"\tHigher tile coordinates = [{aboveRow}, {column}]");

         if (higherTile != null && currentTile == null)
         {
            // Visually slide this tile down.
            DebugLogger.Instance.Log($"\t\tMove [{aboveRow}, {column}]({(int)higherTile.GemRef.CurrentGem}) down");
            higherTile.MoveTile(row, column, TileSize);
            compressed = true;

            // Swap the data between grid slots to shift the non-null slot into the null.
            _gameBoard[row, column] = _gameBoard[aboveRow, column];
            _gameBoard[aboveRow, column] = null;

            int belowRow = row + 1;
            if (belowRow < TileCount && _gameBoard[belowRow, column] == null)
            {
               DebugLogger.Instance.Log($"\t\tContinue compression from below (= [{belowRow}, {column}]) starting row {startingRow}");
               CompressColumn(belowRow, column, startingRow);
            }
            else
            {
               DebugLogger.Instance.Log($"\t\tContinue compression from [{row}, {column}] starting row {startingRow}");
               CompressColumn(row, column, startingRow);
            }
         }
         // Else, we need to continue to move up until we have a valid higher tile and a potential null.
         else
         {
            DebugLogger.Instance.Log($"\t\tContinue compression from above (= [{aboveRow}, {column}]) starting row {startingRow}");
            CompressColumn(aboveRow, column, startingRow);
         }
      }
      else if (_gameBoard[startingRow, column] == null)
      {
         DebugLogger.Instance.Log($"\tTile [{startingRow}, {column}] is null. Check if compression is complete.");

         // If we're still null at the bottom, see if there are any more tiles to compress down.
         // If there are any tiles, start all over again until we've moved everything down.
         bool compressionComplete = true;
         for (int check = startingRow; check >= 0; --check)
         {
            if (_gameBoard[check, column] != null)
            {
               DebugLogger.Instance.Log($"\t\tTile at [{check}, {column}]({(int)_gameBoard[check, column].GemRef.CurrentGem}) is not null.");
               compressionComplete = false;
            }
         }

         if (!compressionComplete)
         {
            DebugLogger.Instance.Log($"\tCompression not complete. CompressColumn() again with [{startingRow}, {column}] starting row {startingRow}");
            CompressColumn(startingRow, column, startingRow);
         }
      }

      if (compressed)
      {
         DebugLogger.Instance.LogGameBoard($"CompressColumn() ([{row}, {column}] startingRow = {startingRow}) resulting game board:", TileCount, ref _gameBoard);
      }

      DebugLogger.Instance.Log($"CompressColumn() ([{row}, {column}] startingRow = {startingRow}) returning");
   }

   /// <summary>
   /// Goes through the board and replaces any instances of a null entry with a new tile and random gem.
   /// </summary>
   private void ReplaceRemovedTiles()
   {
      DebugLogger.Instance.Log("ReplaceRemovedTiles() begin...");

      for (int row = 0; row < TileCount; ++row)
      {
         for (int column = 0; column < TileCount; ++column)
         {
            if (_gameBoard[row, column] == null)
            {
               DebugLogger.Instance.Log($"\tReplacing [{row}, {column}]");
               var result = GenerateTile(row, column);
               if (result != null)
               {
                  DebugLogger.Instance.Log($"\tNew tile generated at [{row}, {column}] with gem {(int)result.GemRef.CurrentGem}");
               }
            }
         }
      }

      DebugLogger.Instance.Log("ReplaceRemovedTiles() complete.");
   }

   /// <summary>
   /// Generates a tile at the specified grid location.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <exception cref="Exception"></exception>
   private Tile GenerateTile(int row, int column)
   {
      var tile = GD.Load<PackedScene>("res://GameObjectResources/Grid/Tile.tscn").Instantiate<Tile>();
      if (tile == null)
      {
         throw new Exception("Could not instantiate tile.");
      }

      AddChild(tile);
      tile.MoveTile(row, column, TileSize);

      var gem = GD.Load<PackedScene>("res://GameObjectResources/Grid/Gem.tscn").Instantiate<Gem>();
      if (gem != null)
      {
         int randomized = Random.Shared.Next(0, Convert.ToInt32(Gem.GemType.GemType_Count));
         gem.CurrentGem = (Gem.GemType)Enum.ToObject(typeof(Gem.GemType), randomized);
         gem.OnGemMouseEvent += Gem_OnGemMouseEvent;
         tile.SetGemReference(gem, row, column, TileSize);
      }

      _gameBoard[row, column] = tile;

      return tile;
   }

   /// <summary>
   /// Handle mouse events that have bubbled up from a gem.
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   private void Gem_OnGemMouseEvent(object sender, GemMouseEventArgs e)
   {
      if (_isProcessingTurn)
      {
         // Do not handle mouse input while a turn is processing.
         return;
      }

      if (sender is Gem gem)
      {
         Tile parentTile = gem.GetParent<Tile>();
         if (parentTile != null)
         {
            switch (e.EventType)
            {
               case GemMouseEventArgs.MouseEventType.Enter:
                  {
                     if (parentTile != _primarySelection && parentTile != _secondarySelection)
                     {
                        var border = parentTile.GetNode<AnimatedSprite2D>("Border");
                        border.SpriteFrames = GD.Load<SpriteFrames>($"res://GameObjectResources/Grid/border_hover.tres");
                     }
                  }
                  break;
               case GemMouseEventArgs.MouseEventType.Leave:
                  {
                     if (parentTile != _primarySelection && parentTile != _secondarySelection)
                     {
                        var border = parentTile.GetNode<AnimatedSprite2D>("Border");
                        border.SpriteFrames = GD.Load<SpriteFrames>($"res://GameObjectResources/Grid/border_default.tres");
                     }
                  }
                  break;
               case GemMouseEventArgs.MouseEventType.Click:
                  {
                     bool selectionMade = false;

                     if (_primarySelection == null && parentTile != _primarySelection)
                     {
                        _primarySelection = parentTile;
                        selectionMade = true;

                        if (LogDebugInfo)
                        {
                           var coordinates = GetTileCoordinates(_primarySelection);
                           DebugLogger.Instance.Log($"Gem_OnGemMouseEvent() first selection [{coordinates.Item1}, {coordinates.Item2}]({(int)_primarySelection.GemRef.CurrentGem})");
                        }
                     }
                     else if (_secondarySelection == null && parentTile != _secondarySelection)
                     {
                        _secondarySelection = parentTile;
                        selectionMade = true;

                        if (LogDebugInfo)
                        {
                           var coordinates = GetTileCoordinates(_secondarySelection);
                           DebugLogger.Instance.Log($"Gem_OnGemMouseEvent() second selection [{coordinates.Item1}, {coordinates.Item2}]({(int)_secondarySelection.GemRef.CurrentGem})");
                        }
                     }

                     if (selectionMade)
                     {
                        var border = parentTile.GetNode<AnimatedSprite2D>("Border");
                        border.SpriteFrames = GD.Load<SpriteFrames>($"res://GameObjectResources/Grid/border_selected.tres");

                        var selectionSound = _audioNode.GetNode<AudioStreamPlayer>("MainAudio_Selection");
                        selectionSound.Play();
                     }

                     if (_primarySelection != null && _secondarySelection != null)
                     {
                        SetProcessInput(false);
                        _isProcessingTurn = true;
                        {
                           DebugLogger.Instance.Log($"Gem_OnGemMouseEvent() both selections made. Calling SwapSelectedTiles().");
                           DebugLogger.Instance.Log("MOVE BEGIN");
                           DebugLogger.Instance.IndentLevel = 1;

                           SwapSelectedTiles(_primarySelection, _secondarySelection);

                           DebugLogger.Instance.IndentLevel = 0;
                           DebugLogger.Instance.LogGameBoard($"MOVE END - Resulting game board:", TileCount, ref _gameBoard);
                        }
                        SetProcessInput(true);
                        _isProcessingTurn = false;
                     }
                  }
                  break;
               default:
                  {
                     // do nothing
                  }
                  break;
            }
         }
      }
   }

   #endregion

   #region Private Members

   // Manager for this game board's animated points pool.
   private AnimatedPointsManager _animatedPointsManager = null;

   // Cache of the UI node so we don't have to look for it every time we need to access a UI element.
   private Node2D _uiNode = null;

   // Cache of the starting position for the board's top-leftmost corner.
   private Vector2 _startPosition = new Vector2(0, 0);

   // Cache of the screen size used in calculating board placement.
   private Vector2 _screenSize = new Vector2(0, 0);

   // Grid layout representation of the game board.
   private Tile[,] _gameBoard;

   // Local ref to the audio node.
   private Node _audioNode;

   // Selected tiles for swap consideration.
   private Tile _primarySelection;
   private Tile _secondarySelection;

   // Boolean flag to denote the game is processing a play and should not handle mouse events.
   private bool _isProcessingTurn = false;

   // Boolean flag to indicate the board is ready for player input.
   // Primarily used to block generation sounds at start-up.
   private bool _isReady = false;

   #endregion
}
