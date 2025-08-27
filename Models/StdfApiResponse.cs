using System.Collections.Generic;

namespace TSMCMapGenerator.Models
{
    public class StdfApiResponse
    {
        public int Count { get; set; }
        public List<Stdf_BinsModel> stdf_BinsModels { get; set; }
    }
}
