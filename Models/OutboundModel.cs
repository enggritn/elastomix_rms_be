using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WMS_BE.Models
{
    public class OutboundHeaderVM
    {
        public string ID { get; set; }
        public string Remarks { get; set; }
        public string WarehouseCode { get; set; }
    }

    public class OutboundHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Remarks { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public List<OutboundOrderDTO> Details { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }

        public bool SaveAction { get; set; }
        public bool CancelAction { get; set; }
        public bool ConfirmAction { get; set; }
        public bool AddOrderAction { get; set; }
        public bool RemoveOrderAction { get; set; }
    }

    public class OutboundOrderVM
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public decimal TotalQty { get; set; }
    }

    public class OutboundOrderDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string TotalQty { get; set; }
        public string PickedQty { get; set; }
        public string DiffQty { get; set; }
        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }

        public bool PickingAction { get; set; }
        public bool PrintBarcodeAction { get; set; }
        public bool PutawayAction { get; set; }
        public bool ReturnAction { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
    }

    public class OutboundOrderDTO2
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string TotalQty { get; set; }
        public string PickedQty { get; set; }
        public string DiffQty { get; set; }
        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }

        public bool PickingAction { get; set; }
        public bool ReturnAction { get; set; }
    }

    public class OutboundPickingVM
    {
        public string OrderID { get; set; }
        public string StockID { get; set; }
        public int BagQty { get; set; }
    }

    public class OutboundReturnVM
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public decimal Qty { get; set; }
        public string Remarks { get; set; }
    }

    public class OutboundReturnRes
    {
        public string OrderID { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public decimal Qty { get; set; }
    }

    public class OutboundPutawayReturnReq
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public int BagQty { get; set; }
        public string BinRackID { get; set; }
    }
    public class OutboundPutawayReturnRes
    {
        public string OrderID { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class OutboundPickingDTO
    {
        public string ID { get; set; }
        public string OrderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string PickingMethod { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string PickedBy { get; set; }
        public string PickedOn { get; set; }

        public string DiffQty { get; set; }
        public string ReturnedTotalQty { get; set; }
        public string AvailableReturnQty { get; set; }
    }
    public class OutboundReturnDTO
    {
        public string ID { get; set; }
        public string OrderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string Remarks { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string ReturnQty { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string UoM { get; set; }
        public string ReturnMethod { get; set; }
        public string ReturnedBy { get; set; }
        public string ReturnedOn { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string TotalPutawayQty { get; set; }
        public bool PutawayReturnAction { get; set; }
    }

    public class OutboundReturnList
    {
        public string ID { get; set; }
        public string OrderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string BagQty { get; set; }
        public string Remarks { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string ReturnQty { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string TotalPutawayQty { get; set; }
    }
    public class OutboundReturnPutawayDTO
    {
        public string ID { get; set; }
        public string OrderID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string PutawayQty { get; set; }
        public string QtyPerBag { get; set; }
        public string UoM { get; set; }
        public string PutawayMethod { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }

    }

    public class OutbounReportDTO
    {
        public string DocumentNo { get; set; }
        public string WHName { get; set; }
        public string RMCode { get; set; }
        public string RMName { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string LotNo { get; set; }
        public string Bag { get; set; }
        public string FullBag { get; set; }
        public string Total { get; set; }
        public string CreateBy { get; set; }
        public string CreateOn { get; set; }
        public string PickingBag { get; set; }
        public string PickingFullBag { get; set; }
        public string PickingTotal { get; set; }
        public string PickingBinRack { get; set; }
        public string PickingBy { get; set; }
        public string PickingOn { get; set; }
        public string PutawayBag { get; set; }
        public string PutawayFullBag { get; set; }
        public string PutawayTotal { get; set; }
        public string PutawayBinRack { get; set; }
        public string PutawayBy { get; set; }
        public string PutawayOn { get; set; }
        public string Status { get; set; }
        public string Memo { get; set; }
    }
}
