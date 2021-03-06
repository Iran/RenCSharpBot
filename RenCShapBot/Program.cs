﻿using System;
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
        public static string AdminChannel = "#renCsharpbot-admin";
        public static string PublicChannel = "#renCsharpbot";
        public static string BotName = "RenCsharpBot";
        public static string BotDescription = "Renegade bot in C#";

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

        private static List<E> GetAll<E>() where E : class
        {
            CreateAndOpenDb();
            using (var _connection = _dbConnection)
            {
                return _connection.GetAll<E>().ToList();
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

        static void DeleteEntity<E>(E entity) where E : class
        {
            CreateAndOpenDb();

            using (var _connection = _dbConnection)
            {
                _connection.Delete(entity);
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
            [Nick] NVARCHAR(128) NOT NULL UNIQUE,
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

            _dbConnection.ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS [RegisteredUser] (
            [RegisteredUserID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            [LinkedToRegisteredUserID] INTEGER NOT NULL,
            [NickName] NVARCHAR(128) NOT NULL,
            [ModFlags] NVARCHAR(128) NOT NULL,
            [Permission] NVARCHAR(1) NOT NULL,
            [AuthViaWOL] BOOLEAN NOT NULL,
            [AuthViaSerialHash] BOOLEAN NOT NULL,
            [SerialHash] NVARCHAR(128) NOT NULL,
            [AuthViaHostName] BOOLEAN NOT NULL,
            [HostName] NVARCHAR(128) NOT NULL,
            [AuthViaIP] BOOLEAN NOT NULL,
            [IP] NVARCHAR(128) NOT NULL,
            [AuthViaPassword] BOOLEAN NOT NULL,
            [Password] NVARCHAR(128) NOT NULL )");

        _dbConnection.ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS [RegisteredIRCNick] (
            [RegisteredIRCNickID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            [NickName] NVARCHAR(128) NOT NULL UNIQUE,
            [RegisteredUserID] INTEGER NOT NULL
            )");

            _dbConnection.ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS [Recommendation] (
            [RecommendationID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            [NickName] NVARCHAR(128) NOT NULL,
            [Reason] NVARCHAR(128) NOT NULL,
            [RecommendedBy] NVARCHAR(128) NOT NULL,
            [Type] NVARCHAR(128) NOT NULL,
            [DateTime] DATETIME NOT NULL
            )");
            _dbConnection.ExecuteNonQuery(@"
        CREATE TABLE IF NOT EXISTS [JoinMessage] (
            [JoinMessageID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            [NickName] NVARCHAR(128) NOT NULL,
            [Message] NVARCHAR(128) NOT NULL
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

        internal static void Handle_Auth_Password_FDS_Message(string[] msgArr)
        {
            String Nick = msgArr[1];
            String Password = msgArr[2];

            var r = GetRegisteredUser(Nick);
            if (r != null)
            {
                tcp.Send("AUTHPASSWORD\t{0}\t{1}\t{2}\t{3}", Nick, r.ModFlags, r.Permission, r.Password == Password ? "MATCH" : "NOMATCH");
            }
        }

        internal static void Handle_Auto_Rec_FDS_Message(string[] msgArr, string msg)
        {
            String Type = msgArr[1];
            string Target = msgArr[2];
            string Reason = msg.Substring(msgArr[0].Length + msgArr[1].Length + msgArr[2].Length + 2);

            SaveEntity(new Recommendation(Target, "Type", "", Reason, DateTime.UtcNow));
            tcp.Send("MSG\t{0} has been automatically recommended for {1}", Target, Reason);

        }

        internal static void Handle_Set_Join_Message_FDS_Message(string[] msgArr, string msg)
        {
            string Nick = msgArr[1];
            string message = msg.Substring(msgArr[0].Length);

            SaveEntity(new JoinMessage(Nick, message));
        }

        internal static void Handle_Delete_Join_Message_FDS_Message(string[] msgArr)
        {
            string Nick = msgArr[1];
            JoinMessage jm = GetAll<JoinMessage>().Where(j => j.NickName.ToLower() == Nick.ToLower()).FirstOrDefault();
            if (jm != null)
            {
                DeleteEntity(jm);
            }
        }

        internal static void Handle_Rec_FDS_Message(string[] msgArr, string msg)
        {
            string Target = msgArr[1];
            string RecommendedBy = msgArr[2];
            string Reason = msg.Substring(msgArr[0].Length + msgArr[1].Length + msgArr[2].Length + 2);

            SaveEntity(new Recommendation(Target, "USERREC", RecommendedBy, Reason, DateTime.UtcNow));
            tcp.Send("MSG\t{0} has been recommended by {1} for {2}", Target, RecommendedBy, Reason);
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
            String Password = msgArr[8];
            String HostName = GetHostName(IP);

            var client = new WebClient();

            try
            {
                // TODO: set FDS to WOL mode so I can get IPs correctly
                var response = client.DownloadString("http://freegeoip.net/json/" + IP);

                var geoIp = JsonConvert.DeserializeObject<GeoIpInfo>(response);

                SaveEntity(new PlayerJoin(Nick, IsWolUser, revision, Version, Team, IP, SerialHash, HostName,
                    geoIp.country_name, geoIp.region_name, geoIp.city, geoIp.zip_code, geoIp.time_zone, geoIp.latitude, geoIp.longitude, geoIp.metro_code ));

                tcp.Send("JOINPLAYER\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}", 
                   Nick, IP, SerialHash, HostName, GetWhiteSpaceIfStringIsEmpty(geoIp.country_name), GetWhiteSpaceIfStringIsEmpty(geoIp.region_name),
                   GetWhiteSpaceIfStringIsEmpty(geoIp.city), GetWhiteSpaceIfStringIsEmpty(geoIp.zip_code), GetWhiteSpaceIfStringIsEmpty(geoIp.time_zone), GetWhiteSpaceIfStringIsEmpty(geoIp.latitude),
                   GetWhiteSpaceIfStringIsEmpty(geoIp.longitude), GetWhiteSpaceIfStringIsEmpty(geoIp.metro_code));
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception occured while trying to retrieve GeoIP info");
                Console.WriteLine(ex.Message);

                SaveEntity(new PlayerJoin(Nick, IsWolUser, revision, Version, Team, IP, SerialHash, HostName,
                    "", "", "", "", "", "", "", ""));

                tcp.Send("JOINPLAYER\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}]\t{9}\t{10}\t{11}",
                Nick, IP, SerialHash, HostName, " ", " ", " ", " ", " ", " ", " ", " ");
            }

            RegisteredUser r = GetRegisteredUser(Nick);

            if (r != null)
            {

                tcp.Send("AUTH\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}", r.NickName, GetWhiteSpaceIfStringIsEmpty(r.Permission), 
                    GetWhiteSpaceIfStringIsEmpty(r.ModFlags), GetIntValueFromBool(r.AuthViaPassword), GetWhiteSpaceIfStringIsEmpty(r.Password),
                    GetIntValueFromBool(r.AuthViaHostname), GetWhiteSpaceIfStringIsEmpty(r.HostName), GetIntValueFromBool(r.AuthViaIP), 
                    GetWhiteSpaceIfStringIsEmpty(r.IP), GetIntValueFromBool(r.AuthViaSerialHash), 
                    GetWhiteSpaceIfStringIsEmpty(r.SerialHash), GetIntValueFromBool(r.AuthViaWOL));
            }

            JoinMessage jm = GetAll<JoinMessage>().Where(j => j.NickName.ToLower() == Nick.ToLower()).FirstOrDefault();
            if (jm != null)
            {
                tcp.Send("[{0}] {1}", jm.NickName, jm.Message);
            }
        }

        static public string GetWhiteSpaceIfStringIsEmpty(string str)
        {
            return str == "" ? " " : str;
        }

        static public int GetIntValueFromBool(bool b)
        {
            return b == true ? 1 : 0;
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
            string modFlags = GetModFlags(GetRegisteredUserByIRCNick(nick));

            tcp.Send("IRCMSG\t{0}\t{1}\t{2}\t{3}\t{4}", channel, permission, nick, modFlags, msg);
        }

        public static string GetModFlags(RegisteredUser r)
        {
            string modFlags = "";
            if (r != null)
            {
                modFlags = r.ModFlags;
            }
            return GetWhiteSpaceIfStringIsEmpty(modFlags);
        }

        public static RegisteredUser GetRegisteredUser(string nick)
        {
            return GetAll<RegisteredUser>().Where(u => u.NickName.ToLower() == nick.ToLower()).FirstOrDefault();
        }

        public static RegisteredUser GetRegisteredUserByIRCNick(string ircnick)
        {
            RegisteredIRCNick ri = GetAll<RegisteredIRCNick>().Where(u => u.NickName.ToLower() == ircnick.ToLower()).FirstOrDefault();
            if (ri == null) return null;

            return GetEntityById<RegisteredUser>(ri.RegisteredUserID);
        }


    public static void Restart()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            irc.SendMessage(SendType.Message, Program.PublicChannel, location.AbsolutePath);

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

            tcp.Send("IRCMSG\t{0}\t{1}\t{2}\t{3}", nick, permission, nick, msg);
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
                irc.Login(Program.BotName, Program.BotDescription);
                // join the channel
                irc.RfcJoin(Program.PublicChannel);
                irc.RfcJoin(Program.AdminChannel);
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
