using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace DChess.Chess.Playground
{
	public class Move
	{
		public readonly List<BoardChange> Changes = new();

		public void AddChange(BoardChange change)
		{
			Changes.Add(change);
		}

		public void AddChange(Vector2Int position, Piece oldPiece, Piece newPiece)
		{
			Changes.Add(new BoardChange(position, oldPiece, newPiece));
		}

		public void Apply(Board board)
		{
			foreach (var change in Changes)
			{
				board.PlacePiece(change.boardPosition, change.newPiece);
				if (change.newSquare.HasValue)
					board.SquareMap[change.boardPosition.x, change.boardPosition.y] = change.newSquare.Value;
			}
			foreach (var piece in MovingPieces()) piece.MoveCount++;
		}

		public void Undo(Board board)
		{
			foreach (var change in Changes.Reverse<BoardChange>())
			{
				board.PlacePiece(change.boardPosition, change.oldPiece);
				if (change.oldSquare.HasValue)
					board.SquareMap[change.boardPosition.x, change.boardPosition.y] = change.oldSquare.Value;

			}
			foreach (var piece in MovingPieces()) piece.MoveCount--;
		}

		private IEnumerable<Piece> MovingPieces() => Changes
			.Where(c => c.newPiece == Piece.NULL_PIECE && c.oldPiece != Piece.NULL_PIECE
				&& Changes.Any(destination => destination.newPiece == c.oldPiece))
			.Select(c => c.oldPiece).Distinct();

		internal Move CloneForBoard(Board board, Dictionary<Piece, Piece> pieces) {
			Piece Copy(Piece piece) {
				if (piece == Piece.NULL_PIECE) return piece;
				if (!pieces.TryGetValue(piece, out var copy)) {
					copy = piece.ClonePiece(board);
					pieces.Add(piece, copy);
				}
				return copy;
			}
			var move = new Move();
			foreach (var change in Changes)
				move.AddChange(new BoardChange(change.boardPosition, Copy(change.oldPiece), Copy(change.newPiece),
					change.oldSquare, change.newSquare));
			return move;
		}


		/// <returns>The value of the attacked Piece.</returns>
		public float AttackScore()
		{
			float maxAttack = 0;
			foreach (var change in Changes)
			{
				if (change.newPiece != Piece.NULL_PIECE && change.oldPiece != Piece.NULL_PIECE)
				{
					float pieceScore = change.oldPiece.GetPieceScore();
					if (pieceScore < maxAttack) continue;

					maxAttack = pieceScore;
				}
			}
			return maxAttack;
		}

		public override bool Equals(object obj)
		{
			if (obj is not Move)
			{
				return false;
			}

			Move move = obj as Move;
			if (Changes.Count != move.Changes.Count) return false;
			foreach (var change in Changes) {
				if (!move.Changes.Contains(change))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode() {
			int hash = 0;
			foreach (var change in Changes) hash ^= change.GetHashCode();
			return hash;
		}

		public override string ToString()
		{
			BoardChange boardChange1 = Changes[0];
			BoardChange boardChange2 = Changes[1];

			return $"Move: from {boardChange1.boardPosition}, to {boardChange2.boardPosition} with {Changes[0].oldPiece}";
		}
	}
}
