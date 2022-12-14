using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class TransferModel
    {
        public TransferModel()
        {
            Details = new List<TransferDetailModel>();
            Stocks = new List<TableStock>();
            TransferDetails = new List<ActualStockDTO>();
            TransformSources = new List<TransformSource>();
            TransformTarget = new List<TransformSource>();
        }

        public Guid ID { get; set; }
        public string TransferNo { get;set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedOnStr { get; set; }
        public string ItemSourceType { get; set; }
        public string ItemSourceMaterialCode { get; set; }
        public string ItemTargetType { get; set; }
        public string ItemTargetMaterialCode { get; set; }
        public decimal TotalTransfer { get; set; }
        public decimal TotalTransferOutstanding { get; set; }
        public List<TransferDetailModel> Details { get; set; }
        public List<TableStock> Stocks { get; set; }

        public List<ActualStockDTO> TransferDetails { get; set; }


        // For View Detail 
        public List<TransformSource> TransformSources { get; set; }
        public List<TransformSource> TransformTarget { get; set; }
    }

    public class TableStock
    {
        public bool Selected { get; set; }
        public string ID { get; set; }
        public string StockCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string LotNumber { get; set; }
        public decimal QtyPerBag { get; set; }
        public decimal Qty { get; set; }
        public decimal QtyTransfer { get; set; }
        public decimal QtyBag { get; set; }

        public string QtyStr { get; set; }
        public string QtyPerBagStr { get; set; }

        public string InDate { get; set; }
        public string ExpDate { get; set; }

        public bool OnInspect { get; set; }
        public bool IsExpired { get; set; }
    }

    public class TransferDetailModel
    {
        public ItemLookupModel ItemSource { get; set; }
        public decimal QtyTransfer { get; set; }
        public ItemLookupModel ItemTarget { get; set; }

        public System.Guid ID { get; set; }
        public System.Guid TransferID { get; set; }
        
        public decimal Qty { get; set; }
        public string ProductIDSource { get; set; }
        public string ProductIDTarget { get; set; }
        public string ProductTypeSource { get; set; }
        public string ProductTypeTarget { get; set; }
        public string StockIDSource { get; set; }
        public string StockIDTarget { get; set; }

        public ActualStockDTO StockSource { get; set; }
        public ActualStockDTO StockTarget { get; set; }
    }

    public class TransferTableModel
    {
        public Guid ID { get; set; }
        public string TransferNo { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedOnStr { get; set; }
        public string MaterialSource { get; set; }
        public string MaterialSourceType { get; set; }
        public string MaterialTarget { get; set; }
        public string MaterialTargetType { get; set; }

        public decimal QtyStock { get; set; }
        public decimal QtyTransfer { get; set; }
        public decimal QtyRemain { get; set; }
        public string Status { get; set; }

    }


    public class TransformSource : ActualStockDTO
    {
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string QtyBefore { get; set; }
        public string QtyToBe { get; set; }
    }


}