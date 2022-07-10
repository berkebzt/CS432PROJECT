using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace Server
{
    public partial class Form1 : Form
    {

        // IMPORTANT: Disable every component in GUI by setting Enabled to false, set them enable in ActivateComponentsAfterDBAssignment
        // Flow is as below:
        //      Choose the ID, 0 is for master and 1-2 is for the normal servers (button_assignID_Click)
        //      
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
        //      0 -> Sending a file to Server(upload)
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
        //      3 -> //Sending verification message, (and signature if file is successfully saved) (for download)
        //      4 -> Send Requested file
        //      5 -> Send signed error message
        //      6 -> 
        //      7 -> 
        // -----------------------


        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Socket serverSocket1;
        Socket serverSocket2;                       //sockets are global for keeping the connection alive throughout the execution of the program 

        Byte[] sessionKey = new Byte[48];           //new session key is required whenever re-connections occur
        int amountOfSessionKeysBeenSent = 0;        //only needed for the master server, keeps track of how many session keys are sent during this connection
        bool sessionKeyIsSet = false;               //true whenever; for master whenever session key is sent to the other 2 servers7 for the other servers whenever the session key is received

        object sessionKeyLock = new object();
        object writeLock = new object();
        object logsLock = new object();

        string DB_Path = "";

        // Server ID
        // 0 is master
        // 1 and 2 is usual id's
        int serverID;

        // Storing the Clients and Usernames
        List<Socket> clientSockets = new List<Socket>();
        List<String> clientUsernames = new List<String>();
        Socket[] serverSendingSockets = new Socket[3];          //sockets for sending and receiving are kept seperate
        Socket[] serverReceivingSockets = new Socket[3];        //server IDs are used for indexing of the sockets
        List<int> connectedServerIDs = new List<int>();         //for keeping track of the servers that are currently connected to this server

        Queue<String> filesToBeReplicated = new Queue<String>();    //file replication queue; files are iteratively sent whenever servers re-connect

        bool terminating = false;
        bool listening = false;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void ActivateComponentsAfterDBAssignment()  //start listening after DB assignment
        {
            button_listen.Enabled = true;
            if (serverID == 0) 
            {
                textBox_masterPort.Enabled = true;
            }
            else if(serverID == 1)
            {
                textBox_server1Port.Enabled = true;
            }
            else
            {
                textBox_server2Port.Enabled = true;
            }
        }

        private void DeactivateOtherServerPortTextBoxes()   //text boxes needed for connection to other servers become inactive
        {
            if (serverID == 0)
            {
                textBox_server1Port.Enabled = false;
                textBox_server2Port.Enabled = false;
            }
            else if (serverID == 1)
            {
                textBox_masterPort.Enabled = false;
                textBox_server2Port.Enabled = false;
            }
            else
            {
                textBox_masterPort.Enabled = false;
                textBox_server1Port.Enabled = false;
            }

            button_masterPort.Enabled = false;
        }

        private void ActivateOtherServerPortTextBoxes()     //text boxes needed for connection to other servers become active
        {
            if (serverID == 0)
            {
                textBox_server1Port.Enabled = true;
                textBox_server2Port.Enabled = true;
            }
            else if (serverID == 1)
            {
                textBox_masterPort.Enabled = true;
                textBox_server2Port.Enabled = true;
            }
            else
            {
                textBox_masterPort.Enabled = true;
                textBox_server1Port.Enabled = true;
            }

            button_masterPort.Enabled = true;
        }

        private TextBox getThisServerPortTextBox()      //the text box needed for taking port number to listen
        {
            TextBox thisServer;

            if(serverID == 0)
            {
                thisServer = textBox_masterPort;
            }
            else if(serverID == 1)
            {
                thisServer = textBox_server1Port;
            }
            else
            {
                thisServer = textBox_server2Port;
            }

            return thisServer;
        }

        private void button_assignID_Click(object sender, EventArgs e)
        {
            if (Int32.TryParse(textbox_ID.Text, out serverID) && serverID >= 0 && serverID <= 2)
            {
                logs.AppendText("Server ID set as " + serverID + ".\n");
                if (serverID == 0)
                {
                    logs.AppendText("This server is Master server.\n");
                }

                button_choose_db.Enabled = true;
                textbox_ID.Enabled = false;
                button_assignID.Enabled = false;

            }
            else
            {
                logs.AppendText("Please enter a valid server ID (0 for Master, 1-2 for normal servers).\n");

            }
        }

        private void button_listen_Click(object sender, EventArgs e) //first listen then establish server to server connections
        {
            int serverPort;

            TextBox textBox = getThisServerPortTextBox();  //changes with respect to ID number of the server, 0 for master, 1 and 2 for server 1 and 2

            if (Int32.TryParse(textBox.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(200);
                listening = true;

                textBox.Enabled = false;
                button_listen.Enabled = false;

                ActivateOtherServerPortTextBoxes();

                Thread acceptThread = new Thread(Accept);       //start accepting servers and clients
                acceptThread.Start();
                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please enter a valid port number.\n");
            }

        }

        private void button_masterPort_Click(object sender, EventArgs e) //connect to other servers and establish sockets for sending information to other servers
        {
            int serverPort1, serverPort2;

            if(serverID == 0)   //the textboxes where we read the other server's port numbers from, changes with respect to which server we are
            {
                if (Int32.TryParse(textBox_server1Port.Text, out serverPort1) && Int32.TryParse(textBox_server2Port.Text, out serverPort2))
                {
                    Thread serverConnectionThread = new Thread(() => serverToServerConnection(serverPort1, 1, serverPort2, 2));
                    serverConnectionThread.Start();     //start server to server connection
                }
                else
                {
                    logs.AppendText("Please enter a valid port number.\n");
                }
            }
            else if(serverID == 1)
            {
                if (Int32.TryParse(textBox_masterPort.Text, out serverPort1) && Int32.TryParse(textBox_server2Port.Text, out serverPort2))
                {
                    Thread serverConnectionThread = new Thread(() => serverToServerConnection(serverPort1, 0, serverPort2, 2));
                    serverConnectionThread.Start();
                }
                else
                {
                    logs.AppendText("Please enter a valid port number.\n");
                }
            }
            else
            {
                if (Int32.TryParse(textBox_masterPort.Text, out serverPort1) && Int32.TryParse(textBox_server1Port.Text, out serverPort2))
                {
                    Thread serverConnectionThread = new Thread(() => serverToServerConnection(serverPort1, 0, serverPort2, 1));
                    serverConnectionThread.Start();
                }
                else
                {
                    logs.AppendText("Please enter a valid port number.\n");
                }
            }
        }

        //first this server tries to connect to each of the other servers, then this server waits until the other 2 servers connect to this server
        private void serverToServerConnection(int serverPort1, int serverID1, int serverPort2, int serverID2)  //server ports with server IDs
        {
            serverSocket1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            bool isConnected1 = false;      //we have 3 phases on server to server connections
            bool isConnected2 = false;      //first and second phases are connecting to the other 2 servers
            bool isConnected3 = false;      //last phase is to wait until other 2 servers connects to us

            DeactivateOtherServerPortTextBoxes();

            while (!isConnected1 || !isConnected2 || !isConnected3)
            {
                if (!isConnected1)
                {
                    try
                    {
                        isConnected1 = true;
                        serverSocket1.Connect("127.0.0.1", serverPort1);            //connect to server with server id = serverID1

                        Byte[] buffer = new Byte[4];
                        buffer = Encoding.Default.GetBytes(serverID.ToString());    //send this server's id
                        serverSocket1.Send(buffer);

                        serverSendingSockets[serverID1] = serverSocket1;           //this socket is received at the other end where it is used as 
                                                                                   //the receiving socket for this server
                    }
                    catch
                    {
                        isConnected1 = false;
                    }
                }
                if (!isConnected2)
                {
                    try
                    {
                        isConnected2 = true;
                        serverSocket2.Connect("127.0.0.1", serverPort2);             //connect to server with server id = serverID2

                        Byte[] buffer2 = new Byte[4];
                        buffer2 = Encoding.Default.GetBytes(serverID.ToString());    //send this server's id
                        serverSocket2.Send(buffer2);

                        serverSendingSockets[serverID2] = serverSocket2;            //this socket is received at the other end where it is used as 
                                                                                    //the receiving socket for this server
                    }
                    catch
                    {
                        isConnected2 = false;
                    }
                }
                if (!isConnected3)
                {
                    isConnected3 = true;
                    if (connectedServerIDs.Count < 2)       //both of the other servers must be connected to this server
                    {
                        isConnected3 = false;
                    }
                    else
                    {
                        if(serverID1 == 0)
                        {
                            //get session keys
                            Byte[] buffer3 = new Byte[1];           //header server to server, for session key request, 1 for requesting session key
                            buffer3[0] = 1;
                            serverSocket1.Send(buffer3);
                        }
                        else if(serverID2 == 0)
                        {
                            //get session keys
                            Byte[] buffer3 = new Byte[1];           //header server to server, for session key request, 1 for requesting session key
                            buffer3[0] = 1;
                            serverSocket1.Send(buffer3);
                        }
                    }
                    
                }
            }

        }

        private void Accept()
        {   // Accepting a Client or Server
            while (listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
                    Byte[] buffer = new Byte[64];
                    newClient.Receive(buffer);
                    string incomingUsername = Encoding.Default.GetString(buffer);
                    incomingUsername = incomingUsername.Substring(0, incomingUsername.IndexOf("\0"));

                    if (incomingUsername == "0" || incomingUsername == "1" || incomingUsername == "2")  //0, 1, and 2 are reserved for servers
                    {
                        if (incomingUsername != serverID.ToString() && !connectedServerIDs.Contains(Int32.Parse(incomingUsername))) //this server has not already been connected
                        {
                            serverReceivingSockets[Int32.Parse(incomingUsername)] = newClient;  //accept this socket as the receiver of the other server's messages
                            connectedServerIDs.Add(Int32.Parse(incomingUsername));              //add the ID of the that we accepted to the connected server list
                            
                            logs.AppendText("Server \"" + incomingUsername + "\" is connected.\n");
                            Thread receiveThread = new Thread(() => ReceiveServer(newClient, incomingUsername));    //start communicating with this server
                            receiveThread.Start();
                        }
                    }
                    else if (clientUsernames.Exists(element => element == incomingUsername))
                    {

                        // Telling the Client, server is sending a message
                        Byte[] infoHeader = new Byte[1];
                        infoHeader[0] = 0;
                        newClient.Send(infoHeader);


                        logs.AppendText("The username \"" + incomingUsername + "\" is already taken! Cannot connect to the server.\n");
                        string errorUsername = "error_username";
                        Byte[] buffer2 = Encoding.Default.GetBytes(errorUsername);



                        newClient.Send(buffer2);
                        newClient.Close();
                    }
                    else
                    {

                        // Telling the Client, server is sending a message
                        Byte[] infoHeader = new Byte[1];
                        infoHeader[0] = 0;
                        newClient.Send(infoHeader);


                        clientSockets.Add(newClient);
                        string noError = "All OK.";
                        Byte[] buffer2 = Encoding.Default.GetBytes(noError);



                        newClient.Send(buffer2);
                        clientUsernames.Add(incomingUsername);                      //client is connected
                        logs.AppendText("\"" + incomingUsername + "\" is connected.\n");
                        Thread receiveThread = new Thread(() => Receive(newClient, incomingUsername));
                        receiveThread.Start();
                    }

                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }
                }
            }
        }

        private void sendQueuedFileReplications()   //send unreplicated files to the other servers
        {
            while (filesToBeReplicated.Count > 0 && sessionKeyIsSet)
            {
                string nextFileName = filesToBeReplicated.Dequeue();    //at each iteration pop 1 filename from the queue

                try
                {
                    byte[] nextFile = File.ReadAllBytes(DB_Path + "/" + nextFileName);  //read the file from the database

                    byte[] fileNameToBeReplicated = encryptWithAES128(nextFileName, sessionKey[..16], sessionKey[16..32]);
                    byte[] fileToBeReplicated = encryptWithAES128(Convert.ToBase64String(nextFile), sessionKey[..16], sessionKey[16..32]);
                    byte[] hmacOfFileName = applyHMACwithSHA256(nextFileName, sessionKey[32..]);
                    byte[] hmacOfFile = applyHMACwithSHA256(Convert.ToBase64String(nextFile), sessionKey[32..]);

                    logs.AppendText("HMAC of file name: " + generateHexStringFromByteArray(hmacOfFileName) + "\n\n");

                    logs.AppendText("Lengths for replication: " + fileNameToBeReplicated.Length.ToString() + " " + fileToBeReplicated.Length.ToString() + " " + hmacOfFileName.Length.ToString() + " " + hmacOfFile.Length.ToString() + "\n\n");

                    int len = 16;    //16 for 4 integers
                    Byte[] datalength = new Byte[len];
                    System.Buffer.BlockCopy(BitConverter.GetBytes(fileNameToBeReplicated.Length), 0, datalength, 0, 4);   //since we will send integers, 4 byte per element is enough
                    System.Buffer.BlockCopy(BitConverter.GetBytes(fileToBeReplicated.Length), 0, datalength, 4, 4);
                    System.Buffer.BlockCopy(BitConverter.GetBytes(hmacOfFileName.Length), 0, datalength, 8, 4);
                    System.Buffer.BlockCopy(BitConverter.GetBytes(hmacOfFile.Length), 0, datalength, 12, 4);

                    int fnameLen = fileNameToBeReplicated.Length;
                    int fileLen = fileToBeReplicated.Length;
                    int hmacFnameLen = hmacOfFileName.Length;
                    int hmacFileLen = hmacOfFile.Length;

                    int len2 = fnameLen + fileLen + hmacFnameLen + hmacFileLen;
                    Byte[] data = new Byte[len2];
                    System.Buffer.BlockCopy(fileNameToBeReplicated, 0, data, 0, fnameLen);
                    System.Buffer.BlockCopy(fileToBeReplicated, 0, data, fnameLen, fileLen);
                    System.Buffer.BlockCopy(hmacOfFileName, 0, data, fnameLen + fileLen, hmacFnameLen);
                    System.Buffer.BlockCopy(hmacOfFile, 0, data, fnameLen + fileLen + hmacFnameLen, hmacFileLen);

                    Byte[] infoHeader = new Byte[1];
                    infoHeader[0] = 3;

                    foreach (Socket s in serverSendingSockets)
                    {
                        if (s != null && connectedServerIDs.Count == 2)
                        {
                            s.Send(infoHeader);     //Send info header; 3 for sending(receiving) file replications
                            Thread.Sleep(10);
                            s.Send(datalength);     //send the data length first
                            Thread.Sleep(10);
                            s.Send(data);           //send the actual data
                        }
                    }
                }
                catch
                {
                    logs.AppendText("Could not replicate the file\n\n");
                }
            }
        }

        private void ReceiveServer(Socket thisServer, string serverUsername)    //receive function special for server-to-server communication
        {
            string serverPrivateKey;
            if (serverID == 0)              //get current server's private key
            {
                serverPrivateKey = File.ReadAllText("../../../keys/MasterServer_pub_prv.txt");
            }
            else
            {
                serverPrivateKey = File.ReadAllText($"../../../keys/Server{serverID}_pub_prv.txt");
            }

            bool connected = true;
            while (connected && !terminating)
            {
                try
                {
                    // Receive the operation information
                    Byte[] receivedInfoHeader = new Byte[1];
                    thisServer.Receive(receivedInfoHeader);

                    if (receivedInfoHeader[0] == 0) {}

                    if (serverID == 0 && receivedInfoHeader[0] == 1)    //1 for requesting session key, only master is allowed to send session keys
                    {
                        lock (sessionKeyLock)   //for preventing data hazard
                        {
                            if (amountOfSessionKeysBeenSent == 0)   //this server has not sent any session key to any server on this connection; new session key is needed
                            {
                                using (var rng = new RNGCryptoServiceProvider())    //create random key
                                {
                                    rng.GetNonZeroBytes(sessionKey);
                                }
                                sessionKeyIsSet = true;
                            }
                        }

                        string filePath = $"../../../keys/Server{serverUsername}_pub.txt";    //other server's public key path
                        logs.AppendText("filePath: " + filePath + "\n\n");

                        //send key
                        try
                        {
                            string serverPubKey = File.ReadAllText(filePath);
                            logs.AppendText($"session key without encryption with length {sessionKey.Length}: " + generateHexStringFromByteArray(sessionKey) + "\n\n");
                            logs.AppendText("AES key: " + generateHexStringFromByteArray(sessionKey[..16]) + "\n\n");
                            logs.AppendText("IV: " + generateHexStringFromByteArray(sessionKey[16..32]) + "\n\n");
                            logs.AppendText("HMAC key: " + generateHexStringFromByteArray(sessionKey[32..]) + "\n\n");

                            byte[] encodedKey = encryptWithRSA(sessionKey, 3072, serverPubKey); //encrypt using other server's public key
                            logs.AppendText($"with encryption {encodedKey.Length}: " + generateHexStringFromByteArray(encodedKey) + "\n\n");

                            Byte[] info = new Byte[1];
                            info[0] = 2;                //2 for sending session key and signature


                            byte[] signature = signWithRSA(Encoding.Default.GetString(encodedKey), 3072, serverPrivateKey);
                            logs.AppendText("signature: " + generateHexStringFromByteArray(signature) + "\n\n");

                            Socket sender = serverSendingSockets[Int32.Parse(serverUsername)];

                            sender.Send(info);          //send header
                            Thread.Sleep(10);
                            sender.Send(encodedKey);   //send the key
                            Thread.Sleep(10);
                            sender.Send(signature);    //send the signature

                            amountOfSessionKeysBeenSent++;

                            if(amountOfSessionKeysBeenSent == 2)    //after master sends session keys to both servers, it can send the files to be replicated waiting in the queue
                            {
                                Thread.Sleep(100);
                                sendQueuedFileReplications();
                            }
                        }
                        catch
                        {
                            logs.AppendText("session key sending failed!!\n");  //shouldn't happen normally
                        }
                    }

                    if ((serverID == 1 || serverID == 2) && receivedInfoHeader[0] == 2 && !sessionKeyIsSet) //receive session key
                    {
                        try
                        {
                            Byte[] sessionKeyEncrypted = new Byte[3072 / 8];     //get encrypted key, always sized 3072 bits
                            thisServer.Receive(sessionKeyEncrypted);
                            logs.AppendText("sKeyEncrypted: " + generateHexStringFromByteArray(sessionKeyEncrypted) + "\n\n");
                            string sKeyEncrypted = Encoding.Default.GetString(sessionKeyEncrypted); //encrypted key as string. Since original key was a byte array; there is no need for trimming empty chars.

                            Byte[] sessionKeySignature = new Byte[3072 / 8];     //get signature, always sized 3072 bits
                            thisServer.Receive(sessionKeySignature);
                            logs.AppendText("sKeySignature: " + generateHexStringFromByteArray(sessionKeySignature) + "\n\n");
                            string sKeySignature = Encoding.Default.GetString(sessionKeySignature);  //similar to encrypted key

                            string rsaPubKeyPath = "../../../keys/MasterServer_pub.txt";
                            string masterPubKey = File.ReadAllText(rsaPubKeyPath);
                            logs.AppendText("masterPubKey: " + masterPubKey + "\n\n");  //get master server's public key for signature verification

                            if (verifyWithRSA(sKeyEncrypted, 3072, masterPubKey, sessionKeySignature))   //verification is successfull
                            {
                                logs.AppendText("session key is verified\n");

                                byte[] decryptedKey = decryptWithRSA(sessionKeyEncrypted, 3072, serverPrivateKey);  //decrypt the key

                                logs.AppendText("decrypted AES key: " + generateHexStringFromByteArray(decryptedKey[..16]) + "\n\n");
                                logs.AppendText("decrypted IV: " + generateHexStringFromByteArray(decryptedKey[16..32]) + "\n\n");
                                logs.AppendText("decrypted HMAC key: " + generateHexStringFromByteArray(decryptedKey[32..]) + "\n\n");

                                sessionKey = decryptedKey;
                                sessionKeyIsSet = true;
                               
                                DeactivateOtherServerPortTextBoxes();

                                Thread.Sleep(100);
                                sendQueuedFileReplications();
                            }
                            else
                            {
                                logs.AppendText("session key could not get verified!!\n");
                            }
                        }
                        catch
                        {
                            logs.AppendText("Error during encryption, decryption or signature verification...\n");
                        }
                    }

                    if (receivedInfoHeader[0] == 3)     //receive replicated file
                    {
                        Byte[] receivedSizes = new Byte[16];    //receive sizes
                        thisServer.Receive(receivedSizes);

                        int lenOfFileName = BitConverter.ToInt32(receivedSizes[..4]);
                        int lenOfFile = BitConverter.ToInt32(receivedSizes[4..8]);
                        int lenOfFnameHMAC = BitConverter.ToInt32(receivedSizes[8..12]);
                        int lenOfFileHMAC = BitConverter.ToInt32(receivedSizes[12..]);
                        int totalLen = lenOfFileName + lenOfFile + lenOfFnameHMAC + lenOfFileHMAC;

                        logs.AppendText("Lengths: " + lenOfFileName.ToString() + " " + lenOfFile.ToString() + " " + lenOfFnameHMAC.ToString() + " " + lenOfFileHMAC.ToString() + "\n\n");

                        try
                        {
                            Byte[] fileProperties = new Byte[totalLen];     //receive a message as big as the total length
                            thisServer.Receive(fileProperties);

                            Byte[] fileNameEncrypted = fileProperties[..lenOfFileName];                 //get information from respective indexes
                            Byte[] fileEncrypted = fileProperties[lenOfFileName..(lenOfFileName + lenOfFile)];
                            Byte[] hmacFileName = fileProperties[(lenOfFileName + lenOfFile)..(lenOfFileName + lenOfFile + lenOfFnameHMAC)];
                            Byte[] hmacFile = fileProperties[(lenOfFileName + lenOfFile + lenOfFnameHMAC)..];

                            logs.AppendText("HMAC of file name: " + generateHexStringFromByteArray(hmacFileName) + "\n\n");

                            string filename = decryptWithAES128(fileNameEncrypted, sessionKey[..16], sessionKey[16..32]);       //decrypt file and file name using AES128
                            string decryptedFileAsString = decryptWithAES128(fileEncrypted, sessionKey[..16], sessionKey[16..32]);

                            byte[] hmacOfReceivedFileName = applyHMACwithSHA256(filename, sessionKey[32..]);
                            byte[] hmacOfReceivedFile = applyHMACwithSHA256(decryptedFileAsString, sessionKey[32..]);

                            logs.AppendText("HMAC of file name regenerated: " + generateHexStringFromByteArray(hmacOfReceivedFileName) + "\n\n");

                            if (generateHexStringFromByteArray(hmacOfReceivedFileName) == generateHexStringFromByteArray(hmacFileName) && generateHexStringFromByteArray(hmacOfReceivedFile) == generateHexStringFromByteArray(hmacFile))   //hmac check
                            {
                                logs.AppendText("File name and file itself are verified for replication! Decrypted file name: " + filename + "\n\n");
                                string pos_message = "Replicated file is verified! File is added in to the database.";
                                Byte[] signed_msg = signWithRSA(pos_message, 3072, serverPrivateKey);
                                logs.AppendText("Length of the message " + signed_msg.Length.ToString() + "\n\n");

                                // Create the file and write into it
                                lock (writeLock)
                                {
                                    ////BinaryWriter bWrite = new BinaryWriter(File.Open // using system.I/O
                                    ////    (DB_Path + "/" + fileName, FileMode.Create));   // FileMode.Create for overwrite

                                    ////bWrite.Write(file);
                                    ////bWrite.Flush();
                                    ////bWrite.Close();
                                    File.WriteAllBytes((DB_Path + "/" + filename), Convert.FromBase64String(decryptedFileAsString));
                                }

                                Socket sender = serverSendingSockets[Int32.Parse(serverUsername)];

                                Byte[] header2 = new Byte[1];
                                header2[0] = 4;             //4 for sending(receiving) file replication verification message
                                sender.Send(header2);
                                Thread.Sleep(10);
                                sender.Send(signed_msg);    //send positive verification message

                            }
                            else
                            {
                                logs.AppendText("Replicated file could not get verified!!\n\n");
                                string neg_message = "Replicated file could not get verified! File will not be stored in the database.";
                                Byte[] signed_msg = signWithRSA(neg_message, 3072, serverPrivateKey);
                                logs.AppendText("Length of the message " + signed_msg.Length.ToString() + "\n\n");

                                Socket sender = serverSendingSockets[Int32.Parse(serverUsername)];

                                Byte[] header2 = new Byte[1];
                                header2[0] = 4;             //4 for sending(receiving) file replication verification message
                                sender.Send(header2);
                                Thread.Sleep(10);
                                sender.Send(signed_msg);    //send negative verification message
                            }
                        }
                        catch
                        {
                            logs.AppendText("Error during file replication decryption!\n\n");
                        }
                    }

                    if (receivedInfoHeader[0] == 4) 
                    {
                        try
                        {
                            Byte[] verificationMessage = new Byte[384];     //get file replication verification message
                            thisServer.Receive(verificationMessage);

                            string filePath;
                            if(serverUsername == "0")
                            {
                                filePath = "../../../keys/MasterServer_pub.txt";
                            }
                            else
                            {
                                filePath = $"../../../keys/Server{serverUsername}_pub.txt";    //other server's public key path
                            }

                            string serverPubKey = File.ReadAllText(filePath);

                            lock (logs)
                            {
                                logs.AppendText("Server " + serverUsername + " has sended a verification message\n");

                                if (verifyWithRSA("Replicated file is verified! File is added in to the database.", 3072, serverPubKey, verificationMessage))   //positive message is verified?
                                {
                                    logs.AppendText("Replicated file is verified! File is added in to the database.\n\n");
                                }
                                else if (verifyWithRSA("Replicated file could not get verified! File will not be stored in the database.", 3072, serverPubKey, verificationMessage)) //negative message is verified?
                                {
                                    logs.AppendText("Replicated file could not get verified! File will not be stored in the database.\n\n");
                                }
                                else
                                {
                                    logs.AppendText("Something went terribly wrong try again later.\n\n");
                                }
                            }
                        }
                        catch
                        {
                            logs.AppendText("Verification message did not arrive!\n\n");
                        }
                    }

                    if (receivedInfoHeader[0] == 5) { }

                    if (receivedInfoHeader[0] == 6) { }

                    if (receivedInfoHeader[0] == 7) { }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (!terminating)
                    {
                        logs.AppendText("\"" + serverUsername + "\" has disconnected.\n");
                    }

                    //if one server disconnects, cut connection with all the other servers as well
                    foreach (Socket s in serverReceivingSockets)    
                    {
                        if (s != null)
                        {
                            s.Close();
                        }
                    }
                    foreach (Socket s in serverSendingSockets)
                    {
                        if (s != null)
                        {
                            s.Close();
                        }
                    }

                    serverReceivingSockets = new Socket[3];
                    serverSendingSockets = new Socket[3];
                    connectedServerIDs = new List<int>();
                    sessionKeyIsSet = false;
                    amountOfSessionKeysBeenSent = 0;


                    ActivateOtherServerPortTextBoxes();

                    connected = false;
                }

            }
        }

        private void Receive(Socket thisClient, string clientUsername)
        {

            bool connected = true;
            while (connected && !terminating)
            {

                try
                {
                    // Receive the operation information
                    Byte[] receivedInfoHeader = new Byte[1];
                    thisClient.Receive(receivedInfoHeader);
                    string serverPrivateKey;
                    if (serverID == 0)              //get current server's private key
                    {
                        serverPrivateKey = File.ReadAllText("../../../keys/MasterServer_pub_prv.txt");
                    }
                    else
                    {
                        serverPrivateKey = File.ReadAllText($"../../../keys/Server{serverID}_pub_prv.txt");
                    }

                    if (receivedInfoHeader[0] == 0)     // header = 0 means client wants to save a file
                    {
                        Byte[] header = new Byte[1];    //tell client we are sending our server id
                        header[0] = 1;                  //needed in the client side
                        thisClient.Send(header);

                        Byte[] serverId = new Byte[4];                                  //Send server id
                        serverId = Encoding.Default.GetBytes(serverID.ToString());
                        thisClient.Send(serverId);

                        Byte[] receivedSizes = new Byte[16];    //receive sizes of file and other information
                        thisClient.Receive(receivedSizes);

                        int lenOfFileName = BitConverter.ToInt32(receivedSizes[..4]);
                        int lenOfFile = BitConverter.ToInt32(receivedSizes[4..8]);
                        int lenOfAES = BitConverter.ToInt32(receivedSizes[8..12]);
                        int lenOfIV = BitConverter.ToInt32(receivedSizes[12..]);
                        int totalLen = lenOfFileName + lenOfFile + lenOfAES + lenOfIV;

                        logs.AppendText("Lengths: " + lenOfFileName.ToString() + " " + lenOfFile.ToString() + " " + lenOfAES.ToString() + " " + lenOfIV.ToString() + "\n\n");

                        try
                        {

                            Byte[] fileProperties = new Byte[totalLen];     //receive a message as big as total length
                            thisClient.Receive(fileProperties);

                            Byte[] fileNameEncrypted = fileProperties[..lenOfFileName];                 //get information from respective indexes
                            Byte[] fileEncrypted = fileProperties[lenOfFileName..(lenOfFileName + lenOfFile)];
                            Byte[] AESencrypted = fileProperties[(lenOfFileName + lenOfFile)..(lenOfFileName + lenOfFile + lenOfAES)];
                            Byte[] IVencrypted = fileProperties[(lenOfFileName + lenOfFile + lenOfAES)..];

                            byte[] decryptedAES = decryptWithRSA(AESencrypted, 3072, serverPrivateKey);         //first decrypt AES and IV
                            byte[] decryptedIV = decryptWithRSA(IVencrypted, 3072, serverPrivateKey);
                            string fileAsString = decryptWithAES128(fileEncrypted, decryptedAES, decryptedIV);  //using AES and IV, decrypt file and filename
                            string fileName = decryptWithAES128(fileNameEncrypted, decryptedAES, decryptedIV);
                            logs.AppendText("fileName: " + fileName + "\n\n");
                            logs.AppendText("fileAsString: " + fileAsString + "\n\n");
                            logs.AppendText("AES decrypted: " + generateHexStringFromByteArray(decryptedAES) + "\n\n");
                            logs.AppendText("IV decrypted: " + generateHexStringFromByteArray(decryptedIV) + "\n\n");

                            byte[] file = Convert.FromBase64String(fileAsString);


                            // Create the file and write into it
                            lock (writeLock)
                            {
                                //BinaryWriter bWrite = new BinaryWriter(File.Open // using system.I/O
                                //    (DB_Path + "/" + fileName, FileMode.Create));   // FileMode.Create for overwrite

                                //bWrite.Write(file);
                                //bWrite.Flush();
                                //bWrite.Close();
                                File.WriteAllBytes((DB_Path + "/" + fileName), file);
                            }


                            // Write into LOGS.txt
                            string time = DateTime.Now.ToString("MM/dd/yyyy HH:mm");
                            logs.AppendText(clientUsername + "\t" + fileName /*+ "\t" + "0"*/ + "\t" + file.Length + "bytes" + "\t" + time + "\n");

                            if (connectedServerIDs.Count == 2)  //file replication
                            {
                                //encrypt & apply HMAC using the session key
                                byte[] fileNameToBeReplicated = encryptWithAES128(fileName, sessionKey[..16], sessionKey[16..32]);  
                                byte[] fileToBeReplicated = encryptWithAES128(fileAsString, sessionKey[..16], sessionKey[16..32]);
                                byte[] hmacOfFileName = applyHMACwithSHA256(fileName, sessionKey[32..]);
                                byte[] hmacOfFile = applyHMACwithSHA256(fileAsString, sessionKey[32..]);

                                logs.AppendText("HMAC of file name: " + generateHexStringFromByteArray(hmacOfFileName) + "\n\n");

                                logs.AppendText("Lengths for replication: " + fileNameToBeReplicated.Length.ToString() + " " + fileToBeReplicated.Length.ToString() + " " + hmacOfFileName.Length.ToString() + " " + hmacOfFile.Length.ToString() + "\n\n");

                                int len = 16;    //16 for 4 integers
                                Byte[] datalength = new Byte[len];
                                System.Buffer.BlockCopy(BitConverter.GetBytes(fileNameToBeReplicated.Length), 0, datalength, 0, 4);   //since we will send integers, 4 byte per element is enough
                                System.Buffer.BlockCopy(BitConverter.GetBytes(fileToBeReplicated.Length), 0, datalength, 4, 4);
                                System.Buffer.BlockCopy(BitConverter.GetBytes(hmacOfFileName.Length), 0, datalength, 8, 4);
                                System.Buffer.BlockCopy(BitConverter.GetBytes(hmacOfFile.Length), 0, datalength, 12, 4);

                                int fnameLen = fileNameToBeReplicated.Length;
                                int fileLen = fileToBeReplicated.Length;
                                int hmacFnameLen = hmacOfFileName.Length;
                                int hmacFileLen = hmacOfFile.Length;

                                int len2 = fnameLen + fileLen + hmacFnameLen + hmacFileLen;
                                Byte[] data = new Byte[len2];           //send the data in concatanated manner

                                System.Buffer.BlockCopy(fileNameToBeReplicated, 0, data, 0, fnameLen);
                                System.Buffer.BlockCopy(fileToBeReplicated, 0, data, fnameLen, fileLen);
                                System.Buffer.BlockCopy(hmacOfFileName, 0, data, fnameLen + fileLen, hmacFnameLen);
                                System.Buffer.BlockCopy(hmacOfFile, 0, data, fnameLen + fileLen + hmacFnameLen, hmacFileLen);

                                Byte[] infoHeader = new Byte[1];
                                infoHeader[0] = 3;                      //3 for sending replicated file

                                foreach (Socket s in serverSendingSockets)  //send replication to both of the other servers
                                {
                                    if (s != null)
                                    {
                                        s.Send(infoHeader);
                                        Thread.Sleep(10);
                                        s.Send(datalength);
                                        Thread.Sleep(10);
                                        s.Send(data);
                                    }
                                }
                            }
                            else    //servers are not connected
                            {
                                logs.AppendText("Servers are not connected! File \"" + fileName + "\" is added in to the queue.\n\n");
                                filesToBeReplicated.Enqueue(fileName);   //server connection is not established, put filename in the queue
                            }

                            Byte[] to_verify = new Byte[384 * 3];                   //Send verification message to the client
                            string pos_message = "File received and saved successfully.";
                            Byte[] signed_msg = signWithRSA(pos_message, 3072, serverPrivateKey);
                            Byte[] signed_filename = signWithRSA(fileName, 3072, serverPrivateKey);
                            Byte[] signed_filestring = signWithRSA(fileAsString, 3072, serverPrivateKey);
                            System.Buffer.BlockCopy(signed_msg, 0, to_verify, 0, 384);
                            System.Buffer.BlockCopy(signed_filename, 0, to_verify, 384, 384);
                            System.Buffer.BlockCopy(signed_filestring, 0, to_verify, 384 * 2, 384);


                            Byte[] header2 = new Byte[1];   //inform client that we are sending verification message and potentially the signature
                            header2[0] = 2;
                            thisClient.Send(header2);

                            thisClient.Send(to_verify);     //send positive verification message together with the signature


                            receivedSizes = null;
                            fileProperties = null;          // In order to prevent creating files over and over again
                        }
                        catch
                        {
                            string neg_message = "File not received successfully.";
                            Byte[] signed_msg = signWithRSA(neg_message, 3072, serverPrivateKey);
                            Byte[] header2 = new Byte[1];   //inform client that we are sending verification message and potentially the signature
                            header2[0] = 2;
                            thisClient.Send(header2);
                            thisClient.Send(signed_msg);    //send negative verification message
                            logs.AppendText("error during receiving file properties!!");
                        }
                    }

                    if (receivedInfoHeader[0] == 1) // header = 1 means client requests to download a file
                    {
                        Byte[] header = new Byte[1];    //tell client we are sending our server id
                        header[0] = 1;                  //needed in the client side
                        thisClient.Send(header);

                        Byte[] serverId = new Byte[4];                                  //Send server id
                        serverId = Encoding.Default.GetBytes(serverID.ToString());
                        thisClient.Send(serverId);

                        Byte[] receivedSizes = new Byte[4];    // receive size of file length 
                        thisClient.Receive(receivedSizes);

                        int lenOfFileName = BitConverter.ToInt32(receivedSizes);

                        Byte[] fileName_byte_array = new Byte[lenOfFileName];     //receive a message as big as total length
                        thisClient.Receive(fileName_byte_array);

                        string requested_file_name = Encoding.Default.GetString(fileName_byte_array);

                        Byte[] header2 = new Byte[1];   //inform client that we are sending verification message and potentially the signature
                        header2[0] = 3;
                        bool doesExist = File.Exists(DB_Path + "/" + requested_file_name);
                        thisClient.Send(header2);
                        logs.AppendText(DB_Path + "/" + requested_file_name + "\n");
                        if (!doesExist)
                        {
                            // File doesn't exist
                            string neg_message = "File doesn't exist.";
                            Byte[] signed_msg = signWithRSA(neg_message, 3072, serverPrivateKey);
                            
                            thisClient.Send(signed_msg);    //send negative verification message
                            logs.AppendText("File doesn't exist \n");
                        }
                        else
                        {
                            string message = "File exists.";
                            Byte[] signed_msg = signWithRSA(message, 3072, serverPrivateKey);

                            thisClient.Send(signed_msg);    //send positive verification message
                            logs.AppendText("File exists. \n");

                            Byte[] requested_file = File.ReadAllBytes(DB_Path + "/" + requested_file_name);  //read the file from the database
                            Byte[] signed_filestring = signWithRSA(BitConverter.ToString(requested_file), 3072, serverPrivateKey);

                            Byte[] info_header = new Byte[384];
                            System.Buffer.BlockCopy(signed_filestring, 0, info_header, 0, 384);
                            thisClient.Send(info_header);


                            int fileProperties = 256; // FileName + The Data's Length
                            int fileNameLength = 128; // FileName
                            string fileLength = requested_file.Length.ToString(); // The Data's Length is turned into string 
                                                                                                   // to put into a Byte Array with the FileName

                            Byte[] filePropertiesBuffer = new Byte[fileProperties]; // Allocate space for FileName and The Data's Length

                            // Copy the FileName and The Data's Length into the filePropertiesBuffer
                            Array.Copy(Encoding.Default.GetBytes(requested_file_name), filePropertiesBuffer, requested_file_name.Length);
                            Array.Copy(Encoding.ASCII.GetBytes(fileLength), 0, filePropertiesBuffer, fileNameLength, fileLength.Length);

                            // Send the filePropertiesBuffer to the Server
                            thisClient.Send(filePropertiesBuffer);

                            // Send the data to the surver via generalBuffer
                            thisClient.Send(requested_file);

                        }
                    }

                    if (receivedInfoHeader[0] == 2) { }

                    if (receivedInfoHeader[0] == 3) { }

                    if (receivedInfoHeader[0] == 4) { }

                    if (receivedInfoHeader[0] == 5) { }

                    if (receivedInfoHeader[0] == 6) { }

                    if (receivedInfoHeader[0] == 7) { }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (!terminating)
                    {
                        logs.AppendText(e.Message + "\n");
                        logs.AppendText("\"" + clientUsername + "\" has disconnected.\n");
                    }

                    clientSockets.Remove(thisClient);
                    clientUsernames.Remove(clientUsername);
                    thisClient.Close();
                    connected = false;
                }

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
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

        static string generateHexStringFromByteArray(byte[] input)
        {
            string hexString = BitConverter.ToString(input);
            return hexString.Replace("-", "");
        }

        // signing with RSA
        static byte[] signWithRSA(string input, int algoLength, string xmlString)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create RSA object from System.Security.Cryptography
            RSACryptoServiceProvider rsaObject = new RSACryptoServiceProvider(algoLength);
            // set RSA object with xml string
            rsaObject.FromXmlString(xmlString);
            byte[] result = null;

            try
            {
                result = rsaObject.SignData(byteInput, "SHA256");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }

        // verifying with RSA
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

        // RSA encryption with varying bit length
        static byte[] encryptWithRSA(byte[] byteInput, int algoLength, string xmlStringKey)
        {
            // convert input string to byte array
            //byte[] byteInput = Encoding.Default.GetBytes(input);
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

        // RSA decryption with varying bit length
        static byte[] decryptWithRSA(byte[] byteInput, int algoLength, string xmlStringKey)
        {
            // create RSA object from System.Security.Cryptography
            RSACryptoServiceProvider rsaObject = new RSACryptoServiceProvider(algoLength);
            // set RSA object with xml string
            rsaObject.FromXmlString(xmlStringKey);
            byte[] result = null;

            try
            {
                result = rsaObject.Decrypt(byteInput, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
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

        // HMAC with SHA-256
        static byte[] applyHMACwithSHA256(string input, byte[] key)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create HMAC applier object from System.Security.Cryptography
            HMACSHA256 hmacSHA256 = new HMACSHA256(key);
            // get the result of HMAC operation
            byte[] result = hmacSHA256.ComputeHash(byteInput);

            return result;
        }
    }
}
