using DChess.Chess;
using DChess.Multiplayer;
using DChess.Server;
using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes {
	public class SceneMenu : Scene {
		private Game1 _game;
		private Board _board;

		public SceneMenu(Game1 game, Board Board) {
			_game = game;
			_board = Board;

			ButtonText serverButton = new("Start Server", WindowAlignment.Center, new Vector2Int(0, -20), new Vector2Int(100, 20), Color.Black, Color.White);
			serverButton.Initialize(buttonManager, () => startServer());
			content.Add(serverButton);

			ButtonText clientButton = new("Start Client", WindowAlignment.Center, new Vector2Int(0, 20), new Vector2Int(100, 20), Color.Black, Color.White);
			clientButton.Initialize(buttonManager, () => startClient());
			content.Add(clientButton);

			BackGroundColor = Color.Gray;
		}

		private void startServer() {
			var server = new ChessServer();
		}

		private void startClient() {
			_game.SwitchScene(GameScenes.Board);
			var client = new ChessClient(_board);
			_board.ChessClient = client;
		}
	}
}
