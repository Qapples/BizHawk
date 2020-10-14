using System.Net;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Networking class which allows the transferal of input across two clients. For now, it'll use simple delay
	/// based netcode. But later it should support rollback. Since this requires precise timings we'll use TCP instead
	/// of UDP
	/// </summary>
	public class NetworkClient
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
		public int Port { get; set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="hostEndPoint">End point of the host</param>
		/// <param name="frameDelay">amount of frames before an input is registered</param>
		/// <param name="port">port on the console of this client</param>
		public NetworkClient(IPEndPoint hostEndPoint, int frameDelay, int port)
		{
			(HostEndPoint, FrameDelay, Port) = (hostEndPoint, frameDelay, port);
		}
		
		public bool Update
	}
}