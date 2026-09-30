using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Multiplayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class NetworkingTests {
        [TestMethod]
        public void Castling_round_trip_reconstructs_both_piece_moves() {
            var sender = CastleReadyBoard();
            var receiver = sender.CloneBoard();
            var move = FindMove(sender, new(4, 0), new(6, 0));
            sender.MakeMove(move);
            var bytes = ByteConverter.ToBytes(move);
            Assert.AreEqual(16, bytes.Length);
            var received = ByteConverter.ToMove(bytes, receiver);
            Assert.IsNotNull(received);
            receiver.MakeMove(received);
            Assert.AreEqual(sender.ToString(), receiver.ToString());
            Assert.AreEqual(PieceType.Rook, receiver.GetPiece(new(5, 0)).Type);
        }

        [TestMethod]
        public void Promotion_round_trip_reapplies_variant_on_receiver() {
            var sender = EmptyBoard();
            sender.Variants.Add(new DChess.Chess.Variants.VariantPawnQueenPromotion());
            Place(sender, 0, 0, PieceType.King, TeamType.White);
            Place(sender, 7, 7, PieceType.King, TeamType.Black);
            Place(sender, 4, 6, PieceType.Pawn, TeamType.White);
            var receiver = sender.CloneBoard();
            var move = FindMove(sender, new(4, 6), new(4, 7));
            sender.MakeMove(move);
            receiver.MakeMove(ByteConverter.ToMove(ByteConverter.ToBytes(move), receiver));
            Assert.AreEqual(sender.ToString(), receiver.ToString());
            Assert.AreEqual(PieceType.Queen, receiver.GetPiece(new(4, 7)).Type);
        }

        [TestMethod]
        public void Invalid_or_wrong_turn_packets_are_rejected() {
            var board = CastleReadyBoard();
            var bytes = ByteConverter.ToBytes(FindMove(board, new(7, 0), new(7, 3)));
            board.MakeMove(FindMove(board, new(4, 0), new(4, 1)));
            Assert.IsNull(ByteConverter.ToMove(bytes, board));
            Assert.ThrowsException<ArgumentException>(() => ByteConverter.ToMove(new byte[3], board));
        }

        [TestMethod]
        public async Task Client_handles_fragmented_packets_and_sends_complete_moves() {
            var board = CastleReadyBoard();
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var accept = listener.AcceptTcpClientAsync();
            using var client = new ChessClient(board, "127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port);
            using var peer = await accept.WaitAsync(TimeSpan.FromSeconds(5));
            var stream = peer.GetStream();
            var move = FindMove(board, new(7, 0), new(7, 3));
            var bytes = ByteConverter.ToBytes(move);
            client.SendMove(move);
            var sent = new byte[16];
            await stream.ReadExactlyAsync(sent).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            CollectionAssert.AreEqual(bytes, sent);
            await stream.WriteAsync(bytes.AsMemory(0, 3));
            await Task.Delay(20);
            client.ApplyPendingMoves();
            Assert.AreEqual(0, board.GetMoveCount());
            await stream.WriteAsync(bytes.AsMemory(3));
            Assert.IsTrue(SpinWait.SpinUntil(() => {
                client.ApplyPendingMoves();
                return board.GetMoveCount() == 1;
            }, TimeSpan.FromSeconds(5)));
            Assert.AreEqual(PieceType.Rook, board.GetPiece(new(7, 3)).Type);
        }
    }
}
