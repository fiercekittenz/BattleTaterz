using BattleTaterz.Core;
using BattleTaterz.Core.Enums;
using BattleTaterz.Core.GameObjects;
using BattleTaterz.Core.Utility;
using Godot;
using System;
using System.Collections.Generic;

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

      _gameBoardScene = GD.Load<PackedScene>("res://Scenes/GameBoard.tscn");

      //TODO - this is temporary for testing the game board.
      // Create the single player game board.
      CreateGameBoard();
   }

   #region Private Methods

   /// <summary>
   /// Creates a new game board for a player and adds it to the scene.
   /// </summary>
   private void CreateGameBoard()
   {
      PlayerInfo playerInfo = new PlayerInfo("Player");
      GameBoard gameBoard = _gameBoardScene.Instantiate<GameBoard>();
      gameBoard.Player = playerInfo;

      float centeredX = (GetViewportRect().Size.X / 2) - ((Globals.TileSize * Globals.TileCount) / 2);
      float centeredY = (GetViewportRect().Size.Y / 2) - ((Globals.TileSize * Globals.TileCount) / 2);
      gameBoard.Position = new Vector2(centeredX, centeredY);

      _activeBoards.Add(playerInfo, gameBoard);
      AddChild(gameBoard);
   }

   #endregion

   #region Private Members

   // Map of active game boards indexed by player information.
   private Dictionary<PlayerInfo, GameBoard> _activeBoards = new Dictionary<PlayerInfo, GameBoard>();

   // Cache of the UI node so we don't have to look for it every time we need to access a UI element.
   private Node2D _uiNode = null;

   // Local ref to the audio node.
   private Node2D _audioNode = null;

   // Pre-loaded game board scene data for quicker instantiation.
   private PackedScene _gameBoardScene = null;

   #endregion
}
