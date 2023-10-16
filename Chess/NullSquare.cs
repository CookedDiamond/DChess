using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public class NullSquare : Square {
		public NullSquare() : base(null, Vector2Int.ZERO, TeamType.None) {
			Piece = null;
		}

		public override bool IsPieceEnemyTeam(TeamType team) {
			return false;
		}

		public override void OnClick() {
			
		}

		public override string ToString() {
			return "Null Square";
		}
	}
}
