using DChess.BotApi;
using DChess.Chess.Playground;
using System;
using System.Collections.Generic;

namespace DChess.Bots {
	/// <summary>
	/// The simplest possible bot: plays a random legal move.
	/// </summary>
	public class RandomBot : IChessBot {
		private readonly Random _random = new();

		public string Name => "RandomBot";

		public Move Think(BotBoard board, BotTimer timer) {
			List<Move> moves = board.GetLegalMoves();
			return moves[_random.Next(moves.Count)];
		}
	}
}
