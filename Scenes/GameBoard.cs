using BattleTaterz.Core;
using BattleTaterz.Core.Enums;
using BattleTaterz.Core.GameObjects;
using BattleTaterz.Core.Gameplay;
using BattleTaterz.Core.UI;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// Object representing an individual game board. There may be multiple game boards present at any given time (e.g. Multiplayer).
/// </summary>
public partial class GameBoard : Node2D
{
   #region Public Properties

   /// <summary>
   /// The current score.
   /// </summary>
   public Score Score { get; private set; } = new Score();

   /// <summary>
   /// The id of the player that owns this board.
   /// </summary>
   public PlayerInfo Player { get; set; }

   /// <summary>
   /// Denotes if the game board is ready for input from the player or not.
   /// </summary>
   public bool IsReady { get; private set; } = false;

   /// <summary>
   /// State of the game board used to determine behavior on frame ticks.
   /// </summary>
   public GameBoardState State { get; private set; } = GameBoardState.Initializing;

   /// <summary>
   /// The round that is currently being processed.
   /// </summary>
   public int ProcessingRound { get; private set; } = 0;
   public int RoundsToProcess { get; private set; } = 0;

   #endregion

   #region Public Methods

   /// <summary>
   /// Called when the node enters the scene tree for the first time. 
   /// </summary>
   public override void _Ready()
   {
      DebugLogger.Instance.Log($"GameBoard [TODO player id] has entered the node tree.", LogLevel.Info);

      // Cache specific nodes.
      _gameScene = GetParent<GameScene>();
      _uiNode = GetNode<VBoxContainer>("UI");

      // Initialize the tile pool.
      _tilePool = GetNode<TilePool>("TilePool");
      _tilePool.Initialize();

      // Setup the timer.
      _moveTimerLabel = _uiNode.GetNode<MoveTimerLabel>("Labels/MoveTimerLabel");
      _moveTimerLabel.OnTimerFinished += _moveTimerLabel_OnTimerFinished;

      // Create RNGesus
      _rngesus = new Random(Guid.NewGuid().GetHashCode());

      // Create the animated points pool.
      _animatedPointPool = GetNode<AnimatedPointPool>("AnimatedPointPool");

      // Generate the initial board.
      DebugLogger.Instance.Enabled = false;
      Generate();
      DebugLogger.Instance.Enabled = true;

      // Initialize the move timer for this game board after it's been generated.
      //TODO - need a countdown timer before doing this
      _moveTimerLabel.Initialize(GetNode<Timer>("MoveTimer"));
      _moveTimerLabel.Start();

      // The game board is now ready for input.
      IsReady = true;
      State = GameBoardState.Playable;

      // Log the starting game board.
      DebugLogger.Instance.LogGameBoard("Initial Game Board", Globals.TileCount, ref _gameBoard, LogLevel.Info);
   }

   /// <summary>
   /// Called every frame. 'delta' is the elapsed time since the previous frame. 
   /// </summary>
   /// <param name="delta"></param>
   public override void _Process(double delta)
   {
      // Take care of animating tiles on frame ticks.
      if (State == GameBoardState.AnimatingMoveResults)
      {
         TileAnimationRequest request = null;

         DebugLogger.Instance.Log($"\tProcess() {_moveRequests.Count} total remaining move requests yet to be processed.", LogLevel.Trace);

         var roundRequests = _moveRequests.Where(r => r.RoundMoved == ProcessingRound);
         if (roundRequests != null && roundRequests.Any())
         {
            if (_previouslyProcessedRequest != null)
            {
               var peek = roundRequests.First();
               if (_previouslyProcessedRequest.Tile == peek.Tile &&
                   _previouslyProcessedRequest.Type == TileAnimationRequest.AnimationType.Static &&
                   peek.Type == TileAnimationRequest.AnimationType.Animated &&
                   peek.Tile.IsAnimating)
               {
                  // This move request is for a tile that is still busy animating from a static move out of the tile pool.
                  // It's still busy positioning itself for this animation, which will be the visible drop.
                  // Early return and come back to it.
                  DebugLogger.Instance.Log($"\t\tProcess() {peek.ToString()} is waiting on {_previouslyProcessedRequest.ToString()} to finish animating.", LogLevel.Info);
                  return;
               }
            }

            _moveRequests.TryTake(out request);
            _previouslyProcessedRequest = request;
            DebugLogger.Instance.Log($"\t\t_Process() {roundRequests.Count()} remain for round {ProcessingRound}.", LogLevel.Trace);
         }
         else
         {
            DebugLogger.Instance.Log($"\t\t_Process() no requests pending for {ProcessingRound}. Waiting on round to be advanced.", LogLevel.Trace);
         }

         if (request != null)
         {
            _movingTiles.Add(request);

            DebugLogger.Instance.Log($"\t\t_Process() {request.Tile.Name} moving from [{request.Tile.Row}, {request.Tile.Column}] to [{request.Row}, {request.Column}]. Round = {request.RoundMoved}", LogLevel.Trace);

            switch (request.Type)
            {
               case TileAnimationRequest.AnimationType.Static:
                  {
                     // If this is a static move, it's meant to prepare the tile for dropping.
                     request.Tile.PrepareForDrop(this, request);
                  }
                  break;

               case TileAnimationRequest.AnimationType.Animated:
                  {
                     request.Tile.MoveTile(this, request);
                  }
                  break;

               case TileAnimationRequest.AnimationType.Recycling:
                  {
                     request.Tile.AnimateRecycle(this, request);
                  }
                  break;
            }
         }
      }
   }

