using System.Net;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.Networking
{
	public partial class JoinForm : Form
	{
		public JoinForm()
		{
			InitializeComponent();
		}

		private void ConnectButton_Click(object sender, System.EventArgs e)
		{
			IPAddress address;
			int port;

			if (!IPAddress.TryParse(IPBox.Text, out address))
			{
				MessageBox.Show("Input in IP box is invalid. Must only have numbers or periods.", "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (!int.TryParse(PortBox.Text, out port))
			{
				MessageBox.Show("Input in IP box is invalid. Must only have numbers.", "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			new ConnectionForm(false, new IPEndPoint(address, port), NameBox.Text).Show();
			Close();
		}
	}
}