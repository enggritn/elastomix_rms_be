using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class SearchItemModel
    {
        public string Source { get; set; }
        public string BinRackAreaID { get; set; }
        public string BinRackID { get; set; }
        public string Name { get; set; }
        public List<String> ExcludeItems { get; set; }
    }
}