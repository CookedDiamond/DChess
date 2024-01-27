
using DChess;
using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;

var board = new Board(new Vector2Int(8, 8));
board.Variants.Add(new VariantPawnQueenPromotion());
//board.Variants.Add(new VariantFriendlyFire());
//board.Variants.Add(new VariantBattleRoyale(15, 1f));

BoardManager boardManager = new(board, new BoardNetworking());
boardManager.Build8x8StandardBoard();
//boardManager.AddComputerPlayer(TeamType.Black);

//board.PlacePiece(new Vector2Int(1,1), new PieceQueen(TeamType.White, board));
//board.PlacePiece(new Vector2Int(1,0), new PieceRook(TeamType.White, board));
//board.PlacePiece(new Vector2Int(0,0), new PieceKing(TeamType.White, board));

//board.PlacePiece(new Vector2Int(1,7), new PieceKing(TeamType.Black, board));
//board.PlacePiece(new Vector2Int(1,6), new PiecePawn(TeamType.Black, board));


board.LastEval = new Evaluation(board).GetEvaluation();

var game = new Game1(boardManager);
game.SwitchScene(SceneType.Board);
game.Run();

