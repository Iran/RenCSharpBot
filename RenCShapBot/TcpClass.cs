using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;
using System.Net;


namespace RenCShapBot
{
    public class TcpClass
    {
        int port;
        long lastHeartBeatreceive;
        long lastHeartBeatSend;
        Socket socket;
        public TcpClass(int port)
        {
            this.port = port;
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            CreateSocket();
            lastHeartBeatreceive = 0;
            lastHeartBeatSend = 0;
        }

        public byte[] StringToBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public string BytesToString(byte[] bytes)
        {
            return System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        }

        public void CreateSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Blocking = false;
        }

        public void Think()
        {
            long lastResponseInterval = GetMillisecondsTicks() - lastHeartBeatreceive;
            long lastSendInterval = GetMillisecondsTicks() - lastHeartBeatSend;
            if (socket.Available > 0)
            {
                byte[] buf = new byte[1024];
                int Receive = socket.Receive(buf);
                string msg = BytesToString(buf);
                HandleFDSMessages(msg);
                lastHeartBeatreceive = GetMillisecondsTicks();
                return;
            }

            if (lastHeartBeatreceive == 0 || lastResponseInterval > 10000)
            {
                Console.WriteLine("No hearbeat, attempting to close and reopen socket..");
                socket.Close();
                CreateSocket();
                TryConnect();

                try
                {
                   //Send("HI CONNECTED FROM CSHARPBOT");
                }
                catch(SocketException e)
                {
                    Console.WriteLine("Failed to send!");
                    Console.WriteLine(e.Message);
                }
                return;
            }
            if (lastSendInterval > 1000)
            {
            try
            {
                Send("HeartBeat");
                lastHeartBeatSend = GetMillisecondsTicks();
            }
            catch (SocketException e)
            {
                Console.WriteLine("Failed to send heartbeat!");
                Console.WriteLine(e.Message);
            }
           }
        }

        public long GetMillisecondsTicks()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public void TryConnect()
        {
            try
            {
                var result = socket.BeginConnect("127.0.0.1", port, null, null);

                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(0.1f));

                /*if (!success)
                {
                    Console.WriteLine("Failed to connect.");
                    return;
                } */

                // we have connected
                socket.EndConnect(result);
                lastHeartBeatreceive = GetMillisecondsTicks();
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception thrown in TryConnect, Failed to connect");
                Console.WriteLine(e.Message);
                return;
            }
        }

        public void Send(string msg)
        {
            byte[] msgBytes = StringToBytes(msg);
            byte[] bytes = new byte[msgBytes.Length + 1];
            msgBytes.CopyTo(bytes, 0);
            bytes[bytes.Length-1] = 0x0a; // newline character

            socket.Send(bytes, bytes.Length, 0);
           
        }

        public void Send(string format, params object[] args)
        {
            socket.Send(StringToBytes(String.Format(format, args)));

        }

        public void HandleFDSMessages(string msg)
        {
            string[] sendArr = msg.Split('\r');
            foreach (string sendMsg_ in sendArr) {
                string sendMsg = sendMsg_.Trim('\0').Trim();
                if (sendMsg == "") continue;

                string[] msgArr = sendMsg.Split(' ');

                Console.WriteLine("MSG FROM FDS: {0}", sendMsg);

                if (msgArr[0] == null) return;
                string Type = msgArr[0];

                if (Type == "PLAYERJOIN")
                {
                    Program.Handle_Player_Join_FDS_Message(msgArr);
                }

                if (Type == "AUTHPASS")
                {
                    Program.Handle_Auth_Password_FDS_Message(msgArr);
                }

                if (Type == "REBOOT")
                {
                    Program.irc.SendMessage(Meebey.SmartIrc4net.SendType.Message, "#renCsharpBot", "Rebooting bot..");
                    Program.Restart();
                }
                if (Type == "AUTOREC")
                {
                    Program.Handle_Auto_Rec_FDS_Message(msgArr, sendMsg);
                }
                if (Type == "REC")
                {
                    Program.Handle_Rec_FDS_Message(msgArr, sendMsg);
                }
                if (Type == "SETJOIN")
                {
                    Program.Handle_Set_Join_Message_FDS_Message(msgArr, sendMsg);
                }
                if (Type == "DELJOIN")
                {
                    Program.Handle_Delete_Join_Message_FDS_Message(msgArr);
                }

                if (Type == "IRC") {
                    string ircMsg = String.Join(" ", msgArr.Skip(2).ToArray());
                    string[] ircMsgArr = ircMsg.Split('\n');

                    foreach (string temp in ircMsgArr)
                    {
                        Program.irc.SendMessage(Meebey.SmartIrc4net.SendType.Message, msgArr[1], temp);
                    }
                }
            }
        }
    }
}
