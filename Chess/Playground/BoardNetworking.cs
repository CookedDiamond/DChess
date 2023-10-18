using DChess.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Playground {
	public class BoardNetworking {
		public ChessClient ChessClient { get; set; }

		public void MakeMove(Move move) {
			ChessClient?.SendMove(move);
		}
	}
}
