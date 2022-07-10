
namespace Client
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
            this.textBox_IP = new System.Windows.Forms.TextBox();
            this.textBox_port = new System.Windows.Forms.TextBox();
            this.label_IP = new System.Windows.Forms.Label();
            this.label_port = new System.Windows.Forms.Label();
            this.button_connect = new System.Windows.Forms.Button();
            this.textBox_username = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button_uploadFile = new System.Windows.Forms.Button();
            this.button_disconnect = new System.Windows.Forms.Button();
            this.button_choose_db = new System.Windows.Forms.Button();
            this.button_request_download = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textbox_file_name = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // logs
            // 
            this.logs.Location = new System.Drawing.Point(12, 12);
            this.logs.Margin = new System.Windows.Forms.Padding(2);
            this.logs.Name = "logs";
            this.logs.ReadOnly = true;
            this.logs.Size = new System.Drawing.Size(692, 522);
            this.logs.TabIndex = 1;
            this.logs.Text = "";
            // 
            // textBox_IP
            // 
            this.textBox_IP.Enabled = false;
            this.textBox_IP.Location = new System.Drawing.Point(731, 139);
            this.textBox_IP.Margin = new System.Windows.Forms.Padding(2);
            this.textBox_IP.Name = "textBox_IP";
            this.textBox_IP.Size = new System.Drawing.Size(150, 31);
            this.textBox_IP.TabIndex = 3;
            // 
            // textBox_port
            // 
            this.textBox_port.Enabled = false;
            this.textBox_port.Location = new System.Drawing.Point(731, 218);
            this.textBox_port.Margin = new System.Windows.Forms.Padding(2);
            this.textBox_port.Name = "textBox_port";
            this.textBox_port.Size = new System.Drawing.Size(150, 31);
            this.textBox_port.TabIndex = 4;
            // 
            // label_IP
            // 
            this.label_IP.AutoSize = true;
            this.label_IP.Location = new System.Drawing.Point(731, 102);
            this.label_IP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_IP.Name = "label_IP";
            this.label_IP.Size = new System.Drawing.Size(97, 25);
            this.label_IP.TabIndex = 15;
            this.label_IP.Text = "IP Address";
            // 
            // label_port
            // 
            this.label_port.AutoSize = true;
            this.label_port.Location = new System.Drawing.Point(731, 190);
            this.label_port.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_port.Name = "label_port";
            this.label_port.Size = new System.Drawing.Size(44, 25);
            this.label_port.TabIndex = 16;
            this.label_port.Text = "Port";
            // 
            // button_connect
            // 
            this.button_connect.Enabled = false;
            this.button_connect.Location = new System.Drawing.Point(731, 272);
            this.button_connect.Margin = new System.Windows.Forms.Padding(2);
            this.button_connect.Name = "button_connect";
            this.button_connect.Size = new System.Drawing.Size(112, 34);
            this.button_connect.TabIndex = 5;
            this.button_connect.Text = "Connect";
            this.button_connect.UseVisualStyleBackColor = true;
            this.button_connect.Click += new System.EventHandler(this.button_connect_Click);
            // 
            // textBox_username
            // 
            this.textBox_username.Enabled = false;
            this.textBox_username.Location = new System.Drawing.Point(731, 54);
            this.textBox_username.Margin = new System.Windows.Forms.Padding(2);
            this.textBox_username.Name = "textBox_username";
            this.textBox_username.Size = new System.Drawing.Size(150, 31);
            this.textBox_username.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(731, 26);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 25);
            this.label1.TabIndex = 9;
            this.label1.Text = "Username";
            // 
            // button_uploadFile
            // 
            this.button_uploadFile.Enabled = false;
            this.button_uploadFile.Location = new System.Drawing.Point(731, 499);
            this.button_uploadFile.Margin = new System.Windows.Forms.Padding(2);
            this.button_uploadFile.Name = "button_uploadFile";
            this.button_uploadFile.Size = new System.Drawing.Size(112, 34);
            this.button_uploadFile.TabIndex = 7;
            this.button_uploadFile.Text = "Upload File";
            this.button_uploadFile.UseVisualStyleBackColor = true;
            this.button_uploadFile.Click += new System.EventHandler(this.uploadFile_Click);
            // 
            // button_disconnect
            // 
            this.button_disconnect.Enabled = false;
            this.button_disconnect.Location = new System.Drawing.Point(731, 324);
            this.button_disconnect.Margin = new System.Windows.Forms.Padding(2);
            this.button_disconnect.Name = "button_disconnect";
            this.button_disconnect.Size = new System.Drawing.Size(112, 34);
            this.button_disconnect.TabIndex = 6;
            this.button_disconnect.Text = "Disconnect";
            this.button_disconnect.UseVisualStyleBackColor = true;
            this.button_disconnect.Click += new System.EventHandler(this.button_disconnect_Click);
            // 
            // button_choose_db
            // 
            this.button_choose_db.Location = new System.Drawing.Point(12, 563);
            this.button_choose_db.Margin = new System.Windows.Forms.Padding(2);
            this.button_choose_db.Name = "button_choose_db";
            this.button_choose_db.Size = new System.Drawing.Size(120, 56);
            this.button_choose_db.TabIndex = 17;
            this.button_choose_db.Text = "Choose DB";
            this.button_choose_db.UseVisualStyleBackColor = true;
            this.button_choose_db.Click += new System.EventHandler(this.choose_db_Click);
            // 
            // button_request_download
            // 
            this.button_request_download.Enabled = false;
            this.button_request_download.Location = new System.Drawing.Point(523, 563);
            this.button_request_download.Margin = new System.Windows.Forms.Padding(2);
            this.button_request_download.Name = "button_request_download";
            this.button_request_download.Size = new System.Drawing.Size(190, 56);
            this.button_request_download.TabIndex = 18;
            this.button_request_download.Text = "Request Download";
            this.button_request_download.UseVisualStyleBackColor = true;
            this.button_request_download.Click += new System.EventHandler(this.request_download_file);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(335, 543);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 25);
            this.label2.TabIndex = 19;
            this.label2.Text = "File Name";
            // 
            // textbox_file_name
            // 
            this.textbox_file_name.Enabled = false;
            this.textbox_file_name.Location = new System.Drawing.Point(334, 576);
            this.textbox_file_name.Margin = new System.Windows.Forms.Padding(2);
            this.textbox_file_name.Name = "textbox_file_name";
            this.textbox_file_name.Size = new System.Drawing.Size(150, 31);
            this.textbox_file_name.TabIndex = 20;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(892, 669);
            this.Controls.Add(this.textbox_file_name);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button_request_download);
            this.Controls.Add(this.button_choose_db);
            this.Controls.Add(this.button_disconnect);
            this.Controls.Add(this.button_uploadFile);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_username);
            this.Controls.Add(this.button_connect);
            this.Controls.Add(this.label_port);
            this.Controls.Add(this.label_IP);
            this.Controls.Add(this.textBox_port);
            this.Controls.Add(this.textBox_IP);
            this.Controls.Add(this.logs);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Client";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox logs;
        private System.Windows.Forms.TextBox textBox_IP;
        private System.Windows.Forms.TextBox textBox_port;
        private System.Windows.Forms.Label label_IP;
        private System.Windows.Forms.Label label_port;
        private System.Windows.Forms.Button button_connect;
        private System.Windows.Forms.TextBox textBox_username;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button_uploadFile;
        private System.Windows.Forms.Button button_disconnect;
        private System.Windows.Forms.Button button_choose_db;
        private System.Windows.Forms.Button button_request_download;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textbox_file_name;
    }
}

