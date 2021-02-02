using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Buffers.Binary;
using System.Text;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Client.EmuHawk;
using System.Drawing;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Networking class which allows the transferal of input across two clients. For now, it'll use simple delay
	/// based netcode. But later it should support rollback. Since this requires precise timings we'll use TCP instead
	/// of UDP
	/// </summary>
	public class NetworkClient : IDisposable
	{
		/// <summary>
		/// IPEndPoint of the host client
		/// </summary>
		public IPEndPoint HostEndPoint { get; set; }

		/// <summary>
		/// Frame of delays
		/// </summary>
		public byte FrameDelay { get; set; }

		/// <summary>
		/// Port of this client.
		/// </summary>
		public byte ConsolePort { get; set; }

		/// <summary>
		/// NetworkController used to interface with consoles
		/// </summary>
		public NetworkController NetworkController { get; set; }

		/// <summary>
		/// Is this client the host or not? Determines 
		/// </summary>
		public bool IsHost { get; set; }

		/// <summary>
		/// Client used by the user. this class needs to delay inputs from the client to ensure that the games
		/// are synced
		/// </summary>
		public IController UserController
		{
			get => NetworkController.UserController;
			set => NetworkController.UserController = value;
		}


		Queue<byte[]> _inputStack = new Queue<byte[]>();

		UdpClient _client;
		IPEndPoint _endPoint;

		byte[] _prevStackBytes = new byte[256];
		bool _firstChange = true;

		/// <summary>
		///
		/// </summary>
		/// <param name="hostEndPoint">End point of the host</param>
		/// <param name="frameDelay">amount of frames before an input is registered</param>
		/// <param name="consolePort">port on the console of this client</param>
		public NetworkClient(IPEndPoint hostEndPoint, IController userController, byte frameDelay, byte consolePort)
		{
			NetworkController = new NetworkController(userController, 1, consolePort);
			(HostEndPoint, _endPoint, UserController, FrameDelay, ConsolePort) =
				(hostEndPoint, hostEndPoint, userController, frameDelay, consolePort);

			for (int i = 0; i < frameDelay; i++)
			{
				_inputStack.Enqueue(null);
			}
		}

		/// <summary>
		/// connects to a server using the HostEndPoint property of the obbject
		/// </summary>
		public void Connect(bool isHost)
		{
			IsHost = isHost;
			Connect(HostEndPoint, isHost);
		}

		/// <summary>
		/// connects to a server using a parameter
		/// </summary>
		/// <param name="endPoint">endpoint of the server</param>
		/// <param name="isHost">are we hosting are are we not?</param>
		public void Connect(IPEndPoint endPoint, bool isHost)
		{
			//connect to end point
			//starting protocol is handled by the tcp client in the connection forms
			_client = isHost ? new UdpClient(endPoint) : new UdpClient();
			 IsHost = isHost;
		}


		bool _isEndian = BitConverter.IsLittleEndian;

		public async Task<byte[]> ReceiveDataAsync()
		{
			var result = await _client.ReceiveAsync();
			_endPoint = result.RemoteEndPoint;

			return result.Buffer;
		}

		public async Task<int> SendDataAsync(byte[] data) => await _client.SendAsync(data, data.Length, _endPoint);


		/// <summary>
		/// Updates the NetworkClient class. WARNING: WILL HANG WHEN WAITING FOR AN INPUT. ONLY USE WHEN RUNNING CORES
		/// </summary>
		public async Task Update(int frameCount)
		{
			frameCount--;

			if (_firstChange)
			{
				_prevStackBytes = NetworkController.GetBlankControllerInput(UserController.Definition, ConsolePort);
				_firstChange = false;
			}

			_inputStack.Enqueue(NetworkController.ControllerToBytes());
			byte[] stackBytes = _inputStack.Dequeue();

			if (stackBytes == null  || _prevStackBytes == null || stackBytes.Length < 1 || _prevStackBytes.Length < 1)
			{
				return;
			}

			Task _contUpdateTask = NetworkController.Update();

			List<List<byte>> changedBytes = new List<List<byte>>();

			List<byte> bufferBytes = new List<byte>();
			List<byte> prevBufferBytes = new List<byte>();

			int i = 0;
			int j = 0;
			while (i < stackBytes.Length && j < _prevStackBytes.Length)
			{
				if (stackBytes[i] != 255)
				{
					bufferBytes.Add(stackBytes[i]);
					i++;
				}

				if (_prevStackBytes[j] != 255)
				{
					prevBufferBytes.Add(_prevStackBytes[j]);
					j++;
				}


				if (_prevStackBytes[j] == 255 && stackBytes[i] == 255)
				{
					if (prevBufferBytes.Count != bufferBytes.Count || !Enumerable.SequenceEqual(prevBufferBytes, bufferBytes))
					{
						Console.WriteLine($"changed: {string.Join(" ", bufferBytes)}");
						changedBytes.Add(bufferBytes);
					}

					//string bufferBytesStr = NetworkController.PacketToString(bufferBytes.ToArray(), UserController.Definition);
					//string prevBytesStr = NetworkController.PacketToString(prevBufferBytes.ToArray(), UserController.Definition);
					//if (bufferBytesStr.Contains("Start")) Console.WriteLine(bufferBytesStr);
					//if (prevBytesStr.Contains("Start")) Console.WriteLine(prevBytesStr);

					bufferBytes.Clear();
					prevBufferBytes.Clear();

					i++;
					j++;
				}
			}

			await _contUpdateTask;

			UdpReceiveResult result;

			if (IsHost)
			{
				result = await _client.ReceiveAsync();
				//await Task.Run(() => OSDTask(result.Buffer, true));

				if (changedBytes.Count < 1)
				{
					await _client.SendAsync(new byte[] { (byte)'I' }, 1, _endPoint);
				}
				else 
				{
					foreach (List<byte> bytes in changedBytes)
					{
						Console.WriteLine(NetworkController.PacketToString(bytes.ToArray(), UserController.Definition));
						await _client.SendAsync(bytes.ToArray(), bytes.Count, _endPoint);
					}

					//await Task.Run(() => OSDTask(changedBytes, false));
				}


			}
			else
			{
				if (changedBytes.Count < 1)
				{
					await _client.SendAsync(new byte[] { (byte)'I' }, 1, _endPoint);
				}
				else
				{
					foreach (List<byte> bytes in changedBytes)
					{
						Console.WriteLine(NetworkController.PacketToString(bytes.ToArray(), UserController.Definition));
						await _client.SendAsync(bytes.ToArray(), bytes.Count, _endPoint);
					}

					//await Task.Run(() => OSDTask(changedBytes, false));
				}


				result = await _client.ReceiveAsync();
				//await Task.Run(() => OSDTask(result.Buffer, true));
			}

			if (result != null && result.Buffer.Length > 0 && result.Buffer[0] != 'I') NetworkController.PacketToController(result.Buffer);

			_prevStackBytes = (byte[])stackBytes.Clone();
		}

		Task OSDTask(byte[][] bytes, bool recieved)
		{
			MessagePosition pos = new MessagePosition()
			{
				X = -100,
				Y = 0,
				Anchor = recieved ? MessagePosition.AnchorType.BottomRight : MessagePosition.AnchorType.TopRight
			};

			string str = $"{(recieved ? "Recieved" : "Sending")}:\n";

			foreach(byte[] arr in bytes)
			{
				str += NetworkController.PacketToString(arr, UserController.Definition) + "\n";
			}

			GlobalWin.OSD.AddGuiText(str, pos, Color.Transparent, Color.Red);

			return Task.CompletedTask;
		}

		async Task OSDTask(List<List<byte>> bytes, bool recieved) => await OSDTask(bytes.Select(e => e.ToArray()).ToArray(), recieved);

		async Task OSDTask(byte[] bytes, bool recieved)
		{
			List<byte[]> inBytes = new List<byte[]>();
			List<byte> bufferBytes = new List<byte>();

			foreach (byte b in bytes)
			{
				if (b == 255)
				{
					inBytes.Add(bufferBytes.ToArray());
					bufferBytes.Clear();
				}

				bufferBytes.Add(b);
			}

			await OSDTask(inBytes.ToArray(), recieved);
		}


		/// <summary>
		/// Send a sync byte to the other end and waits
		/// </summary>
		public void Sync()
		{
			Console.WriteLine("attempted sync");
			if (IsHost)
			{
				byte[] dataBytes = _client.Receive(ref _endPoint);
				Console.WriteLine($"{Encoding.ASCII.GetString(dataBytes)} received.");

				Console.WriteLine($"Sending C success: {_client.Send(new[] { (byte)'C' }, 1, _endPoint)}");
			}
			else
			{
				Console.WriteLine($"Sending C success: {_client.Send(new[] { (byte)'C' }, 1, _endPoint)}");

				byte[] dataBytes = _client.Receive(ref _endPoint);
				Console.WriteLine($"{Encoding.ASCII.GetString(dataBytes)} received.");
			}

			Console.WriteLine("sync completed");
		}

		int ReadEndianBytes(bool isLittleEndian, byte[] bytes)
		{
			return isLittleEndian ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : BinaryPrimitives.ReadInt32BigEndian(bytes);
		}

		byte[] WriteEndianBytes(bool isLittleEndian, int val)
		{
			byte[] output = new byte[4];
			if (isLittleEndian) BinaryPrimitives.WriteInt32LittleEndian(output, val); 
			else BinaryPrimitives.WriteInt32BigEndian(output, val);

			return output;
		} 

		/// <summary>
		/// disposes resources used by the network client and closes all connections
		/// </summary>
		public void Dispose()
		{
			_client?.Close();
			_client?.Dispose();
		}
	}
}