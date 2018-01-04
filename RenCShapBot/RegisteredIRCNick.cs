using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;


namespace RenCShapBot
{
    [Table("RegisteredIRCNick")]
    class RegisteredIRCNick
    {
        [Key]
        public int RegisteredIRCNickID { get; set; }

        public String NickName { get; set; }
        public int RegisteredUserID { get; set; }
    }
}
