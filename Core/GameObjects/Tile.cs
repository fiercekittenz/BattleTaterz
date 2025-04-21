using BattleTaterz.Core;
using BattleTaterz.Core.Enums;
using Godot;
using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Tile;

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
   /// Provide the GameBoard parent node as a parameter to the method, because
   /// it is possible for the Tile to be removed from the tree while animating.
   /// </summary>
   /// <param name="parent"></param>
   /// <param name="row"></param>
   /// <param name="column"></param>
   /// <param name="isNew"></param>
   /// <param name="shouldAnimate"></param>
   public void MoveTile(GameBoard parent, int row, int column, bool isNew, bool shouldAnimate)
   {
      float offset = (Globals.TileSize / 2) + 10;

      Row = row;
      Column = column;
      Godot.Vector2 newPosition = new Godot.Vector2((column * Globals.TileSize) + offset, 
                                                    (row * Globals.TileSize) + offset);

      if (shouldAnimate)
      {
         if (isNew)
         {
            // This is a freshly generated tile. It won't have a position yet, so the
            // start position needs to be above the column it'll drop from.
            Position = new Godot.Vector2((column * Globals.TileSize) + offset, (Globals.TileSize + offset) * -1);
         }

         var tween = GetTree().CreateTween();
         tween.SetParallel(true);
         tween.TweenProperty(this, "position", newPosition, 0.3f).SetEase(Tween.EaseType.In);
         tween.TweenProperty(this, "position", new Godot.Vector2(newPosition.X, newPosition.Y - 20), 0.15f).SetEase(Tween.EaseType.Out);
         tween.TweenProperty(this, "position", newPosition, 0.1f).SetEase(Tween.EaseType.In);
         tween.Finished += (() =>
         {
            // Clean up and let the GameBoard know that this tile is done animating.
            tween.Kill();
            parent.HandleTileMoveAnimationFinished(this, row, column);
         });
      }
      else
      {
         Position = newPosition;
      }
   }

   #endregion
}
