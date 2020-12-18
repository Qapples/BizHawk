using System.ComponentModel;

namespace BizHawk.Client.EmuHawk.Networking
{
	partial class HostForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.label2 = new System.Windows.Forms.Label();
            this.PortBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.HostButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.OpenButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Port:";
            // 
            // PortBox
            // 
            this.PortBox.Location = new System.Drawing.Point(36, 2);
            this.PortBox.Name = "PortBox";
            this.PortBox.Size = new System.Drawing.Size(100, 20);
            this.PortBox.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1, 36);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Name:";
            // 
            // NameBox
            // 
            this.NameBox.Location = new System.Drawing.Point(36, 33);
            this.NameBox.Name = "NameBox";
            this.NameBox.Size = new System.Drawing.Size(100, 20);
            this.NameBox.TabIndex = 7;
            // 
            // HostButton
            // 
            this.HostButton.Location = new System.Drawing.Point(36, 102);
            this.HostButton.Name = "HostButton";
            this.HostButton.Size = new System.Drawing.Size(75, 23);
            this.HostButton.TabIndex = 8;
            this.HostButton.Text = "Host";
            this.HostButton.UseVisualStyleBackColor = true;
            this.HostButton.Click += new System.EventHandler(this.ConnectButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(1, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "ROM:";
            // 
            // OpenButton
            // 
            this.OpenButton.Location = new System.Drawing.Point(42, 65);
            this.OpenButton.Name = "OpenButton";
            this.OpenButton.Size = new System.Drawing.Size(75, 23);
            this.OpenButton.TabIndex = 11;
            this.OpenButton.Text = "Open ROM";
            this.OpenButton.UseVisualStyleBackColor = true;
            this.OpenButton.Click += new System.EventHandler(this.OpenButton_Click);
            // 
            // HostForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(138, 130);
            this.Controls.Add(this.OpenButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.HostButton);
            this.Controls.Add(this.NameBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.PortBox);
            this.Controls.Add(this.label2);
            this.Name = "HostForm";
            this.Text = "HostForm";
            this.Load += new System.EventHandler(this.HostForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox PortBox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox NameBox;
		private System.Windows.Forms.Button HostButton;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Button OpenButton;
	}
}