using System.Net;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.Networking
{
	public partial class HostForm : Form
	{
		private string _romLocation;

		public HostForm()
		{
			InitializeComponent();
		}

		private void ConnectButton_Click(object sender, System.EventArgs e)
		{
			if (!int.TryParse(PortBox.Text, out int port))
			{
				MessageBox.Show("Input in port box is invalid. Must only have numbers.", "Parse error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			new ConnectionForm(true, _romLocation, new IPEndPoint(IPAddress.Any, port), NameBox.Text).Show();
			Close();
		}

		private void HostForm_Load(object sender, System.EventArgs e)
		{

		}

		private void OpenButton_Click(object sender, System.EventArgs e)
		{
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				_romLocation = openFileDialog.FileName;
			}
		}
	}
}