using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class vStockAllv2
    {
        public string ID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string Code { get; set; }
        public string LotNumber { get; set; }
        public System.DateTime? InDate { get; set; }
        public System.DateTime? ExpiredDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal QtyPerBag { get; set; }
        public Nullable<decimal> BagQty { get; set; }
        public string Type { get; set; }
        public System.DateTime ReceivedAt { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string BinRackAreaType { get; set; }
        public bool OnInspect { get; set; }
    }
}