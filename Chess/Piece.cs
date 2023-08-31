﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public class Piece {
		public PieceType type { get; set; }

		public TeamType team { get; set; }

		public Piece(PieceType type, TeamType team) {
			this.type = type;
			this.team = team;
		}

		private char typeAsChar() {
			return type switch {
				PieceType.Pawn => 'p',
				PieceType.Bishop => 'b',
				PieceType.Knight => 'n',
				PieceType.Rook => 'r',
				PieceType.Queen => 'q',
				PieceType.King => 'k',
				_ => throw new NotImplementedException()
			};
		}

		private String teamAsChar() {
			return team switch {
				TeamType.Black => "black",
				TeamType.White => "white",
				_ => throw new NotImplementedException()
			};
		}

		public override string ToString() {
			return $"type: {typeAsChar()} team: {teamAsChar()}";
		}
	}

	public enum PieceType {
		Pawn,
		Bishop,
		Knight,
		Rook,
		Queen,
		King

	}
}
