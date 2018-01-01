using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenCShapBot
{
    [Table("PlayerJoin")]
    class PlayerJoin
    {
        public PlayerJoin(string nick, bool isWolUser, long scriptsRevision, float scriptsVersion, string team, string iP, string serialHash, string HostName)
        {
            Nick = nick;
            IsWolUser = isWolUser;
            ScriptsRevision = scriptsRevision;
            ScriptsVersion = scriptsVersion;
            Team = team;
            IP = iP;
            SerialHash = serialHash;
            this.DateTime = DateTime.UtcNow;
            this.HostName = HostName;
        }

        [Key]
        public int PlayerJoinID { get; set; }

        public string Nick { get; set; }
        public bool IsWolUser { get; set; }
        public long ScriptsRevision { get; set; }
        public float ScriptsVersion { get; set; }
        public string Team { get; set; }
        public string IP { get; set; }
        public string SerialHash { get; set; }
        public string HostName { get; set; }
        public DateTime DateTime { get; set; }
    }
}
