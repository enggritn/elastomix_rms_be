using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class MovementModel
    {
        public MovementModel()
        {
            Details = new List<MovementVM>();
        }

        public string MaterialCode { get; set; }
        public string WarehouseCode { get; set; }
        public List<MovementVM> Details { get; set; }
    }

    public class MovementVM
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string RawMaterialID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string PrevArea { get; set; }
        public string PrevBinRackID { get; set; }
        public string PrevBinRackCode { get; set; }
        public string PrevBinRackName { get; set; }
        public decimal QtyPerBag { get; set; }
        public decimal Qty { get; set; }
        public decimal QtyAvailable { get; set; }
        public decimal QtyTransfer { get; set; }
        public string NewArea { get; set; }
        public string NewBinRackID { get; set; }
        public string NewBinRackCode { get; set; }
        public string NewBinRackName { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class MovementDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string RawMaterialID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string PrevBinRackID { get; set; }
        public string PrevBinRackCode { get; set; }
        public string PrevBinRackName { get; set; }
        public string Qty { get; set; }
        public string NewBinRackID { get; set; }
        public string NewBinRackCode { get; set; }
        public string NewBinRackName { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class StockRMDTO
    {
        public string ID { get; set; }
        public string Barcode { get; set; }
        public string RawMaterialID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string Qty { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaID { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseID { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string ReceivedAt { get; set; }
        public bool IsExpired { get; set; }
    }
}