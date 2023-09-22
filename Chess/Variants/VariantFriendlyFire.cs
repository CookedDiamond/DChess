using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Variants {
	public class VariantFriendlyFire : Variant {

		public override bool IsPieceEnemyTeam(bool normalResult) {
			return true;
		}
	}
}
