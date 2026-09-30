using DChess.Chess.Playground;
using DChess.Server;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace DChess.Multiplayer {
    public class ChessClient : IDisposable {
        private readonly Board _board;
        private readonly ConcurrentQueue<byte[]> _receivedMoves = new();
        private TcpClient _tcpClient;

        public ChessClient(Board board, string server = "127.0.0.1", int port = 13000) {
            _board = board;
            try {
                _tcpClient = new TcpClient(server, port);
                new Thread(ReadMoves) { IsBackground = true }.Start();
            } catch (SocketException ex) {
                Debug.WriteLine("Could not connect: " + ex.Message);
                Dispose();
            }
        }

        private void ReadMoves() {
            try {
                var stream = _tcpClient.GetStream();
                while (true) {
                    var data = new byte[ByteConverter.MOVE_LENGTH];
                    stream.ReadExactly(data);
                    _receivedMoves.Enqueue(data);
                }
            } catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException) {
                Debug.WriteLine("Closed client connection: " + ex.Message);
            } finally {
                Dispose();
            }
        }

        // Called by the game thread to avoid mutating the board while it is drawn.
        public void ApplyPendingMoves() {
            while (_receivedMoves.TryDequeue(out var data)) {
                var move = ByteConverter.ToMove(data, _board);
                if (move != null) _board.MakeMove(move);
            }
        }

        public void SendMove(Move move) {
            if (_tcpClient == null || !_tcpClient.Connected) return;
            try {
                var data = ByteConverter.ToBytes(move);
                lock (_tcpClient) _tcpClient.GetStream().Write(data);
            } catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException) {
                Debug.WriteLine("Could not send move: " + ex.Message);
                Dispose();
            }
        }

        public void Dispose() => _tcpClient?.Dispose();
    }
}
