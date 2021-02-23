using System;
using System.Buffers.Binary;
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
		/// Every button in the user controller definition
		/// </summary>
		public string[] ButtonsArr { get; set; }

		/// <summary>
		/// Every axis in the user controller definition
		/// </summary>
		public string[] AxesArr { get; set; }

		bool _isEndian => BitConverter.IsLittleEndian;

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

			if (userController is null) return;
			UpdateDefinition(userController.Definition);
		}
		
		/// <summary>
		/// Adjusts the ButtonsArr and AxesArr based on a definition
		/// </summary>
		/// <param name="definition"></param>
		public void UpdateDefinition(ControllerDefinition definition)
        {
			int bCnt = definition.BoolButtons.Count;
			int aCnt = definition.Axes.Keys.Count();
			ButtonsArr = new string[bCnt];
			AxesArr = new string[aCnt];

			//the first two buttons are conosle wide in the definition
			ButtonsArr[0] = "Reset";
			ButtonsArr[1] = "Power";

			int i;
			for (i = 2; i < bCnt; i++)
			{
				ButtonsArr[i] = definition.BoolButtons[i].Substring(3);
			}

			i = 0;
			foreach (string s in definition.Axes.Keys)
			{
				AxesArr[i] = s.Substring(3);
				i++;
			}
		}

		/// <summary>
		/// Generates byte array data from the UserController that can beq parsed by network clients. Format is
		/// (4 bytes: current frame count (ADD BEFORE HAND!) (0 for button, 1 for axis) (Name ID) (Value)
		/// 3 bytes represent an axis or button that can be pressed or not
		/// </summary>
		/// <param name="frameCount">the current frame count. the first 4 bytes of each button will be this number</param>
		/// <returns>string data</returns>
		public byte[] ControllerToBytes(int frameCount)
		{
			List<byte> output = new List<byte>();

			for (byte i = 0; i < ButtonsArr.Length; i++)
			{
				bool isPressed = UserController.IsPressed(i < 2 ? ButtonsArr[i] : $"P{UserPort} {ButtonsArr[i]}");
				output.AddRange(new byte[] { 0, i, isPressed ? (byte)1 : (byte)0});
			}

			for (byte i = 0; i < AxesArr.Length; i++)
			{
				int axisValue = UserController.AxisValue($"P{UserPort} {AxesArr[i]}");
				output.AddRange(new byte[] { 1, i, (byte)axisValue });
			}

			return output.ToArray();
		}

		/// <summary>
		/// Adjusts this object based on an array of bytes which are usually recevied from another client
		/// </summary>
		/// <param name="packet">data to adjust the object from</param>
		/// <param name="port">Determines which port will be affected by the packet</param>
		public void PacketToController(byte[] packet, int port)
		{
			byte[] buffer = new byte[3];

			for (int i = 0; i < packet.Length; i++)
			{
				if (i % 3 == 0 && i > 0)
				{
					//4th index determines if it is an axis or not. 0 = button, 1 = axis
					if (buffer[0] == 0)
                    {
						Buttons[ButtonsArr[buffer[1]]] = buffer[2] == 1;
                    }
                    else
                    {
						Axes[AxesArr[buffer[1]]] = buffer[2];
                    }
                }

				buffer[i % 3] = packet[i];
            }

			if (buffer[0] == 0)
			{
				Buttons[$"P{port} {ButtonsArr[buffer[1]]}"] = buffer[2] == 1;
			}
			else
			{
				Axes[$"P{port} {AxesArr[buffer[1]]}"] = buffer[2];
			}
		}

		/// <summary>
		/// gets a byte array with all defienitoins with a default value of zero
		/// </summary
		/// <returns></returns>
		public byte[] GetBlankControllerInput(int frameCount)
		{
			List<byte> output = new List<byte>();

			for (byte i = 0; i < ButtonsArr.Length; i++)
			{
				output.AddRange(new byte[] { 0, i, 0 });
			}

			for (byte i = 0; i < AxesArr.Length; i++)
			{
				output.AddRange(new byte[] { 1, i, 0 });
			}

			return output.ToArray();
		}

		/// <summary>
		/// Generates a string that is represenative of a packet (this packet contains 7 bytes instead of 4! the first 4 bytes are the frame count)
		/// </summary>
		/// <param name="packet">packet to convert from</param>
		/// <returns></returns>
		public string PacketToString(byte[] packet)
		{
			StringBuilder output = new StringBuilder();

			byte[] buffer = new byte[7];
			int frameCount;
			bool isAxis;
			string name;

			for (int i = 0; i < packet.Length; i++)
            {
				if (i % 7 == 0 && i > 0)
                {
					frameCount = ReadEndianBytes(_isEndian, buffer.Take(4).ToArray());
					isAxis = buffer[4] == 1;
					name = isAxis ? AxesArr[buffer[5]] : ButtonsArr[buffer[5]];

					output.Append($"{frameCount} {isAxis} {name} {buffer[6]}\n");
                }

				buffer[i % 7] = packet[i];
            }

			frameCount = ReadEndianBytes(_isEndian, buffer.Take(4).ToArray());
			isAxis = buffer[4] == 1;
			name = isAxis ? AxesArr[buffer[5]] : ButtonsArr[buffer[5]];

			output.Append($"{frameCount} {isAxis} {name} {buffer[6]}\n");

			return output.ToString();
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

		public bool IsPressed(string button) => Buttons[button];

		public int AxisValue(string axes) => Axes[axes];
	}
}