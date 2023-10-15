using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI {
	public class MinMaxNode {

		public Move Move { get; set; }
		public Board Board { get; set; }
		public MinMaxNode Previous { get; set; }
		public float Evaluation { get; set; }

		public MinMaxNode(Move move, MinMaxNode previous, Board board) {
			Move = move;
			Previous = previous;
			Board = board;
		}

		public void CalculateEval() {
			Evaluation = Board.GetEvaluaton().GetEvaluation();
		}


		public override string ToString() {
			return $"Node: Eval:{Evaluation}, {Move}";
		}
	}
}