   /// <summary>
   /// Handle any clean-up when the game board is removed from the scene.
   /// </summary>
   public override void _ExitTree()
   {
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
      _gameBoard = new Tile[Globals.TileCount, Globals.TileCount];

      // Iterate and create rows, columns, and the gems for each tile.
      for (int row = 0; row < Globals.TileCount; ++row)
      {
         for (int column = 0; column < Globals.TileCount; ++column)
         {
            PullTile(row, column, 0);
         }
      }

      var matches = CheckForMatches(0);
      if (matches.Any())
      {
         HandleMatches(matches);

         List<PotentialMoveInfo> potentialMoves = GetPossibleMoves();
         if (!potentialMoves.Any())
         {
            // This shouldn't happen often, but we need to start over when it does.
            // No moves = no valid gameplay.
            Generate();
         }
      }

      // Recycle any tiles that were flagged for recycling during the generation process.
      _tilePool.DoRecycle();

      // Now show!
      Show();

      // Play a sound to indicate that the board is ready for play.
      // Do not play this at the very beginning of the game, only when the board has been regenerated
      // due to no valid moves.
      if (State == GameBoardState.Playable)
      {
         var boardReadySound = _gameScene.AudioNode.GetNode<AudioStreamPlayer>("MainAudio_GameBoardReady");
         boardReadySound?.Play();
      }

      // The game's afoot!
      return true;
   }

   public void Relocate(float x, float y)
   {
      Godot.Vector2 newPosition = new Godot.Vector2(x, y);
      GlobalPosition = newPosition;
   }

   /// <summary>
   /// Clears the game board of all tiles.
   /// </summary>
   public void Clear()
   {
      // Reset any selections prior to clearing the board.
      _primarySelection = null;
      _secondarySelection = null;

      // Reset the state.
      ProcessingRound = 0;
      RoundsToProcess = 0;
      State = GameBoardState.Initializing;

      // Recycle all tiles.
      _tilePool.DoRecycle();
   }

   /// <summary>
   /// Clears just the tile borders.
   /// </summary>
   public void ClearTileBorders()
   {
      if (_primarySelection != null)
      {
         _primarySelection.ResetBorderToBehaviorDefault();
      }

      if (_secondarySelection != null)
      {
         _secondarySelection.ResetBorderToBehaviorDefault();
      }
   }

   /// <summary>
   /// Clears the current selection.
   /// </summary>
   public void ClearSelection()
   {
      ClearTileBorders();

      _primarySelection = null;
      _secondarySelection = null;

      DebugLogger.Instance.Log("Selections cleared.", LogLevel.Trace);
   }

   /// <summary>
   /// Algorithm for checking the player's game board for matched gems.
   /// </summary>
   /// <param name="round"></param>
   public List<MatchDetails> CheckForMatches(int round)
   {
      DebugLogger.Instance.Log($"CheckForMatches(round {round}) Checking for matches...", LogLevel.Info);

      List<MatchDetails> matches = new List<MatchDetails>();

      for (int row = 0; row < Globals.TileCount; ++row)
      {
         for (int column = 0; column < Globals.TileCount; ++column)
         {
            // Evaluate horizontal-only
            List<MatchedTileInfo> horizontalmatches = new List<MatchedTileInfo>();
            if (EvaluateTileForMatch(row, column, Gem.GemType.UNKNOWN, EvaluationDirection.Horizontal, ref horizontalmatches))
            {
               var firstTile = horizontalmatches.FirstOrDefault().TileRef;
               float slice = horizontalmatches.Count / 2.0f;
               float midPoint = firstTile.GlobalPosition.X + (slice * Globals.TileSize) - (Globals.TileSize / 2.0f);

               matches.Add(new MatchDetails(horizontalmatches, EvaluationDirection.Horizontal, new Godot.Vector2(midPoint, firstTile.GlobalPosition.Y)));
            }

            // Now evaluate vertical-only
            List<MatchedTileInfo> verticalmatches = new List<MatchedTileInfo>();
            if (EvaluateTileForMatch(row, column, Gem.GemType.UNKNOWN, EvaluationDirection.Vertical, ref verticalmatches))
            {
               var firstTile = horizontalmatches.FirstOrDefault().TileRef;
               float slice = horizontalmatches.Count / 2.0f;
               float midPoint = firstTile.GlobalPosition.Y + (slice * Globals.TileSize) - (Globals.TileSize / 2.0f);

               matches.Add(new MatchDetails(verticalmatches, EvaluationDirection.Vertical, new Godot.Vector2(firstTile.GlobalPosition.X, midPoint)));
            }
         }
      }

      DebugLogger.Instance.Log($"CheckForMatches(round {round}) Match search complete. {matches.Count} matches found.", LogLevel.Trace);
      return matches;
   }

   /// <summary>
   /// Looks at the current state of the board's tiles and determines if there are possible moves.
   /// </summary>
   /// <returns>List of potential moves that could be played to create a match.</returns>
   public List<PotentialMoveInfo> GetPossibleMoves()
   {
      DebugLogger.Instance.Log("GetPossibleMoves() begin...", LogLevel.Info);

      List<PotentialMoveInfo> possibleMoves = new List<PotentialMoveInfo>();

      for (int row = 0; row < Globals.TileCount; ++row)
      {
         for (int column = 0; column < Globals.TileCount; ++column)
         {
            Tile currentTile = _gameBoard[row, column];
            if (currentTile != null)
            {
               MoveDirection possibleDirection = IsPotentialMatch(row, column, currentTile.CurrentGemType);
               if (possibleDirection != MoveDirection.NONE)
               {
                  DebugLogger.Instance.Log($"\tPossible move located at [{row}, {column}] (Direction = {(int)possibleDirection}) (Gem = {(int)currentTile.CurrentGemType})", LogLevel.Trace);
                  possibleMoves.Add(new PotentialMoveInfo(currentTile, row, column, possibleDirection));
               }
            }
         }
      }
      DebugLogger.Instance.Log($"GetPossibleMoves() complete. {possibleMoves.Count} possible moves found.", LogLevel.Info);
      return possibleMoves;
   }

