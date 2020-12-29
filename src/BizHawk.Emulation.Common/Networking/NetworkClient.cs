using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Buffers.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace BizHawk.Emulation.Common
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

		IController _userController;
		/// <summary>
		/// Client used by the user. this class needs to delay inputs from the client to ensure that the games
		/// are synced
		/// </summary>
		public IController UserController
		{
			get => _userController;
			set
			{
				NetworkController.UserController = value;
				_userController = value;
			}
		}

		Stack<byte[]> _inputStack = new Stack<byte[]>();

		UdpClient _client;
		IPEndPoint _endPoint;

		/// <summary>
		///
		/// </summary>
		/// <param name="hostEndPoint">End point of the host</param>
		/// <param name="frameDelay">amount of frames before an input is registered</param>
		/// <param name="consolePort">port on the console of this client</param>
		public NetworkClient(IPEndPoint hostEndPoint, IController userController, byte frameDelay, byte consolePort)
		{
			NetworkController = new NetworkController(userController, ConsolePort, 1);
			(HostEndPoint, _endPoint, UserController, FrameDelay, ConsolePort) =
				(hostEndPoint, hostEndPoint, userController, frameDelay, consolePort);
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
			if (frameCount > FrameDelay)
			{
				_inputStack.Push(NetworkController.ControllerToBytes());
			}
			else
			{
				//blank controller, don't accept inputs from either the user or the host
				//_inputStack.Push(NetworkController.GetBlankControllerInput(NetworkController.Definition, ConsolePort));
			}
			if (frameCount < 1) return;

			Console.WriteLine($"Local frame: {frameCount}");
		}

		/// <summary>
		/// Send a sync byte to the other end and waits
		/// </summary>
		public void Sync()
		{
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