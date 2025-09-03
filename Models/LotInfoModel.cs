using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSMCMapGenerator.Models
{
    public class LotInfoModel
    {
        public string LotId { get; set; }
        public string Cp { get; set; }
        public string Rp { get; set; }
        public int Tester_Count { get; set; }
        public string AllWfNo { get; set; }
        public string Device { get; set; }
        public string Cust_Code { get; set; }
    }
}
