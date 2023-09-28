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
      _gameBoard = new Tile[TileSize, TileSize];

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
      CheckForMatches();
   }

   #endregion

   #region Private Members

   // Cache of the starting position for the board's top-leftmost corner.
   private Vector2 _startPosition = new Vector2(0, 0);

   // Cache of the screen size used in calculating board placement.
   private Vector2 _screenSize = new Vector2(0, 0);

   // Grid layout representation of the game board.
   private Tile[,] _gameBoard;

   #endregion
}
