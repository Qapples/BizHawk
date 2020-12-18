using System.ComponentModel;

namespace BizHawk.Client.EmuHawk.Networking
{
	partial class ConnectionForm
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
            this.ChatBox = new System.Windows.Forms.RichTextBox();
            this.PlayerBox = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.ChatTextBox = new System.Windows.Forms.TextBox();
            this.SendButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.frameNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.BeginButton = new System.Windows.Forms.Button();
            this.DropButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.frameNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // ChatBox
            // 
            this.ChatBox.Location = new System.Drawing.Point(12, 25);
            this.ChatBox.Name = "ChatBox";
            this.ChatBox.Size = new System.Drawing.Size(656, 347);
            this.ChatBox.TabIndex = 2;
            this.ChatBox.Text = "";
            // 
            // PlayerBox
            // 
            this.PlayerBox.Location = new System.Drawing.Point(681, 25);
            this.PlayerBox.Name = "PlayerBox";
            this.PlayerBox.Size = new System.Drawing.Size(107, 347);
            this.PlayerBox.TabIndex = 3;
            this.PlayerBox.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(324, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Chat:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(715, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Players:";
            // 
            // ChatTextBox
            // 
            this.ChatTextBox.Location = new System.Drawing.Point(12, 378);
            this.ChatTextBox.Name = "ChatTextBox";
            this.ChatTextBox.Size = new System.Drawing.Size(582, 20);
            this.ChatTextBox.TabIndex = 6;
            // 
            // SendButton
            // 
            this.SendButton.Location = new System.Drawing.Point(600, 378);
            this.SendButton.Name = "SendButton";
            this.SendButton.Size = new System.Drawing.Size(68, 20);
            this.SendButton.TabIndex = 7;
            this.SendButton.Text = "Send";
            this.SendButton.UseVisualStyleBackColor = true;
            this.SendButton.Click += new System.EventHandler(this.SendButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 428);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Frame Delay:";
            // 
            // frameNumericUpDown
            // 
            this.frameNumericUpDown.Location = new System.Drawing.Point(84, 426);
            this.frameNumericUpDown.Name = "frameNumericUpDown";
            this.frameNumericUpDown.Size = new System.Drawing.Size(35, 20);
            this.frameNumericUpDown.TabIndex = 9;
            this.frameNumericUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // BeginButton
            // 
            this.BeginButton.Location = new System.Drawing.Point(699, 410);
            this.BeginButton.Name = "BeginButton";
            this.BeginButton.Size = new System.Drawing.Size(75, 28);
            this.BeginButton.TabIndex = 10;
            this.BeginButton.Text = "Begin";
            this.BeginButton.UseVisualStyleBackColor = true;
            this.BeginButton.Click += new System.EventHandler(this.BeginButton_Click);
            // 
            // DropButton
            // 
            this.DropButton.Location = new System.Drawing.Point(618, 410);
            this.DropButton.Name = "DropButton";
            this.DropButton.Size = new System.Drawing.Size(75, 28);
            this.DropButton.TabIndex = 11;
            this.DropButton.Text = "Drop";
            this.DropButton.UseVisualStyleBackColor = true;
            this.DropButton.Click += new System.EventHandler(this.DropButton_Click);
            // 
            // ConnectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.DropButton);
            this.Controls.Add(this.BeginButton);
            this.Controls.Add(this.frameNumericUpDown);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.SendButton);
            this.Controls.Add(this.ChatTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PlayerBox);
            this.Controls.Add(this.ChatBox);
            this.Name = "ConnectionForm";
            this.Text = "ConnectionForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ConnectionForm_FormClosed);
            this.Load += new System.EventHandler(this.ConnectionForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.frameNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.RichTextBox ChatBox;
		private System.Windows.Forms.RichTextBox PlayerBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox ChatTextBox;
		private System.Windows.Forms.Button SendButton;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown frameNumericUpDown;
		private System.Windows.Forms.Button BeginButton;
		private System.Windows.Forms.Button DropButton;
	}
}