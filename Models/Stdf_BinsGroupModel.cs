using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSMCMapGenerator.Models
{
    public class Stdf_BinsGroupModel
    {
        public string LotId { get; set; }
        public string CP { get; set; }
        public string Wf_No { get; set; }
        public List<Stdf_BinsModel> stdf_BinsModels { get; set; }
    }
}
