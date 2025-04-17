using BattleTaterz.Core.Enums;
using BattleTaterz.Core.Utility;
using Godot;
using System;

public partial class GameScene : Node2D
{
   /// <summary>
   /// Denotes if the game should log out data on swaps, matches, and board state.
   /// </summary>
   [Export]
   public bool LoggingEnabled { get; set; } = true;

   /// <summary>
   /// The level at which we should log output.
   /// </summary>
   [Export]
   public LogLevel LoggingLevel { get; set; } = LogLevel.Info;

   /// <summary>
   /// Accessor: UI node
   /// </summary>
   public Node2D UINode
   {
      get { return _uiNode; }
   }

   /// <summary>
   /// Accessor: Audio node
   /// </summary>
   public Node2D AudioNode
   {
      get { return _audioNode; }
   }

   /// <summary>
   /// Handles when the game scene enters the node tree.
   /// </summary>
   public override void _Ready()
   {
      DebugLogger.Instance.Log("GameScene Ready.", LogLevel.Info);
      DebugLogger.Instance.Enabled = LoggingEnabled;
      DebugLogger.Instance.LoggingLevel = LoggingLevel;

      _uiNode = GetNode<Node2D>("UI");
      _audioNode = GetNode<Node2D>("Audio");
   }

   #region Private Members

   // Cache of the UI node so we don't have to look for it every time we need to access a UI element.
   private Node2D _uiNode = null;

   // Local ref to the audio node.
   private Node2D _audioNode = null;

   #endregion
}
