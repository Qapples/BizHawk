using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using Newtonsoft.Json.Linq;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// IController class used to interface with the networking classes for netplay
	/// </summary>
	public class NetworkController : IController
	{
		/// <summary>
		/// Definition of the user controller
		/// </summary>
		public ControllerDefinition Definition => UserController.Definition;

		/// <summary>
		/// Controller used by the user.
		/// </summary>
		public IController UserController { get; set; }

		/// <summary>
		/// port that the user controls 
		/// </summary>
		public byte UserPort { get; set; }

		/// <summary>
		/// The port on the console that is used by the other networking client or host
		/// </summary>
		public byte ClientPort { get; set; }

		public WorkingDictionary<string, bool> Buttons { get; set; }
		public WorkingDictionary<string, int> Axes { get; set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="userController">Controller that is controlled by the user</param>
		/// <param name="userPort">Port on the console where the controller controlled by the user is</param>
		/// <param name="clientPort">Port on the console that is controlled by the other network client</param>
		public NetworkController(IController userController, byte userPort, byte clientPort)
		{
			(Buttons, Axes, UserController, UserPort, ClientPort) = 
				(new WorkingDictionary<string, bool>(), new WorkingDictionary<string, int>(), userController, userPort, clientPort);
		}


		/// <summary>
		/// Takes the information in the UserController interface and adjusts the NetworkController accordingly.
		/// </summary>
		/// <returns></returns>
		public Task Update()
		{
			foreach (StringBuilder button in Definition.BoolButtons.Where(e => e[1] - '0' == UserPort).Select(e => new StringBuilder(e)))
			{
				bool value = UserController.IsPressed(button.ToString());
				button[1] = (char)(ClientPort + '0');
				Buttons[button.ToString()] = value;
				
				if (value)
				{
					Console.WriteLine($"{button} true");
				}
			}

			foreach (StringBuilder axis in Definition.Axes.Keys.Where(e => e[1] - '0' == UserPort).Select(e => new StringBuilder(e)))
			{
				int value = UserController.AxisValue(axis.ToString());
				axis[1] = (char)(ClientPort + '0');
				Axes[axis.ToString()] = value;
				
				if (value > 0)
				{
					Console.WriteLine($"{axis} {value}");
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Generates byte array data from the UserController that can beq parsed by network clients. Format is
		/// ([B]utton/[A]xis) (Controller Port) (Name of Button/Axis) (Button/Axis value)
		/// </summary>
		/// <returns>string data</returns>
		public byte[] ControllerToBytes()
		{
			List<byte> output = new List<byte>();
			
			//e[1] is the port number
			foreach (string button in Definition.BoolButtons.Where(e => e[1] - '0' == UserPort))
			{
				output.AddRange(GetBytesFromController(button, ClientPort, false));
			}

			foreach (string axis in Definition.Axes.Keys.Where(e => e[1] - '0' == UserPort))
			{
				output.AddRange(GetBytesFromController(axis, ClientPort, true));
			}


			return output.ToArray();
		}

		/// <summary>
		/// Gets the bytes from a controller
		/// </summary>
		/// <param name="name"></param>
		/// <param name="port"></param>
		/// <param name="isAxis"></param>
		/// <returns></returns>
		byte[] GetBytesFromController(string name, byte port, bool isAxis)
		{
			List<byte> output = new List<byte> {isAxis ? (byte)1 : (byte)0, (byte) name.Length, port};

			output.AddRange(Encoding.ASCII.GetBytes(name));
			output.Add(3); //end of text in ascii.
			if (isAxis) output.Add((byte)UserController.AxisValue(name));
			else output.Add(UserController.IsPressed(name) ? (byte)1 : (byte)0);
			output.Add(255); //end of transmission

			return output.ToArray();
		}

		/// <summary>
		/// Adjusts the oject based on a string value usually recevied from another client
		/// </summary>
		/// <param name="packet">packet to obtain data from</param>
		public void PacketToController(byte[] packet)
		{
			int i = 0;

			while (i < packet.Length)
			{
				bool isAxis = packet[i] == 1;
				byte length = packet[i + 1];
				int port = packet[i + 2];

				//get the bytes and name and check if there is a mismatch
				byte[] nameBytes = new byte[length];
				int eotIndex = -1;
				int j;
				for (j = i + 3; j < i + 3 + length; j++) 
				{
					nameBytes[j - (i + 3)] = packet[j];

					if (packet[j] == 3) eotIndex = j;
				}
				
				//TODO: In this case, the packet is malformed. Perhaps find a better way to handle it other than just ignoring it?
				if (eotIndex < 0 || eotIndex > length)
				{
					Console.WriteLine($"Packet malformed. eotIndex: {eotIndex}. Length: {length}. i: {i}. Value: {packet[i]}");
					return;
				}
				
				string name = Encoding.ASCII.GetString(packet, i + 3, length);

				//if isAxis is ture then we are dealing with an axis value and we need to parse it as an int 
				if (isAxis)
				{
					Axes[port + " " + name] = packet[packet.Length - 1];
				}
				else
				{
					Buttons[port + " " + name] = packet[packet.Length - 1] == 1;
				}

				i += length + 4;
			}

		}

		//---------------
		// Static Methods
		//---------------

		/// <summary>
		/// gets a blank controller output with a definiation and a port
		/// </summary>
		/// <param name="definition">controller definitions. determines what axis and buttons to use</param>
		/// <param name="port">console port number</param>
		/// <returns></returns>
		public static byte[] GetBlankControllerInput(ControllerDefinition definition, byte port)
		{
			List<byte> output = new List<byte>();
			
			foreach (string button in definition.BoolButtons.Where(e => e[1] - '0' == port))
			{
				output.AddRange(GetBlankControllerBytes(button, port, false));
			}

			foreach (string axis in definition.Axes.Keys.Where(e => e[1] - '0' == port))
			{
				output.AddRange(GetBlankControllerBytes(axis, port, true));
			}

			return output.ToArray();
		}

		/// <summary>
		/// Generates a string that is represenative of the packet;
		/// </summary>
		/// <param name="packet">packet to convert from</param>
		/// <param name="definition">defenition used to convert</param>
		/// <returns></returns>
		public static string PacketToString(byte[] packet, ControllerDefinition definition)
		{
			int i = 0;

			StringBuilder output = new StringBuilder();
			while (i < packet.Length && packet.Length > 3)
			{
				bool isAxis = packet[i] == 1;
				byte length = packet[i + 1];
				int port = packet[i + 2];

				//get the bytes and name and check if there is a mismatch
				byte[] nameBytes = new byte[length];
				int eotIndex = -1;
				int j;
				for (j = i + 3; j < i + 3 + length; j++) 
				{
					nameBytes[j - (i + 3)] = packet[j];
					if (packet[j] == 3) eotIndex = j;
				}
				
				string name = Encoding.ASCII.GetString(packet, i + 3, length);

				output.Append($"{isAxis} {port} {name} {packet[packet.Length - 1]}");

				i += length + 5;
			}

			return output.ToString();
		}

		/// <summary>
		/// Gets byte data with all base information. The last byte (the value of the name) is missing however, you must add that on your own terms
		/// </summary>
		/// <param name="name"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		static byte[] GetBlankControllerBytes(string name, byte port, bool isAxis)
		{
			List<byte> output = new List<byte> {isAxis ? (byte)1 : (byte) 0, (byte)name.Length, port };

			output.AddRange(Encoding.ASCII.GetBytes(name));
			output.AddRange(new byte[] { 3, 0, 255 }); //end of text in ascii.

			return output.ToArray();
		}


		public bool IsPressed(string button) => Buttons[button];


		public int AxisValue(string axes) => Axes[axes];
	}
}