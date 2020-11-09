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

		Task _connectionTask;
		Task _listenerTask;
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
			_tokenSource = new CancellationTokenSource();

			if (!isHost)
			{
				label3.Visible = false;
				frameNumericUpDown.Visible = false;
				BeginButton.Visible = false;

				TcpClient client = new TcpClient(endPoint);
				Stream = client.GetStream();

				_connectionTask = ConnectionTask();
			}
			else
			{
				TcpListener listener = new TcpListener(endPoint);
				listener.Start();

				AddPlayer(name);
				_listenerTask = ListenerTask(listener);
				
			}
		}

		//player names method. We're gonna have to do it the good old fashined Java way... no properties :(

		/// <summary>
		/// Adds a player to the playerNames array and updaes the playerTextBox
		/// </summary>
		/// <param name="playerName">name of the player to add</param>
		/// <param name="index">index of where the player should be put. The player will be put at the end of the list if there is no parameter</param>
		public void AddPlayer(string playerName, int index = -1)
		{
			if (index < 0) PlayerNames.Add(playerName);
			else PlayerNames.Insert(index, playerName);

			PlayerBox.AppendText(playerName + '\n');
		}

		/// <summary>
		/// Remvoes a player from playerNames and from the player text box
		/// </summary>
		/// <param name="playerName">name of the player to remove</param>
		public void RemovePlayer(string playerName)
		{
			PlayerNames.Remove(playerName);

			StringBuilder playerText = new StringBuilder();
			foreach (string s in PlayerNames)
			{
				playerText.Append(s + '\n');
			}
			PlayerBox.Text = playerText.ToString();

		}
		
		/// <summary>
		/// Task that updates the chat and sends game start requests.
		/// </summary>
		async Task ConnectionTask()
		{
			while (!_tokenSource.IsCancellationRequested)
			{
				byte[] data = await ReadStreamBytes();

				//77 ("M") means a chat message
				if (data[0] == 77)
				{
					//if were hosting and if we are receiving a chat message
					string username = !IsHost ? PlayerNames[0] : PlayerNames[1];
					ChatBox.AppendText($"{username}: {Encoding.ASCII.GetString(data, 1, data.Length - 1)}");
				}
			}
		}

		Task<byte[]> ReadStreamBytes() 
		{
			int data = Stream.ReadByte();
			List<byte> output = new List<byte>();

			while (data != -1) 
			{
				output.Add((byte)data);
			}

			return Task.FromResult<byte[]>(output.ToArray());
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

				//get their name and put it on the form
				byte[] recieveBytes = await ReadStreamBytes();

				//Must have an 'N' or 78 for there to be a name
				if (recieveBytes[0] == 78)
				{
					AddPlayer(Encoding.ASCII.GetString(recieveBytes, 1, recieveBytes.Length - 1), 1);
				}

				//Then, send the name back. PlayerNames[0] is the name of the host when this form is hosting.
				byte[] sendBytes = Encoding.ASCII.GetBytes('N' + PlayerNames[0]);
				await Stream.WriteAsync(sendBytes, 0, sendBytes.Length);

				await ConnectionTask();
			}

			listener.Stop();
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
					return await listener.AcceptTcpClientAsync().ConfigureAwait(false);
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

			if (_connectionTask != null)
			{
				await _connectionTask;
				_connectionTask.Dispose();
			}

			if (_listenerTask != null)
			{
				await _listenerTask;
				_listenerTask.Dispose();
			}

			Stream?.Close();
		}
	}
}
