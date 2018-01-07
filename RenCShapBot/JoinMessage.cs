using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace RenCShapBot
{
    [Table("JoinMessage")]
    class JoinMessage
    {
        public JoinMessage(string NickName, String Message)
        {
            this.NickName = NickName;
            this.Message = Message;
        }

        [Key]
        public int JoinMessageID { get; set; }

        public String NickName { get; set; }
        public String Message { get; set; }
    }
}
