using BattleTaterz.Utility;
using Godot;
using System;

public partial class Gem : Node2D
{
   #region Public Properties

   /// <summary>
   /// Enumeration of possible gems in the game.
   /// </summary>
   public enum GemType
   {
      PotatoCat,
      BlueSquare,
      GreenSquare,
      PurpleSquare,
      RedSquare,
      YellowSquare,

      GemType_Count,

      UNKNOWN
   }

   /// <summary>
   /// Enumeration of possible Gem selection states.
   /// </summary>
   public enum GemState
   {
      Default,
      Selected
   }

   /// <summary>
   /// The current type of gem used by this node.
   /// </summary>
   public GemType CurrentGem { get; set; } = GemType.PotatoCat;

   /// <summary>
   /// The current state of the gem in gameplay.
   /// </summary>
   public GemState State { get; set; } = GemState.Default;

   /// <summary>
   /// An event that can be listened to for mouse input to the gem.
   /// </summary>
   public event EventHandler<GemMouseEventArgs> OnGemMouseEvent;

   #endregion

   #region Public Methods

   /// <summary>
   /// Called when the node enters the scene tree for the first time.
   /// </summary>
   public override void _Ready()
   {
      _sprite = GetNode<AnimatedSprite2D>("Sprite");

      int currentGemValue = Convert.ToInt32(CurrentGem);
      _sprite.SpriteFrames = GD.Load<SpriteFrames>($"res://SpriteResources/gem{currentGemValue}.tres");
   }

   /// <summary>
   /// Called every frame. 'delta' is the elapsed time since the previous frame.
   /// </summary>
   /// <param name="delta"></param>
   public override void _Process(double delta)
   {
   }

   /// <summary>
   /// Handle mouse input.
   /// </summary>
   /// <param name="node"></param>
   /// <param name="eventInfo"></param>
   /// <param name="shape"></param>
   protected void OnMouseDetectorInputEvent(Node node, InputEvent eventInfo, int shape)
   {
      if (eventInfo is InputEventMouseButton mouseEvent &&
          mouseEvent.Pressed && 
          mouseEvent.ButtonIndex == MouseButton.Left)
      { 
         var bubbledArgs = new GemMouseEventArgs()
         {
            EventType = GemMouseEventArgs.MouseEventType.Click
         };

         OnGemMouseEvent.Invoke(this, bubbledArgs);
      }
   }

   /// <summary>
   /// Handle mouse enter.
   /// </summary>
   protected void OnMouseDetectorMouseEntered()
   {
      var bubbledArgs = new GemMouseEventArgs()
      {
         EventType = GemMouseEventArgs.MouseEventType.Enter
      };

      OnGemMouseEvent.Invoke(this, bubbledArgs);
   }

   /// <summary>
   /// Handle mouse exit.
   /// </summary>
   protected void OnMouseDetectorMouseExited()
   {
      var bubbledArgs = new GemMouseEventArgs()
      {
         EventType = GemMouseEventArgs.MouseEventType.Leave
      };

      OnGemMouseEvent.Invoke(this, bubbledArgs);
   }

   #endregion

   #region Private Members

   private AnimatedSprite2D _sprite;

   #endregion
}
