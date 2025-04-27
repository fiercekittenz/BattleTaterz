using BattleTaterz.Core;
using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Gameplay;
using BattleTaterz.Core.Gameplay.TileBehaviors;
using BattleTaterz.Core.System;
using BattleTaterz.Core.UI;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Data.Common;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Godot.WebSocketPeer;
using static Tile;

public partial class Tile : PoolObject
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
   /// Describes the behavior of this tile as it pertains to gameplay.
   /// </summary>
   public TileBehavior Behavior { get; set; } = new DefaultBehavior();

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
   /// Indicates if the tile is currently animating in a drop or not.
   /// </summary>
   public bool IsAnimating { get; private set; } = false;

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

      _border = GetNode<AnimatedSprite2D>("Border");
      _border.SpriteFrames = GD.Load<SpriteFrames>("res://GameObjectResources/Grid/tile_borders.tres");
      ChangeBorder(TileBorder.Default);
   }

   /// <summary>
   /// Override ToString() for easier debugging of logs.
   /// </summary>
   /// <returns></returns>
   public override string ToString()
   {
      return $"{Name} (Row = {Row}, Column = {Column}, CurrentGemType = {(int)CurrentGemType}, IsAvailable = {IsAvailable.ToString()}, IsAnimating = {IsAnimating.ToString()}, RecyclePostMove = {MarkedForRecycling.ToString()})";
   }

   /// <summary>
   /// Updates the gem to use the specified gemtype.
   /// </summary>
   /// <param name="gemType"></param>
   public void SetGemType(Gem.GemType gemType)
   {
      DebugLogger.Instance.Log($"{Name} changing gem type from [{(int)CurrentGemType}] to [{(int)gemType}].", LogLevel.Trace);

      _gem.SetGemType(gemType);
   }

   /// <summary>
   /// Changes the border of the tile to the appropriate sprite frame.
   /// </summary>
   /// <param name="borderType"></param>
   public void ChangeBorder(TileBorder borderType)
   {
      _border.Frame = (int)borderType;
   }

   /// <summary>
   /// Resets the border to the appropriate sprite for the tile's behavior.
   /// </summary>
   public void ResetBorderToBehaviorDefault()
   {
      _border.Frame = (int)Behavior.Graphic;
   }

   /// <summary>
   /// Sets the row and column coordinates for the tile.
   /// </summary>
   /// <param name="row"></param>
   /// <param name="column"></param>
   public void UpdateCoordinates(int row, int column)
   {
      DebugLogger.Instance.Log($"{Name} updating coordinates from [{Row}, {Column}] to [{row}, {column}].", LogLevel.Trace);

      Row = row;
      Column = column;
   }

   /// <summary>
   /// Clears current gem and coordinate information and sets the tile as available to the tile pool.
   /// </summary>
   public override void Recycle()
   {
      DebugLogger.Instance.Log($"{Name} recycling. (row = {Row}, column = {Column}, gemType == {(int)CurrentGemType}", LogLevel.Trace);

      Hide();

      Row = -1;
      Column = -1;
      GlobalPosition = new Godot.Vector2(-1 * Globals.TileSize, -1 * Globals.TileSize);
      _gem.SetGemType(Gem.GemType.UNKNOWN);
      IsAvailable = true;
      Behavior = new DefaultBehavior();
      ChangeBorder(TileBorder.Default);
      MarkedForRecycling = false;

      // Halt processing in the scene graph for this node while it isn't in use.
      SetProcess(false);
   }

   /// <summary>
   /// Animates the recycling of the tile.
   /// </summary>
   /// <param name="gameBoard"></param>
   /// <param name="request"></param>
   public void AnimateRecycle(GameBoard gameBoard, TileAnimationRequest request)
   {
      IsAnimating = true;

      var tween = GetTree().CreateTween();
      tween.SetProcessMode(Tween.TweenProcessMode.Physics);
      tween.SetParallel(true);
      tween.TweenProperty(this, "modulate:a", 0.0f, 0.1f).SetEase(Tween.EaseType.In);
      tween.Finished += (() =>
      {
         // Clean up and let the GameBoard know that this tile is done animating.
         tween.Kill();

         DebugLogger.Instance.Log($"\tAnimateRecycle() {Name} finished animating ({Row}, {Column}) New position = ({Position.X}, {Position.Y})", LogLevel.Trace);

         IsAnimating = false;
         gameBoard.HandleTileMoveAnimationFinished(request);
      });
   }

   /// <summary>
   /// Places the tile above the column where it will drop in from.
   /// </summary>
   /// <param name="gameBoard"></param>
   /// <param name="request"></param>
   public void PrepareForDrop(GameBoard gameBoard, TileAnimationRequest request)
   {
      // This is a freshly generated tile. It won't have a position yet, so the
      // start position needs to be above the column it'll drop from.
      IsAnimating = true;

      Hide();
      Modulate = new Godot.Color(Modulate.R, Modulate.G, Modulate.B, 0.0f);
      Position = new Godot.Vector2((request.Column * Globals.TileSize) + Globals.TileGridOffset, (Globals.TileSize + Globals.TileGridOffset) * -1);

      DebugLogger.Instance.Log($"\tPrepareForDrop() {Name} is freshly positioned above column {request.Column} for drop: {Position.ToString()}", LogLevel.Info);

      IsAnimating = false;
      gameBoard.HandleTileMoveAnimationFinished(request);
   }

   /// <summary>
   /// Moves the tile to a specific position based on the row and column.
   /// Provide the GameBoard parent node as a parameter to the method, because
   /// it is possible for the Tile to be removed from the tree while animating.
   /// </summary>
   /// <param name="gameBoard"></param>
   public void MoveTile(GameBoard gameBoard, TileAnimationRequest request)
   {
      DebugLogger.Instance.Log($"{Name} move from [{Row}, {Column}] to [{request.Row}, {request.Column}] begin...", LogLevel.Trace);

      Row = request.Row;
      Column = request.Column;
      Godot.Vector2 newPosition = new Godot.Vector2((request.Column * Globals.TileSize) + Globals.TileGridOffset,
                                                    (request.Row * Globals.TileSize) + Globals.TileGridOffset);

      DebugLogger.Instance.Log($"\tMoveTile() {Name} new position = {newPosition.ToString()}", LogLevel.Trace);

      if (request.Type == TileAnimationRequest.AnimationType.Animated)
      {
         IsAnimating = true;

         DebugLogger.Instance.Log($"\tMoveTile() {Name} animate moving from ({Position.ToString()}) to ({newPosition.ToString()}).", LogLevel.Trace);

         if (!Visible)
         {
            Show();
         }

         var tween = GetTree().CreateTween();
         tween.SetProcessMode(Tween.TweenProcessMode.Physics);
         tween.SetParallel(true);
         tween.TweenProperty(this, "modulate:a", 1.0f, 0.1f).SetEase(Tween.EaseType.In);
         tween.TweenProperty(this, "position", newPosition, 0.4f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Spring).From(Position);

         // Play an escalating sound chime.
         string hypeSoundName = "Sound_MatchHypeLevel";
         if (request.RoundMoved >= Globals.MaxHypeLevel)
         {
            hypeSoundName = $"{hypeSoundName}{Globals.MaxHypeLevel}";
         }
         else
         {
            hypeSoundName = $"{hypeSoundName}{request.RoundMoved + 1}";
         }

         DebugLogger.Instance.Log($"\tMoveTile() {Name} playing {hypeSoundName}", LogLevel.Trace);
         var soundToPlay = gameBoard.GetParent<GameScene>().AudioNode.GetNode<AudioStreamPlayer>(hypeSoundName);
         soundToPlay?.Play();

         // Play a drop sound.
         //TODO: find some drop sounds that aren't as clunky/chopping wood sounding.
         if (Globals.RNGesus.Next(0, 10) % 3 == 0)
         {
            int dropSoundId = Globals.RNGesus.Next(1, 3);
            var dropSound = gameBoard.GetParent<GameScene>().AudioNode.GetNode<AudioStreamPlayer>($"Sound_Drop{dropSoundId}");
            dropSound?.Play();
         }

         tween.Finished += (() =>
         {
            // Clean up and let the GameBoard know that this tile is done animating.
            tween.Kill();

            DebugLogger.Instance.Log($"\tMoveTile() {Name} finished animating ({Row}, {Column}) New position = ({Position.X}, {Position.Y})", LogLevel.Trace);

            IsAnimating = false;
            gameBoard.HandleTileMoveAnimationFinished(request);
         });
      }
      else
      {
         DebugLogger.Instance.Log($"\tMoveTile() {Name} just set position from ({Position.ToString()}) to ({newPosition.ToString()}).", LogLevel.Trace);
         Position = newPosition;
         Show();

         IsAnimating = false;
         gameBoard.HandleTileMoveAnimationFinished(request);
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
      if (OwningBoard != null && OwningBoard.IsReady && CurrentGemType != Gem.GemType.UNKNOWN)
      {
         OnTileMouseEvent?.Invoke(this, e);
      }
   }

   #endregion

   #region Private Members

   private Gem _gem;

   private AnimatedSprite2D _border;

   #endregion
}
