using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace DChess.Server {
	public class ChessServer {

		private static List<TcpClient> tcpClients = new List<TcpClient>();

		public static readonly string IP_ADRESS = "127.0.0.1";
		public static readonly int PORT = 13000;

		public ChessServer() {
			Thread t = new Thread(new ThreadStart(Main));
			t.Start();
		}

		public static void Main() {
			TcpListener server = null;
			try {
				IPAddress localAddr = IPAddress.Parse(IP_ADRESS);
				server = new TcpListener(localAddr, PORT);
				server.Start();


				Debug.WriteLine("Waiting for connections...");
				// Enter the listening loop.
				while (true) {
					TcpClient client = server.AcceptTcpClient();
					new Thread(() => handleClient(client)).Start();

				}
			}
			catch (SocketException e) {
				Debug.WriteLine("SocketException: {0}", e);
			}
			finally {
				server.Stop();
			}

		}

		private static void handleClient(TcpClient client) {

			Debug.WriteLine("Connected a client to the server!");
			var stream = client.GetStream();
			tcpClients.Add(client);

			while (true) {

				try {
					// Buffer for reading data
					byte[] bytes = new byte[256];
					stream.Read(bytes, 0, bytes.Length);
					// Send back a response.
					SendDataToAllClients(client, bytes);
				}
				catch {
					tcpClients.Remove(client);
					client.Dispose();
				}
			}
		}

		private static void SendDataToAllClients(TcpClient from, byte[] data) {
			foreach (var client in tcpClients) {
				if (client != from) {
					var stream = client.GetStream();
					stream.Write(data, 0, data.Length);
				}
			}
		}
	}


}