   /// <summary>
   /// Swaps the primary and secondary tiles provided.
   /// </summary>
   /// <param name="primary"></param>
   /// <param name="secondary"></param>
   public void SwapSelectedTiles(Tile primary, Tile secondary)
   {
      if (DebugLogger.Instance.Enabled && DebugLogger.Instance.LoggingLevel == LogLevel.Info)
      {
         // Only check this information if we want to log debug info to avoid the additional cycles.
         var primaryCoordinates = GetTileCoordinates(primary);
         var secondaryCoordinates = GetTileCoordinates(secondary);
         DebugLogger.Instance.Log($"Attempting to swap [{primaryCoordinates.Item1}, {primaryCoordinates.Item2}]({(int)primary.CurrentGemType}) with [{secondaryCoordinates.Item1}, {secondaryCoordinates.Item2}]({(int)secondary.CurrentGemType})", LogLevel.Info);
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
      List<MatchDetails> matches = CheckForMatches(0);
      if (matches.Any())
      {
         // Matches found, so go ahead and swap positions.
         DebugLogger.Instance.Log("\tSwap approved. Matches found.", LogLevel.Info);

         primary.UpdateCoordinates(originalSecondaryCoordinates.Item1, originalSecondaryCoordinates.Item2);
         primary.GlobalPosition = originalSecondaryPosition;

         secondary.UpdateCoordinates(originalPrimaryCoordinates.Item1, originalPrimaryCoordinates.Item2);
         secondary.GlobalPosition = originalPrimaryPosition;

         // Reset the processing round back to one and handle the results of the matches.
         ProcessingRound = 0;
         RoundsToProcess = 0;
         HandleMatches(matches, ProcessingRound);

         // Reset the move timer.
         //TODO - scale this based on level/difficulty
         _moveTimerLabel.ResetTime();
      }
      else
      {
         // No matches. Swap everything back to the original positions and play a womp womp sound.
         DebugLogger.Instance.Log("\tNo matches would have resulted from this swap.", LogLevel.Info);

         _gameBoard[originalPrimaryCoordinates.Item1, originalPrimaryCoordinates.Item2] = primary;
         _gameBoard[originalSecondaryCoordinates.Item1, originalSecondaryCoordinates.Item2] = secondary;

         var badMoveSound = _gameScene.AudioNode.GetNode<AudioStreamPlayer>("Sound_BadMove");
         badMoveSound?.Play();

         // No swap, so no moves to animate. Turn input back on.
         DebugLogger.Instance.Log("Bad swap, so re-enabling input!", LogLevel.Info);
         SetProcessInput(true);
         State = GameBoardState.Playable;
      }

      // Clear any selections.
      ClearSelection();

      // If matches were found and handled, flag the board as ready for animating the drop.
      if (matches.Any())
      {
         DebugLogger.Instance.Log($"SwapSelectedTiles() has matches to process and is now sorted. {RoundsToProcess} rounds to process in total. Begin processing...", LogLevel.Trace);

         if (DebugLogger.Instance.Enabled && DebugLogger.Instance.LoggingLevel == LogLevel.Info)
         {
            int count = 0;
            foreach (var request in _moveRequests)
            {
               ++count;
               DebugLogger.Instance.Log($"\t\t{count} SwapSelectedTiles() {request.ToString()}", LogLevel.Trace);
            }
         }

         ProcessingRound = 0;
         DebugLogger.Instance.Log($"************************************************** ROUND {ProcessingRound} \"**************************************************", LogLevel.Trace);
         State = GameBoardState.AnimatingMoveResults;
      }
   }

   /// <summary>
   /// Removes the tile move request from the list of requested moves. 
   /// If all moves are finished and the list is empty, restore processing input
   /// to the gameboard.
   /// </summary>
   /// <param name="fulfilledRequest"></param>
   public void HandleTileMoveAnimationFinished(TileAnimationRequest fulfilledRequest)
   {
      DebugLogger.Instance.Log($"\tHandleTileMoveAnimationFinished (round {ProcessingRound}) {fulfilledRequest.Tile.ToString()} finished move. (request = {fulfilledRequest.ToString()})", LogLevel.Info);

      if (State == GameBoardState.AnimatingMoveResults)
      {
         int remainingMoveRequestsThisRound = 0;
         int remainingMoveRequestsInTotal = 0;
         IEnumerable<TileAnimationRequest> roundMoveRequests = null;

         remainingMoveRequestsInTotal = _moveRequests.Count;
         roundMoveRequests = _moveRequests.Where(r => r.RoundMoved == ProcessingRound);
         if (roundMoveRequests != null && roundMoveRequests.Any())
         {
            remainingMoveRequestsThisRound = roundMoveRequests.Count();
         }

         DebugLogger.Instance.Log($"\t\tHandleTileMoveAnimationFinished (round {ProcessingRound}) {_moveRequests.Count} requests remaining in total, {remainingMoveRequestsThisRound} remaining in this round.", LogLevel.Trace);

         int movingTilesRemainingThisRound = 0;
         int movingTilesRemainingInTotal = 0;
         IEnumerable<TileAnimationRequest> movingTilesInRound = null;

         movingTilesInRound = _movingTiles.Where(r => r.RoundMoved == ProcessingRound);
         DebugLogger.Instance.Log($"\t\tHandleTileMoveAnimationFinished (round {ProcessingRound}) {movingTilesInRound.Count()} left moving in this round, {_movingTiles.Count} remain in total.", LogLevel.Trace);
         if (movingTilesInRound != null && movingTilesInRound.Any())
         {
            remainingMoveRequestsThisRound = movingTilesInRound.Count();

            DebugLogger.Instance.Log($"\t\t\tHandleTileMoveAnimationFinished (round {ProcessingRound}) {remainingMoveRequestsThisRound} moving tiles in this round remaining.", LogLevel.Trace);

            TileAnimationRequest requestToRemove = null;
            _movingTiles.TryTake(out requestToRemove);
            if (requestToRemove != null)
            {
               DebugLogger.Instance.Log($"\t\t\tHandleTileMoveAnimationFinished (round {ProcessingRound}) Tile [{fulfilledRequest.Row}, {fulfilledRequest.Column}] has finished animating. Remove from the list!", LogLevel.Trace);
            }
            else
            {
               DebugLogger.Instance.Log($"\t\t\tHandleTileMoveAnimationFinished (round {ProcessingRound}) Tile [{fulfilledRequest.Row}, {fulfilledRequest.Column}] has finished animating, but couldn't be found in _movingTiles.", LogLevel.Trace);
            }
         }

         // Refresh the number of moving tiles left in this round.
         movingTilesInRound = _movingTiles.Where(r => r.RoundMoved == ProcessingRound);
         if (movingTilesInRound != null)
         {
            movingTilesRemainingThisRound = movingTilesInRound.Count();

            if (DebugLogger.Instance.LoggingLevel == LogLevel.Trace)
            {
               DebugLogger.Instance.Log($"\t\t\tmovingTilesInRound (round {ProcessingRound}) (count = {movingTilesRemainingThisRound}) Output:", LogLevel.Trace);
               foreach (var req in movingTilesInRound)
               {
                  DebugLogger.Instance.Log($"\t\t\t\t{req.ToString()}", LogLevel.Trace);
               }
            }
         }

         // Refresh the number of move requests in this round.
         roundMoveRequests = _moveRequests.Where(r => r.RoundMoved == ProcessingRound);
         if (roundMoveRequests != null)
         {
            remainingMoveRequestsThisRound = roundMoveRequests.Count();

            if (DebugLogger.Instance.LoggingLevel == LogLevel.Trace)
            {
               DebugLogger.Instance.Log($"\t\t\troundMoveRequests (round {ProcessingRound}) (count = {roundMoveRequests.Count()}) Output:", LogLevel.Trace);
               foreach (var req in roundMoveRequests)
               {
                  DebugLogger.Instance.Log($"\t\t\t\t{req.ToString()}", LogLevel.Trace);
               }
            }
         }

         if (DebugLogger.Instance.LoggingLevel == LogLevel.Info)
         {
            // Update the moving tiles and move request total counts.
            movingTilesRemainingInTotal = _movingTiles.Count;
            remainingMoveRequestsInTotal = _moveRequests.Count;

            DebugLogger.Instance.Log($"\t\tHandleTileMoveAnimationFinished (round {ProcessingRound}) movingTilesRemaining = {movingTilesRemainingInTotal}, remainingMoveRequests = {remainingMoveRequestsInTotal}, {ProcessingRound} >= {RoundsToProcess}, State = {State.ToString()}, remainingMoveRequestsThisRound = {remainingMoveRequestsThisRound}, movingTilesRemainingThisRound = {movingTilesRemainingThisRound}", LogLevel.Info);
         }

         // Check to see if this round is finished. If so, advance to the next round.
         // Do an additional check after advancing to see if the move is finished.
         if (remainingMoveRequestsThisRound == 0 && movingTilesRemainingThisRound == 0)
         {
            DebugLogger.Instance.Log($"\t\t\t4a. HandleTileMoveAnimationFinished (round {ProcessingRound}) done with this round; advance processing round by one.", LogLevel.Trace);
            DebugLogger.Instance.Log($"*******************************************************************************************************************************", LogLevel.Trace);

            ++ProcessingRound;
            if (ProcessingRound < RoundsToProcess)
            {
               DebugLogger.Instance.Log($"************************************************** ROUND {ProcessingRound} \"**************************************************", LogLevel.Trace);
            }

            if (ProcessingRound >= RoundsToProcess)
            {
               DebugLogger.Instance.Log($"Done handling all rounds! End Turn.", LogLevel.Trace);
               EndTurn();
            }
         }
      }
   }

   #endregion

   #region Private Methods

   /// <summary>
   /// Gets the board back into a state where it is playable again after processing a turn.
   /// </summary>
   private void EndTurn()
   {
      // No more animated moves being tracked. Turn input back on.
      DebugLogger.Instance.Log("Re-enabling input!", LogLevel.Info);
      DebugLogger.Instance.LogGameBoard($"MOVE END - Resulting game board:", Globals.TileCount, ref _gameBoard, LogLevel.Info);
      State = GameBoardState.Playable;
      SetProcessInput(true);

      ProcessingRound = 0;
      RoundsToProcess = 0;

      _tilePool.DoRecycle();

      List<PotentialMoveInfo> potentialMoves = GetPossibleMoves();
      if (!potentialMoves.Any())
      {
         // No moves are possible with the current board.
         // A new board needs to be generated.
         //TODO - figure out how this should be handled in multiplayer battle scenarios. It isn't the player's fault if this happens.
         DebugLogger.Instance.Log("\tNo possible moves detected on this game board.", LogLevel.Info);
         var noMoreMovesSound = _gameScene.AudioNode.GetNode<AudioStreamPlayer>("MainAudio_NoMoreMoves");
         noMoreMovesSound?.Play();
      }
   }

   /// <summary>
   /// Given a list of Tile objects, locate their row and column coordinates in the game board and return them as a list of tuples.
   /// </summary>
   /// <param name="tilesToLocate"></param>
   /// <returns>A list of tuples representing the coordinates of the tiles in the provided list.</returns>
   private Tuple<int, int> GetTileCoordinates(Tile tileToLocate)
   {
      for (int row = 0; row < Globals.TileCount; ++row)
      {
         for (int column = 0; column < Globals.TileCount; ++column)
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
      DebugLogger.Instance.Log($"Evaluate [{row}, {column}]({(int)gemType}) for potential matches...", LogLevel.Trace);

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
             _gameBoard[row, column - 1].CurrentGemType != gemType &&
             _gameBoard[row, column - 2].CurrentGemType == gemType &&
             _gameBoard[row, column - 3].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match to the left.", LogLevel.Trace);
            return MoveDirection.Left;
         }

         // Look right.
         if (column <= Globals.TileCount - 4 &&
             _gameBoard[row, column + 1].CurrentGemType != gemType &&
             _gameBoard[row, column + 2].CurrentGemType == gemType &&
             _gameBoard[row, column + 3].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match to the right.", LogLevel.Trace);
            return MoveDirection.Right;
         }

         // Look up.
         if (row >= 3 &&
             _gameBoard[row - 1, column].CurrentGemType != gemType &&
             _gameBoard[row - 2, column].CurrentGemType == gemType &&
             _gameBoard[row - 3, column].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match above.", LogLevel.Trace);
            return MoveDirection.Up;
         }

         // Look down.
         if (row <= Globals.TileCount - 4 &&
             _gameBoard[row + 1, column].CurrentGemType != gemType &&
             _gameBoard[row + 2, column].CurrentGemType == gemType &&
             _gameBoard[row + 3, column].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible linear slide match below.", LogLevel.Trace);
            return MoveDirection.Down;
         }
      }

      // Potential Move Type: Wedge Slide (x o x -> move another x in from above or below the middle tile)
      {
         // Look left.
         if (column >= 1 && column < Globals.TileCount && row >= 1 && row < Globals.TileCount - 1 &&
             _gameBoard[row - 1, column - 1].CurrentGemType == gemType &&
             _gameBoard[row + 1, column - 1].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide match to the left.", LogLevel.Trace);
            return MoveDirection.Left;
         }

         // Look right.
         if (column >= 0 && column < Globals.TileCount - 1 && row >= 1 && row < Globals.TileCount - 1 &&
             _gameBoard[row - 1, column + 1].CurrentGemType == gemType &&
             _gameBoard[row + 1, column + 1].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide match to the right.", LogLevel.Trace);
            return MoveDirection.Right;
         }

         // Look up.
         if (column >= 1 && column < Globals.TileCount - 1 && row >= 1 && row < Globals.TileCount - 1 &&
             _gameBoard[row + 1, column - 1].CurrentGemType == gemType &&
             _gameBoard[row + 1, column + 1].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide match up.", LogLevel.Trace);
            return MoveDirection.Up;
         }

         // Look down.
         if (column >= 1 && column < Globals.TileCount - 1 && row >= 0 && row < Globals.TileCount - 1 &&
             _gameBoard[row + 1, column - 1].CurrentGemType == gemType &&
             _gameBoard[row + 1, column + 1].CurrentGemType == gemType)
         {
            DebugLogger.Instance.Log("\tPossible wedge slide down.", LogLevel.Trace);
            return MoveDirection.Down;
         }
      }

      DebugLogger.Instance.Log("\tNo possible moves found.", LogLevel.Trace);
      return MoveDirection.NONE;
   }

