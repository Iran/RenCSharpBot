using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper;
using System.Data.SqlClient;
using Dapper.Contrib.Extensions;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using Meebey.SmartIrc4net;
using System.Collections;

namespace RenCShapBot
{
    class Program
    {
        private static SQLiteConnection _dbConnection;
        public static IrcClient irc = new IrcClient();
        public static TcpClass tcp = new TcpClass(44441);


        static void Main(string[] args)
        {
            CreateAndOpenDb();
            SeedDatabase();

            var te = new TestEntity()
            {
                derp = "hi",
                herp = 5.0f,
                ok = false,
                testInt = 6
            };

            SaveEntity(te);

            var newTe = GetEntityById<TestEntity>(1);
           

                Console.Out.WriteLine("derp = {0}, herp = {1}, ok = {2}, testInt = {3}", newTe.derp, newTe.herp, newTe.ok, newTe.testInt);

            InitIrcClient();
            try
            {
                while (true)
                {
                    tcp.Think();
                    irc.ListenOnce(false);
                    Thread.Sleep(1);
                }
            }
            catch (ConnectionException)
            {
                // this exception is handled because Disconnect() can throw a not
                // connected exception
            }
            catch (Exception e)
            {
                // this should not happen by just in case we handle it nicely
                System.Console.WriteLine("Error occurred! Message: " + e.Message);
                System.Console.WriteLine("Exception: " + e.StackTrace);
                irc.Disconnect();
            }

        }

        private static E GetEntityById<E>(int v) where E : class
        {
            CreateAndOpenDb();
            using (var _connection = _dbConnection)
            {
                return _connection.Get<E>(1);
            }
        }

        static void SaveEntity<E>(E entity) where E : class
        {
            CreateAndOpenDb();

            using (var _connection = _dbConnection)
            {
                _connection.Insert(entity);
            }
        }

        public static void CreateAndOpenDb()
        {
            var dbFilePath = "./TestDb.sqlite";
            if (!File.Exists(dbFilePath))
            {
                SQLiteConnection.CreateFile(dbFilePath);
            }
            _dbConnection = new SQLiteConnection(string.Format(
                "Data Source={0};Version=3;", dbFilePath));
            _dbConnection.Open();
        }

