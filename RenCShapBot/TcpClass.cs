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
        Socket socket;
        public TcpClass(int port)
        {
            this.port = port;
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Blocking = false;
        }

        public byte[] StringToBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public void Think()
        {
            if (socket.Connected == false)
            {
                TryConnect();
                return;
            }

            socket.Send(StringToBytes("HI CONNECTED FROM CSHARPBOT"));
        }

        public void TryConnect()
        {
            try
            {
                var result = socket.BeginConnect("127.0.0.1", port, null, null);

                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                if (!success)
                {
                    Console.WriteLine("Failed to connect.");
                }

                // we have connected
                socket.EndConnect(result);
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception thrown in TryConnect, Failed to connect");
            }
        }
    }
}
