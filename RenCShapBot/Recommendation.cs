using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace RenCShapBot
{
    [Table("Recommendation")]
    class Recommendation
    {
        public Recommendation(string NickName, String Type, String RecommendedBy, String Reason, DateTime DateTime)
        {
            this.NickName = NickName;
            this.Type = Type;
            this.RecommendedBy = RecommendedBy;
            this.Reason = Reason;
            this.DateTime = DateTime;
        }

        [Key]
        public int RecommendationID { get; set; }

        public String NickName { get; set; }
        public String Type { get; set; }
        public String RecommendedBy { get; set; }
        public String Reason { get; set; }
        public DateTime DateTime { get; set; }
    }
}
