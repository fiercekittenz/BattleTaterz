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
   public Control UINode
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

      _uiNode = GetNode<Control>("UI");
      _audioNode = GetNode<Node2D>("Audio");

      // Setup the cloud manager.
      _cloudManager = GetNode<CloudManager>("CloudManager");
      _cloudManager.Initialize();

      _gameBoardScene = GD.Load<PackedScene>("res://Scenes/GameBoard.tscn");

      // Start game background music.
      var backgroundMusic = _audioNode.GetNode<AudioStreamPlayer>("Music_BackgroundCG");
      backgroundMusic?.Play();

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
      gameBoard.Relocate(centeredX, centeredY);

      _activeBoards.Add(playerInfo, gameBoard);
      AddChild(gameBoard);
   }

   #endregion

   #region Private Members

   // Map of active game boards indexed by player information.
   private Dictionary<PlayerInfo, GameBoard> _activeBoards = new Dictionary<PlayerInfo, GameBoard>();

   // Pre-loaded game board scene data for quicker instantiation.
   private PackedScene _gameBoardScene = null;

   // Reference to the cloud manager, which animates the clouds in the background.
   private CloudManager _cloudManager = null;

   // Cache of the UI node so we don't have to look for it every time we need to access a UI element.
   private Control _uiNode = null;

   // Local ref to the audio node.
   private Node2D _audioNode = null;

   #endregion
}
