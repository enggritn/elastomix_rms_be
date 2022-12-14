using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class ItemLookupModel
    {
        public string DataSource { get; set; }
        public string ID { get; set; }
        public string Barcode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public string LotNumber { get; set; }
        public string BinRackAreaID { get; set; }
        public string BinRackArea { get; set; }
        public string BinRackID { get; set; }
        public string BinRack { get; set; }
        public decimal Quantity { get; set; }

        public bool IsExclude { get; set; }
    }
}