using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI
{

    /// <summary>
    /// NOT WORKING!
    /// </summary>
    public class MinMaxAlgorithm {
		private const int _depth = 5;
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
				fillNextLayer(i, team);
				Debug.WriteLine($"At Layer {i} with {_nodes[i].Count} nodes.");
			}
			return getBestMoveWithMinMax(team);
		}

		private void fillFirstLayer() {
			List<Move> moves = _startBoard.GetAllLegalMovesForTeam(_startBoard.GetTurnTeamType());
			foreach (Move move in moves) {
				Board boardWithMove = _startBoard.CloneBoard();
				boardWithMove.MakeMove(move);

				MinMaxNode node = new(move, null, boardWithMove);
				node.CalculateEval();
				_nodes[0].Add(node);
			}
		}

		private void fillNextLayer(int i, TeamType team) {
			List<MinMaxNode> currentLayer = _nodes[i - 1];
			List<MinMaxNode> nodes = new();
			bool isMaxBool = whitesTurn(i, team);
			float notAddValue = getWorseEval(isMaxBool);

			foreach (MinMaxNode node in currentLayer) {
				// Skip creating more nodes whith checkmate prev checkmate nodes.
				if (Math.Abs(node.Evaluation - notAddValue) <= 1 || Math.Abs(node.Evaluation + notAddValue) <= 1) {
					continue;
				}
				List<Move> moves = node.Board.GetAllLegalMovesForTeam(node.Board.GetTurnTeamType());
				foreach (Move move in moves) {
					Board boardWithMove = node.Board.CloneBoard();
					boardWithMove.MakeMove(move);

					var newNode = new MinMaxNode(move, node, boardWithMove);
					newNode.CalculateEval();

					nodes.Add(newNode);
				}

			}

			_nodes[i] = nodes;
		}

		private bool whitesTurn(int depth, TeamType team) {
			if (team == TeamType.White) {
				return depth % 2 == 0;
			}
			else return depth % 2 == 1;
		}

		private float getWorseEval(bool isWhitesTurn) {
			if (isWhitesTurn) return float.MinValue;
			else return float.MaxValue;
		}

		private Move getBestMoveWithMinMax(TeamType team) {
			// Fill last layer with eval
			foreach (MinMaxNode node in _nodes[_depth]) {
				node.Evaluation = node.Board.GetEvaluaton();
			}

			// Fill layers below, to the first.
			for (int i = _depth; i >= 1; i--) {
				// Get all nodes with same previous.
				List<MinMaxNode> previousNodes = _nodes[i - 1];
				bool isWhitesTurn = whitesTurn(i, team);
				foreach (var prevNode in previousNodes) {
					// All the nodes in the current layer with this prev node.
					IEnumerable<MinMaxNode> samePrevNodes = _nodes[i].Where(n => n.Previous == prevNode);

					float resultEval = getWorseEval(isWhitesTurn);

					// Select max eval if whites turn.
					// Select min eval if blacks turn.
					foreach (var thisLayerNode in samePrevNodes) {
						if (isWhitesTurn) {
							if (thisLayerNode.Evaluation >= resultEval) {
								resultEval = thisLayerNode.Evaluation;

							}
						}
						else {
							if (thisLayerNode.Evaluation <= resultEval) {
								resultEval = thisLayerNode.Evaluation;

							}
						}
					}

					prevNode.Evaluation = resultEval;
				}

			}

			// Pick the one with the best eval in the first layer.

			MinMaxNode bestNode = null;
			bool max = whitesTurn(0, team);
			float bestEval = getWorseEval(max);

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
