using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Variants {
	public abstract class Variant {

		public virtual bool IsPieceEnemyTeam(bool normalResult) {
			return normalResult;
		}

	}
}
