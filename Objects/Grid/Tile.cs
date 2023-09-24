using Godot;
using System;

public partial class Tile : Node2D
{
   #region Public Properties

   /// <summary>
   /// Reference to the Gem assigned to this tile.
   /// </summary>
   public Gem GemRef { get; set; } = null;

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

   #endregion
}
