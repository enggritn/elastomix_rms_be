using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class InboundManualVM
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string InDate { get; set; }
        public string ExpiredDate { get; set; }
        public string Lot { get; set; }
        public string BagQty { get; set; }
        public string FullBagQty { get; set; }
        public string FullQty { get; set; }
        public string RemainQty { get; set; }
        public string TotalQty { get; set; }
        public string UoM { get; set; }
        public string MakerName { get; set; }
        public string PrintDate { get; set; }
        public int Sequence { get; set; }
        public int ShelfLife { get; set; }
    }
}