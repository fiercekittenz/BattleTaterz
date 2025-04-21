using BattleTaterz.Core;
using BattleTaterz.Core.Enums;
using BattleTaterz.Core.UI;
using Godot;
using System;
using System.Data.Common;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static Tile;

public partial class Tile : Node2D
{
   #region Public Properties

   /// <summary>
   /// Local ref to the game board that owns this tile.
   /// </summary>
   public GameBoard OwningBoard { get; set; }

   /// <summary>
   /// References to the row and column where this tile resides.
   /// </summary>
   public int Row { get; set; } = 0;
   public int Column { get; set; } = 0;

   /// <summary>
   /// There's no performant way to check if an event handler is set,
   /// so tracking a boolean value is the safest bet.
   /// </summary>
   public bool MouseEventHandlerRegistered { get; set; } = false;

   /// <summary>
   /// Accessor for the current gem type assigned to this tile.
   /// </summary>
   public Gem.GemType CurrentGemType
   {
      get
      {
         if (_gem != null)
         {
            return _gem.CurrentGem;
         }

         return Gem.GemType.UNKNOWN;
      }
   }

   /// <summary>
   /// Indicates if the tile is available for reuse from the tile pool.
   /// </summary>
   public bool IsAvailable { get; private set; } = true;

   /// <summary>
   /// An event that can be listened to for mouse input to the tile.
   /// </summary>
   public event EventHandler<TileMouseEventArgs> OnTileMouseEvent;

   #endregion

   #region Public Methods

   /// <summary>
   /// Called when the node enters the scene tree for the first time. 
   /// </summary>
   public override void _Ready()
   {
      Hide();
      _gem = GetNode<Gem>("Gem");

      int randomized = Random.Shared.Next(0, Convert.ToInt32(Gem.GemType.GemType_Count));
      _gem.SetGemType((Gem.GemType)Enum.ToObject(typeof(Gem.GemType), randomized));
      _gem.Position = new Godot.Vector2(0, 0);
      _gem.OnGemMouseEvent += _gem_OnGemMouseEvent;
   }

   /// <summary>
   /// Called every frame. 'delta' is the elapsed time since the previous frame.
   /// </summary>
   /// <param name="delta"></param>
   public override void _Process(double delta)
   {
   }

   /// <summary>
   /// Updates the gem to use the specified gemtype.
   /// </summary>
   /// <param name="gemType"></param>
   public void SetGemType(Gem.GemType gemType)
   {
      _gem.SetGemType(gemType);
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
   /// Mark the tile as unavailable for use.
   /// </summary>
   public void MarkUnavailable()
   {
      IsAvailable = false;
   }

   /// <summary>
   /// Clears current gem and coordinate information and sets the tile as available to the tile pool.
   /// </summary>
   public void Recycle()
   {
      Row = -1;
      Column = -1;
      _gem.SetGemType(Gem.GemType.UNKNOWN);
      IsAvailable = true;
      GlobalPosition = new Godot.Vector2(-1, -1);
      Hide();
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
         tween.TweenProperty(this, "position", newPosition, 0.4f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Spring);
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

   #region Private Methods

   /// <summary>
   /// Handles mouse events for the attached gem node.
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   private void _gem_OnGemMouseEvent(object sender, BattleTaterz.Core.UI.TileMouseEventArgs e)
   {
      // Bubble up the mouse event if this tile is active.
      if (OwningBoard != null &&
          OwningBoard.IsReady &&
          !IsAvailable &&
          Row >= 0 && 
          Column >= 0 &&
          CurrentGemType != Gem.GemType.UNKNOWN)
      {
         OnTileMouseEvent?.Invoke(this, e);
      }
   }

   #endregion

   #region Private Members

   private Gem _gem;

   #endregion
}
