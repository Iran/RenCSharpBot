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
        public PlayerJoin(string nick, bool isWolUser, long scriptsRevision, float scriptsVersion, string team, string iP, 
            string serialHash, string HostName, String Country, String Region, String City, String ZipCode, String TimeZone, String Latitude,
            String Longitude, String MetroCode)
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

            this.Country = Country;
            this.Region = Region;
            this.City = City;
            this.ZipCode = ZipCode;
            this.TimeZone = TimeZone;
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            this.MetroCode = MetroCode;
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

        public String Country { get; set; }
        public String Region { get; set; }
        public String City { get; set; }
        public String ZipCode { get; set; }
        public String TimeZone { get; set; }
        public String Latitude { get; set; }
        public String Longitude { get; set; }
        public String MetroCode { get; set; }

        public DateTime DateTime { get; set; }
    }
}
