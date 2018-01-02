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
using System.Reflection;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

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

            _dbConnection.ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS [PlayerJoin] (
            [PlayerJoinID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            [Nick] NVARCHAR(128) NOT NULL,
            [IsWolUser] BOOLEAN NOT NULL,
            [ScriptsRevision] BIGINT NOT NULL,
            [ScriptsVersion] FLOAT NOT NULL,
            [Team] NVARCHAR(128) NOT NULL,
            [IP] NVARCHAR(32) NOT NULL,
            [SerialHash] NVARCHAR(64) NOT NULL,
            [HostName] NVARCHAR(256) NOT NULL,
            [Country] NVARCHAR(256),
            [Region] NVARCHAR(256),
            [City] NVARCHAR(128),
            [ZipCode] NVARCHAR(128),
            [TimeZone] NVARCHAR(128),
            [Latitude] NVARCHAR(128),
            [Longitude] NVARCHAR(128),
            [MetroCode] NVARCHAR(128),
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

        internal static void Handle_Player_Join_FDS_Message(string[] msgArr)
        {
            //if (msgArr[0] != "PLAYERJOIN") return;
            String Nick = msgArr[1];
            String Team = msgArr[2];
            String SerialHash = msgArr[3];
            bool IsWolUser = int.Parse(msgArr[4]) == 1 ? true : false;
            float Version = float.Parse(msgArr[5]);
            long revision = long.Parse(msgArr[6]);
            String IP = msgArr[7];
            String HostName = GetHostName(IP);

            var client = new WebClient();

            try
            {
                // TODO: set FDS to WOL mode so I can get IPs correctly
                var response = client.DownloadString("http://freegeoip.net/json/" + IP);

                var geoIp = JsonConvert.DeserializeObject<GeoIpInfo>(response);

                SaveEntity(new PlayerJoin(Nick, IsWolUser, revision, Version, Team, IP, SerialHash, HostName,
                    geoIp.country_name, geoIp.region_name, geoIp.city, geoIp.zip_code, geoIp.time_zone, geoIp.latitude, geoIp.longitude, geoIp.metro_code ));

                tcp.Send("JOINPLAYER {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", 
                   Nick, IP, SerialHash, HostName, GetNullIfEmpty(geoIp.country_name), GetNullIfEmpty(geoIp.region_name),
                   GetNullIfEmpty(geoIp.city), GetNullIfEmpty(geoIp.zip_code), GetNullIfEmpty(geoIp.time_zone), GetNullIfEmpty(geoIp.latitude),
                   GetNullIfEmpty(geoIp.longitude), GetNullIfEmpty(geoIp.metro_code));
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception occured while trying to retrieve GeoIP info");
                Console.WriteLine(ex.Message);

                SaveEntity(new PlayerJoin(Nick, IsWolUser, revision, Version, Team, IP, SerialHash, HostName,
                    "", "", "", "", "", "", "", ""));

                tcp.Send("JOINPLAYER {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
                Nick, IP, SerialHash, HostName, "null", "null", "null", "null", "null", "null", "null", "null");
            }

            
        }

        static public string GetNullIfEmpty(string str)
        {
            return str == "" ? "null" : str;
        }

        static public string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch (SocketException ex)
            {
                //unknown host or
                //not every IP has a name
                //log exception (manage it)
            }

            return null;
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

           //irc.SendMessage(SendType.Message, channel, IrcConstants.IrcColor + IrcColors.LightRed + " message in red");
            tcp.Send("IRCMSG {0} {1} {2} {3}", channel, permission, nick, msg);
        }


    public static void Restart()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            irc.SendMessage(SendType.Message, "#renCsharpbot", location.AbsolutePath);

            ProcessStartInfo startInfo = new ProcessStartInfo(location.AbsolutePath);
            Process.Start(startInfo);
            Environment.Exit(0);
        }

    private static char GetUserPermissionChar(NonRfcChannelUser user)
        {
            if (user.IsOwner) return '~';
            if (user.IsChannelAdmin) return '&';
            if (user.IsIrcOp) return '@';
            if (user.IsHalfop) return '%';
            if (user.IsVoice) return '+';
            return '?';
        }

        public static void OnPrivateMessage(object sender, IrcEventArgs e)
        {
            var nick = e.Data.Nick;
            //var realname = user.Realname;
            //var channel = user.Channel;
            //var host = user.Host;
            var msg = e.Data.Message;
            char permission = '?';

            tcp.Send("IRCMSG {0} {1} {2} {3}", nick, permission, nick, msg);
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
