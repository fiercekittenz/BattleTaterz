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
   /// Handles when the game scene enters the node tree.
   /// </summary>
   public override void _Ready()
   {
      DebugLogger.Instance.Log("GameScene Ready.", LogLevel.Info);
      DebugLogger.Instance.Enabled = LoggingEnabled;
      DebugLogger.Instance.LoggingLevel = LoggingLevel;
   }
}
