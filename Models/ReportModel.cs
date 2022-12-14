using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class ExpiredStockDTO
    {
        public string Barcode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string ReceivedAt { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
    }

    public class ActualStockDTO
    {
        public string ID { get; set; }
        public string Barcode { get; set; }
        public string LotNo { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public bool IsExpired { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }
}