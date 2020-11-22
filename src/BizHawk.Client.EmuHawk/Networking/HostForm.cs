using System.Net;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.Networking
{
	public partial class HostForm : Form
	{
		public HostForm()
		{
			InitializeComponent();
		}

		private void ConnectButton_Click(object sender, System.EventArgs e)
		{
			int port;
			if (!int.TryParse(PortBox.Text, out port))
			{
				MessageBox.Show("Input in port box is invalid. Must only have numbers.", "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			new ConnectionForm(true, new IPEndPoint(IPAddress.Loopback, port), NameBox.Text).Show();
			Close();
		}
	}
}