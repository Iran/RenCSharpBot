using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace RenCShapBot
{
    [Table("ChatLogLine")]
    class ChatLogLine
    {
        public ChatLogLine(string nick, string realName, string host, string channel, string message, char permission)
        {
            Nick = nick;
            RealName = realName;
            Host = host;
            Channel = channel;
            Message = message;
            Permission = permission;
            DateTime = DateTime.UtcNow;
        }

        [Key]
        public int LineID { get; set; }

        public string Nick { get; set; }
        public string RealName { get; set; }
        public string Host { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public char Permission { get; set; }
        public DateTime DateTime { get; set; }
    }
}
