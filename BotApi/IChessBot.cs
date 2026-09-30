using DChess.Chess.Playground;

namespace DChess.BotApi {
	/// <summary>
	/// Implement this interface to create a bot. See BOTS.md for a full guide.
	///
	/// Every class in the project that implements IChessBot (and has a public constructor without parameters)
	/// automatically shows up in the bot selection of the menu.
	///
	/// A new instance of your bot is created for every game, so you can keep state in fields during a game.
	/// </summary>
	public interface IChessBot {
		/// <summary>Name shown in the menu. Defaults to the class name.</summary>
		string Name => GetType().Name;

		/// <summary>
		/// Called whenever it is your turn. Return one of the moves from board.GetLegalMoves().
		///
		/// The board is your own private copy: you may make and undo as many moves on it as you like.
		/// Returning null, an illegal move, throwing an exception or running out of time loses the game.
		/// </summary>
		Move Think(BotBoard board, BotTimer timer);
	}
}
