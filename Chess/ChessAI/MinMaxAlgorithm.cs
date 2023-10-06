using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI {
	public class MinMaxAlgorithm {
		private const int _depth = 3;
		private readonly Board _startBoard;

		// Current layer nodes.
		private List<MinMaxNode>[] _nodes = new List<MinMaxNode>[_depth + 1];

		public MinMaxAlgorithm(Board board) {
			_startBoard = board;

			for (int i = 0; i < _nodes.Length; i++) {
				_nodes[i] = new();
			}
		}

		public Move GetBestMove(TeamType team) {
			// Build Graph
			fillFirstLayer();
			Debug.WriteLine("start");
			for (int i = 1; i <= _depth; i++) {
				fillNextLayer(i);
				Debug.WriteLine($"At Layer {i} with {_nodes[i].Count} nodes.");
			}
			return getBestMoveWithMinMax(team);
		}

		private void fillFirstLayer() {
			List<Move> moves = _startBoard.GetAllLegalMovesForTeam(_startBoard.GetTurnTeamType());
			foreach (Move move in moves) {
				Board boardWithMove = ChessUtil.CloneBoard(_startBoard);
				boardWithMove.MakeMove(move);

				MinMaxNode node = new(move, null, boardWithMove);
				_nodes[0].Add(node);
			}
		}

		private void fillNextLayer(int i) {
			List<MinMaxNode> currentLayer = _nodes[i - 1];
			List<MinMaxNode> nodes = new();

			foreach (MinMaxNode node in currentLayer) {
				List<Move> moves = node.Board.GetAllLegalMovesForTeam(node.Board.GetTurnTeamType());
				foreach (Move move in moves) {
					Board boardWithMove = ChessUtil.CloneBoard(node.Board);
					boardWithMove.MakeMove(move);

					nodes.Add(new MinMaxNode(move, node, boardWithMove));
				}

			}

			_nodes[i] = nodes;
		}

		private bool isMax(int depth, TeamType team) {
			if (team == TeamType.White) {
				return depth % 2 == 0;	
			}
			else return depth % 2 == 1;
		}

		private float getExtremEval(bool isMax) {
			if (isMax) return float.MinValue; 
			else return float.MaxValue;
		}

		private Move getBestMoveWithMinMax(TeamType team) {
			// Fill last layer with eval
			foreach (MinMaxNode node in _nodes[_depth]) {
				node.Evaluation = node.Board.GetEvaluaton().GetEvaluation();
			}

			// fill layers below, to the first.
			for (int i = _depth; i >= 1; i--) {
				MinMaxNode currNode = _nodes[i].First().Previous;
				bool maxBool = isMax(i, team);
				float eval = getExtremEval(maxBool);
				foreach (var node in _nodes[i]) {
					if (node.Previous == currNode) {
						if (maxBool) {
							if (eval <= node.Evaluation) {
								eval = node.Evaluation;
							}
						}
						else {
							if (eval >= node.Evaluation) {
								eval = node.Evaluation;
							}
						}
					}
					else {
						// Sets the previous node.
						currNode.Evaluation = eval;

						// Switches to next node.
						currNode = node.Previous;
						eval = currNode.Evaluation;
					}
				}
			}

			// Pick the one with the best eval in the first layer.

			MinMaxNode bestNode = null;
			bool max = isMax(0, team);
			float bestEval = getExtremEval(max);

			foreach (var node in _nodes[0]) {
				if (max && bestEval <= node.Evaluation
					|| !max && bestEval >= node.Evaluation) {
						bestEval = node.Evaluation;
						bestNode = node;
				}
			}

			return bestNode.Move;
		}

	}
}