        static void SeedDatabase()
        {
            // Create a Users table
            _dbConnection.ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS [TestEntity] (
            [TestEntityID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            [derp] NVARCHAR(64) NOT NULL,
            [herp] REAL NOT NULL,
            [ok] BOOLEAN NOT NULL,
            [testInt] INTEGER NOT NULL
            )");

            _dbConnection.ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS [ChatLogLine] (
            [LineID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            [Nick] NVARCHAR(128) NOT NULL,
            [RealName] NVARCHAR(128) NOT NULL,
            [Host] NVARCHAR(128) NOT NULL,
            [Channel] NVARCHAR(128) NOT NULL,
            [Message] NVARCHAR(128) NOT NULL,
            [Permission] NVARCHAR(1) NOT NULL,
            [DateTime] DATETIME NOT NULL
            )");
    }

        // We are calling this from the RAW MESSAGE event handler as the QUIT event handler is
        // fired after the library has removed all the channel data for this user, while in 
        // the RAW MESSAGE event handler this isn't done yet.
        private static void OnUserQuit(string nickname)
        {
            var user = irc.GetIrcUser(nickname);
            var channels = String.Join(",", user.JoinedChannels);
        }

        private static void OnUserJoin(object sender, JoinEventArgs e)
        {
            Console.WriteLine("Channel JOIN: nick = {0}, channel = {1}", e.Data.Nick, e.Data.Channel);
        }

        private static void OnUserPartChannel(object sender, PartEventArgs e)
        {
            Console.WriteLine("Channel PART: nick = {0}, channel = {1}", e.Data.Nick, e.Data.Channel);
        }

        private static void OnChannelMessage(object sender, IrcEventArgs e)
        {
            var user = (NonRfcChannelUser)irc.GetChannelUser(e.Data.Channel, e.Data.Nick);
            var nick = user.Nick;
            var realname = user.Realname;
            var channel = user.Channel;
            var host = user.Host;
            var msg = e.Data.Message;
            char permission = GetUserPermissionChar(user);


            SaveEntity(new ChatLogLine(nick, realname, host, channel, msg, permission));

            irc.SendMessage(SendType.Message, channel, IrcConstants.IrcColor + IrcColors.LightRed + " message in red");
        }

        private static char GetUserPermissionChar(NonRfcChannelUser user)
        {
            if (user.IsOwner) return '~';
            if (user.IsChannelAdmin) return '&';
            if (user.IsIrcOp) return '@';
            if (user.IsHalfop) return '%';
            if (user.IsVoice) return '+';
            return ' ';
        }

        public static void OnPrivateMessage(object sender, IrcEventArgs e)
        {
            var channel = e.Data.Channel;
            if (e.Data.RawMessage.StartsWith("!"))
            {
                irc.SendMessage(SendType.Message, channel, "command found");
            }
        }

        // this method handles when we receive "ERROR" from the IRC server
        public static void OnError(object sender, Meebey.SmartIrc4net.ErrorEventArgs e)
        {
            System.Console.WriteLine("Error: " + e.ErrorMessage);
        }

        // this method will get all IRC messages
        public static void OnRawMessage(object sender, IrcEventArgs e)
        {
            var nick = e.Data.Nick;
            var channel = e.Data.Channel;
            var rawmsg = e.Data.RawMessage;

            if (e.Data.RawMessageArray.Length > 1 && e.Data.RawMessageArray[1] == "QUIT") {
                OnUserQuit(nick);
            }

            System.Console.WriteLine("Received: " + e.Data.RawMessage);

            if (channel != null && nick != null)
            {
                // System.Console.WriteLine("channel = {0}, user = {1}, message = {2}", e.Data.Channel, e.Data.Nick, e.Data.Message);
               // OnChannelMessage(sender, e);
            }
            
            if (e.Data.RawMessage.Contains("@VERSION@"))
            {
                System.Console.WriteLine("got @VERSION@");
            }
        }

        static void InitIrcClient()
        {
            Thread.CurrentThread.Name = "Main";
            irc.Encoding = System.Text.Encoding.UTF8;

            // wait time between messages, we can set this lower on own irc servers
            irc.SendDelay = 200;
            // we use channel sync, means we can use irc.GetChannel() and so on
            irc.ActiveChannelSyncing = true;
            irc.AutoReconnect = true;
            irc.AutoRejoin = true;
            irc.AutoRejoinOnKick = true;
            irc.SupportNonRfc = true;

            // here we connect the events of the API to our written methods
            // most have own event handler types, because they ship different data
            irc.OnQueryMessage += new IrcEventHandler(OnPrivateMessage);
            irc.OnError += new Meebey.SmartIrc4net.ErrorEventHandler(OnError);
            irc.OnRawMessage += new IrcEventHandler(OnRawMessage);
            irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
            irc.OnJoin += new JoinEventHandler(OnUserJoin);
            irc.OnPart += new PartEventHandler(OnUserPartChannel);

            string[] serverlist;
            // the server we want to connect to, could be also a simple string
            serverlist = new string[] { "irc.miners-zone.net" };
            int port = 6667;
            string channel = "#renCsharpBot";
            try
            {
                // here we try to connect to the server and exceptions get handled
                irc.Connect(serverlist, port);
            }
            catch (ConnectionException e)
            {
                // something went wrong, the reason will be shown
                System.Console.WriteLine("couldn't connect! Reason: " + e.Message);
            }

            try
            {
                // here we logon and register our nickname and so on 
                irc.Login("renCsharpBot", "Renegade C# Bot");
                // join the channel
                irc.RfcJoin(channel);
            }
            catch (ConnectionException)
            {
                // this exception is handled because Disconnect() can throw a not
                // connected exception
            }
            catch (Exception e)
            {
                // this should not happen by just in case we handle it nicely
                System.Console.WriteLine("Error occurred! Message: " + e.Message);
                System.Console.WriteLine("Exception: " + e.StackTrace);
                irc.Disconnect();
            }
        }
    }
}
