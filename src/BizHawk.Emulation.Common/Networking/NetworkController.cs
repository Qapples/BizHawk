using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// IController class used to interface with the networking classes for netplay
	/// </summary>
	public class NetworkController : IController
	{
		public ControllerDefinition Definition { get; set; }
		
		/// <summary>
		/// Frame delay before the inputs in the client controller are recgonized
		/// </summary>
		public int FrameDelay { get; set; }
		
		/// <summary>
		/// Controller used by the user.
		/// </summary>
		public IController UserController { get; set; }
		
		/// <summary>
		/// port that the user controls 
		/// </summary>
		public int UserPort { get; set; }
		
		/// <summary>
		/// The port on the console that is used by the other networking client or host
		/// </summary>
		public int ClientPort { get; set; }
		
		public WorkingDictionary<string, bool> Buttons { get; set; }
		public WorkingDictionary<string, int> Axes { get; set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="clientController">Controller that is controlled by the user</param>
		/// <param name="userPort">Port on the console where the controller controlled by the user is</param>
		/// <param name="clientPort">Port on the console that is controlled by the other network client</param>
		public NetworkController(IController clientController, int userPort, int clientPort)
		{
			(UserController, Definition, UserPort, ClientPort) = (clientController, clientController.Definition, userPort, clientPort);
		}

		/// <summary>
		/// Generates string data from the UserController that can beq parsed by network clients. Format is
		/// ([B]utton/[A]xis) (Controller Port) (Name of Button/Axis) (Button/Axis value)
		/// </summary>
		/// <returns>string data</returns>
		public string ControllerToString()
		{
			StringBuilder output = new StringBuilder();
			
			//e[1] is the port number
			foreach (string button in Definition.BoolButtons.Where(e => e[1] - '0' == UserPort))
			{
				output.Append($"B{UserPort} {button} {UserController.IsPressed(button)}\n");
			}

			foreach (string axis in Definition.Axes.Keys.Where(e => e[1] - '0' == UserPort))
			{
				output.Append($"A{UserPort} {UserController.AxisValue(axis)}\n");
			}

			output.Append("END\n");

			return output.ToString();
		}

		/// <summary>
		/// Adjusts the oject based on a string value usually recevied from another client
		/// </summary>
		/// <param name="str">String to convert from</param>
		public void StringToController(string str)
		{
			foreach (string line in str.Split('\n'))
			{
				string[] splitLn = line.Split(' ');

				int port = splitLn[0][1] - '0';
				char dataType = splitLn[0][0];
				string dataName = splitLn[1];
				
				if (dataType == 'B' && bool.TryParse(splitLn[2], out bool boolVal))
				{
					Buttons[port + " " + dataName] = boolVal;
				}
				else if (dataType == 'A' && int.TryParse(splitLn[2], out int axisValue))
				{
					Axes[port + " " + dataName] = axisValue;
				}
				else
				{
					throw new InvalidDataException(
						"Received string value is invalid. Format should be ([B]utton/[A]xis)(Controller Port) (Name of Button/Axis) (Button/Axis value)");
				}
			}
		}

		/// <summary>
		/// gets a blank controller output with a definiation and a port
		/// </summary>
		/// <param name="definition">controller definitions. determines what axis and buttons to use</param>
		/// <param name="port">console port number</param>
		/// <returns></returns>
		public static string GetBlankController(ControllerDefinition definition, int port)
		{
			StringBuilder output = new StringBuilder();
			foreach (string button in definition.BoolButtons)
			{
				output.Append($"B {port} {button} false\n");
			}

			foreach (string axis in definition.Axes.Keys)
			{
				output.Append($"A {port} {axis} 0\n");
			}

			return output.ToString();
		}



		public bool IsPressed(string button) => Buttons[button];

		public int AxisValue(string name) => Axes[name];
	}
}