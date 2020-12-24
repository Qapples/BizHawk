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
		bool _isHost;

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
			_isHost = isHost;
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

			_isHost = isHost;
		}


		bool _isEndian = BitConverter.IsLittleEndian;
		/// <summary>
		/// Updates the NetworkClient class. WARNING: WILL HANG WHEN WAITING FOR AN INPUT. ONLY USE WHEN RUNNING CORES
		/// </summary>
		public void Update(int frameCount)
		{
			/*
            if (frameCount > FrameDelay)
            {
                _inputStack.Push(NetworkController.ControllerToBytes());
            }
            else
            {
                //blank controller, don't accept inputs from either the user or the host
                _inputStack.Push(NetworkController.GetBlankControllerInput(NetworkController.Definition, ConsolePort));
				return;
            }

            //get imputs from the input stack from the user and apply them to the output controller boject
            NetworkController.PacketToController(_inputStack.Pop());

			byte[] a = ReadBytes();
			Console.Write("data bytes: ");

			foreach (byte b in a)
			{
				Console.Write(b + " ");
			}
			Console.WriteLine();

			NetworkController.PacketToController(a);
			*/
			//send the input to the other client
			//if we are the host, then receive first and send later. if we are the connector, send first and rleceive later

			Console.WriteLine("framecount: " + frameCount);
			if (frameCount == 1)
			{
				byte[] recvWaitBytes = null;
				do
				{
					_client.Send(new[] { (byte)'C' }, 1, _endPoint);
				}
				while ((recvWaitBytes = _client.Receive(ref _endPoint)) != null);
				Console.WriteLine($"{Encoding.ASCII.GetString(recvWaitBytes)} end received. now starting.");
			}

			if (_isHost)
			{
				int clientFrame;

				//wait untill the other client has finished their frame. we are usaully ahead since we are the host
				while ((clientFrame = ReadEndianBytes(_isEndian, _client.Receive(ref _endPoint))) < frameCount) ;
				Console.WriteLine($"client frame: {clientFrame} local frame {frameCount}");

				byte[] sendBytes = WriteEndianBytes(_isEndian, frameCount);
				_client.Send(sendBytes, sendBytes.Length, _endPoint);

			}
			else
			{
				int hostFrame;

				byte[] sendBytes = WriteEndianBytes(_isEndian, frameCount);
				_client.Send(sendBytes, sendBytes.Length, _endPoint);

				while ((hostFrame = ReadEndianBytes(_isEndian, _client.Receive(ref _endPoint))) < frameCount) ;
				Console.WriteLine($"host frame: {hostFrame}. local frame {frameCount}");
			}
		}

		int ReadEndianBytes(bool isLittleEndian, byte[] bytes)
		{
			Console.WriteLine($"Endian byte size: {bytes.Length}");
			return isLittleEndian ? BinaryPrimitives.ReadInt32LittleEndian(bytes) : BinaryPrimitives.ReadInt32BigEndian(bytes);
		}

		byte[] WriteEndianBytes(bool isLittleEndian, int val)
		{
			byte[] output = new byte[512];
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