
namespace Server
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.logs = new System.Windows.Forms.RichTextBox();
            this.button_listen = new System.Windows.Forms.Button();
            this.textBox_server1Port = new System.Windows.Forms.TextBox();
            this.textbox_ID = new System.Windows.Forms.TextBox();
            this.button_assignID = new System.Windows.Forms.Button();
            this.button_choose_db = new System.Windows.Forms.Button();
            this.textBox_masterPort = new System.Windows.Forms.TextBox();
            this.button_masterPort = new System.Windows.Forms.Button();
            this.textBox_server2Port = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // logs
            // 
            this.logs.Location = new System.Drawing.Point(10, 10);
            this.logs.Margin = new System.Windows.Forms.Padding(2);
            this.logs.Name = "logs";
            this.logs.ReadOnly = true;
            this.logs.Size = new System.Drawing.Size(554, 418);
            this.logs.TabIndex = 0;
            this.logs.Text = "";
            // 
            // button_listen
            // 
            this.button_listen.Enabled = false;
            this.button_listen.Location = new System.Drawing.Point(588, 279);
            this.button_listen.Margin = new System.Windows.Forms.Padding(2);
            this.button_listen.Name = "button_listen";
            this.button_listen.Size = new System.Drawing.Size(100, 29);
            this.button_listen.TabIndex = 6;
            this.button_listen.Text = "Listen";
            this.button_listen.UseVisualStyleBackColor = true;
            this.button_listen.Click += new System.EventHandler(this.button_listen_Click);
            // 
            // textBox_server1Port
            // 
            this.textBox_server1Port.Enabled = false;
            this.textBox_server1Port.Location = new System.Drawing.Point(586, 193);
            this.textBox_server1Port.Margin = new System.Windows.Forms.Padding(2);
            this.textBox_server1Port.Name = "textBox_server1Port";
            this.textBox_server1Port.Size = new System.Drawing.Size(100, 27);
            this.textBox_server1Port.TabIndex = 4;
            // 
            // textbox_ID
            // 
            this.textbox_ID.Location = new System.Drawing.Point(588, 18);
            this.textbox_ID.Margin = new System.Windows.Forms.Padding(2);
            this.textbox_ID.Name = "textbox_ID";
            this.textbox_ID.Size = new System.Drawing.Size(100, 27);
            this.textbox_ID.TabIndex = 1;
            // 
            // button_assignID
            // 
            this.button_assignID.Location = new System.Drawing.Point(588, 48);
            this.button_assignID.Margin = new System.Windows.Forms.Padding(2);
            this.button_assignID.Name = "button_assignID";
            this.button_assignID.Size = new System.Drawing.Size(99, 29);
            this.button_assignID.TabIndex = 2;
            this.button_assignID.Text = "Assign ID";
            this.button_assignID.UseVisualStyleBackColor = true;
            this.button_assignID.Click += new System.EventHandler(this.button_assignID_Click);
            // 
            // button_choose_db
            // 
            this.button_choose_db.Enabled = false;
            this.button_choose_db.Location = new System.Drawing.Point(10, 431);
            this.button_choose_db.Margin = new System.Windows.Forms.Padding(2);
            this.button_choose_db.Name = "button_choose_db";
            this.button_choose_db.Size = new System.Drawing.Size(96, 45);
            this.button_choose_db.TabIndex = 8;
            this.button_choose_db.Text = "Choose DB";
            this.button_choose_db.UseVisualStyleBackColor = true;
            this.button_choose_db.Click += new System.EventHandler(this.choose_db_Click);
            // 
            // textBox_masterPort
            // 
            this.textBox_masterPort.Enabled = false;
            this.textBox_masterPort.Location = new System.Drawing.Point(587, 141);
            this.textBox_masterPort.Name = "textBox_masterPort";
            this.textBox_masterPort.Size = new System.Drawing.Size(99, 27);
            this.textBox_masterPort.TabIndex = 3;
            // 
            // button_masterPort
            // 
            this.button_masterPort.Enabled = false;
            this.button_masterPort.Location = new System.Drawing.Point(588, 313);
            this.button_masterPort.Name = "button_masterPort";
            this.button_masterPort.Size = new System.Drawing.Size(100, 77);
            this.button_masterPort.TabIndex = 7;
            this.button_masterPort.Text = "connect to other servers";
            this.button_masterPort.UseVisualStyleBackColor = true;
            this.button_masterPort.Click += new System.EventHandler(this.button_masterPort_Click);
            // 
            // textBox_server2Port
            // 
            this.textBox_server2Port.Enabled = false;
            this.textBox_server2Port.Location = new System.Drawing.Point(587, 247);
            this.textBox_server2Port.Name = "textBox_server2Port";
            this.textBox_server2Port.Size = new System.Drawing.Size(99, 27);
            this.textBox_server2Port.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(587, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 20);
            this.label1.TabIndex = 9;
            this.label1.Text = "Master Port";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(587, 171);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 20);
            this.label2.TabIndex = 10;
            this.label2.Text = "Server 1 Port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(587, 224);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 20);
            this.label3.TabIndex = 11;
            this.label3.Text = "Server 2 Port";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(714, 535);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_server2Port);
            this.Controls.Add(this.button_masterPort);
            this.Controls.Add(this.textBox_masterPort);
            this.Controls.Add(this.button_choose_db);
            this.Controls.Add(this.button_assignID);
            this.Controls.Add(this.textbox_ID);
            this.Controls.Add(this.textBox_server1Port);
            this.Controls.Add(this.button_listen);
            this.Controls.Add(this.logs);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Server";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox logs;
        private System.Windows.Forms.Button button_listen;
        private System.Windows.Forms.TextBox textBox_server1Port;
        private System.Windows.Forms.TextBox textbox_ID;
        private System.Windows.Forms.Button button_assignID;
        private System.Windows.Forms.Button button_choose_db;
        private System.Windows.Forms.TextBox textBox_masterPort;
        private System.Windows.Forms.Button button_masterPort;
        private System.Windows.Forms.TextBox textBox_server2Port;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}

