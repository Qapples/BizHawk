using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
		public int FrameDelay { get; set; }
		
		/// <summary>
		/// Port of this client.
		/// </summary>
		public int ConsolePort { get; set; }
		
		/// <summary>
		/// NetworkController used to interface with consoles
		/// </summary>
		public NetworkController NetworkController { get; set; }
		
		/// <summary>
		/// Client used by the user. this class needs to delay inputs from the client to ensure that the games
		/// are synced
		/// </summary>
		public IController UserController { get; set; }

		private Stack<string> _inputStack = new Stack<string>();
		private TcpClient _client;

		private StreamReader _streamReader;
		private StreamWriter _streamWriter;

		/// <summary>
		///
		/// </summary>
		/// <param name="hostEndPoint">End point of the host</param>
		/// <param name="frameDelay">amount of frames before an input is registered</param>
		/// <param name="consolePort">port on the console of this client</param>
		public NetworkClient(IPEndPoint hostEndPoint, IController userController, int frameDelay, int consolePort)
		{
			(HostEndPoint, UserController, FrameDelay, ConsolePort) = (hostEndPoint, userController, frameDelay, consolePort);
			NetworkController = new NetworkController(userController, ConsolePort, 1);
		}

		/// <summary>
		/// connects to a server using the HostEndPoint property of the obbject
		/// </summary>
		public void Connect() => Connect(HostEndPoint);

		/// <summary>
		/// connects to a server using a parameter
		/// </summary>
		/// <param name="endPoint">endpoint of the server</param>
		public void Connect(IPEndPoint endPoint)
		{
			_client = new TcpClient(endPoint);
			_streamReader = new StreamReader(_client.GetStream());
			_streamWriter = new StreamWriter(_client.GetStream());
		}
		
		/// <summary>
		/// Updates the NetworkClient class. WARNING: WILL HANG WHEN WAITING FOR AN INPUT. ONLY USE WHEN RUNNING CORES
		/// </summary>
		public void Update(int frameCount)
		{
			if (frameCount > FrameDelay)
			{
				_inputStack.Push(NetworkController.ControllerToString());
			}
			else
			{
				//blank controller, don't accept inputs from either the user or the host
				_inputStack.Push(NetworkController.GetBlankController(NetworkController.Definition, ConsolePort));
				return;
			}
			
			//get imputs from the input stack from the user and apply them to the output controller boject
			NetworkController.StringToController(_inputStack.Pop());
			
			//read input from the stream reader and wait for input from the other end
			string inputLn = _streamReader.ReadLine();
			while (inputLn != null) inputLn = _streamReader.ReadLine();

			//read the input and adjust the network controller
			StringBuilder input = new StringBuilder();
			while (inputLn != "END")
			{
				input.Append(inputLn);
				inputLn = _streamReader.ReadLine();
			}
			NetworkController.StringToController(input.ToString());
			
			//send the input to the other client
			_streamWriter.WriteLine(NetworkController.ControllerToString());

		}

		/// <summary>
		/// disposes resources used by the network client and closes all connections
		/// </summary>
		public void Dispose()
		{
			_client?.Close();
			_client?.Dispose();
			_streamReader?.Dispose();
			_streamWriter?.Dispose();
		}
	}
}