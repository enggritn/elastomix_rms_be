using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WMS_BE.Models
{
    public class TransformHeaderVM
    {
        public string MaterialCode { get; set; }
        public decimal TotalQty { get; set; }
        public string MaterialCodeTarget { get; set; }
    }

    public class TransformHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string TotalQty { get; set; }
        public string MaterialCodeTarget { get; set; }
        public string MaterialNameTarget { get; set; }
        public string MaterialTypeTarget { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string TransactionStatus { get; set; }
    }

    public class TransformPickingVM
    {
        public string MaterialCode { get; set; }
        public string StockID { get; set; }
        public int BagQty { get; set; }
    }
    public class TransformPickingStockDTO
    {
        public string TransformID { get; set; }
        public string ID { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string ReceivedAt { get; set; }
        public bool IsExpired { get; set; }
        public bool QCInspected { get; set; }
        public decimal Quantity { get; set; }


        public bool QCAction { get; set; }
        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }
    }
}
