using System.Net;
using System.Windows.Forms;
using System;

namespace BizHawk.Client.EmuHawk.Networking
{
	public partial class JoinForm : Form
	{
		public JoinForm()
		{
			InitializeComponent();
		}

		string _romLocation;
		private void ConnectButton_Click(object sender, EventArgs e)
		{
			if (!IPAddress.TryParse(IPBox.Text, out IPAddress address))
			{
				MessageBox.Show("Input in IP box is invalid. Must only have numbers or periods.", "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (!int.TryParse(PortBox.Text, out int port))
			{
				MessageBox.Show("Input in IP box is invalid. Must only have numbers.", "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			new ConnectionForm(false, _romLocation, new IPEndPoint(address, port), NameBox.Text).Show();
			Close();
		}

		private void OpenButton_Click(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				_romLocation = openFileDialog.FileName;
			}
		}
	}
}