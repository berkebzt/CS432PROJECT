using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace Client
{
    public partial class Form1 : Form
    {
        // The headers explanation
        // -----------------------
        //      Server --> Server
        //      0 -> 
        //      1 -> Request session key
        //      2 -> Receive session key + signature
        //      3 -> Receive server replicated file
        //      4 -> Receive server replication verification message
        //      5 -> 
        //      6 -> 
        //      7 -> 
        // -----------------------
        //      Client --> Server
        //      0 -> Sending a file to Server
        //      1 -> Request file download
        //      2 -> 
        //      3 ->
        //      4 -> 
        //      5 -> 
        //      6 ->
        //      7 ->
        // -----------------------
        //      Server --> Client
        //      0 -> Sending file to Client (Download).
        //      1 -> Send server id
        //      2 -> Sending verification message, (and signature if file is successfully saved) (for upload)
        //      3 -> Sending verification message, (and signature if file is successfully saved) (for download)
        //      4 -> 
        //      5 -> 
        //      6 -> 
        //      7 -> 
        // -----------------------

        string DB_Path = "";

        bool terminating = false;
        bool connected = false;
        Socket clientSocket;
        string downloadpath = "";
        List<String> list = new List<String>();
        List<String> publiclist = new List<String>();
        string username = "";
        string fileName = "";
        string serverPubKey = "";
        string filePath = "";
        AutoResetEvent waitHandle = new AutoResetEvent(false);

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            // Connecting a Server



            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_IP.Text;
            int portNum;
            string userName = textBox_username.Text;
            username = userName;

            if (Int32.TryParse(textBox_port.Text, out portNum))
            {
                try
                {
                    if (userName != "" && userName.Length <= 64 && userName != "0" && userName != "1" && userName != "2")
                    {
                        clientSocket.Connect(IP, portNum);
                        Byte[] buffer = new Byte[64];
                        buffer = Encoding.Default.GetBytes(userName);
                        clientSocket.Send(buffer);
                        // Receive the operation information
                        Byte[] receivedInfoHeader = new Byte[1];
                        clientSocket.Receive(receivedInfoHeader);

                        if (receivedInfoHeader[0] == 0)
                        {
                            Byte[] buffer2 = new Byte[64];
                            clientSocket.Receive(buffer2);
                            string incomingMessage = Encoding.Default.GetString(buffer2);
                            incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                            if (incomingMessage == "error_username")
                            {
                                logs.AppendText("This username is already taken! Cannot connect to the server.\n");
                                clientSocket.Close();
                            }
                            else
                            {
                                button_connect.Enabled = false;
                                textBox_IP.Enabled = false;
                                textBox_port.Enabled = false;
                                textBox_username.Enabled = false;

                                connected = true;
                                logs.AppendText("Connected to the server!\n\n");
                                button_uploadFile.Enabled = true;
                                button_request_download.Enabled = true;
                                button_disconnect.Enabled = true;
                                textbox_file_name.Enabled = true;
                                Thread receiveThread = new Thread(Receive);
                                receiveThread.Start();
                            }
                        }

                    }
                    else
                    {
                        if (userName == "")
                        {
                            logs.AppendText("Username cannot be empty.\n");
                        }
                        else if (userName == "0" || userName == "1" || userName == "2")    //0, 1, and 2 are reserved for servers
                        {
                            logs.AppendText("Username cannot be 0, 1 or 2.\n");
                        }
                        else
                        {
                            logs.AppendText("Username cannot be larger than 64 characters.\n");
                        }
                    }


                }
                catch
                {
                    logs.AppendText("Cannot connect to the server...\n");
                }
            }
            else
            {
                logs.AppendText("Check the port number.\n");
            }
        }

        private void ActivateComponentsAfterDBAssignment()  // enable button and textboxes after DB assignment
        {
            button_connect.Enabled = true;
            textBox_username.Enabled = true;
            textBox_IP.Enabled = true;
            textBox_port.Enabled = true;
        }

        private void choose_db_Click(object sender, EventArgs e)
        {
            // Choose the Database folder
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string folderPath = fbd.SelectedPath;
                    ActivateComponentsAfterDBAssignment();
                    button_choose_db.Enabled = false;

                    //DB_Path = folderPath;
                    DB_Path = folderPath.Replace(@"\", "/"); // DO NOT CHANGE PATH CORRECTION

                    logs.AppendText("You choosed: " + DB_Path + " as db path.\n");

                }

            }


        }

        private void request_download_file(object sender, EventArgs e)
        {
            string file_to_be_requested = textbox_file_name.Text;

            // Send the 1 byte to inform the server that the client is sending a download request
            Byte[] infoHeader = new Byte[1];
            infoHeader[0] = 1;
            clientSocket.Send(infoHeader);

            serverPubKey = "";
            while (serverPubKey == "")
            {
                waitHandle.WaitOne();   //wait for the server id to be received and server public key to be set
            }

            // Send Len Header to inform the length of file name
            Byte[] lenHeader = new byte[4];
            System.Buffer.BlockCopy(BitConverter.GetBytes(file_to_be_requested.Length), 0, lenHeader, 0, 4);

            // Convert fileName to byte array            
            Byte[] fileName_byte_array = Encoding.ASCII.GetBytes(file_to_be_requested);


            Thread.Sleep(10);
            clientSocket.Send(lenHeader);
            Thread.Sleep(10);

            clientSocket.Send(fileName_byte_array);
        }

        private void Receive()
        {

            // Receiving a message from the Server
            while (connected)
            {
                try
                {
                    // Receive the operation information
                    Byte[] receivedInfoHeader = new Byte[1];
                    clientSocket.Receive(receivedInfoHeader);

                    if (receivedInfoHeader[0] == 0)
                    {
                        // Receive the incoming File's name and size
                        Byte[] fileProperties = new byte[256]; // First 128 Bytes are for Name, Last 128 for Size
                        clientSocket.Receive(fileProperties); // Receive the Buffer

                        // Take the file name from the buffer
                        string fileName = Encoding.Default.GetString(fileProperties.Take(128).ToArray());
                        fileName = fileName.Substring(0, fileName.IndexOf("\0"));

                        // Take the file size from buffer
                        int fileSize = Int32.Parse(Encoding.Default.GetString(fileProperties.Skip(128).Take(128).ToArray()));
                        string filename_pre = fileName;
                        // Get the file data
                        Byte[] buffer2 = new Byte[fileSize]; // The buffer size is allocated by the file size
                        clientSocket.Receive(buffer2);
                        int count = 1;
                        if (File.Exists(downloadpath + "/" + fileName))
                        {
                            fileName = filename_pre.Split('.')[filename_pre.Split('.').Length - 2] + "(" + count + ")." + filename_pre.Split('.').Last();
                            while (File.Exists(downloadpath + "/" + fileName))
                            {
                                count += 1;
                                fileName = filename_pre.Split('.')[filename_pre.Split('.').Length - 2] + "(" + count + ")." + filename_pre.Split('.').Last();
                            }

                        }
                        BinaryWriter bWrite = new BinaryWriter(File.Open // using system.I/O
                                        (downloadpath + "/" + fileName, FileMode.Append));
                        bWrite.Write(buffer2);
                        bWrite.Close();
                        buffer2 = null; // In order to prevent creating files over and over again

                        // Print the logs and send the confirmation message to the Client
                        logs.AppendText("Downloaded file: \"" + fileName + "\" from: the server." + "\n\n"); // Log message
                    }

                    if (receivedInfoHeader[0] == 1) //receive server id
                    {
                        serverPubKey = "";          //set public key with respect to received server id
                        logs.AppendText("server header = 1 \n\n");
                        Byte[] serverId = new Byte[4];
                        clientSocket.Receive(serverId);
                        string incomingServerId = Encoding.Default.GetString(serverId);
                        incomingServerId = incomingServerId.Substring(0, incomingServerId.IndexOf("\0"));
                        logs.AppendText("Server ID: \"" + incomingServerId + "\" \n\n");

                        if (incomingServerId == "0")
                        {
                            serverPubKey = File.ReadAllText($"../../../keys/MasterServer_pub.txt").Trim();
                        }
                        else                                //server id is 1 or 2
                        {
                            serverPubKey = File.ReadAllText($"../../../keys/Server{incomingServerId}_pub.txt").Trim();
                        }

                        logs.AppendText("serverPubKey: \"" + serverPubKey + "\" \n\n");

                        waitHandle.Set();         //when sending file to the server, server first sends its id number;
                                                  //sending file operation should be halted until id is received and public key of server read

                    }

                    if (receivedInfoHeader[0] == 2)     //receive verification message and the signature, after sending the file
                    {
                        Byte[] verification = new Byte[384 * 3];        //we have 3 signed messages, first one for server's message
                                                                        //second for filename, third for the file itself
                        clientSocket.Receive(verification);
                        Byte[] msg_verify = verification[..384];        //first 384 bytes signed message
                                                                        //if a failure message is returned we don't have the signature for filename or the file

                        if (verifyWithRSA("File received and saved successfully.", 3072, serverPubKey, msg_verify))   //positive server message
                        {
                            Byte[] fname_verify = verification[384..(384 * 2)];
                            Byte[] file_verify = verification[(384 * 2)..];
                            
                            //try to verify both filename and the file itself using server's public key
                            if (verifyWithRSA(fileName, 3072, serverPubKey, fname_verify) && verifyWithRSA(Convert.ToBase64String(File.ReadAllBytes(filePath)), 3072, serverPubKey, file_verify))
                            {
                                logs.AppendText($"Verification Succesful for file upload of \"{fileName}\"\n\n");
                            }
                            else
                            {
                                logs.AppendText($"Verification Unsuccesful for file upload of \"{fileName}\"\n\n");
                            }
                        }
                        else if (verifyWithRSA("File not received successfully.", 3072, serverPubKey, msg_verify))  //negative server message
                        {
                            logs.AppendText($"File upload of \"{fileName}\" was unsuccessful try again later.\n\n");
                        }
                        else    //something else went wrong
                        {
                            logs.AppendText("Something went terribly wrong try again later.\n\n");
                        }
                    }

                    if (receivedInfoHeader[0] == 3) // receive verification message and the signature, after sending the requested file name
                    {
                        Byte[] verification_msg  = new byte[384];        // signed message

                        clientSocket.Receive(verification_msg);



                        if (verifyWithRSA("File doesn't exist.", 3072, serverPubKey, verification_msg)) 
                        {
                            logs.AppendText($"File cannot be found in server database:  \"{textbox_file_name.Text}\"\n\n");
                        }
                        else if(verifyWithRSA("File exists.", 3072, serverPubKey, verification_msg))
                        {
                            Byte[] file_sign = new Byte[384];
                            clientSocket.Receive(file_sign); // Receive the Buffer


                            // Receive the incoming File's name and size
                            Byte[] fileProperties = new byte[256]; // First 128 Bytes are for Name, Last 128 for Size
                            clientSocket.Receive(fileProperties); // Receive the Buffer


                            // Take the file name from the buffer
                            string fileName = Encoding.Default.GetString(fileProperties.Take(128).ToArray());
                            fileName = fileName.Substring(0, fileName.IndexOf("\0"));


                            // Take the file size from buffer
                            int fileSize = Int32.Parse(Encoding.Default.GetString(fileProperties.Skip(128).Take(128).ToArray()));
                            string filename_pre = fileName;
                            // Get the file data
                            Byte[] buffer2 = new Byte[fileSize]; // The buffer size is allocated by the file size
                            clientSocket.Receive(buffer2);



                            if (verifyWithRSA(BitConverter.ToString(buffer2), 3072, serverPubKey, file_sign))
                            {

                                int count = 1;
                                if (File.Exists(DB_Path + "/" + fileName))
                                {
                                    fileName = filename_pre.Split('.')[filename_pre.Split('.').Length - 2] + "(" + count + ")." + filename_pre.Split('.').Last();
                                    while (File.Exists(DB_Path + "/" + fileName))
                                    {
                                        count += 1;
                                        fileName = filename_pre.Split('.')[filename_pre.Split('.').Length - 2] + "(" + count + ")." + filename_pre.Split('.').Last();
                                    }

                                }
                                BinaryWriter bWrite = new BinaryWriter(File.Open // using system.I/O
                                                (DB_Path + "/" + fileName, FileMode.Append));
                                bWrite.Write(buffer2);
                                bWrite.Close();
                                buffer2 = null; // In order to prevent creating files over and over again

                                // Print the logs and send the confirmation message to the Client
                                logs.AppendText("Downloaded file: \"" + fileName + "\" from: the server." + "\n\n"); // Log message
                            }
                            else
                            {
                                logs.AppendText("File received but not genuine due to signature problem \n");
                            }

                        }
                        else
                        {
                            logs.AppendText("Huge problem occured, unexpected. \n\n");
                        }

                    }

                    if (receivedInfoHeader[0] == 4) { }

                    if (receivedInfoHeader[0] == 5) { }

                    if (receivedInfoHeader[0] == 6) { }

                    if (receivedInfoHeader[0] == 7) { }
                }
                catch
                {
                    if (!terminating)
                    {
                        logs.AppendText("The server has disconnected.\n");
                        button_connect.Enabled = true;
                        button_disconnect.Enabled = false;
                        textBox_port.Enabled = true;
                        textBox_IP.Enabled = true;
                        button_uploadFile.Enabled = false;
                        textBox_username.Enabled = true;

                        textbox_file_name.Enabled = false;

                        button_request_download.Enabled = false;

                    }

                    clientSocket.Close();
                    connected = false;

                }
            }

        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }
        private void uploadFile_Click(object sender, EventArgs e)
        {
            // Uploading a file to the Server
            try
            {
                // Select the file
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"; // Taken directly from docs


                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    // If the file is selected
                    string path = dialog.FileName;
                    logs.AppendText("File path: " + path + "\n\n");

                    serverPubKey = "";  //reset the server public key

                    // Send the 1 byte to inform the server that the client is sending a file
                    Byte[] infoHeader = new Byte[1];
                    infoHeader[0] = 0;
                    clientSocket.Send(infoHeader);
                    
                    while(serverPubKey == "") 
                    {
                        waitHandle.WaitOne();   //wait for the server id to be received and server public key to be set
                    }
                    try
                    {
                        Aes aes = Aes.Create();     //generate random AES key and IV
                        aes.KeySize = 128;
                        aes.BlockSize = 128;
                        aes.GenerateKey();
                        aes.GenerateIV();

                        fileName = dialog.SafeFileName;     //set globally available filename and filepath
                        filePath = dialog.FileName;

                        byte[] AES_key = aes.Key;           //Randomly generated AES key
                        logs.AppendText("randomly generated aes: \"" + generateHexStringFromByteArray(AES_key) + "\" \n\n");
                        logs.AppendText("aes key length: \"" + AES_key.Length.ToString() + "\" \n\n");

                        byte[] IV = aes.IV;                 //Randomly generated IV
                        logs.AppendText("randomly generated IV: \"" + generateHexStringFromByteArray(IV) + "\" \n\n");
                        logs.AppendText("IV length: \"" + IV.Length.ToString() + "\" \n\n");
                        
                        byte[] encryptedAES = encryptWithRSA(AES_key, 3072, serverPubKey);                          //encrypt with rsa using server's public key
                        logs.AppendText("encryptedAES: \"" + generateHexStringFromByteArray(encryptedAES) + "\" \n\n");

                        byte[] encryptedIV = encryptWithRSA(IV, 3072, serverPubKey);                                //encrypt with rsa using server's public key
                        logs.AppendText("encryptedIV: \"" + generateHexStringFromByteArray(encryptedIV) + "\" \n\n");

                        byte[] file = File.ReadAllBytes(dialog.FileName.ToString());                                           //file read as byte array
                        byte[] encryptedFile = encryptWithAES128(Convert.ToBase64String(file), AES_key, IV);    //encrypt our file using AES key and IV
                        logs.AppendText("Plain file length: \"" + file.Length + "\" \n\n");
                        logs.AppendText("Plain file: \"" + Encoding.Default.GetString(file) + "\" \n\n");
                        logs.AppendText("encrypted file length: " + encryptedFile.Length + "\n\n");
                        logs.AppendText("encrypted file: " + generateHexStringFromByteArray(encryptedFile) + "\n\n");

                        byte[] encyptedFileName = encryptWithAES128(dialog.SafeFileName.ToString(), AES_key, IV);              //encrypt our filename using AES key and IV
                        logs.AppendText("encrypted file name: " + generateHexStringFromByteArray(encyptedFileName) + "\n\n");

                        
                        int len = 16;    //16 for 4 integers
                        Byte[] datalength = new Byte[len];
                        System.Buffer.BlockCopy(BitConverter.GetBytes(encyptedFileName.Length), 0, datalength, 0, 4);   //since we will send integers, 4 byte per element is enough
                        System.Buffer.BlockCopy(BitConverter.GetBytes(encryptedFile.Length), 0, datalength, 4, 4);
                        System.Buffer.BlockCopy(BitConverter.GetBytes(encryptedAES.Length), 0, datalength, 8, 4);
                        System.Buffer.BlockCopy(BitConverter.GetBytes(encryptedIV.Length), 0, datalength, 12, 4);
                        clientSocket.Send(datalength);  //we first send length of our variables to inform server about how big of a buffer needed to receive our message

                        int fnameLen = encyptedFileName.Length;
                        int fileLen = encryptedFile.Length;
                        int aesLen = encryptedAES.Length;
                        int ivLen = encryptedIV.Length;

                        int len2 = fnameLen + fileLen + aesLen + ivLen;
                        Byte[] data = new Byte[len2];
                        System.Buffer.BlockCopy(encyptedFileName, 0, data, 0, fnameLen);
                        System.Buffer.BlockCopy(encryptedFile, 0, data, fnameLen, fileLen);
                        System.Buffer.BlockCopy(encryptedAES, 0, data, fnameLen + fileLen, aesLen);
                        System.Buffer.BlockCopy(encryptedIV, 0, data, fnameLen + fileLen + aesLen, ivLen);
                        clientSocket.Send(data);        // send the actual data
                                                        // data format: (encrypted filename | encrypted file | RSA encrypted AES | RSA encrypted IV)

                                                        //receiving the acknowledgement and verification messages are done inside receive function

                    }
                    catch
                    {
                        logs.AppendText("error during encryption or sending");
                    }

                    /*logs.AppendText("Sent file: \"" + dialog.SafeFileName + "\" \n\n");

                    int fileProperties = 256; // FileName + The Data's Length
                    int fileNameLength = 128; // FileName
                    string fileLength = File.ReadAllBytes(dialog.FileName).Length.ToString(); // The Data's Length is turned into string 
                                                                                                // to put into a Byte Array with the FileName

                    Byte[] filePropertiesBuffer = new Byte[fileProperties]; // Allocate space for FileName and The Data's Length
                    // Copy the FileName and The Data's Length into the filePropertiesBuffer
                    Array.Copy(Encoding.Default.GetBytes(dialog.SafeFileName), filePropertiesBuffer, dialog.SafeFileName.Length);
                    Array.Copy(Encoding.ASCII.GetBytes(fileLength), 0, filePropertiesBuffer, fileNameLength, fileLength.Length);

                    // Send the filePropertiesBuffer to the Server
                    clientSocket.Send(filePropertiesBuffer);

                    // Copy the data into generalBuffer
                    Byte[] generalBuffer = new Byte[File.ReadAllBytes(dialog.FileName).Length];
                    generalBuffer = File.ReadAllBytes(dialog.FileName);

                    // Send the data to the surver via generalBuffer
                    clientSocket.Send(generalBuffer);*/
                }
                else
                {
                    Console.WriteLine("could not read\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private void button_disconnect_Click(object sender, EventArgs e)
        {
            string user_Name = textBox_username.Text;
            logs.AppendText(user_Name + "  has disconnected.\n");
            clientSocket.Close();
            connected = false;
            terminating = true;

            button_disconnect.Enabled = false;
            button_connect.Enabled = true;

            textBox_port.Enabled = true;
            textBox_port.Text = String.Empty;

            textBox_IP.Enabled = true;
            textBox_IP.Text = String.Empty;

            textBox_username.Enabled = true;
            textBox_username.Text = String.Empty;

            textbox_file_name.Enabled = false;
            textbox_file_name.Text = String.Empty;

            button_request_download.Enabled = false;
            button_uploadFile.Enabled = false;
        }

        public static string GenerateRandomString(int length, string allowableChars = null)
        {
            if (string.IsNullOrEmpty(allowableChars))
                allowableChars = @"ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            // Generate random data
            var rnd = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(rnd);

            // Generate the output string
            var allowable = allowableChars.ToCharArray();
            var l = allowable.Length;
            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = allowable[rnd[i] % l];

            return new string(chars);
        }

        public static byte[] GenerateRandomData(int length)
        {
            var rnd = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(rnd);
            return rnd;
        }

        static byte[] encryptWithAES128(string input, byte[] key, byte[] IV)
        {
            //byte[] byteInput = Encoding.Default.GetBytes(input);
            //// create AES object from System.Security.Cryptography
            //RijndaelManaged aesObject = new RijndaelManaged();
            //// set the key
            //aesObject.Key = key;
            //// set the IV
            //aesObject.IV = IV;
            //// create an encryptor with the settings provided
            //ICryptoTransform encryptor = aesObject.CreateEncryptor();
            //byte[] result = null;

            //try
            //{
            //    result = encryptor.TransformFinalBlock(byteInput, 0, byteInput.Length);
            //}
            //catch (Exception e) // if encryption fails
            //{
            //    Console.WriteLine(e.Message); // display the cause
            //}

            //return result;

            byte[] encrypted;
            // Create a new AesManaged.    
            using (AesManaged aes = new AesManaged())
            {
                // Create encryptor    
                ICryptoTransform encryptor = aes.CreateEncryptor(key, IV);
                // Create MemoryStream    
                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt    
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream    
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(input);
                        encrypted = ms.ToArray();
                    }
                }
            }
            // Return encrypted data    
            return encrypted;
        }

        // RSA encryption with varying bit length
        static byte[] encryptWithRSA(byte[] byteInput, int algoLength, string xmlStringKey)
        {
            // create RSA object from System.Security.Cryptography
            RSACryptoServiceProvider rsaObject = new RSACryptoServiceProvider(algoLength);
            // set RSA object with xml string
            rsaObject.FromXmlString(xmlStringKey);
            byte[] result = null;

            try
            {
                //true flag is set to perform direct RSA encryption using OAEP padding
                result = rsaObject.Encrypt(byteInput, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }

        static bool verifyWithRSA(string input, int algoLength, string xmlString, byte[] signature)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create RSA object from System.Security.Cryptography
            RSACryptoServiceProvider rsaObject = new RSACryptoServiceProvider(algoLength);
            // set RSA object with xml string
            rsaObject.FromXmlString(xmlString);
            bool result = false;

            try
            {
                result = rsaObject.VerifyData(byteInput, "SHA256", signature);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }

        static string generateHexStringFromByteArray(byte[] input)
        {
            string hexString = BitConverter.ToString(input);
            return hexString.Replace("-", "");
        }

        static string decryptWithAES128(byte[] cipherText, byte[] key, byte[] IV)
        {
            string plaintext = null;
            // Create AesManaged    
            using (AesManaged aes = new AesManaged())
            {
                // Create a decryptor    
                ICryptoTransform decryptor = aes.CreateDecryptor(key, IV);
                // Create the streams used for decryption.    
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    // Create crypto stream    
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        // Read crypto stream    
                        using (StreamReader reader = new StreamReader(cs))
                            plaintext = reader.ReadToEnd();
                    }
                }
            }
            return plaintext;
        }

    }
}
