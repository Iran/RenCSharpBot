using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace RenCShapBot
{
    [Table("RegisteredUser")]
    class RegisteredUser
    {
        [Key]
        public int RegisteredUserID { get; set; }

        public String NickName { get; set; }
        public String ModFlags { get; set; }
        public String Permission { get; set; }
        public bool AuthViaWOL { get; set; }
        public bool AuthViaIP { get; set; }
        public bool AuthViaSerialHash { get; set; }
        public bool AuthViaPassword { get; set; }
        public bool AuthViaHostname { get; set; }

        public int LinkedToRegisteredUserID { get; set; }
        public String SerialHash { get; set; }
        public String HostName { get; set; }
        public String IP { get; set; }
        public String Password { get; set; }
    }
}
