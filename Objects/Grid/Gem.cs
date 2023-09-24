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
      PotatoCat      = 0,
      Grogu          = 1,
      GrilledCheese  = 2,
      Claw           = 3,
      Jon            = 4,
      BurgerStaff    = 5,
      Cupcake        = 6,
      Sock           = 7,

      GemType_Count
   }

   /// <summary>
   /// The current type of gem used by this node.
   /// </summary>
   public GemType CurrentGem { get; set; } = GemType.PotatoCat;

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

   #endregion

   #region Private Members

   private AnimatedSprite2D _sprite;

   #endregion
}
