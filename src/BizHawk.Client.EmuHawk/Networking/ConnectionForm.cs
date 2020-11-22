using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Configuration;
using BizHawk.Emulation.Common;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

namespace BizHawk.Client.EmuHawk.Networking
{
	/// <summary>
	/// Connection form. Right now connections only support 2 player P2P
	/// </summary>
	public partial class ConnectionForm : Form
	{
		
		/// <summary>
		/// If this is true, then the user is hosting. If not, then the user joined a host.
		/// </summary>
		public bool IsHost { get; set; }

		/// <summary>
		/// Stream that is used to relay chat messages and to receive and parse game info (inputs, game start, etc)
		/// </summary>
		public NetworkStream Stream { get; set; }

		/// <summary>
		/// Network controller object used to interface with the core
		/// </summary>
		public NetworkController NetworkController { get; set; }

		/// <summary>
		/// Names of the players.
		/// </summary>
		public List<string> PlayerNames { get; set; }

		Task _clientTask;
		Task _listenerTask;
		//Task _connectionTask;
		CancellationTokenSource _tokenSource;

		/// <summary>
		/// Constructor for the connection form class
		/// </summary>
		/// <param name="isHost">if this is being hosted or is this connecting to a client? determines what will be visible in the form</param>
		/// <param name="endPoint">endpoint that is used for hosting or the end point that is connected to</param>
		/// <param name="name">name of the user</param>
		public ConnectionForm(bool isHost, IPEndPoint endPoint, string name)
		{
			InitializeComponent();

			IsHost = isHost;
			PlayerNames = new List<string>(4);
			//for (int i = 0; i < 4; i++) PlayerNames.Add("");
			_tokenSource = new CancellationTokenSource();

			if (!isHost)
			{
				label3.Visible = false;
				frameNumericUpDown.Visible = false;
				BeginButton.Visible = false;

				AddPlayer(name);
				_clientTask = ClientTask(endPoint, name);
			}
			else
			{
				TcpListener listener = new TcpListener(endPoint);
				listener.Start();

				AddPlayer(name);
				_listenerTask = ListenerTask(listener);
				
			}
		}
		
		/// <summary>
		/// Connects to a host using a TCP client
		/// </summary>
		/// <param name="endPoint">end point of the host</param>
		/// <param name="name">name of the user.</param>
		/// <returns></returns>
		async Task ClientTask(IPEndPoint endPoint, string name)
		{
			//connect to host
			TcpClient client = new TcpClient();
			client.Connect(endPoint.Address, endPoint.Port);
			Stream = client.GetStream();

			//send name
			await Stream.WriteAsync(Encoding.ASCII.GetBytes($"N{name}"), 0, name.Length + 1);
			await Stream.FlushAsync();
			
			string hostName = Encoding.ASCII.GetString(await ReadStreamBytes());

			//add name to the form
			Console.WriteLine("Host name: " + hostName);
			if (hostName[0] == 'N')
			{
				AddPlayer(hostName.Substring(1));
			}
			else
			{
				Console.WriteLine("Name does not have N at the beginning and is malformed. Will not connect.");
				return;
			}

			await Task.Run(ConnectionTask);
		}

		/// <summary>
		/// Taskthat updates the chat and sends game start requests.
		/// </summary>
		async Task ConnectionTask()
		{
			while (!_tokenSource.IsCancellationRequested)
			{
				if (Stream.DataAvailable)
				{
					byte[] data = await ReadStreamBytes();

					//77 ("M") means a chat message
					if (data[0] == 'M')
					{
						//if were hosting and if we are receiving a chat messag;
						Console.WriteLine($"Got message: {Encoding.ASCII.GetString(data, 1, data.Length - 1)}");
						string username = !IsHost ? PlayerNames[0] : PlayerNames[1];
						ChatBox.Text += $"{username}: {Encoding.ASCII.GetString(data, 1, data.Length - 1)}\n";
					}
				}

				await Task.Delay(50);
			}
		}

