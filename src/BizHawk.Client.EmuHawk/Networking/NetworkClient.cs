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
using System.Threading;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Networking class which allows the transferal of input across two clients.
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


		Dictionary<int, byte[]> _pendingLocalInputs = new Dictionary<int, byte[]>();
		Dictionary<int, byte[]> _pendingNetworkInputs = new Dictionary<int, byte[]>();

		UdpClient _client;
		IPEndPoint _endPoint;

		byte[] _prevContBytes = new byte[256];
		bool _firstChange = true;

		Task _receiveTask;
		Task _sendTask;
		CancellationTokenSource _tokenSource;

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
			_tokenSource = new CancellationTokenSource();
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
			_client = isHost ? new UdpClient(endPoint.Port) : new UdpClient();
			 IsHost = isHost;
		}


		bool _isEndian = BitConverter.IsLittleEndian;

		/// <summary>
		/// Updates the NetworkClient class. WARNING: WILL HANG WHEN WAITING FOR AN INPUT. ONLY USE WHEN RUNNING CORES
		/// </summary>
		public void Update(int frameCount)
		{
			if (_firstChange)
			{
				NetworkController.UpdateDefinition(NetworkController.UserController.Definition);
				_prevContBytes = NetworkController.GetBlankControllerInput(frameCount);
				_receiveTask = ReceivceTask();
				_firstChange = false;
			}

			//bytes that represents the controller data
			byte[] contBytes = NetworkController.ControllerToBytes(frameCount);
			if (contBytes == null  || _prevContBytes == null || contBytes.Length < 1 || _prevContBytes.Length < 1)
			{
				return;
			}

			byte[] prevBuffer = new byte[3];
			byte[] buffer = new byte[3];
            List<byte[]> sendBytes = new List<byte[]>();
			byte[] frameBytes = WriteEndianBytes(_isEndian, frameCount + FrameDelay);

			//get the bytes that changed and add them to the changedBytes array
			int i;
			for (i = 0; i < contBytes.Length && i < _prevContBytes.Length; i++)
			{
				if (i % 3 == 0)
                {
					if (!Enumerable.SequenceEqual(buffer, prevBuffer))
                    {
						//framecount, then buffer
						sendBytes.Add(frameBytes.Concat(buffer).ToArray());
                    }
                }

				buffer[i % 3] = contBytes[i];
				prevBuffer[i % 3] = _prevContBytes[i];
            }


			//write data to the other clients
			//each byte array in send bytes is 7 bytes long. first 4 bytes describe the current frame
			//and the last 3 describe what button has changed.
			if (sendBytes.Count > 0)
			{
				byte[] sendBytesArr = CondenseByteList(sendBytes, 7);
				_client.Send(sendBytesArr, sendBytesArr.Length, _endPoint);
				_pendingLocalInputs.Add(frameCount + FrameDelay, sendBytesArr.Skip(4).ToArray());
			}

			//update the networkcontroller based on the data we have.
			byte[] localBuffer;
			byte[] networkBuffer;
			if (_pendingLocalInputs.TryGetValue(frameCount, out localBuffer))
            {
				NetworkController.PacketToController(localBuffer, IsHost ? 1 : 2);
            }
			if (_pendingNetworkInputs.TryGetValue(frameCount, out networkBuffer))
            {
				NetworkController.PacketToController(networkBuffer, IsHost ? 2 : 1);
            }
			
			if (_receiveTask.IsFaulted)
            {
				throw _receiveTask.Exception;
            }
			_prevContBytes = (byte[])contBytes.Clone();
		}

		async Task ReceivceTask()
        {
			while (!_tokenSource.IsCancellationRequested)
			{
				UdpReceiveResult result = await _client.ReceiveAsync().ConfigureAwait(false);

				//parse the result and adjust the pending networkInputs accordingly
				//first 4 bytes is the frame where it has been pressed
				int frameCount = ReadEndianBytes(_isEndian, result.Buffer.Take(4).ToArray());
				_pendingNetworkInputs.Add(frameCount, result.Buffer.Skip(4).ToArray());

				Console.WriteLine($"Received: {NetworkController.PacketToString(result.Buffer)}");
			}
        }

		/// <summary>
		/// Send a sync byte to the other end and waits
		/// </summary>
		public void Sync()
		{
			Console.WriteLine("attempted sync");
			if (IsHost)
			{
				_endPoint = new IPEndPoint(IPAddress.Any, 0);
				byte[] dataBytes = _client.Receive(ref _endPoint);
				Console.WriteLine($"{Encoding.ASCII.GetString(dataBytes)} received.");

				Console.WriteLine($"Sending C success: {_client.Send(new[] { (byte)'C' }, 1, _endPoint)}");
			}
			else
			{
				Console.WriteLine($"Sending C success: {_client.Send(new[] { (byte)'C' }, 1, _endPoint)}");

				_endPoint = new IPEndPoint(IPAddress.Any, 0);
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
		
		byte[] CondenseByteList(List<byte[]> list, int inc)
        {
			//it's a bit faster to explcity to this rather than using linq
			byte[] output = new byte[list.Count * inc];

			int i = 0;
			foreach (byte[] arr in list)
            {
				foreach (byte b in arr)
				{ 
					output[i++] = b;
                }
            }

			return output;
        }

		/// <summary>
		/// disposes resources used by the network client and closes all connections
		/// </summary>
		public async void Dispose()
		{
			_tokenSource.Cancel();

			if (!(_receiveTask is null))
            {
				await _receiveTask;
            }

			_client?.Close();
			_client?.Dispose();
		}
	}
}