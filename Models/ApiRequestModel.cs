using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{

    //RECEIVING RM
    public class ReceivingRMListReq
    {
        public string Date { get; set; }
        public string WarehouseCode { get; set; }
        public string SourceName { get; set; }
        public string MaterialName { get; set; }
    }


    public class ReceivingRMReq
    {
        public string ReceivingHeaderId { get; set; }
        public string DoNo { get; set; }
        public string LotNo { get; set; }
        public int BagQty { get; set; }
    }

    public class InspectionRMReq
    {
        public string ReceivingDetailId { get; set; }
        public int OKBagQty { get; set; }
        public int DamageQty { get; set; }
        public int WetQty { get; set; }
        public int ContaminationQty { get; set; }
    }

    //public class JudgementReq
    //{
    //    public string ReceivingDetailId { get; set; }
    //    public int OKBagQty { get; set; }
    //}

    public class ReceivingRMPrintReq
    {
        public string ReceivingDetailId { get; set; }
        public string Type { get; set; }
        public string ID { get; set; }
        public int PrintQty { get; set; }
        public bool UseSeries { get; set; }
        public string Printer { get; set; }
    }

    public class PutawayRMReq
    {
        public string ReceivingDetailId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }


    public class ReceivingDataRMResp
    {
        public string ID { get; set; }
        public string DocumentNo { get; set; }
        public string RefNumber { get; set; }
        public string SourceType { get; set; }
        public string SourceCode { get; set; }
        public string SourceName { get; set; }
        public string WarehouseName { get; set; }
        public string WarehouseCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string DoNo { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string UoM { get; set; }
        public string ATA { get; set; }
        public string Remarks { get; set; }

        public string OKQty { get; set; }
        public string OKBagQty { get; set; }
        public string NGQty { get; set; }
        public string NGBagQty { get; set; }

        public string PutawayTotalQty { get; set; }
        public string PutawayTotalBagQty { get; set; }
        public bool InspectionAction { get; set; }
        public bool JudgementAction { get; set; }
        public bool PutawayAction { get; set; }

        public string PutawayAvailableQty { get; set; }
        public string PutawayAvailableBagQty { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }


    //END OF RECEIVING RM


    //ISSUE SLIP

    public class IssueSlipPickingReq
    {
        public string OrderId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class IssueSlipReturnListResp
    {
        public string OrderId { get; set; }
        public string StockCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string PutawayQty { get; set; }


        public bool PutawayAction { get; set; }
        public bool PrintBarcodeAction { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class IssueSlipReturnReq
    {
        public string OrderId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public decimal RemainderQty { get; set; }
    }

    public class IssueSlipPutawayReq
    {
        public string OrderId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class IssueSlipReturnPrintReq
    {
        public string OrderId { get; set; }
        public string StockCode { get; set; }
        public string Printer { get; set; }
    }


    public class IssueSlipPutawayListResp
    {
        public string OrderId { get; set; }
        public string StockCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string PutawayQty { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }


        public bool PutawayAction { get; set; }
        public bool PrintBarcodeAction { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    //END OF ISSUE SLIP

    public class BarcodeResp
    {
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    // OUTBOUND

    public class OutboundHeaderReq
    {
        public string ID { get; set; }
        public string Remarks { get; set; }
        public string WarehouseCode { get; set; }
    }

    public class OutboundOrderReq
    {
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public decimal RequestQty { get; set; }
    }

    public class OutboundOrderResp
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string TotalQty { get; set; }
    }


    public class OutboundPickingReq
    {
        public string OrderId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class OutboundExcessPrintReq
    {
        public string OrderId { get; set; }
        public string Printer { get; set; }
    }

    public class OutboundExcessPutawayReq
    {
        public string OrderId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class OutboundReturnResp
    {
        public string ID { get; set; }
        public string OutboundOrderID { get; set; }
        public string OrderId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string BagQty { get; set; }
        public string StockCode { get; set; }
        public string PutawayQty { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string ReturnQty { get; set; }
        public string Remarks { get; set; }
        public int LastSeries { get; set; }
        public bool PutawayAction { get; set; }
        public bool PrintBarcodeAction { get; set; }
    }

    // END OF OUTBOUND


    // INBOUND

    public class InboundHeaderReq
    {
        public string ID { get; set; }
        public string Remarks { get; set; }
        public string WarehouseCode { get; set; }
    }

    public class InboundOrderReq
    {
        public string HeaderID { get; set; }
        public string MaterialCode { get; set; }
        public decimal InboundQty { get; set; }
    }


    public class InboundReceiveReq
    {
        public string OrderId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public decimal RemainderQty { get; set; }
        public bool ScanBarcode { get; set; }
    }

    public class InboundReceivePrintReq
    {
        public string ReceiveId { get; set; }
        public string Printer { get; set; }
    }
    public class OutboundReturnPrintReq
    {
        public string ReturnId { get; set; }
        public string Printer { get; set; }
    }

    public class InboundReceiveWebReq
    {
        public string OrderId { get; set; }
        public bool ScanBarcode { get; set; }
        public int BagQty { get; set; }
        public decimal RemainderQty { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string LotNumber { get; set; }
        public decimal QtyPerBag { get; set; }

    }

    public class InboundPutawayReq
    {
        public string ReceiveId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }
    public class InboundPutawayWebReq
    {
        public string ReceiveId { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }
    // END OF INBOUND

    //MOVEMENT

    public class PickingMovementReq
    {
        public string BinRackCode { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class PutawayMovementReq
    {
        public string MovementId { get; set; }
        public string BinRackCode { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
    }

    public class MovementListResp
    {
        public string MovementId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string TotalQty { get; set; }
        public string QtyPerBag { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public bool PutawayAction { get; set; }
    }

    //END OF MOVEMENT


    //STOCK OPNAME

    public class StockOpnameScanReq
    {
        public string HeaderId { get; set; }
        public string BinRackCode { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
    }

    //END OF STOCK OPNAME

    //STOCK TRANSFORM

    public class StockTransformHeaderReq
    {
        public string MaterialCode { get; set; }
        public decimal TotalQty { get; set; }
        public string MaterialCodeTarget { get; set; }
    }

    public class StockTransformListResp
    {
        public string ID { get; set; }
        public string TransformNo { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string MaterialCodeTarget { get; set; }
        public string MaterialNameTarget { get; set; }
        public string MaterialTypeTarget { get; set; }
        public string TransformQty { get; set; }
        public string OutstandingQty { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }

    public class StockTransformDetailResp
    {
        public string ID { get; set; }
        public string TransformID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string BagQtyX { get; set; }
        public string LastSeries { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string PrintedBy { get; set; }
        public string PrintedAt { get; set; }

        public bool PrintBarcodeAction { get; set; }
        public string OutstandingQty { get; set; }
        public string OutstandingBagQty { get; set; }
    }

    public class StockTransformPickingReq
    {
        public string HeaderId { get; set; }
        public string BinRackCode { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public decimal Qty { get; set; }
    }
    public class StockTransformPickingWebReq
    {
        public string HeaderId { get; set; }
        public string StockId { get; set; }
        public string BinRackCode { get; set; }
        public decimal Qty { get; set; }
    }

    public class StockTransformPrintReq
    {
        public string TransformDetailId { get; set; }
        public string Printer { get; set; }
    }

    //END OF STOCK TRANSFORM


    //RECEIVING SFG

    public class ReceivingSFGHeaderResp
    {
        public string ID { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string ProdDate { get; set; }
        public string OKQty { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string NGQty { get; set; }
        public string OutstandingQty { get; set; }

        public bool ReceiveAction { get; set; }

    }

    public class ReceivingSFGReq
    {
        public string ReceiveId { get; set; }
        public decimal TotalQty { get; set; }
    }

    public class ReceivingSFGDetailResp
    {
        public string ReceiveId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }

        public string PutawayBagQty { get; set; }

        public string OutstandingBagQty { get; set; }

        public bool PrintBarcodeAction { get; set; }
        public bool PutawayAction { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class ReceiveSFGPrintReq
    {
        public string StockCode { get; set; }
        public int LastSeriesBarcode { get; set; }
        public string ReceiveId { get; set; }
        public bool UseSeries { get; set; }
        public int PrintQty { get; set; }
        public string Printer { get; set; }
    }

    public class ReceivingSFGPutawayReq
    {
        public string ReceiveId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    //END OF RECEIVING SFG

    // QC INSPECTION

    public class InspectionCountResp
    {
        public string StatusName { get; set; }
        public int TotalRow { get; set; }
    }

    public class InspectionListResp
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string MaterialType { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string NewExpDate { get; set; }
        public string CreatedBy { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedOn { get; set; }

        public string InspectionStatus { get; set; }
        public string InspectedBy { get; set; }
        public string InspectedOn { get; set; }

        public string TotalJudgementQty { get; set; }
        public string TotalDisposalQty { get; set; }

        public bool Priority { get; set; }

        public bool JudgementAction { get; set; }
        public bool DisposeAction { get; set; }
    }

    public class InspectionListRespReturn
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string MaterialType { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string NewExpDate { get; set; }
        public string CreatedBy { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedOn { get; set; }

        public string InspectionStatus { get; set; }
        public string InspectedBy { get; set; }
        public string InspectedOn { get; set; }

        public decimal TotalQty { get; set; }
        public decimal TotalReturnQty { get; set; }
        public bool Priority { get; set; }
        public bool JudgementAction { get; set; }
        public bool DisposeAction { get; set; }
    }
    public class InspectionWaitingListResp
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string PickingMethod { get; set; }
        public string PickedBy { get; set; }
        public string PickedOn { get; set; }
        public string PutawayBagQty { get; set; }

        public bool PutawayAction { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class PickingWaitingWebReq
    {
        public string InspectionId { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string LotNumber { get; set; }
        public decimal QtyPerBag { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }
    public class PickingDisposeWebReq
    {
        public string InspectionId { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string LotNumber { get; set; }
        public decimal QtyPerBag { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }
    public class PickingWaitingReq
    {
        public string InspectionId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class PutawayWaitingReq
    {
        public string PickingId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }
    public class PutawayWaitingWebReq
    {
        public string PickingId { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string LotNumber { get; set; }
        public decimal QtyPerBag { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }


    public class InspectionDisposeListResp
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }

        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }


    public class PickingDisposeReq
    {
        public string InspectionId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class InspectionExtendListResp
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string PickingMethod { get; set; }
        public string PickedBy { get; set; }
        public string PickedOn { get; set; }
        public string PutawayBagQty { get; set; }
        public bool PutawayAction { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public bool Sample { get; set; }
    }

    public class InspectionReturnListResp
    {
        public string ID { get; set; }
        public string InspectionID { get; set; }
        public string BinRackID { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string Qty { get; set; }
        public string QtyPerBag { get; set; }
        public string BagQty { get; set; }
        public string PutawayMethod { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
        public string PutawayBagQty { get; set; }
        public bool PutawayAction { get; set; }
        public bool ReturnAction { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class MobileInspectionListReq
    {
        public string MaterialName { get; set; }
        public string InspectionStatus { get; set; }
    }

    public class PutawayExtendReq
    {
        public string PutawayId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int BagQty { get; set; }
        public string BinRackCode { get; set; }
    }

    public class PutawayReturnReq
    {
        public string ReturnId { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
        public int ReturnBagQty { get; set; }
        public string BinRackCode { get; set; }
    }
    public class PutawayExtendWebReq
    {
        public string PutawayId { get; set; }
        public DateTime InDate { get; set; }
        public DateTime ExpDate { get; set; }
        public string LotNumber { get; set; }
        public string BinRackCode { get; set; }
    }

    public class PrintExtendPrintReq
    {
        public string InspectionId { get; set; }
        public string StockCode { get; set; }
        public string Printer { get; set; }
        public bool UseSeries { get; set; }
        public int PrintQty { get; set; }
    }

    public class PrintReturnPrintReq
    {
        public string InspectionId { get; set; }
        public string StockCode { get; set; }
        public string Printer { get; set; }
        public bool UseSeries { get; set; }
        public int PrintQty { get; set; }
    }

    public class MobilePrintReq
    {
        public string StockId { get; set; }
        public bool UseSeries { get; set; }
        public int PrintQty { get; set; }
        public string Printer { get; set; }
        public decimal Qty { get; set; }
    }

    public class ListDataReturnReq
    {
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
    }

    public class MobilePrintListReq
    {
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
    }

    public class MobilePrintResp
    {
        public string StockId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string Qty { get; set; }
        public string Warehouse { get; set; }
        public string Area { get; set; }
        public string BinRackCode { get; set; }
    }


}