using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class StockOpnameHeaderVM
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string BinRackAreaID { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string Remarks { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class StockOpnameHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string BinRackAreaID { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string Remarks { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }

        public string RemainingTask { get; set; }
    }

    //public class StockOpnameDetailVM
    //{
    //    public string ID { get; set; }
    //    public string HeaderID { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string Barcode { get; set; }
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public decimal Qty { get; set; }
    //    public decimal ActualQty { get; set; }
    //}

    public class StockOpnameDetailDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string TotalQty { get; set; }
        public string ScannedQty { get; set; }
        public string UnscannedQty { get; set; }
        public string TotalBagQty { get; set; }
        public string ScannedBagQty { get; set; }
        public string UnscannedBagQty { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public bool IsScanned { get; set; }
    }

    public class StockOpnameItemDTO
    {
        public string ID { get; set; }
        public string DetailID { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string ScannedBy { get; set; }
        public string ScannedOn { get; set; }
    }

    public class vStockOpnameDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string BinRackCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string MaterialType { get; set; }
        public string ScannedBy { get; set; }
        public DateTime ScannedOn { get; set; }
    }
}