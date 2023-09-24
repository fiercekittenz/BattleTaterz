using Godot;
using System;
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

   #endregion

   #region Public Methods

   /// <summary>
   /// Called when the node enters the scene tree for the first time. 
   /// </summary>
   public override void _Ready()
   {
      _screenSize = GetViewportRect().Size;
      _startPosition = GlobalPosition;

      Generate();
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
   public void Generate()
   {
      // Clear the board before generating a new one.
      Clear();

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
            }
         }
      }

      // Reposition the entire board.
      float centeredX = (_screenSize.X / 2) - ((TileSize * TileCount) / 2);
      float centeredY = (_screenSize.Y / 2) - ((TileSize * TileCount) / 2);
      GlobalPosition = new Vector2(centeredX, centeredY);
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
   /// A debug button action to generate a whole new board.
   /// </summary>
   public void OnDebugResetButtonPressed()
   {
      Generate();
   }

   #endregion

   #region Private Members

   // Cache of the starting position for the board's top-leftmost corner.
   private Vector2 _startPosition = new Vector2(0, 0);

   // Cache of the screen size used in calculating board placement.
   private Vector2 _screenSize = new Vector2(0, 0);

   #endregion
}