   /// <summary>
   /// Evaluates the tile for existing matches and moves on to the next based on the provided direction.
   /// Note: Does not predict the potential of a match using this tile, only the existance of a valid match in the present.
   /// </summary>
   private bool EvaluateTileForMatch(int row, int column, Gem.GemType previousGem, EvaluationDirection direction, ref List<MatchedTileInfo> matches)
   {
      DebugLogger.Instance.Log($"EvaluateTileForMatch() [{row}, {column}]({(int)previousGem}) (Direction = {(int)direction})", LogLevel.Trace);

      if (row >= 0 && row < Globals.TileCount && column >= 0 && column < Globals.TileCount)
      {
         // Look at the current tile and compare against the previous tile. If the tile is
         // of type "unknown" then add it to the matches list, because that is the starting gem for this check.
         // The minimum match count check at the end will determine if there was truly a match or not.
         Tile currentTile = _gameBoard[row, column];
         if (currentTile != null)
         {
            if (currentTile.CurrentGemType == previousGem || previousGem == Gem.GemType.UNKNOWN)
            {
               matches.Add(new MatchedTileInfo(currentTile, row, column));
            }
            else if (previousGem != Gem.GemType.UNKNOWN && currentTile.CurrentGemType != previousGem)
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
               if (nextColumn >= Globals.TileCount)
               {
                  if (matches.Count >= Globals.MinimumMatchCount)
                  {
                     return true;
                  }

                  return false;
               }

               EvaluateTileForMatch(row, nextColumn, currentTile.CurrentGemType, direction, ref matches);
            }
            else if (direction == EvaluationDirection.Vertical)
            {
               int nextRow = row + 1;

               // If we have reached the end of the column, see if we have enough vertical matches.
               if (nextRow >= Globals.TileCount)
               {
                  if (matches.Count >= Globals.MinimumMatchCount)
                  {
                     return true;
                  }

                  return false;
               }

               EvaluateTileForMatch(nextRow, column, currentTile.CurrentGemType, direction, ref matches);
            }
         }
      }