		async Task<byte[]> ReadStreamBytes() 
		{
			byte[] buffer = new byte[512];

			while (Stream.DataAvailable)
			{
				await Stream.ReadAsync(buffer, 0, buffer.Length);
			}

			return buffer;
		}

		/// <summary>
		/// Listener task that waits for an accepted socket.
		/// </summary>
		/// <returns></returns>
		async Task ListenerTask(TcpListener listener)
		{
			while (!_tokenSource.IsCancellationRequested)
			{
				TcpClient client = await GetTcpClient(listener, _tokenSource.Token);
				if (_tokenSource.IsCancellationRequested) break;

				Stream = client.GetStream();
				byte[] recieveBytes = await ReadStreamBytes();

				//Must have an 'N' or 78 for there to be a name
				if (recieveBytes[0] == 'N')
				{
					AddPlayer(Encoding.ASCII.GetString(recieveBytes, 1, recieveBytes.Length - 1));
				}
				else
				{
					Console.WriteLine("Caution. Client connected, but name packet is malforemd.");
				}

				//Then, send the name back. PlayerNames[0] is the name of the host when this form is hosting.
				Console.WriteLine("send name: " + PlayerNames[0]);
				byte[] sendBytes = Encoding.ASCII.GetBytes('N' + PlayerNames[0]);
				await Stream.WriteAsync(sendBytes, 0, sendBytes.Length);
				await Stream.FlushAsync();

				await Task.Run(ConnectionTask);
			}

			//listener.Stop();
		}

		/// <summary>
		/// Gets a tcpclient from a listener asynchournsly while also listenting to cancelation tokens. (Thanks Darchuk.NET!)
		/// </summary>
		/// <param name="listener"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		async Task<TcpClient> GetTcpClient(TcpListener listener, CancellationToken token)
		{
			using (_tokenSource.Token.Register(listener.Stop))
			{
				try
				{
					return await listener.AcceptTcpClientAsync();

				}
				catch (ObjectDisposedException ex)
				{
					if (token.IsCancellationRequested) return null;
					throw ex;
				}
			}

		}

		private async void ConnectionForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			_tokenSource?.Cancel();

			if (_clientTask != null)
			{
				await _clientTask;
				_clientTask.Dispose();
			}

			if (_listenerTask != null)
			{
				await _listenerTask;
				_listenerTask.Dispose();
			}

			Stream?.Close();
		}

		//player names method. We're gonna have to do it the good old fashined Java way... no properties :(

		/// <summary>
		/// Adds a player to playerNames and updatres the player text box
		/// </summary>
		/// <param name="playerName">name to add</param>
		/// <param name="index">where to add the name into playerNames</param>
		public void AddPlayer(string playerName, int index = -1)
		{
			if (index > -1)
			{
				PlayerNames.Insert(index, playerName);
			}
			else
			{
				PlayerNames.Add(playerName);
			}

			UpdatePlayerTextBox();
		}

		void UpdatePlayerTextBox()
		{
			StringBuilder playerText = new StringBuilder();
			foreach (string s in PlayerNames)
			{
				playerText.Append(s + '\n');
			}
			PlayerBox.Text = playerText.ToString();
		}

		/// <summary>
		/// Remvoes a player from playerNames and from the player text box
		/// </summary>
		/// <param name="playerName">name of the player to remove</param>
		public void RemovePlayer(string playerName)
		{
			PlayerNames.Remove(playerName);
			UpdatePlayerTextBox();
		}

		/// <summary>
		/// Sends a messager to the other client
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void SendButton_Click(object sender, EventArgs e)
		{
			byte[] sendBytes = Encoding.ASCII.GetBytes('M' + ChatTextBox.Text);

			await Stream.WriteAsync(sendBytes, 0, sendBytes.Length);
			await Stream.FlushAsync();
		}
	}
}
