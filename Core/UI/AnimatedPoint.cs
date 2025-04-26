using BattleTaterz.Core.Gameplay;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace BattleTaterz.Core.UI
{
   public partial class AnimatedPoint : Node2D
   {
      #region Properties

      /// <summary>
      /// The point value to be displayed.
      /// </summary>
      public int DisplayValue { get; set; } = 0;

      /// <summary>
      /// Is this AnimatedPoint available for use?
      /// </summary>
      public bool IsAvailable { get; set; } = true;

      #endregion

      #region Public Methods

      /// <summary>
      /// Called when the node enters the scene tree for the first time. 
      /// </summary>
      public override void _Ready()
      {
         Hide();

         // Create a label that will be used by this node.
         _label = new Godot.Label();
         _label.Name = $"{this.Name}Label";
         _label.GlobalPosition = new Godot.Vector2(0.0f, 0.0f);
         _label.Text = "0";
         _label.Theme = GD.Load<Theme>($"res://UIResources/animated_points_theme.tres");

         AddChild(_label);
      }

      /// <summary>
      /// Called every frame. 'delta' is the elapsed time since the previous frame.
      /// </summary>
      /// <param name="delta"></param>
      public override void _Process(double delta)
      {
      }

      /// <summary>
      /// Claims the node as in-use and animates the label to display for some period of time before
      /// it can be made available again to the pool.
      /// </summary>
      /// <param name="globalPosition"></param>
      /// <param name="value"></param>
      public void Animate(Godot.Vector2 globalPosition, ScoreUpdateResults scoreUpdate)
      {
         // Halt availability.
         IsAvailable = false;

         // Format the display text.
         string displayText = scoreUpdate.BasePoints.ToString();
         if (scoreUpdate.ChangeType == Enums.ScoreChangeType.Increase && (scoreUpdate.BonusPointsRewarded > 0 || scoreUpdate.SpecialPoints > 0))
         {
            //TODO - simple formatting to include the special points with bonus for now.
            displayText = $"{displayText} (+{scoreUpdate.BonusPointsRewarded + scoreUpdate.SpecialPoints})";
         }

         // Set new values on the label and reposition it.
         _label.Text = displayText;
         _label.GlobalPosition = globalPosition;
         _label.Modulate = new Godot.Color(_label.Modulate.R, _label.Modulate.G, _label.Modulate.B, 1.0f);
         _label.Scale = new Godot.Vector2(1.0f, 1.0f);
         _label.ZIndex = 10;
         Show();

         // Create a tween for the label and animate it.
         var tween = GetTree().CreateTween();
         tween.SetParallel(true);
         tween.TweenProperty(_label, "position", new Godot.Vector2(_label.Position.X, _label.Position.Y - 100), 1.0f).SetEase(Tween.EaseType.In);
         tween.TweenProperty(_label, "modulate:a", 0.0f, 1.0f).SetEase(Tween.EaseType.In);
         tween.Finished += (() =>
         {
            // Clean up!
            tween.Kill();
            Hide();
            IsAvailable = true;
         });
      }

      #endregion

      #region Private Members

      /// <summary>
      /// The UI label node for this object.
      /// </summary>
      private Godot.Label _label = null;

      #endregion
   }
}