      DebugLogger.Instance.Log($"EvaluateTileForMatch() {matches.Count} found.", LogLevel.Trace);
      return matches.Count >= Globals.MinimumMatchCount;
   }

   /// <summary>
   /// Handles the matches by going through each tile matched, calculating the score, removing, and replacing tiles
   /// by shifting downward.
   /// </summary>
   private void HandleMatches(List<MatchDetails> matches, int round = 0)
   {
      foreach (var match in matches)
      {
         // Update the score and display points gained animation.
         if (State == GameBoardState.ProcessingTurn)
         {
            var scoreUpdateResults = Score.IncreaseScore(match, round);
            DebugLogger.Instance.Log($"HandleMatches(round {round}) scoreUpdateResults (BasePoints = {scoreUpdateResults.BasePoints}) (Bonus = {scoreUpdateResults.BonusPointsRewarded})", LogLevel.Info);

            //TODO this isn't always triggering, especially when there are subsequent rounds of matches. [BUG]
            _animatedPointPool.Play(match.GlobalPositionAverage, scoreUpdateResults);
            _uiNode.GetNode<Godot.Label>("Labels/ScoreValueLabel").Text = scoreUpdateResults.UpdatedScore.ToString("N0");
         }

         // Remove the matched tiles from the board.
         foreach (var tile in match.Tiles)
         {
            DebugLogger.Instance.Log($"HandleMatches(round {round}) recycling [{tile.Row}, {tile.Column}]({(int)tile.TileRef.CurrentGemType})...", LogLevel.Trace);

            // Put the tile back in the pool for availability.
            DebugLogger.Instance.Log($"\tFlagging tile for recycling after the move ends.", LogLevel.Trace);
            tile.TileRef.RecyclePostMove = true;
            RequestTileAnimate(tile.TileRef, tile.TileRef.Row, tile.TileRef.Column, round, TileAnimationRequest.AnimationType.Recycling);

            // Remove the tile from the game board grid.
            DebugLogger.Instance.Log($"\tSetting [{tile.Row}, {tile.Column}] to null", LogLevel.Trace);
            _gameBoard[tile.Row, tile.Column] = null;
         }
      }

      // Collapse the board such that null tiles are only above valid tiles.
      for (int column = 0; column < Globals.TileCount; ++column)
      {
         // Move up the column starting from the bottom row to
         // find the first null entry in the grid. Once it has been
         // identified, that will be the starting point for the collapse of
         // the column.
         for (int row = (Globals.TileCount - 1); row > 0; --row)
         {
            if (_gameBoard[row, column] == null)
            {
               CompressColumn(ref matches, row, column, row, round /* cache in the recursive method the actual starting point */);
               break;
            }
         }
      }

      // After all holes are plugged with new tiles, evaluate the board for any bonus matches made through the drop.
      ReplaceRemovedTiles(round);

      // Need to keep checking for matches after the collapse until no more matches are found.
      var newMatches = CheckForMatches(round);
      if (newMatches.Any())
      {
         HandleMatches(newMatches, round + 1);
      }

      // Increment the number of rounds to process.
      ++RoundsToProcess;

      DebugLogger.Instance.Log($"HandleMatches() level {round} done. Rounds to process = {RoundsToProcess}.", LogLevel.Info);
   }

   /// <summary>
   /// Requests the tile to animate and move to the specified row, column.
   /// </summary>
   /// <param name="tile"></param>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <param name="round"></param>
   /// <param name="animationType"></param>
   private void RequestTileAnimate(Tile tile, int row, int column, int round, TileAnimationRequest.AnimationType animationType)
   {
      if (State == GameBoardState.ProcessingTurn)
      {
         // This is used by multiple animation types, so instantiate it once.
         TileAnimationRequest baseRequest = new TileAnimationRequest()
         {
            Tile = tile,
            Row = row,
            Column = column,
            RoundMoved = round,
            Type = TileAnimationRequest.AnimationType.Animated
         };

         switch (animationType)
         {
            case TileAnimationRequest.AnimationType.Static:
               {
                  // This is a fresh pull. Move the tile into a droppable position first, followed by the move request.
                  DebugLogger.Instance.Log($"RequestTileAnimate() {tile.Name} move from [{tile.Row}, {tile.Column}] to [{row}, {column}]. Round: {round}, Type: Static", LogLevel.Trace);

                  TileAnimationRequest prepareRequest = new TileAnimationRequest()
                  {
                     Tile = tile,
                     Row = row,
                     Column = column,
                     RoundMoved = round,
                     Type = TileAnimationRequest.AnimationType.Static
                  };

                  _moveRequests.Add(prepareRequest);
                  DebugLogger.Instance.Log($"RequestTileAnimate() A adding prepareRequest for {prepareRequest.ToString()}", LogLevel.Info);

                  _moveRequests.Add(baseRequest);
                  DebugLogger.Instance.Log($"RequestTileAnimate() B adding moveRequest for {baseRequest.ToString()}", LogLevel.Info);
               }
               break;

            case TileAnimationRequest.AnimationType.Animated:
               {
                  DebugLogger.Instance.Log($"RequestTileAnimate() {tile.Name} move from [{tile.Row}, {tile.Column}] to [{row}, {column}]. Round: {round}, Type: Animated", LogLevel.Trace);

                  _moveRequests.Add(baseRequest);

                  DebugLogger.Instance.Log($"RequestTileAnimate() C adding moveRequest for {baseRequest.ToString()}", LogLevel.Trace);
               }
               break;

            case TileAnimationRequest.AnimationType.Recycling:
               {
                  // This tile will be recycling after the move ends, so animate it differently from a move.
                  DebugLogger.Instance.Log($"RequestTileAnimate() {tile.ToString()} recycling.", LogLevel.Trace);

                  baseRequest.Type = TileAnimationRequest.AnimationType.Recycling;
                  _moveRequests.Add(baseRequest);

                  DebugLogger.Instance.Log($"RequestTileAnimate() A adding prepareRequest for {baseRequest.ToString()}", LogLevel.Trace);
               }
               break;
         }
      }
      else
      {
         if (tile.RecyclePostMove)
         {
            tile.Hide();
         }
         else
         {
            // Just directly move the tile into position. Largely used by the initial generation of the board.
            tile.MoveTile(this, new TileAnimationRequest() { Tile = tile, Row = row, Column = column, Type = TileAnimationRequest.AnimationType.Static });
         }
      }
   }

   /// <summary>
   /// Moves the higher tiles down, stomping the original tile and setting it to null until the only null
   /// tiles left are above compressed tiles.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <param name="round"></param>
   private void CompressColumn(ref List<MatchDetails> matches, int row, int column, int startingRow, int round)
   {
      bool compressed = false;
      DebugLogger.Instance.Log($"CompressColumn(round {round}) [{row}, {column}] starting from row {startingRow}", LogLevel.Info);

      int aboveRow = row - 1;
      if (aboveRow >= 0)
      {
         Tile currentTile = _gameBoard[row, column];

         // If the tile above is valid, and the current tile is null, start the swap operation to compress.
         Tile higherTile = _gameBoard[aboveRow, column];
         DebugLogger.Instance.Log($"\tHigher tile coordinates = [{aboveRow}, {column}]", LogLevel.Trace);

         if (higherTile != null && currentTile == null)
         {
            // Visually slide this tile down.
            DebugLogger.Instance.Log($"\t\tMove [{aboveRow}, {column}]({(int)higherTile.CurrentGemType}) down", LogLevel.Trace);

            RequestTileAnimate(higherTile, row, column, round, TileAnimationRequest.AnimationType.Animated);
            compressed = true;

            // Swap the data between grid slots to shift the non-null slot into the null.
            _gameBoard[row, column] = _gameBoard[aboveRow, column];
            _gameBoard[aboveRow, column] = null;

            int belowRow = row + 1;
            if (belowRow < Globals.TileCount && _gameBoard[belowRow, column] == null)
            {
               DebugLogger.Instance.Log($"\t\tContinue compression from below (= [{belowRow}, {column}]) starting row {startingRow}", LogLevel.Trace);
               CompressColumn(ref matches, belowRow, column, startingRow, round);
            }
            else
            {
               DebugLogger.Instance.Log($"\t\tContinue compression from [{row}, {column}] starting row {startingRow}", LogLevel.Trace);
               CompressColumn(ref matches, row, column, startingRow, round);
            }
         }
         // Else, we need to continue to move up until we have a valid higher tile and a potential null.
         else
         {
            DebugLogger.Instance.Log($"\t\tContinue compression from above (= [{aboveRow}, {column}]) starting row {startingRow}", LogLevel.Trace);
            CompressColumn(ref matches, aboveRow, column, startingRow, round);
         }
      }
      else if (_gameBoard[startingRow, column] == null)
      {
         DebugLogger.Instance.Log($"\tTile [{startingRow}, {column}] is null. Check if compression is complete.", LogLevel.Trace);

         // If we're still null at the bottom, see if there are any more tiles to compress down.
         // If there are any tiles, start all over again until we've moved everything down.
         bool compressionComplete = true;
         for (int check = startingRow; check >= 0; --check)
         {
            if (_gameBoard[check, column] != null)
            {
               DebugLogger.Instance.Log($"\t\tTile at [{check}, {column}]({(int)_gameBoard[check, column].CurrentGemType}) is not null.", LogLevel.Trace);
               compressionComplete = false;
            }
         }

         if (!compressionComplete)
         {
            DebugLogger.Instance.Log($"\tCompression not complete. CompressColumn() again with [{startingRow}, {column}] starting row {startingRow}", LogLevel.Trace);
            CompressColumn(ref matches, startingRow, column, startingRow, round);
         }
      }

      if (compressed)
      {
         DebugLogger.Instance.LogGameBoard($"CompressColumn() ([{row}, {column}] startingRow = {startingRow}) resulting game board:", Globals.TileCount, ref _gameBoard, LogLevel.Trace);
      }

      DebugLogger.Instance.Log($"CompressColumn(round {round}) ([{row}, {column}] startingRow = {startingRow}) returning", LogLevel.Info);
   }

   /// <summary>
   /// Goes through the board and replaces any instances of a null entry with a new tile and random gem.
   /// </summary>
   /// <param name="round"></param>
   private void ReplaceRemovedTiles(int round)
   {
      DebugLogger.Instance.Log($"ReplaceRemovedTiles(round {round}) begin...", LogLevel.Info);

      for (int row = 0; row < Globals.TileCount; ++row)
      {
         for (int column = 0; column < Globals.TileCount; ++column)
         {
            if (_gameBoard[row, column] == null)
            {
               DebugLogger.Instance.Log($"\tReplacing [{row}, {column}]", LogLevel.Trace);
               var result = PullTile(row, column, round);
               if (result != null)
               {
                  //result.Show();
                  DebugLogger.Instance.Log($"\tNew tile pulled and placed at [{row}, {column}] with gem {(int)result.CurrentGemType}", LogLevel.Info);
               }
            }
         }
      }

      DebugLogger.Instance.Log($"ReplaceRemovedTiles(round {round}) complete.", LogLevel.Info);
   }

   /// <summary>
   /// Generates a tile at the specified grid location.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <param name="round"></param>
   /// <exception cref="Exception"></exception>
   private Tile PullTile(int row, int column, int round)
   {
      // Get a tile from the pool and move it into position.
      Tile tile = _tilePool.Pull();
      if (tile != null)
      {
         DebugLogger.Instance.Log($"{tile.Name} pulled. (old row = {tile.Row}, old column = {tile.Column}, old gem = {(int)tile.CurrentGemType})", LogLevel.Trace);

         if (!tile.MouseEventHandlerRegistered)
         {
            tile.OnTileMouseEvent += Tile_OnTileMouseEvent;
            tile.MouseEventHandlerRegistered = true;
            DebugLogger.Instance.Log($"{tile.Name} registered mouse handler.", LogLevel.Trace);
         }

         int randomized = Random.Shared.Next(0, Convert.ToInt32(Gem.GemType.GemType_Count));
         tile.UpdateCoordinates(row, column);
         tile.SetGemType((Gem.GemType)Enum.ToObject(typeof(Gem.GemType), randomized));
         RequestTileAnimate(tile, row, column, round, TileAnimationRequest.AnimationType.Static);
      }

      _gameBoard[row, column] = tile;

      return tile;
   }

   #endregion

   #region Event Handlers

   /// <summary>
   /// Handle mouse events that have bubbled up from a gem.
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   private void Tile_OnTileMouseEvent(object sender, TileMouseEventArgs e)
   {
      bool isAnimating = _movingTiles.Count() > 0;

      if (!isAnimating && State == GameBoardState.Playable && sender is Tile tile)
      {
         if (tile != null)
         {
            switch (e.EventType)
            {
               case TileMouseEventArgs.MouseEventType.Enter:
                  {
                     if (tile != _primarySelection && tile != _secondarySelection)
                     {
                        tile.ChangeBorder(TileBorder.Hovered);
                     }
                  }
                  break;
               case TileMouseEventArgs.MouseEventType.Leave:
                  {
                     if (tile != _primarySelection && tile != _secondarySelection)
                     {
                        tile.ResetBorderToBehaviorDefault();
                     }
                  }
                  break;
               case TileMouseEventArgs.MouseEventType.Click:
                  {
                     bool selectionMade = false;
                     AudioStreamPlayer selectionSound = null;

                     if (_primarySelection == null && tile != _primarySelection)
                     {
                        _primarySelection = tile;
                        selectionMade = true;
                        selectionSound = _gameScene.AudioNode.GetNode<AudioStreamPlayer>("Sound_SelectPrimary");

                        if (DebugLogger.Instance.Enabled)
                        {
                           var coordinates = GetTileCoordinates(_primarySelection);
                           DebugLogger.Instance.Log($"Gem_OnGemMouseEvent() first selection [{coordinates.Item1}, {coordinates.Item2}]({(int)_primarySelection.CurrentGemType})", LogLevel.Trace);
                        }
                     }
                     else if (_secondarySelection == null && tile != _secondarySelection)
                     {
                        _secondarySelection = tile;
                        selectionMade = true;
                        selectionSound = _gameScene.AudioNode.GetNode<AudioStreamPlayer>("Sound_SelectSecondary");

                        if (DebugLogger.Instance.Enabled)
                        {
                           var coordinates = GetTileCoordinates(_secondarySelection);
                           DebugLogger.Instance.Log($"Gem_OnGemMouseEvent() second selection [{coordinates.Item1}, {coordinates.Item2}]({(int)_secondarySelection.CurrentGemType})", LogLevel.Trace);
                        }
                     }

                     if (selectionMade)
                     {
                        tile.ChangeBorder(TileBorder.Selected);
                        selectionSound.Play();
                     }

                     if (_primarySelection != null && _secondarySelection != null)
                     {
                        // Disable input while the move is playing out.
                        DebugLogger.Instance.Log("Disabling input...", LogLevel.Info);
                        State = GameBoardState.ProcessingTurn;
                        SetProcessInput(false);
                        ClearTileBorders();

                        DebugLogger.Instance.Log($"Gem_OnGemMouseEvent() both selections made. Calling SwapSelectedTiles().", LogLevel.Info);
                        DebugLogger.Instance.Log("MOVE BEGIN", LogLevel.Info);
                        DebugLogger.Instance.IndentLevel = 1;

                        SwapSelectedTiles(_primarySelection, _secondarySelection);

                        DebugLogger.Instance.IndentLevel = 0;
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

   /// <summary>
   /// Handles when the move timer has timed out.
   /// </summary>
   private void _moveTimer_Timeout()
   {
      //TODO game over!
      MoveTimerLabel moveTimerLabel = _uiNode.GetNode<MoveTimerLabel>("MoveTimerLabel");
      if (moveTimerLabel != null)
      {
         moveTimerLabel.ResetAppearance();
      }
   }

   /// <summary>
   /// Handles when the move timer has run out.
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   private void _moveTimerLabel_OnTimerFinished(object sender, EventArgs e)
   {
      if (sender == _moveTimerLabel)
      {
         //TODO - don't reset. This is temporary.
         // Time has run out. Tell the player of this board the game is over for them.
         _moveTimerLabel.ResetTime();
      }
   }

   #endregion

   #region Private Members

   // List of tiles currently being moved. Once they are done animating,
   // they are removed from the list. If all tiles are removed, that is when
   // the game board can process input again.
   private BlockingCollection<TileAnimationRequest> _movingTiles = new BlockingCollection<TileAnimationRequest>();
   private BlockingCollection<TileAnimationRequest> _moveRequests = new BlockingCollection<TileAnimationRequest>();

   // Manager for this game board's animated points pool.
   private AnimatedPointPool _animatedPointPool = null;

   // Manager for a pool of avaialble tile objects.
   private TilePool _tilePool = null;

   // Cache a ref to the parent game scene.
   private GameScene _gameScene = null;

   // Grid layout representation of the game board.
   private Tile[,] _gameBoard;

   // Selected tiles for swap consideration.
   private Tile _primarySelection;
   private Tile _secondarySelection;

   // The previously processed request. Used to determine if the current request was handled as an animated move following a static move.
   private TileAnimationRequest _previouslyProcessedRequest = null;

   // Random number generator.
   private System.Random _rngesus;

   // Reference to the UI node unique to this game board.
   private VBoxContainer _uiNode;

   // Reference to the object that handles the move timer.
   private MoveTimerLabel _moveTimerLabel;

   #endregion
}
