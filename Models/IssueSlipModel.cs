using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WMS_BE.Models
{
    public class IssueSlipHeaderDTO
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ProductionDate { get; set; }
        public string TotalRequestedQty { get; set; }
        public string TransactionStatus { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedOn { get; set; }
    }

    public class IssueSlipOrderDTO
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string Number { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string VendorName { get; set; }
        public string RequestedQty { get; set; }
        public string PickedQty { get; set; }
        public string ReturnedQty { get; set; }
        public string OutstandingQty { get; set; }
        public string PickingBagQty { get; set; }
        public string DiffQty { get; set; }
        public string QtyPerBag { get; set; }

        public string AvailableReturnQty { get; set; }

        public bool PickingAction { get; set; }
        public bool ReturnAction { get; set; }
    }

    public class IssueSlipOrderDTO1
    {
        public string ID { get; set; }
        public string HeaderID { get; set; }
        public string Number { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string MaterialType { get; set; }
        public string VendorName { get; set; }
        public string RequestedQty { get; set; }
        public string PickedQty { get; set; }
        public string ReturnedQty { get; set; }
        public string OutstandingQty { get; set; }
        public string PickingBagQty { get; set; }
        public string DiffQty { get; set; }
        public string QtyPerBag { get; set; }

        public string AvailableReturnQty { get; set; }

        public bool PickingAction { get; set; }
        public bool ReturnAction { get; set; }
    }

    public class FifoStockDTO
    {
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

        public string Barcode { get; set; }
        public string BarcodeLeft { get; set; }
        public string BarcodeRight { get; set; }
    }

    public class IssueSlipPickingVM
    {
        public string OrderID { get; set; }
        public string StockID { get; set; }
        public int BagQty { get; set; }
    }

    public class IssueSlipPickingMobileVM
    {
        public string OrderID { get; set; }
        public string Barcode1 { get; set; }
        public string Barcode2 { get; set; }
        public string MaterialType { get; set; }
        public int BagQty { get; set; }
    }

    public class IssueSlipPickingDTO
    {
        public string ID { get; set; }
        public string OrderID { get; set; }
        public string RowNum { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string UoM { get; set; }
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

    public class IssueSlipReturnVM
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public decimal Qty { get; set; }
    }

    public class IssueSlipReturnVM2
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public decimal Qty { get; set; }
        public string LotNo { get; set; }
    }
    public class IssueSlipReturnDTO
    {
        public string RowNum { get; set; }
        public string HeaderID { get; set; }
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
        public string UoM { get; set; }
        public string ReturnMethod { get; set; }
        public string ReturnedBy { get; set; }
        public string ReturnedOn { get; set; }
        public string TotalPutawayQty { get; set; }
    }

    public class InspectionReturnVM
    {
        public string IssueSlipReturnId { get; set; }
        public int OkQty { get; set; }
    }

    public class JudgementReturnVM
    {
        public string IssueSlipReturnId { get; set; }
        public int OkQty { get; set; }
    }

    public class PutawayReturnVM
    {
        public string OrderID { get; set; }
        public string StockCode { get; set; }
        public int BagQty { get; set; }
        public string BinRackID { get; set; }
    }

    public class IssueSlipPutawayDTO
    {
        public string OrderId { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string StockCode { get; set; }
        public string LotNo { get; set; }
        public string InDate { get; set; }
        public string ExpDate { get; set; }
        public string BagQty { get; set; }
        public string QtyPerBag { get; set; }
        public string TotalQty { get; set; }
        public string PutawayMethod { get; set; }
        public string BinRackCode { get; set; }
        public string BinRackName { get; set; }
        public string BinRackAreaCode { get; set; }
        public string BinRackAreaName { get; set; }
        public string WarehouseCode { get; set; }
        public string WarehouseName { get; set; }
        public string PutBy { get; set; }
        public string PutOn { get; set; }
    }

    public class IssueSlipDTOReport
    {
        public string ID_Header { get; set; }
        public string ID_Order { get; set; }
        public string Header_Code { get; set; }
        public string Header_Name { get; set; }
        public string Header_ProductionDate { get; set; }
        public string RM_Code { get; set; }
        public string RM_Name { get; set; }
        public string RM_VendorName { get; set; }
        public string Wt_Request { get; set; }
        public string SupplyQty { get; set; }
        public string FromBinRackCode { get; set; }
        public string ExpDate { get; set; }
        public string PickedBy { get; set; }
        public string ReturnQty { get; set; }
        public string ToBinRackCode { get; set; }
        public string PutBy { get; set; }
    }

    public class DataInOutDTOReport
    {
        public string ItemCode { get; set; }
        public string Date { get; set; }
        public string UserHanheld { get; set; }
        public string Type { get; set; }
        public string ReceiveQty { get; set; }
        public string IssueSlipQty { get; set; }
        public string BalanceQty { get; set; }
    }

    //public class IssueSlipListDTO
    //{
    //    public string ID { get; set; }
    //    public string DetailID { get; set; }
    //    public string StockCode { get; set; }
    //    public string LotNo { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string UsageSupplyQty { get; set; }
    //    public string UsageBinRackID { get; set; }
    //    public string UsageBinRackCode { get; set; }
    //    public string UsageBinRackName { get; set; }
    //    public string UsagePickingMethod { get; set; }
    //    public string UsagePickedBy { get; set; }
    //    public string UsagePickedOn { get; set; }
    //    public bool UsageQRLabel { get; set; }
    //    public bool UsagePackage { get; set; }
    //    public string UsageInspectionMethod { get; set; }
    //    public string UsageInspectedBy { get; set; }
    //    public string UsageInspectedOn { get; set; }
    //    public bool UsageExpDate { get; set; }
    //    public bool UsageApproveStamp { get; set; }
    //    public string UsageJudgementMethod { get; set; }
    //    public string UsageJudgeBy { get; set; }
    //    public string UsageJudgeOn { get; set; }
    //    public string ReturnActualQty { get; set; }
    //    public string ReturnBinRackID { get; set; }
    //    public string ReturnBinRackCode { get; set; }
    //    public string ReturnBinRackName { get; set; }
    //    public string ReturnPutawayMethod { get; set; }
    //    public string ReturnPutBy { get; set; }
    //    public string ReturnPutOn { get; set; }
    //    public bool ReturnQRLabel { get; set; }
    //    public bool ReturnPackage { get; set; }
    //    public string ReturnInspectionMethod { get; set; }
    //    public string ReturnInspectedBy { get; set; }
    //    public string ReturnInspectedOn { get; set; }
    //}

    //public class UsagePickingVM
    //{
    //    public string HeaderID { get; set; }
    //    public string StockCode { get; set; }
    //    public string LotNo { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public decimal SupplyQty { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string BinRackAreaID { get; set; }
    //    public string BinRackAreaCode { get; set; }
    //    public string BinRackAreaName { get; set; }
    //    public string WarehouseID { get; set; }
    //    public string WarehouseCode { get; set; }
    //    public string WarehouseName { get; set; }
    //    public string PickingMethod { get; set; }
    //    public string PickedBy { get; set; }
    //    public DateTime PickedOn { get; set; }
    //}

    //public class UsageInspectionVM
    //{
    //    public string HeaderID { get; set; }
    //    public string StockCode { get; set; }
    //    public string LotNo { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public bool QRLabel { get; set; }
    //    public bool Package { get; set; }
    //    public string InspectionMethod { get; set; }
    //    public string InspectedBy { get; set; }
    //    public DateTime InspectedOn { get; set; }
    //}

    //public class UsageJudgementVM
    //{
    //    public string HeaderID { get; set; }
    //    public string StockCode { get; set; }
    //    public string LotNo { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public bool ExpDate { get; set; }
    //    public bool ApproveStamp { get; set; }
    //    public string JudgementMethod { get; set; }
    //    public string JudgeBy { get; set; }
    //    public DateTime JudgeOn { get; set; }
    //}

    //public class ReturnPutawayVM
    //{
    //    public string HeaderID { get; set; }
    //    public string StockCode { get; set; }
    //    public string LotNo { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public decimal ActualReturnQty { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string PutawayMethod { get; set; }
    //    public string PutBy { get; set; }
    //    public DateTime PutOn { get; set; }
    //}

    //public class ReturnInspectionVM
    //{
    //    public string HeaderID { get; set; }
    //    public string StockCode { get; set; }
    //    public string LotNo { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public bool QRLabel { get; set; }
    //    public bool Package { get; set; }
    //    public string InspectionMethod { get; set; }
    //    public string InspectedBy { get; set; }
    //    public DateTime InspectedOn { get; set; }
    //}

    //public class RecommendedStockDTO
    //{
    //    public string ID { get; set; }
    //    public string StockCode { get; set; }
    //    public string LotNo { get; set; }
    //    public string BinRackID { get; set; }
    //    public string BinRackCode { get; set; }
    //    public string BinRackName { get; set; }
    //    public string BinRackAreaID { get; set; }
    //    public string BinRackAreaCode { get; set; }
    //    public string BinRackAreaName { get; set; }
    //    public string WarehouseID { get; set; }
    //    public string WarehouseCode { get; set; }
    //    public string WarehouseName { get; set; }
    //    public string RawMaterialID { get; set; }
    //    public string MaterialCode { get; set; }
    //    public string MaterialName { get; set; }
    //    public string ExpDate { get; set; }
    //    public string InDate { get; set; }
    //    public string Qty { get; set; }
    //    public string ReceivedAt { get; set; }
    //    public bool IsExpired { get; set; }
    //    public bool QCInspected { get; set; }
    //}
}