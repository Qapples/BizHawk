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
		/// Controller used by the user.
		/// </summary>
		public IController ClientController { get; set; }
		
		public WorkingDictionary<string, bool> Buttons { get; set; }
		public WorkingDictionary<string, int> Axes { get; set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="clientController">Controller that is controlled by the client</param>
		/// <param name="definition">Controller Definition of both the client controller and this controller</param>
		public NetworkController(IController clientController, ControllerDefinition definition)
		{
			(ClientController, Definition) = (clientController, definition);
		}

		/// <summary>
		/// Converts an iController to string data that can beq parsed by network clients. Format is
		/// ([B]utton/[A]xis) (Controller Port) (Name of Button/Axis) (Button/Axis value)
		/// </summary>
		/// <param name="port">Index/player of the controller to convert to string</param>
		/// <returns>string data</returns>
		public string ControllerToString(int port)
		{
			StringBuilder output = new StringBuilder();
			foreach (string button in Definition.BoolButtons)
			{
				output.Append($"B {port} {button} {IsPressed(button)}\n");
			}

			foreach (string axis in Definition.Axes.Keys)
			{
				output.Append($"A {port} {axis} {AxisValue(axis)}\n");
			}

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

				bool portValid = int.TryParse(splitLn[1], out int port);
				string dataType = splitLn[0];
				string dataName = splitLn[2];
				
				if (dataType == "B" && bool.TryParse(splitLn[3], out bool boolVal) && portValid)
				{
					Buttons[port + " " + dataName] = boolVal;
				}
				else if (splitLn[1] == "A" && int.TryParse(splitLn[3], out int axisValue) && portValid)
				{
					Axes[port + " " + dataName] = axisValue;
				}
				else
				{
					throw new InvalidDataException(
						"Received string value is invalid. Format should be ([B]utton/[A]xis) (Controller Port) (Name of Button/Axis) (Button/Axis value)");
				}
			}
		}

		public bool IsPressed(string button) => Buttons[button];

		public int AxisValue(string name) => Axes[name];
	}
}