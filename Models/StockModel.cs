using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class StockDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNumber { get; set; }
        public string InDate { get; set; }
        public string ExpiredDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal QtyPerBag { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string ReceivedAt { get; set; }
        public string Type { get; set; }
        public decimal BagQty { get; set; }
        public string BinRackAreaName { get; set; }
        public string BinRackAreaType { get; set; }
        public string WarehouseName { get; set; }
        public string WarehouseCode { get; set; }
        public bool IsExpired { get; set; }
    }

    public class LabelDTO
    {
        public string Code { get; set; }
        public string Field1 { get; set; }
        public string Field2 { get; set; }
        public string Field3 { get; set; }
        public string Field4 { get; set; }
        public string Field5 { get; set; }
        public string Field6 { get; set; }
        public string Field7 { get; set; }
        public string Field8 { get; set; }
        public string Field9 { get; set; }
        public string Field10 { get; set; }
        public string Field11 { get; set; }
        public string Field12 { get; set; }
        public string Field13 { get; set; }
        public string Field14 { get; set; }
        public string Field15 { get; set; }
    }

    public class MaterialInfo
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string TotalQty { get; set; }
    }

    public class MaterialMaster
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string QtyPerBag { get; set; }
    }

}