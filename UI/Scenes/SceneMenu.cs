﻿using DChess.Chess.Playground;
using DChess.Multiplayer;
using DChess.Server;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes
{
    public class SceneMenu : Scene {
		private readonly Game1 _game;
		private readonly Board _board;
		private readonly BoardNetworking _boardNetworking;

		public SceneMenu(Game1 game, Board Board, BoardNetworking boardNetworking) {
			_game = game;
			_board = Board;
			_boardNetworking = boardNetworking;

			ButtonText serverButton = new("Start Server", WindowAlignment.Center, new Vector2Int(0, -40), new Vector2Int(100, 20), Color.Black, Color.White);
			serverButton.Initialize(buttonManager, () => startServer());
			content.Add(serverButton);

			ButtonText clientButton = new("Start Client", WindowAlignment.Center, new Vector2Int(0, 0), new Vector2Int(100, 20), Color.Black, Color.White);
			clientButton.Initialize(buttonManager, () => startClient());
			content.Add(clientButton);

			ButtonText testButon = new("Start Game", WindowAlignment.Center, new Vector2Int(0, 40), new Vector2Int(100, 20), Color.Black, Color.White);
			testButon.Initialize(buttonManager, () => startGame());
			content.Add(testButon);

			BackGroundColor = Color.ForestGreen;
		}

		private void startGame() {
			_game.SwitchScene(SceneType.Board);
		}

		private void startServer() {
			_ = new ChessServer();
		}

		private void startClient() {
			_game.SwitchScene(SceneType.Board);
			var client = new ChessClient(_board);
			_boardNetworking.ChessClient = client;
		}
	}
}
