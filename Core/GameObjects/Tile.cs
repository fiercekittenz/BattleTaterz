using Godot;
using System;

public partial class Tile : Node2D
{
   #region Public Properties

   /// <summary>
   /// Reference to the Gem assigned to this tile.
   /// </summary>
   public Gem GemRef { get; private set; } = null;

   /// <summary>
   /// References to the row and column where this tile resides.
   /// </summary>
   public int Row { get; set; } = 0;
   public int Column { get; set; } = 0;

   #endregion

   #region Public Methods

   /// <summary>
   /// Called when the node enters the scene tree for the first time. 
   /// </summary>
   public override void _Ready()
   {
   }

   /// <summary>
   /// Called every frame. 'delta' is the elapsed time since the previous frame.
   /// </summary>
   /// <param name="delta"></param>
   public override void _Process(double delta)
   {
   }

   /// <summary>
   /// Sets the gem reference property value and moves it into position.
   /// </summary>
   /// <param name="gem"></param>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <param name="tileSize"></param>
   public void SetGemReference(Gem gem, int row, int column, int tileSize)
   {
      GemRef = gem;
      AddChild(gem);
      gem.Position = new Vector2(0, 0);
   }

   /// <summary>
   /// Sets the row and column coordinates for the tile.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   public void UpdateCoordinates(int row, int column)
   {
      Row = row;
      Column = column;
   }

   /// <summary>
   /// Moves the tile to a specific position based on the row and column.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <param name="tileSize"></param>
   public void MoveTile(int row, int column, int tileSize)
   {
      Row = row;
      Column = column;

      //TODO - animate this

      Position = new Vector2(column * tileSize, row * tileSize);
   }

   #endregion
}
