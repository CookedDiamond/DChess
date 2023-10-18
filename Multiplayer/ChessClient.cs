using DChess.Chess.Playground;
using DChess.Server;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.Multiplayer
{
    public class ChessClient {

		private Board _board;
		private TcpClient _tcpClient;

		public ChessClient(Board board) {
			_board = board;
			Connect(ChessServer.IP_ADRESS);
		}

		private void Connect(string server) {
			try {

				TcpClient client = new(server, ChessServer.PORT);
				_tcpClient = client;

				new Thread(() => ReadMoves(client, _board)).Start();

			}
			catch  {
				Debug.WriteLine("Closed client connection.");
				_tcpClient.Dispose();
			}
		}

		private static void ReadMoves(TcpClient client, Board board) {
			while (true) {
				var data = new byte[256];
				NetworkStream stream = client.GetStream();
				stream.Read(data, 0, data.Length);
				Move resultMove = ByteConverter.ToMove(data);
				Debug.WriteLine($"Received: {resultMove}");
				board.MakeMove(resultMove);
			}
		}

		public void SendMove(Move move) {
			byte[] data = ByteConverter.ToBytes(move);

			NetworkStream stream = _tcpClient.GetStream();

			// Send the message to the connected TcpServer.
			stream.Write(data, 0, data.Length);
			Debug.WriteLine($"Sent: {move}");
		}
	}
}
