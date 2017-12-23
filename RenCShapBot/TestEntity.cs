using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;

namespace RenCShapBot
{
    [Table("TestEntity")]
    class TestEntity
    {

        [Key]
        public int TestEntityID { get; set; }

        public String derp { get; set; }
        public float herp { get; set; }
        public Boolean ok { get; set; }
        public int testInt { get; set; }
    }
}
